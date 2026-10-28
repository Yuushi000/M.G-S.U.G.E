using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Sondes laser (cahier §12.3) incarnees par les drones du dos (Probe_1..6 du modele) :
    /// une sonde achetee = un drone qui se detache, encercle la cible verrouillee et tire des lasers jaunes
    /// (degats et cadence du fusil a impulsions). Sans cible, les drones reviennent se loger dans les ailes.
    /// Maximum 5 sondes : le 6e drone reste toujours a poste.
    /// </summary>
    public class ProbeController : MonoBehaviour
    {
        public WeaponData data;
        public PlayerController player;
        public LockOn lockOn;
        public FrameRig rig;

        class Probe
        {
            public Transform t, home; public Vector3 homePos, homeScale; public Quaternion homeRot;
            public bool docked = true; public float fireCd, phase; public Vector3 vel;
        }
        readonly List<Probe> probes = new List<Probe>();
        float noTargetTime;
        public int Owned => player != null ? Mathf.Min(player.progress.probes, data.probeMax) : 0;
        public bool Fighting { get; private set; }
        static readonly Color Yellow = new Color32(0xFF, 0xD8, 0x4A, 0xFF);

        void Start()
        {
            if (rig == null) return;
            for (int i = 1; i <= 6; i++)
            {
                var t = rig.Find("Probe_" + i);
                if (t == null) continue;
                probes.Add(new Probe { t = t, home = t.parent, homePos = t.localPosition, homeRot = t.localRotation, homeScale = t.localScale, phase = i * 1.047f });
            }
        }

        void Update()
        {
            if (player == null) return;
            float dt = GameClock.PlayerDt; if (dt <= 0f) return;
            var target = lockOn != null ? lockOn.Target : null;
            bool hasTarget = target != null && target.Alive && !player.dead;
            noTargetTime = hasTarget ? 0f : noTargetTime + dt;
            Fighting = false;
            int owned = Owned;
            for (int i = 0; i < probes.Count; i++)
            {
                var p = probes[i];
                bool active = i < owned && !player.dead;
                if (active && hasTarget)
                {
                    if (p.docked) Undock(p);
                    Fighting = true;
                    // orbite autour de la cible, positions reparties
                    float a = Time.time * data.probeOrbitSpeed + i * Mathf.PI * 2f / Mathf.Max(1, owned);
                    float r = target.radius + data.probeOrbitRadius;
                    Vector3 slot = target.Position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a * 0.7f + i) * r * 0.35f + 6f, Mathf.Sin(a) * r);
                    Fly(p, slot, dt, target.Velocity);
                    p.t.rotation = Quaternion.Slerp(p.t.rotation, Quaternion.LookRotation(target.Position - p.t.position), MathX.Damp(10f, dt));
                    p.fireCd -= dt;
                    if (p.fireCd <= 0f && Vector3.Distance(p.t.position, target.Position) < r * 2.5f)
                    {
                        p.fireCd = player.S.rifleInterval;
                        Vector3 from = p.t.position;
                        if (!Physics.Linecast(from, target.Position, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                        {
                            target.TakeDamage(new DamageInfo { amount = player.S.rifleDamage * player.DamageFactor, impact = data.rifleImpact, point = target.Position, direction = (target.Position - from).normalized, fromPlayer = true, source = this });
                            Fx.Beam(from, target.Position, Yellow, 0.4f, 0.07f);
                            Sfx.Play("probe", from);
                        }
                    }
                }
                else if (!p.docked)
                {
                    // retour au poste dans l'aile
                    Vector3 dock = p.home.TransformPoint(p.homePos);
                    Fly(p, dock, dt, player.velocity);
                    p.t.rotation = Quaternion.Slerp(p.t.rotation, p.home.rotation * p.homeRot, MathX.Damp(6f, dt));
                    if (Vector3.Distance(p.t.position, dock) < 1.2f || player.dead) Dock(p);
                }
            }
        }

        void Fly(Probe p, Vector3 goal, float dt, Vector3 carrierVel)
        {
            Vector3 to = goal - p.t.position;
            Vector3 want = carrierVel + Vector3.ClampMagnitude(to * 3f, data.probeSpeed);
            p.vel = Vector3.Lerp(p.vel, want, MathX.Damp(5f, dt));
            p.t.position += p.vel * dt;
        }
        void Undock(Probe p)
        {
            p.docked = false; p.vel = player.velocity;
            p.t.SetParent(null, true);
            Fx.Flash(p.t.position, 3f, Yellow);
        }
        void Dock(Probe p)
        {
            p.docked = true;
            p.t.SetParent(p.home, false); p.t.localPosition = p.homePos; p.t.localRotation = p.homeRot; p.t.localScale = p.homeScale;
        }
    }
}
