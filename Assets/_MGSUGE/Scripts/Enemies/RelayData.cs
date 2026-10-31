using UnityEngine;

namespace MGSUGE
{
    /// <summary>Relais de brouillage : ne tire pas ; objectif de contrat (noyau octaedrique flottant, 2 anneaux tournants).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Relais de brouillage", fileName = "MGSUGE_Ennemi_Relais")]
    public class RelayData : EnemyData
    {
        [Header("Animation")]
        [Tooltip("vitesse des deux anneaux (degres/s)")] public Vector2 ringSpeeds = new Vector2(40f, -28f);
        [Tooltip("amplitude du flottement vertical (m)")] public float bobAmplitude = 3f;
        [Tooltip("frequence du flottement (rad/s)")] public float bobSpeed = 0.8f;
    }
}
