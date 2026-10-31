using UnityEngine;

namespace MGSUGE
{
    /// <summary>Sentinelle (drone) : vole en orbite autour du joueur et tire un projectile anticipe (§13.1).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Sentinelle", fileName = "MGSUGE_Ennemi_Sentinelle")]
    public class SentinelData : EnemyData
    {
        [Header("Vol en combat")]
        [Tooltip("rayon de l'orbite autour du joueur, tire au hasard entre mini et maxi (m)")] public Vector2 orbitRadius = new Vector2(80f, 150f);
        [Tooltip("hauteur au-dessus du joueur (m)")] public float heightAbovePlayer = 20f;
        [Tooltip("variation de hauteur (+/- m)")] public float heightJitter = 14f;
        [Tooltip("vitesse maximale (m/s)")] public float maxSpeed = 95f;
        [Tooltip("vitesse tangentielle de l'orbite (m/s) — hypothese, non chiffree par le cahier")] public float orbitSpeed = 50f;
        [Tooltip("acceleration (m/s²) — hypothese")] public float acceleration = 140f;
        [Tooltip("hauteur minimale au-dessus du sol (m)")] public float minGroundClearance = 8f;

        [Header("Tir")]
        [Tooltip("vitesse du projectile (m/s)")] public float shotSpeed = 260f;
        [Tooltip("degats par projectile (avant menace)")] public float shotDamage = 170f;
        [Tooltip("dispersion (rad)")] public float shotSpread = 0.01f;
        [Tooltip("intervalle entre deux tirs, tire au hasard (s)")] public Vector2 shotInterval = new Vector2(1.4f, 2.2f);
        [Tooltip("portee de tir (m)")] public float shotRange = 560f;
        [Tooltip("anticipation : 1 = vise la position future exacte")] [Range(0f, 1.5f)] public float lead = 1f;
    }
}
