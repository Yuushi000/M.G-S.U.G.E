using UnityEngine;

namespace MGSUGE
{
    /// <summary>Tourelle de defense : fixe (sol, toit, tour, croiseur), tir en rafales (§13.1).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Tourelle", fileName = "MGSUGE_Ennemi_Tourelle")]
    public class TurretData : EnemyData
    {
        [Header("Visee")]
        [Tooltip("vitesse de rotation de la tete (degres/s) — hypothese")] public float turnSpeed = 140f;
        [Tooltip("ecart angulaire maxi pour tirer (degres) — hypothese")] public float fireAngle = 12f;

        [Header("Rafale")]
        [Tooltip("tirs par rafale")] [Min(1)] public int burstCount = 4;
        [Tooltip("vitesse du projectile (m/s)")] public float shotSpeed = 330f;
        [Tooltip("degats par tir (avant menace)")] public float shotDamage = 190f;
        [Tooltip("intervalle entre tirs d'une rafale (s)")] public float burstInterval = 0.16f;
        [Tooltip("pause entre deux rafales (s)")] public Vector2 burstPause = new Vector2(2.2f, 3f);
        [Tooltip("portee (m)")] public float shotRange = 680f;
        [Tooltip("dispersion (rad) — hypothese")] public float shotSpread = 0.012f;
        [Tooltip("anticipation")] [Range(0f, 1.5f)] public float lead = 1f;
    }
}
