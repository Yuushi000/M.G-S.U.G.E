using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Chassis hostile « Corbeau » (§13.1) : marche a 55 m/s en gardant 150-280 m, strafe et quick boost lateral toutes les
    /// 1,8-3,6 s (150 m/s) ; rafale de 3 tirs (400 m/s, 230, 0,14 s) puis pause de 2-3,2 s jusqu'a 650 m ; 2 missiles (520)
    /// toutes les 6-9 s a moins de 700 m ; monte pour suivre un joueur en altitude (55 m/s). Anime par FrameRig.
    /// </summary>
    public class FrameEnemy : Enemy
    {
        [Tooltip("donnees du Corbeau (Data/Ennemis)")] public FrameEnemyData data;
        [Header("Pieces du prefab")]
        [Tooltip("animation des os du chassis")] public FrameRig rig;
        [Tooltip("bouche du fusil")] public Transform muzzle;
        [Tooltip("sortie des missiles")] public Transform podOut;
        public override EnemyData BaseData => data;

        Vector3 hv; float vy, yaw, strafeDir, strafeT, qbT, burstT, pauseT, missileT, walkPhase, patrolA;
        int burstLeft; Vector3 qbVel;

        protected override void OnRespawn()
        {
            hv = Vector3.zero; vy = 0f; yaw = transform.eulerAngles.y; strafeDir = Random.value < 0.5f ? -1f : 1f;
            strafeT = Rand(data.strafeInterval); qbT = 0f; burstLeft = 0; pauseT = Rand(data.burstPause) * 0.5f;
            missileT = Rand(data.missileInterval) * 0.6f; patrolA = Random.value * Mathf.PI * 2f;
            if (rig != null) { rig.ship = 0f; rig.walkAmount = 0f; }
        }

        protected override void OverloadedTick(float dt)
        {
            base.OverloadedTick(dt);
            if (rig != null) rig.walkAmount = Mathf.MoveTowards(rig.walkAmount, 0f, dt * 4f);
        }

        protected override void Tick(float dt, bool engaged)
        {
            Vector3 pos = transform.position;
            float ground = Ground(pos) + data.frameClear;
            Vector3 wish; float speed = data.walkSpeed; float targetY = ground;
            if (engaged)
            {
                var p = Player;
                Vector3 to = p.Center - pos; to.y = 0f;
                float d = to.magnitude; Vector3 dir = d > 0.1f ? to / d : transform.forward;
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                if (d > data.keepDistance.y) wish = dir;
                else if (d < data.keepDistance.x) wish = -dir;
                else wish = side * strafeDir * 0.8f;
                strafeT -= dt;
                if (strafeT <= 0f)
                {
                    strafeT = Rand(data.strafeInterval); strafeDir = -strafeDir;
                    qbVel = side * strafeDir * data.quickBoostImpulse; qbT = data.quickBoostTime;
                    Fx.Dust(pos - Vector3.up * data.frameClear, 10f); Sfx.Play("qb", pos);
                }
                // monte pour suivre un joueur en altitude
                if (p.Center.y - TerrainSystem.Ground(p.Center.x, p.Center.z) > data.followAltitude) targetY = Mathf.Max(ground, p.Center.y);
                yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, MathX.Damp(6f, dt));
                Shoot(dt);
            }
            else
            {
                patrolA += data.patrolSpeed / Mathf.Max(5f, data.patrolRadius) * dt;
                Vector3 goal = Home + new Vector3(Mathf.Cos(patrolA), 0f, Mathf.Sin(patrolA)) * data.patrolRadius;
                Vector3 to = goal - pos; to.y = 0f;
                wish = to.magnitude > 2f ? to.normalized : Vector3.zero; speed = data.patrolSpeed;
                if (hv.sqrMagnitude > 1f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg, MathX.Damp(3f, dt));
            }
            hv = Vector3.MoveTowards(hv, wish * speed, data.acceleration * dt);
            Vector3 move = hv;
            if (qbT > 0f) { qbT -= dt; move += qbVel; }
            vy = Mathf.Clamp((targetY - pos.y) * 2f, -data.climbSpeed, data.climbSpeed);
            pos += (move + Vector3.up * vy) * dt;
            if (pos.y < ground) pos.y = ground;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

            if (rig != null)
            {
                bool onGround = pos.y - ground < 1f;
                float hs = hv.magnitude;
                rig.walkAmount = Mathf.MoveTowards(rig.walkAmount, onGround ? Mathf.Clamp01(hs / data.walkSpeed) : 0f, dt * 4f);
                walkPhase += hs * dt * Mathf.PI * 2f / 15f;
                rig.walkPhase = walkPhase;
                Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * move;
                rig.leanForward = Mathf.Lerp(rig.leanForward, Mathf.Clamp(local.z / 150f, -0.2f, 0.4f), MathX.Damp(6f, dt));
                rig.leanSide = Mathf.Lerp(rig.leanSide, Mathf.Clamp(local.x / 150f, -0.3f, 0.3f), MathX.Damp(6f, dt));
            }
        }

        void Shoot(float dt)
        {
            Vector3 o = muzzle != null ? muzzle.position : transform.position + transform.forward * 4f;
            if (distToPlayer < data.shotRange)
            {
                if (burstLeft > 0)
                {
                    burstT -= dt;
                    if (burstT <= 0f)
                    {
                        FireShot(o, Predict(o, data.shotSpeed, data.lead), data.shotSpeed, data.shotDamage, data.shotSpread, data.shotRange, data.shotColor, data.shotLength, data.shotExtraRadius);
                        burstLeft--; burstT = data.burstInterval;
                        if (burstLeft == 0) pauseT = Rand(data.burstPause);
                    }
                }
                else { pauseT -= dt; if (pauseT <= 0f) { burstLeft = data.burstCount; burstT = 0f; } }
            }
            missileT -= dt;
            if (missileT <= 0f && distToPlayer < data.missileRange && MissileSystem.I != null)
            {
                missileT = Rand(data.missileInterval);
                Vector3 mo = podOut != null ? podOut.position : transform.position + Vector3.up * 4f;
                for (int i = 0; i < data.missileCount; i++)
                {
                    Vector3 d = (Vector3.up + transform.forward * 0.6f + Random.insideUnitSphere * 0.35f).normalized;
                    MissileSystem.I.LaunchAtPlayer(mo, d, data.missileDamage * Threat.DamageMult);
                }
            }
        }
    }
}
