using UnityEngine;

namespace MGSUGE
{
    /// <summary>Un contrat (§13.4) : textes et recompense. Les ennemis sont les objets enfants du contrat dans la scene.</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Contrat", fileName = "MGSUGE_Contrat")]
    public class ContractData : ScriptableObject
    {
        [Tooltip("titre affiche en grandes majuscules")] public string title = "Contrat";
        [Tooltip("lieu (texte)")] public string location = "";
        [Tooltip("description affichee sous le titre")] [TextArea(2, 4)] public string description = "";
        [Tooltip("recompense de base en credits (x menace)")] [Min(0)] public long reward = 2600;
        [Tooltip("composition attendue (rappel pour la mise en place dans la scene)")] [TextArea(1, 3)] public string enemiesNote = "";
    }
}
