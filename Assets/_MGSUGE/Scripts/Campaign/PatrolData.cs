using UnityEngine;

namespace MGSUGE
{
    /// <summary>Apparitions aleatoires de patrouilles errantes (§13.2).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Patrouilles errantes", fileName = "MGSUGE_Patrouilles")]
    public class PatrolData : ScriptableObject
    {
        [Tooltip("premiere apparition apres (s)")] public float firstDelay = 8f;
        [Tooltip("une tentative toutes les (s)")] public float interval = 16f;
        [Tooltip("seulement si moins de N ennemis libres sont actifs")] public int maxFree = 8;
        [Tooltip("distance d'apparition autour du joueur : mini / maxi (m)")] public Vector2 spawnDistance = new Vector2(800f, 1200f);
        [Tooltip("jamais a moins de (m) de la plateforme Helios")] public float heliosClearance = 500f;
        [Tooltip("probabilite d'un groupe de chasseurs (sinon sentinelles)")] [Range(0f, 1f)] public float fighterChance = 0.4f;
        [Tooltip("nombre de chasseurs par groupe")] public int fighterGroup = 2;
        [Tooltip("nombre de sentinelles par groupe")] public int sentinelGroup = 3;
        [Tooltip("hauteur des chasseurs au-dessus du sol (m)")] public Vector2 fighterHeight = new Vector2(160f, 260f);
        [Tooltip("hauteur des sentinelles au-dessus du sol (m)")] public Vector2 sentinelHeight = new Vector2(50f, 120f);
        [Tooltip("au-dessus de cette altitude du joueur : toujours des chasseurs (m)")] public float highAltitude = 3000f;
        [Tooltip("ecart d'altitude des chasseurs autour du joueur en haute altitude (m)")] public float highAltitudeSpread = 120f;
        [Tooltip("les ennemis libres au-dela de cette distance du joueur sont supprimes (m)")] public float despawnDistance = 3000f;
        [Tooltip("espacement entre les membres d'un groupe (m)")] public float groupSpacing = 35f;
    }
}
