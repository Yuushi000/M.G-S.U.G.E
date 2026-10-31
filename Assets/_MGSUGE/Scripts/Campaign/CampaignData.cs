using UnityEngine;

namespace MGSUGE
{
    /// <summary>Menace (§10.3) et enchainement des contrats (§13.4).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Campagne (menace et contrats)", fileName = "MGSUGE_Campagne")]
    public class CampaignData : ScriptableObject
    {
        [Header("10.3 Niveau de menace")]
        [Tooltip("PV des ennemis standard : x (1 + ce coefficient x menace)")] public float hpPerThreat = 0.3f;
        [Tooltip("degats des tirs, obus et missiles ennemis : x (1 + ce coefficient x menace)")] public float damagePerThreat = 0.2f;
        [Tooltip("recompense des contrats : x (1 + ce coefficient x menace)")] public float rewardPerThreat = 0.3f;
        [Tooltip("message a la hausse de la menace")] public string threatMessage = "Les forces hostiles se renforcent";

        [Header("13.4 Enchainement des contrats")]
        [Tooltip("delai avant le contrat suivant (s)")] public float nextContractDelay = 5f;
        [Tooltip("delai avant le tout premier contrat (s)")] public float firstContractDelay = 1.5f;
        [Tooltip("message de fin ({0} = recompense)")] public string completeMessage = "Contrat terminé — +{0} · kits de réparation rechargés";
        [Tooltip("message de debut ({0} = titre)")] public string newContractMessage = "Nouveau contrat : {0}";
    }
}
