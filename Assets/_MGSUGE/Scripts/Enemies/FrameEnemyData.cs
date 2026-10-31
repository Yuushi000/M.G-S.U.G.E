using UnityEngine;

namespace MGSUGE
{
    /// <summary>Chassis hostile « Corbeau » : meme modele que le joueur en palette hostile (§13.1).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Chassis Corbeau", fileName = "MGSUGE_Ennemi_Corbeau")]
    public class FrameEnemyData : EnemyData
    {
        [Header("Deplacement")]
        [Tooltip("vitesse de marche (m/s)")] public float walkSpeed = 55f;
        [Tooltip("acceleration au sol (m/s²) — hypothese")] public float acceleration = 140f;
        [Tooltip("distance gardee avec le joueur : mini / maxi (m)")] public Vector2 keepDistance = new Vector2(150f, 280f);
        [Tooltip("intervalle entre deux quick boosts lateraux (s)")] public Vector2 strafeInterval = new Vector2(1.8f, 3.6f);
        [Tooltip("impulsion du quick boost lateral (m/s)")] public float quickBoostImpulse = 150f;
        [Tooltip("duree du quick boost (s) — hypothese : meme duree que celui du joueur")] public float quickBoostTime = 0.25f;
        [Tooltip("monte pour suivre un joueur en altitude, jusqu'a (m/s)")] public float climbSpeed = 55f;
        [Tooltip("le joueur est « en altitude » au-dessus de cette hauteur sol (m) — hypothese")] public float followAltitude = 25f;
        [Tooltip("hauteur du bassin au-dessus du sol (m) : chassis de 12 m")] public float frameClear = 7f;

        [Header("Rafale du fusil")]
        [Tooltip("tirs par rafale")] [Min(1)] public int burstCount = 3;
        [Tooltip("vitesse du projectile (m/s)")] public float shotSpeed = 400f;
        [Tooltip("degats par tir (avant menace)")] public float shotDamage = 230f;
        [Tooltip("intervalle entre tirs d'une rafale (s)")] public float burstInterval = 0.14f;
        [Tooltip("pause entre deux rafales (s)")] public Vector2 burstPause = new Vector2(2f, 3.2f);
        [Tooltip("portee (m)")] public float shotRange = 650f;
        [Tooltip("dispersion (rad) — hypothese")] public float shotSpread = 0.012f;
        [Tooltip("anticipation")] [Range(0f, 1.5f)] public float lead = 1f;

        [Header("Missiles")]
        [Tooltip("missiles par salve")] [Min(0)] public int missileCount = 2;
        [Tooltip("degats par missile (avant menace)")] public float missileDamage = 520f;
        [Tooltip("intervalle entre deux salves (s)")] public Vector2 missileInterval = new Vector2(6f, 9f);
        [Tooltip("portee (m)")] public float missileRange = 700f;
    }
}
