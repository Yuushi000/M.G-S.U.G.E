using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Pilotage des os du chassis par code (equivalent de applyPose() du prototype).
    /// Chaque pose est un ensemble de decalages en espace modele appliques a la pose de liaison :
    ///   localRotation = bindLocal * inverse(B) * decalage * B   (B = rotation de l'os a la liaison, en espace modele)
    /// Couches : Mech/Ship (forme), accroupi, lame (levee / balayage), couvercle de nacelle, marche procedurale, tete vers la cible.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class FrameRig : MonoBehaviour
    {
        public PoseLibrary poses;
        [Tooltip("racine du modele importe (FBX)")] public Transform model;

        [Header("Parametres pilotes par le controleur")]
        [Range(0, 1)] public float ship;          // 0 chassis -> 1 vaisseau
        [Range(0, 1)] public float crouch;
        [Range(0, 1)] public float bladeRaise, bladeSweep;
        [Range(0, 1)] public float lidOpen;
        public float walkPhase, walkAmount;       // cycle de jambes
        public float leanForward, leanSide;       // radians
        public Vector2 headLook;                  // lacet / tangage (radians, espace modele)
        public float barrelRoll;                  // radians (tonneau en vaisseau)

        class Bone
        {
            public Transform t; public Quaternion bindLocal, B, invB; public Vector3 bindPos, bindScale; public Quaternion parentB;
            public Quaternion rot; public Vector3 pos; public float scale;
        }
        readonly Dictionary<string, Bone> bones = new Dictionary<string, Bone>();
        FramePose pMech, pShip, pCrouch, pRaise, pSweep, pLid;
        public bool Ready { get; private set; }

        public Transform Find(string name) => bones.TryGetValue(name, out var b) ? b.t : (model != null ? MathX.FindDeep(model, name) : null);

        void Awake() { Init(); }

        public void Init()
        {
            if (Ready || model == null || poses == null) return;
            pMech = poses.Get("Mech"); pShip = poses.Get("Ship"); pCrouch = poses.Get("Crouch");
            pRaise = poses.Get("BladeRaise"); pSweep = poses.Get("BladeSweep"); pLid = poses.Get("LidOpen");
            var names = new HashSet<string>();
            foreach (var p in poses.poses) foreach (var e in p.bones) names.Add(e.bone);
            foreach (var n in new[] { "Body", "Spine", "Torso", "Head", "Hip_L", "Hip_R", "Knee_L", "Knee_R", "Ankle_L", "Ankle_R", "Skirt_Front_L", "Skirt_Front_R" }) names.Add(n);
            // Repere modele deduit du squelette (independant d'une eventuelle rotation ajoutee a l'import FBX) :
            // haut = bassin -> tete, droite = hanche gauche -> hanche droite, avant = droite x haut.
            // Si le repere ne correspond pas a celui du parent (Visuel), on reoriente le modele.
            Quaternion frame = model.rotation;
            var tb = MathX.FindDeep(model, "Body"); var th = MathX.FindDeep(model, "Head");
            var hl = MathX.FindDeep(model, "Hip_L"); var hr = MathX.FindDeep(model, "Hip_R");
            if (tb != null && th != null && hl != null && hr != null)
            {
                Vector3 up = (th.position - tb.position).normalized;
                Vector3 right = Vector3.ProjectOnPlane(hr.position - hl.position, up).normalized;
                Vector3 fwd = Vector3.Cross(right, up);
                if (fwd.sqrMagnitude > 0.5f)
                {
                    frame = Quaternion.LookRotation(fwd, up);
                    Quaternion parentRot = model.parent != null ? model.parent.rotation : Quaternion.identity;
                    Quaternion fix = parentRot * Quaternion.Inverse(frame);
                    if (Quaternion.Angle(fix, Quaternion.identity) > 1f)
                    {
                        Vector3 pivot = model.parent != null ? model.parent.position : model.position;
                        model.SetPositionAndRotation(pivot + fix * (model.position - pivot), fix * model.rotation);
                        Debug.Log("[MGSUGE] Modele reoriente de " + Quaternion.Angle(fix, Quaternion.identity).ToString("0") + " degres (repere d'import).");
                        frame = parentRot;
                    }
                }
            }
            Quaternion invModel = Quaternion.Inverse(frame);
            foreach (var n in names)
            {
                var t = MathX.FindDeep(model, n);
                if (t == null) { Debug.LogWarning("[MGSUGE] Os introuvable dans le modele : " + n); continue; }
                var b = new Bone { t = t, bindLocal = t.localRotation, bindPos = t.localPosition, bindScale = t.localScale };
                b.B = invModel * t.rotation; b.invB = Quaternion.Inverse(b.B);
                b.parentB = t.parent != null ? invModel * t.parent.rotation : Quaternion.identity;
                bones[n] = b;
            }
            Ready = true;
        }

        static void Read(FramePose p, string bone, out Quaternion r, out Vector3 pos, out float s, out bool hasS)
        {
            r = Quaternion.identity; pos = Vector3.zero; s = 1f; hasS = false;
            var e = p?.Get(bone); if (e == null) return;
            if (e.hasRot) r = e.rot; if (e.hasPos) pos = e.pos; if (e.hasScale) { s = e.scale; hasS = true; }
        }
        static void Layer(FramePose p, string bone, float w, ref Quaternion r, ref Vector3 pos)
        {
            if (w <= 0f || p == null) return;
            var e = p.Get(bone); if (e == null) return;
            if (e.hasRot) r = Quaternion.Slerp(r, e.rot, w);
            if (e.hasPos) pos = Vector3.Lerp(pos, e.pos, w);
        }

        void LateUpdate()
        {
            if (!Ready) return;
            float crouchW = crouch * (1f - ship);
            foreach (var kv in bones)
            {
                string n = kv.Key; var b = kv.Value;
                Read(pMech, n, out var rm, out var pm, out var sm, out _);
                Read(pShip, n, out var rs, out var ps, out var ss, out _);
                Quaternion r = Quaternion.Slerp(rm, rs, ship);
                Vector3 pos = Vector3.Lerp(pm, ps, ship);
                float sc = Mathf.Lerp(sm, ss, ship);
                Layer(pCrouch, n, crouchW, ref r, ref pos);
                Layer(pRaise, n, bladeRaise, ref r, ref pos);
                Layer(pSweep, n, bladeSweep, ref r, ref pos);
                Layer(pLid, n, lidOpen, ref r, ref pos);
                r = Procedural(n) * r;
                b.t.localRotation = b.bindLocal * b.invB * r * b.B;
                b.t.localPosition = b.bindPos + Quaternion.Inverse(b.parentB) * pos;
                b.t.localScale = b.bindScale * Mathf.Max(sc, 0f);
            }
        }

        /// <summary>Rotations procedurales ajoutees en espace modele (marche, inclinaison, tete, tonneau).</summary>
        Quaternion Procedural(string n)
        {
            float mech = 1f - ship;
            switch (n)
            {
                case "Body":
                    return Quaternion.AngleAxis(barrelRoll * Mathf.Rad2Deg, Vector3.forward)
                         * Quaternion.Euler(leanForward * Mathf.Rad2Deg * mech, 0f, -leanSide * Mathf.Rad2Deg * mech);
                case "Head":
                    return Quaternion.Euler(-headLook.y * Mathf.Rad2Deg * mech, headLook.x * Mathf.Rad2Deg * mech, 0f);
                case "Hip_L": return Quaternion.Euler(-Mathf.Sin(walkPhase) * 32f * walkAmount * mech, 0f, 0f);
                case "Hip_R": return Quaternion.Euler(Mathf.Sin(walkPhase) * 32f * walkAmount * mech, 0f, 0f);
                case "Knee_L": return Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(walkPhase + 1.2f)) * 45f * walkAmount * mech, 0f, 0f);
                case "Knee_R": return Quaternion.Euler(Mathf.Max(0f, -Mathf.Sin(walkPhase + 1.2f)) * 45f * walkAmount * mech, 0f, 0f);
                case "Skirt_Front_L": return Quaternion.Euler(Mathf.Min(0f, -Mathf.Sin(walkPhase)) * 20f * walkAmount * mech, 0f, 0f);
                case "Skirt_Front_R": return Quaternion.Euler(Mathf.Min(0f, Mathf.Sin(walkPhase)) * 20f * walkAmount * mech, 0f, 0f);
            }
            return Quaternion.identity;
        }
    }
}
