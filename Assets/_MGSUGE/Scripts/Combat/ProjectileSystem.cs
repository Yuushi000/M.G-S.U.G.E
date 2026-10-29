using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Tirs a impulsions (joueur et, plus tard, ennemis) en pool de 700 (§5.1).
    /// Collision continue : segment (position precedente -> nouvelle) contre les spheres des cibles, Linecast contre le decor.
    /// </summary>
    public class ProjectileSystem : MonoBehaviour
    {
        public static ProjectileSystem I { get; private set; }
        public WeaponData data;
        public Material material;
        [Tooltip("rayon de touche du joueur : 4,2 m en chassis, 3,6 m en vaisseau (§10.1)")] public float playerHitMech = 4.2f, playerHitShip = 3.6f;

        class P
        {
            public bool active; public Vector3 pos, vel; public float life, damage, impact, length, extraRadius;
            public bool fromPlayer; public Color color; public Transform t; public Renderer r;
        }
        readonly List<P> pool = new List<P>();
        Mesh mesh; MaterialPropertyBlock mpb;
        public int ActiveCount { get; private set; }
        /// <summary>Statistiques (diagnostic) : tirs du joueur par issue.</summary>
        public int statFired, statTargets, statWorld, statGround, statExpired; public string statLastWorld = "";

        void Awake()
        {
            I = this; mpb = new MaterialPropertyBlock();
            mesh = MeshKit.Spindle(0.28f, 1f, 6);
            int n = data != null ? data.projectilePool : 700;
            for (int i = 0; i < n; i++)
            {
                var go = MeshKit.Spawn("Tir", mesh, material, transform, Vector3.zero, Quaternion.identity);
                var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                go.SetActive(false);
                pool.Add(new P { t = go.transform, r = r });
            }
        }

        public bool Fire(Vector3 pos, Vector3 vel, float damage, float impact, float life, float length, Color color, bool fromPlayer, float extraRadius = 0f)
        {
            P p = null;
            foreach (var x in pool) if (!x.active) { p = x; break; }
            if (p == null) return false;
            if (fromPlayer) statFired++;
            p.active = true; p.pos = pos; p.vel = vel; p.damage = damage; p.impact = impact; p.life = life; p.length = length;
            p.color = color; p.fromPlayer = fromPlayer; p.extraRadius = extraRadius;
            p.t.gameObject.SetActive(true);
            p.t.localScale = new Vector3(1f, 1f, length);
            p.r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", color * 2.5f); p.r.SetPropertyBlock(mpb);
            Place(p);
            return true;
        }
        void Place(P p)
        {
            Vector3 d = p.vel.sqrMagnitude > 1e-4f ? p.vel.normalized : Vector3.forward;
            p.t.SetPositionAndRotation(p.pos - d * p.length * 0.5f, Quaternion.LookRotation(d));
        }

        void Update()
        {
            int count = 0;
            var player = PlayerController.I;
            foreach (var p in pool)
            {
                if (!p.active) continue;
                float dt = p.fromPlayer ? GameClock.PlayerDt : GameClock.WorldDt;
                if (dt <= 0f) { count++; continue; }
                Vector3 a = p.pos, b = p.pos + p.vel * dt;
                p.life -= dt;
                bool done = false;
                if (p.fromPlayer)
                {
                    Targetable best = null; float bestT = 2f;
                    foreach (var tg in Targetable.All)
                    {
                        if (!tg.Alive) continue;
                        if (MathX.SegmentDistance(a, b, tg.Position) < tg.radius + p.extraRadius)
                        {
                            float t = MathX.SegmentT(a, b, tg.Position);
                            if (t < bestT) { bestT = t; best = tg; }
                        }
                    }
                    if (best != null)
                    {
                        Vector3 hp = Vector3.Lerp(a, b, bestT);
                        best.TakeDamage(new DamageInfo { amount = p.damage, impact = p.impact, point = hp, direction = p.vel.normalized, fromPlayer = true, source = this });
                        Fx.Hit(hp, p.color); done = true; statTargets++;
                    }
                }
                else if (player != null && !player.dead)
                {
                    float rad = (player.IsShip ? playerHitShip : playerHitMech) + p.extraRadius;
                    if (MathX.SegmentDistance(a, b, player.Center) < rad)
                    {
                        player.TakeDamage(new DamageInfo { amount = p.damage, impact = p.impact, point = player.Center, direction = p.vel.normalized, fromPlayer = false, source = this });
                        Fx.Hit(player.Center, p.color); done = true;
                    }
                }
                if (!done && Physics.Linecast(a, b, out RaycastHit h, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                { Fx.Hit(h.point, p.color); done = true; if (p.fromPlayer) { statWorld++; statLastWorld = h.collider != null ? h.collider.name : "?"; } }
                if (!done && b.y < TerrainSystem.Ground(b.x, b.z)) { Fx.Hit(b, p.color); done = true; if (p.fromPlayer) statGround++; }
                if (!done && p.life <= 0f && p.fromPlayer) statExpired++;
                if (done || p.life <= 0f) { p.active = false; p.t.gameObject.SetActive(false); continue; }
                p.pos = b; Place(p); count++;
            }
            ActiveCount = count;
        }
    }
}
