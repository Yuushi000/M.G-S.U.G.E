using UnityEngine;

namespace MGSUGE
{
    /// <summary>Reglages des 36 avant-postes hostiles (§13.3).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Avant-postes", fileName = "MGSUGE_AvantPostes")]
    public class OutpostData : ScriptableObject
    {
        [Header("Activation")]
        [Tooltip("la garnison apparait quand le joueur est a moins de (m)")] public float activationDistance = 1700f;
        [Tooltip("distance d'activation quand le joueur est dans l'espace (m)")] public float activationDistanceSpace = 1300f;
        [Tooltip("le joueur est « dans l'espace » au-dessus de cette altitude (m) — hypothese : debut du passage a l'espace")] public float spaceAltitude = 3600f;
        [Tooltip("la garnison disparait au-dela de (m)")] public float deactivationDistance = 2700f;
        [Tooltip("delai minimal depuis la derniere victoire avant de reactiver (s)")] public float refreshDelay = 150f;
        [Tooltip("intervalle de verification de la distance (s)")] public float checkInterval = 0.25f;

        [Header("Victoire")]
        [Tooltip("credits quand tous les ennemis sont detruits")] [Min(0)] public long reward = 400;
        [Tooltip("message ({0} = recompense)")] public string victoryMessage = "Avant-poste neutralisé · +{0}";

        [Header("Generation (constructeur de scene)")]
        [Tooltip("nombre d'avant-postes")] public int count = 36;
        [Tooltip("graine du hasard (reproductible)")] public int seed = 90210;
        [Tooltip("distance minimale a la plateforme Helios (m)")] public float minDistanceFromHelios = 1500f;
        [Tooltip("distance minimale entre deux avant-postes (m)")] public float minSpacing = 850f;
        [Tooltip("ecart de hauteur maximal sous le batiment pour un terrain « plat » (m) — hypothese")] public float maxSlope = 8f;
    }
}
