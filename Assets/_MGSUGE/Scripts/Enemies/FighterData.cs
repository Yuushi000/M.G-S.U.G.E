using UnityEngine;

namespace MGSUGE
{
    /// <summary>Chasseur « Faucon » : passes d'attaque rapides et rafales d'orbes rouges (§13.1).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Chasseur Faucon", fileName = "MGSUGE_Ennemi_Faucon")]
    public class FighterData : EnemyData
    {
        [Header("Vol")]
        [Tooltip("vitesse, tiree au hasard entre mini et maxi (m/s)")] public Vector2 speed = new Vector2(135f, 160f);
        [Tooltip("taux de virage (rad/s) — hypothese")] public float turnRate = 1.6f;
        [Tooltip("anticipation de la passe d'attaque (s)")] public float attackLead = 0.5f;
        [Tooltip("distance a laquelle il degage (m)")] public float breakDistance = 130f;
        [Tooltip("duree du degagement en montant (s)")] public Vector2 breakTime = new Vector2(2.2f, 3.2f);
        [Tooltip("part verticale de la direction de degagement — hypothese")] [Range(0f, 1f)] public float breakClimb = 0.45f;
        [Tooltip("hauteur minimale au-dessus du sol (m) — hypothese")] public float minGroundClearance = 40f;

        [Header("Tir normal")]
        [Tooltip("vitesse du projectile (m/s)")] public float shotSpeed = 420f;
        [Tooltip("degats (avant menace)")] public float shotDamage = 160f;
        [Tooltip("intervalle (s)")] public Vector2 shotInterval = new Vector2(0.9f, 1.4f);
        [Tooltip("cone de tir (rad)")] public float shotCone = 0.35f;
        [Tooltip("portee (m)")] public float shotRange = 700f;
        [Tooltip("dispersion (rad) — hypothese")] public float shotSpread = 0.01f;

        [Header("Rafale « fusil a impulsions » (orbes rouges)")]
        [Tooltip("tirs par rafale")] [Min(1)] public int burstCount = 7;
        [Tooltip("degats par orbe (avant menace)")] public float burstDamage = 140f;
        [Tooltip("vitesse des orbes (m/s)")] public float burstSpeed = 520f;
        [Tooltip("une rafale toutes les N secondes")] public float burstPeriod = 5f;
        [Tooltip("portee (m)")] public float burstRange = 900f;
        [Tooltip("intervalle entre orbes d'une rafale (s) — hypothese")] public float burstShotInterval = 0.08f;
        [Tooltip("couleur des orbes")] public Color orbColor = new Color(1f, 0.12f, 0.12f);
        [Tooltip("rayon de touche supplementaire des orbes (m)")] public float orbExtraRadius = 1.2f;
    }
}
