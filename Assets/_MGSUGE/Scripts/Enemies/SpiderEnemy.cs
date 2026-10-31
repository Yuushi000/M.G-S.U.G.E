using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Araignee de siege (§13.1) : marche lentement (10-18 m/s) en gardant 320-520 m ; mortier : 2 obus de 650 (rayon 18 m,
    /// telegraphe au sol) toutes les 4-5,5 s a moins de 950 m, vises sur la position future du joueur (anticipation 2,2 s),
    /// temps de vol 2,4 s. En surcharge : a l'arret.
    /// </summary>
    public class SpiderEnemy : Enemy
    {
        [Tooltip("donnees de l'araignee (Data/Ennemis)")] public SpiderData data;
        [Header("Pieces du prefab")]
        [Tooltip("corps (balancement)")] public Transform body;
        [Tooltip("tube du mortier (depart des obus)")] public Transform mortar;
        [Tooltip("pattes, pivot a la hanche (animation procedurale)")] public Transform[] legs = new Transform[0];
        public override EnemyData BaseData => data;

        float speed, fireT, legPhase, patrolA, yaw; Quaternion[] legRest;

        protected override void Awake()
        {
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++) if (legs[i] != null) legRest[i] = legs[i].localRotation;
            base.Awake();
        }
        protected override void OnRespawn() { speed = Rand(data.walkSpeed); fireT = Rand(data.interval) * 0.6f; patrolA = Random.value * 6.28f; yaw = transform.eulerAngles.y; }

        protected override void Tick(float dt, bool engaged)
        {
            Vector3 pos = transform.position, wish = Vector3.zero; float spd = speed;
            if (engaged)
            {
                var p = Player;
                Vector3 to = p.Center - pos; to.y = 0f; float d = to.magnitude; Vector3 dir = d > 0.1f ? to / d : transform.forward;
                if (d > data.keepDistance.y) wish = dir; else if (d < data.keepDistance.x) wish = -dir; else wish = Vector3.Cross(Vector3.up, dir) * 0.4f;
                yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, MathX.Damp(2f, dt));
                fireT -= dt;
                if (fireT <= 0f && distToPlayer < data.range)
                {
                    fireT = Rand(data.interval);
                    Fire(p);
                }
            }
            else
            {
                patrolA += data.patrolSpeed / Mathf.Max(5f, data.patrolRadius) * dt;
                Vector3 goal = Home + new Vector3(Mathf.Cos(patrolA), 0f, Mathf.Sin(patrolA)) * data.patrolRadius;
                Vector3 to = goal - pos; to.y = 0f; if (to.magnitude > 2f) wish = to.normalized;
                spd = Mathf.Min(speed, data.patrolSpeed);
                if (wish.sqrMagnitude > 0.01f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(wish.x, wish.z) * Mathf.Rad2Deg, MathX.Damp(2f, dt));
            }
            pos += wish * spd * dt;
            pos.y = Ground(pos) + data.bodyClear;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Animate(dt, wish.magnitude * spd);
        }

        void Fire(PlayerController p)
        {
            var shells = ShellSystem.I; if (shells == null) return;
            Vector3 o = mortar != null ? mortar.position : transform.position + Vector3.up * 6f;
            Vector3 future = p.Center + p.velocity * data.lead * Rand(data.leadRandom);
            for (int i = 0; i < data.shellCount; i++)
            {
                Vector3 t = future + (i == 0 ? Vector3.zero : new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * data.shellSpread);
                t.y = TerrainSystem.Ground(t.x, t.z);
                shells.Launch(o, t, data.flightTime, data.shellDamage * Threat.DamageMult, data.blastRadius, data.edgeDamageFactor);
            }
            Fx.Flash(o, 4f, new Color(1f, 0.5f, 0.2f)); Sfx.Play("missile", o);
        }

        void Animate(float dt, float hs)
        {
            legPhase += (0.6f + hs * 0.25f) * dt * 3f;
            if (body != null) body.localPosition = new Vector3(0f, Mathf.Sin(legPhase * 2f) * 0.25f * Mathf.Clamp01(hs / 5f), 0f);
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float lift = Mathf.Max(0f, Mathf.Sin(legPhase + (i % 2) * Mathf.PI)) * 18f * Mathf.Clamp01(hs / 5f);
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, 0f, -lift);
            }
        }
    }
}
