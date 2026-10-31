using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Chasseur « Faucon » (§13.1) : vole a 135-160 m/s ; passes d'attaque (anticipation 0,5 s), degage 2,2-3,2 s en montant
    /// a moins de 130 m puis revient ; tir normal (420 m/s, 160) toutes les 0,9-1,4 s dans un cone de 0,35 rad a moins de
    /// 700 m ; rafale de 7 orbes rouges (520 m/s, 140) toutes les 5 s a moins de 900 m.
    /// </summary>
    public class FighterEnemy : Enemy
    {
        [Tooltip("donnees du chasseur (Data/Ennemis)")] public FighterData data;
        [Header("Pieces du prefab")]
        [Tooltip("bouche des canons")] public Transform muzzle;
        public override EnemyData BaseData => data;

        float speed, modeT, fireT, burstT, burstShotT, bank, patrolA; int burstLeft; bool breaking; Vector3 fwd, breakDir;

        protected override void OnRespawn()
        {
            speed = Rand(data.speed); fwd = transform.forward; breaking = false;
            fireT = Rand(data.shotInterval); burstT = data.burstPeriod * Random.value; burstLeft = 0; patrolA = Random.value * Mathf.PI * 2f;
        }

        protected override void OverloadedTick(float dt) { Fly(dt, fwd, speed * 0.5f); }   // chasseur en surcharge : vitesse / 2

        protected override void Tick(float dt, bool engaged)
        {
            Vector3 pos = transform.position, want;
            if (engaged)
            {
                var p = Player;
                if (!breaking)
                {
                    Vector3 aim = p.Center + p.velocity * data.attackLead;
                    want = (aim - pos).normalized;
                    if (distToPlayer < data.breakDistance)
                    {
                        breaking = true; modeT = Rand(data.breakTime);
                        Vector3 away = (pos - p.Center); away.y = 0f;
                        if (away.sqrMagnitude < 1f) away = fwd;
                        away = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f) * away.normalized;
                        breakDir = (away * (1f - data.breakClimb) + Vector3.up * data.breakClimb).normalized;
                    }
                }
                else
                {
                    want = breakDir; modeT -= dt;
                    if (modeT <= 0f) breaking = false;
                }
                Shoot(dt, pos);
            }
            else
            {
                patrolA += speed * 0.5f / Mathf.Max(200f, data.patrolRadius) * dt;
                Vector3 goal = Home + new Vector3(Mathf.Cos(patrolA), 0f, Mathf.Sin(patrolA)) * Mathf.Max(200f, data.patrolRadius);
                want = (goal - pos).normalized;
            }
            Fly(dt, want, engaged ? speed : speed * 0.5f);
        }

        void Fly(float dt, Vector3 want, float spd)
        {
            Vector3 pos = transform.position;
            float gmin = Ground(pos) + data.minGroundClearance;
            if (pos.y < gmin + 30f) want.y = Mathf.Max(want.y, Mathf.Lerp(0.6f, 0f, (pos.y - gmin) / 30f));
            Vector3 before = fwd;
            fwd = Vector3.RotateTowards(fwd, want.normalized, data.turnRate * dt, 0f).normalized;
            float turn = Vector3.SignedAngle(Vector3.ProjectOnPlane(before, Vector3.up), Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.up) / Mathf.Max(1e-4f, dt);
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * 0.6f, -70f, 70f), MathX.Damp(4f, dt));
            pos += fwd * spd * dt;
            if (pos.y < gmin) pos.y = gmin;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, bank));
        }

        void Shoot(float dt, Vector3 pos)
        {
            var p = Player;
            Vector3 o = muzzle != null ? muzzle.position : pos + fwd * 4f;
            Vector3 to = p.Center - o;
            fireT -= dt;
            if (fireT <= 0f && distToPlayer < data.shotRange && Vector3.Angle(fwd, to) * Mathf.Deg2Rad < data.shotCone)
            {
                fireT = Rand(data.shotInterval);
                FireShot(o, Predict(o, data.shotSpeed, 1f), data.shotSpeed, data.shotDamage, data.shotSpread, data.shotRange, data.shotColor, data.shotLength, data.shotExtraRadius);
            }
            burstT -= dt;
            if (burstT <= 0f && burstLeft == 0 && distToPlayer < data.burstRange) { burstLeft = data.burstCount; burstShotT = 0f; burstT = data.burstPeriod; }
            if (burstLeft > 0)
            {
                burstShotT -= dt;
                if (burstShotT <= 0f)
                {
                    FireShot(o, Predict(o, data.burstSpeed, 1f), data.burstSpeed, data.burstDamage, data.shotSpread * 2f, data.burstRange, data.orbColor, 2.5f, data.orbExtraRadius);
                    burstLeft--; burstShotT = data.burstShotInterval;
                }
            }
        }
    }
}
