using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Flammes des tuyeres (objets Flame_Thruster_* du modele) : longueur selon la poussee dans leur direction,
    /// clignotement rapide (§17.2). Une flamme ne s'allume que si sa tuyere souffle a l'oppose de la poussee.
    /// </summary>
    public class ThrusterFX : MonoBehaviour
    {
        public PlayerController player;
        public Transform model;
        [Tooltip("longueur maximale de flamme (facteur de la flamme de base de 1 m)")] public float maxLength = 4.5f;
        public float idleLength = 0.25f;

        class Flame { public Transform t; public int axis; public float sign; public Vector3 baseScale; public float len, phase; }
        readonly List<Flame> flames = new List<Flame>();

        void Start()
        {
            if (model == null) return;
            foreach (var tr in model.GetComponentsInChildren<Transform>(true))
            {
                if (!tr.name.StartsWith("Flame_")) continue;
                var mf = tr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var bnd = mf.sharedMesh.bounds; Vector3 s = bnd.size;
                int axis = s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2;
                float sign = Mathf.Sign(bnd.center[axis]); if (sign == 0f) sign = 1f;
                flames.Add(new Flame { t = tr, axis = axis, sign = sign, baseScale = tr.localScale, phase = Random.value * 10f });
            }
        }

        void LateUpdate()
        {
            if (player == null) return;
            float dt = GameClock.PlayerDt;
            Vector3 thrust = player.ThrustVector;
            float mag = Mathf.Min(thrust.magnitude, 1.6f);
            Vector3 dir = mag > 0.01f ? thrust / Mathf.Max(thrust.magnitude, 1e-4f) : Vector3.zero;
            bool alive = !player.dead;
            foreach (var f in flames)
            {
                Vector3 local = Vector3.zero; local[f.axis] = f.sign;
                Vector3 exhaust = f.t.TransformDirection(local).normalized;      // direction des gaz
                float k = mag > 0.01f ? Mathf.Max(0f, Vector3.Dot(exhaust, -dir)) * mag : 0f;
                float target = alive ? idleLength * (player.IsShip ? 1.5f : 0.6f) + k * maxLength : 0f;
                f.len = Mathf.Lerp(f.len, target, MathX.Damp(14f, dt));
                float flick = 1f + Mathf.Sin(Time.time * 61f + f.phase) * 0.12f;
                Vector3 sc = f.baseScale;
                sc[f.axis] *= Mathf.Max(0.001f, f.len * flick);
                float w = Mathf.Lerp(0.6f, 1.1f, Mathf.Clamp01(f.len));
                for (int a = 0; a < 3; a++) if (a != f.axis) sc[a] *= w;
                f.t.localScale = sc;
                f.t.gameObject.SetActive(f.len > 0.02f);
            }
        }
    }
}
