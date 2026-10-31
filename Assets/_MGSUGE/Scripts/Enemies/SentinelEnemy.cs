using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Sentinelle (drone volant, §13.1). Engagee : orbite autour du joueur a 80-150 m, 20 m au-dessus (+/- 14 m), jusqu'a
    /// 95 m/s ; tire un projectile anticipe (260 m/s, 170) toutes les 1,4-2,2 s a moins de 560 m. Sinon : patrouille.
    /// </summary>
    public class SentinelEnemy : Enemy
    {
        [Tooltip("donnees de la sentinelle (Data/Ennemis)")] public SentinelData data;
        [Header("Pieces du prefab")]
        [Tooltip("point de depart des tirs")] public Transform muzzle;
        [Tooltip("anneau qui tourne autour du noyau")] public Transform ring;
        public override EnemyData BaseData => data;

        Vector3 vel; float angle, orbitR, dirSign, phase, fireT, patrolA;

        protected override void OnRespawn()
        {
            vel = Vector3.zero; angle = Random.value * Mathf.PI * 2f; patrolA = angle;
            orbitR = Rand(data.orbitRadius); dirSign = Random.value < 0.5f ? -1f : 1f; phase = Random.value * 10f;
            fireT = Rand(data.shotInterval);
        }

        protected override void Tick(float dt, bool engaged)
        {
            Vector3 pos = transform.position, desired; float vmax;
            if (engaged)
            {
                var p = Player;
                angle += dirSign * data.orbitSpeed / Mathf.Max(10f, orbitR) * dt;
                float h = data.heightAbovePlayer + Mathf.Sin(Time.time * 0.7f + phase) * data.heightJitter;
                desired = p.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitR + Vector3.up * h;
                vmax = data.maxSpeed;
            }
            else
            {
                patrolA += data.patrolSpeed / Mathf.Max(5f, data.patrolRadius) * dt;
                desired = Home + new Vector3(Mathf.Cos(patrolA), 0f, Mathf.Sin(patrolA)) * data.patrolRadius;
                vmax = data.patrolSpeed * 1.5f;
            }
            Vector3 to = desired - pos;
            Vector3 want = to.normalized * Mathf.Min(vmax, to.magnitude * 1.5f);
            vel = Vector3.MoveTowards(vel, want, data.acceleration * dt);
            pos += vel * dt;
            float gmin = Ground(pos) + data.minGroundClearance;
            if (pos.y < gmin) { pos.y = gmin; if (vel.y < 0f) vel.y = 0f; }
            transform.position = pos;

            Vector3 look = engaged ? Player.Center - pos : vel;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), MathX.Damp(5f, dt));
            if (ring != null) ring.Rotate(0f, 120f * dt, 0f, Space.Self);

            if (engaged && distToPlayer < data.shotRange)
            {
                fireT -= dt;
                if (fireT <= 0f)
                {
                    fireT = Rand(data.shotInterval);
                    Vector3 o = muzzle != null ? muzzle.position : pos;
                    FireShot(o, Predict(o, data.shotSpeed, data.lead), data.shotSpeed, data.shotDamage, data.shotSpread, data.shotRange, data.shotColor, data.shotLength, data.shotExtraRadius);
                }
            }
        }
    }
}
