using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    public enum MissileKind { Normal, Salvo, FragCarrier, FragMini, Enemy }

    /// <summary>
    /// Missiles guides en pool de 160 (§9.2) : vitesse 110 -> 340 m/s, virage 3,4 rad/s, guidage apres 0,25 s, vie 6 s,
    /// anticipation de la cible, explosion au contact (< 2,5 m), au sol, contre un obstacle ou en fin de vie.
    /// </summary>
    public class MissileSystem : MonoBehaviour
    {
        public static MissileSystem I { get; private set; }
        [Tooltip("parametres des missiles (vitesses, virage, contact, pool)")] public WeaponData data;
        [Tooltip("prefab d'un missile (corps + TrailRenderer)")] public GameObject missilePrefab;
        [Tooltip("parent des copies creees au lancement (par defaut cet objet)")] public Transform poolParent;
        [Header("Missiles ennemis (§10.4, §13.1)")]
        [Tooltip("rayon de contact avec le joueur (m), en plus du rayon de touche du chassis")] public float enemyContactRadius = 3f;
        [Tooltip("rayon des degats de proximite (m) : 100 % au centre, 40 % au bord")] public float enemySplashRadius = 9f;
        [Range(0, 1)] public float enemySplashMinFactor = 0.4f;
        [Tooltip("intervalle mini entre deux avertissements « Missiles entrants » (s)")] public float incomingWarningInterval = 3f;

        public class M
        {
            public bool active; public MissileKind kind; public Vector3 pos, dir; public float speed, maxSpeed, accel, turn, age, life, delay;
            public Targetable target; public Vector3 point; public bool hasPoint; public float damage, impact; public bool fromPlayer, homePlayer;
            public Vector3 start; public float startDist; public int minis; public float miniDamage;
            public Transform t; public TrailRenderer trail;
        }
        readonly List<M> pool = new List<M>();
        float lastIncoming = -99f;
        /// <summary>Missiles ennemis en vol vers le joueur (alerte de la surimpression).</summary>
        public int IncomingCount { get; private set; }
        public readonly List<Vector3> IncomingPositions = new List<Vector3>();

        void Awake()
        {
            I = this;
            if (missilePrefab == null) { Debug.LogError("[MGSUGE] MissileSystem : prefab de missile non assigne."); return; }
            var parent = poolParent != null ? poolParent : transform;
            int n = data != null ? data.missilePool : 160;
            for (int i = 0; i < n; i++)
            {
                var go = Instantiate(missilePrefab, parent);
                go.name = missilePrefab.name;
                go.SetActive(false);
                pool.Add(new M { t = go.transform, trail = go.GetComponentInChildren<TrailRenderer>() });
            }
        }

        public M Launch(MissileKind kind, Vector3 pos, Vector3 dir, Targetable target, Vector3? point, float damage, float impact, bool fromPlayer)
        {
            M m = null;
            foreach (var x in pool) if (!x.active) { m = x; break; }
            if (m == null) return null;
            m.active = true; m.kind = kind; m.pos = pos; m.dir = dir.normalized; m.target = target;
            m.hasPoint = point.HasValue; m.point = point ?? Vector3.zero;
            m.damage = damage; m.impact = impact; m.fromPlayer = fromPlayer; m.homePlayer = false; m.age = 0f;
            float sf = kind == MissileKind.FragCarrier ? data.fragCarrierSpeedFactor : kind == MissileKind.FragMini ? data.fragMiniSpeedFactor : 1f;
            m.speed = data.missileSpeed0 * sf; m.maxSpeed = data.missileMaxSpeed * sf; m.accel = data.missileAccel * sf;
            m.turn = data.missileTurn; m.delay = kind == MissileKind.FragMini ? data.miniGuidanceDelay : data.missileGuidanceDelay;
            m.life = kind == MissileKind.FragMini ? data.miniLife : data.missileLife;
            m.start = pos; Vector3 goal = target != null ? target.Position : m.point; m.startDist = Vector3.Distance(pos, goal);
            m.t.gameObject.SetActive(true); m.t.SetPositionAndRotation(pos, Quaternion.LookRotation(m.dir));
            if (m.trail != null) m.trail.Clear();
            Color c = kind == MissileKind.FragMini ? new Color(1f, 0.35f, 0.9f) : kind == MissileKind.Enemy ? new Color(1f, 0.2f, 0.2f) : (Random.value < 0.5f ? new Color(1f, 0.31f, 0.85f) : new Color(1f, 0.6f, 0.2f));
            if (m.trail != null) { m.trail.startColor = c * 2f; m.trail.endColor = new Color(c.r, c.g, c.b, 0f); }
            if (fromPlayer) Sfx.Play("missile");
            return m;
        }
        public void SetFrag(M m, int minis, float miniDamage) { m.minis = minis; m.miniDamage = miniDamage; }

        /// <summary>Missile ennemi guide sur le joueur (Corbeau, croiseurs...). Affiche « Missiles entrants » au plus toutes les 3 s.</summary>
        public M LaunchAtPlayer(Vector3 pos, Vector3 dir, float damage)
        {
            var m = Launch(MissileKind.Enemy, pos, dir, null, pos + dir * 100f, damage, 0f, false);
            if (m == null) return null;
            m.homePlayer = true; m.hasPoint = false;
            if (Time.time - lastIncoming > incomingWarningInterval)
            {
                lastIncoming = Time.time;
                Msg.Post("Missiles entrants : quick boost pour esquiver", MsgKind.Danger);
                Sfx.Play("alarm");
            }
            return m;
        }

        void Update()
        {
            IncomingCount = 0; IncomingPositions.Clear();
            foreach (var m in pool) if (m.active && m.homePlayer) { IncomingCount++; IncomingPositions.Add(m.pos); }
            foreach (var m in pool)
            {
                if (!m.active) continue;
                float dt = m.fromPlayer ? GameClock.PlayerDt : GameClock.WorldDt;
                if (dt <= 0f) continue;
                m.age += dt;
                if (m.target != null && !m.target.Alive) { m.hasPoint = true; m.point = m.target.Position; m.target = null; }
                var pl0 = PlayerController.I;
                bool homing = m.homePlayer && pl0 != null && !pl0.dead;
                Vector3 goal = homing ? pl0.Center : m.target != null ? m.target.Position : m.hasPoint ? m.point : m.pos + m.dir * 100f;
                if (m.age > m.delay && (m.target != null || m.hasPoint || homing))
                {
                    Vector3 aim = goal;
                    if (m.target != null || homing)
                    {
                        float tof = Vector3.Distance(m.pos, goal) / Mathf.Max(1f, m.speed);
                        aim = goal + (homing ? pl0.velocity : m.target.Velocity) * tof * data.missileLead;
                    }
                    Vector3 want = (aim - m.pos).normalized;
                    m.dir = Vector3.RotateTowards(m.dir, want, m.turn * dt, 0f).normalized;
                }
                m.speed = Mathf.Min(m.maxSpeed, m.speed + m.accel * dt);
                Vector3 a = m.pos, b = m.pos + m.dir * m.speed * dt;
                m.pos = b; m.t.SetPositionAndRotation(b, Quaternion.LookRotation(m.dir));

                bool boom = false; Targetable direct = null;
                if (m.fromPlayer)
                {
                    foreach (var tg in Targetable.All)
                        if (tg.Alive && MathX.SegmentDistance(a, b, tg.Position) < data.missileContact + tg.radius) { direct = tg; boom = true; break; }
                    if (m.kind == MissileKind.FragCarrier && !boom)
                    {
                        float traveled = Vector3.Distance(m.start, b);
                        bool near = Vector3.Distance(b, goal) < data.fragBurstDistance || (m.startDist > 1f && traveled >= m.startDist * data.fragBurstPath);
                        if (!near) foreach (var tg in Targetable.All) if (tg.Alive && Vector3.Distance(tg.Position, b) < data.fragBurstDistance) { near = true; break; }
                        if (near) { Burst(m); Deactivate(m); continue; }
                    }
                }
                else
                {
                    var pl = PlayerController.I;
                    if (pl != null && !pl.dead && MathX.SegmentDistance(a, b, pl.Center) < data.missileContact + enemyContactRadius) boom = true;
                }
                if (!boom && Physics.Linecast(a, b, Layers.WorldMask, QueryTriggerInteraction.Ignore)) boom = true;
                if (!boom && b.y < TerrainSystem.Ground(b.x, b.z)) boom = true;
                if (!boom && m.age >= m.life) boom = true;
                if (boom)
                {
                    if (m.kind == MissileKind.FragCarrier && m.fromPlayer) Burst(m);
                    Explode(m, direct);
                    Deactivate(m);
                }
            }
        }

        void Deactivate(M m) { m.active = false; m.t.gameObject.SetActive(false); }

        void Explode(M m, Targetable direct)
        {
            Fx.Explosion(m.pos, 5f, false); Sfx.Play("small", m.pos);
            if (m.fromPlayer)
            {
                Targetable hit = direct;
                if (hit == null)
                {
                    float best = data.missileSplash;
                    foreach (var tg in Targetable.All)
                    {
                        if (!tg.Alive) continue;
                        float d = Vector3.Distance(tg.Position, m.pos) - tg.radius;
                        if (d < best) { best = d; hit = tg; }
                    }
                }
                hit?.TakeDamage(new DamageInfo { amount = m.damage, impact = m.impact, point = m.pos, direction = m.dir, fromPlayer = true, source = this });
            }
            else
            {
                // degats pleins au contact, puis decroissants de 100 % a 40 % jusqu'au bord du rayon (§10.1)
                var pl = PlayerController.I;
                if (pl != null && !pl.dead)
                {
                    float d = Mathf.Max(0f, Vector3.Distance(pl.Center, m.pos) - data.missileContact - enemyContactRadius);
                    if (d < enemySplashRadius)
                        pl.TakeDamage(new DamageInfo { amount = m.damage * Mathf.Lerp(1f, enemySplashMinFactor, d / enemySplashRadius), point = m.pos, direction = (pl.Center - m.pos).normalized, fromPlayer = false, source = this });
                }
            }
        }

        /// <summary>Le missile porteur eclate et libere ses mini-missiles a tete chercheuse.</summary>
        void Burst(M m)
        {
            Fx.Flash(m.pos, 10f, new Color(1f, 0.4f, 0.9f));
            var near = new List<Targetable>();
            foreach (var tg in Targetable.All)
                if (tg.Alive && tg.missileTarget && Vector3.Distance(tg.Position, m.pos) < data.missileRange) near.Add(tg);
            near.Sort((x, y) => Vector3.Distance(x.Position, m.pos).CompareTo(Vector3.Distance(y.Position, m.pos)));
            for (int i = 0; i < m.minis; i++)
            {
                Targetable t = near.Count > 0 ? near[i % near.Count] : null;
                Vector3 d = (m.dir + Random.insideUnitSphere * 0.8f).normalized;
                Launch(MissileKind.FragMini, m.pos, d, t, t == null ? (Vector3?)(m.pos + m.dir * 60f + Random.insideUnitSphere * 25f) : null, m.miniDamage, data.missileImpact, true);
            }
        }
    }
}
