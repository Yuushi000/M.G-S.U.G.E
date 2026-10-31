using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Tourelle de defense (§13.1) : fixe ; la tete pivote vers le joueur ; rafale de 4 tirs (330 m/s, 190, 0,16 s) puis pause
    /// de 2,2-3 s, jusqu'a 680 m.
    /// </summary>
    public class TurretEnemy : Enemy
    {
        [Tooltip("donnees de la tourelle (Data/Ennemis)")] public TurretData data;
        [Header("Pieces du prefab")]
        [Tooltip("tete qui pivote (lacet)")] public Transform head;
        [Tooltip("canon qui s'incline (tangage)")] public Transform barrel;
        [Tooltip("bouche du canon")] public Transform muzzle;
        public override EnemyData BaseData => data;

        int burstLeft; float burstT, pauseT;

        protected override void OnRespawn() { burstLeft = 0; pauseT = Rand(data.burstPause) * 0.5f; }

        protected override void Tick(float dt, bool engaged)
        {
            if (head == null) return;
            if (!engaged) { head.Rotate(0f, 12f * dt, 0f, Space.Self); return; }
            Vector3 o = muzzle != null ? muzzle.position : head.position;
            Vector3 aim = Predict(o, data.shotSpeed, data.lead);
            Vector3 to = aim - head.position;
            Vector3 flat = Vector3.ProjectOnPlane(to, transform.up);
            if (flat.sqrMagnitude > 0.01f)
                head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(flat, transform.up), data.turnSpeed * dt);
            if (barrel != null)
            {
                float pitch = -Mathf.Atan2(Vector3.Dot(to, transform.up), flat.magnitude) * Mathf.Rad2Deg;
                barrel.localRotation = Quaternion.RotateTowards(barrel.localRotation, Quaternion.Euler(Mathf.Clamp(pitch, -80f, 15f), 0f, 0f), data.turnSpeed * dt);
            }
            Vector3 fwd = barrel != null ? barrel.forward : head.forward;
            bool aimed = Vector3.Angle(fwd, aim - o) < data.fireAngle;
            if (distToPlayer > data.shotRange) return;
            if (burstLeft > 0)
            {
                burstT -= dt;
                if (burstT <= 0f)
                {
                    FireShot(o, aim, data.shotSpeed, data.shotDamage, data.shotSpread, data.shotRange, data.shotColor, data.shotLength, data.shotExtraRadius);
                    burstLeft--; burstT = data.burstInterval;
                    if (burstLeft == 0) pauseT = Rand(data.burstPause);
                }
            }
            else { pauseT -= dt; if (pauseT <= 0f && aimed) { burstLeft = data.burstCount; burstT = 0f; } }
        }
    }
}
