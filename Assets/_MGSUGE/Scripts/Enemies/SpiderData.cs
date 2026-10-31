using UnityEngine;

namespace MGSUGE
{
    /// <summary>Araignee de siege : marche lentement a distance et tire des obus de mortier annonces par un telegraphe (§13.1).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Ennemis/Araignee de siege", fileName = "MGSUGE_Ennemi_Araignee")]
    public class SpiderData : EnemyData
    {
        [Header("Marche")]
        [Tooltip("vitesse de marche, tiree au hasard (m/s)")] public Vector2 walkSpeed = new Vector2(10f, 18f);
        [Tooltip("distance gardee avec le joueur : mini / maxi (m)")] public Vector2 keepDistance = new Vector2(320f, 520f);
        [Tooltip("hauteur du corps au-dessus du sol (m)")] public float bodyClear = 5f;

        [Header("Mortier")]
        [Tooltip("obus par salve")] [Min(1)] public int shellCount = 2;
        [Tooltip("degats par obus (avant menace)")] public float shellDamage = 650f;
        [Tooltip("rayon d'explosion (m)")] public float blastRadius = 18f;
        [Tooltip("degats au bord du rayon (fraction)")] [Range(0f, 1f)] public float edgeDamageFactor = 0.4f;
        [Tooltip("intervalle entre deux salves (s)")] public Vector2 interval = new Vector2(4f, 5.5f);
        [Tooltip("portee (m)")] public float range = 950f;
        [Tooltip("anticipation de la position du joueur (s)")] public float lead = 2.2f;
        [Tooltip("facteur aleatoire applique a la vitesse du joueur — hypothese")] public Vector2 leadRandom = new Vector2(0.6f, 1.25f);
        [Tooltip("temps de vol (s)")] public float flightTime = 2.4f;
        [Tooltip("temps de vol dans un hangar (s) — jalon 9")] public float hangarFlightTime = 1.1f;
        [Tooltip("ecart entre les obus d'une salve (m) — hypothese")] public float shellSpread = 14f;
    }
}
