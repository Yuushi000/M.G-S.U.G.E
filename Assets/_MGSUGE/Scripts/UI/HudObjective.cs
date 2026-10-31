using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Objectif (haut gauche, §16.1) : « Contrat N / 7 » (+ « · menace N »), titre du contrat en grandes majuscules,
    /// description, compteur « tues / total » et barre orange fine.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Objectif")]
    public class HudObjective : MonoBehaviour
    {
        [Tooltip("racine du HUD (references partagees)")] public HudController hud;
        [Header("Textes")]
        public Text label;
        public Text title;
        public Text description;
        public Text counter;
        [Tooltip("barre orange fine (tues / total)")] public UiBar bar;
        [Header("Formats")]
        [Tooltip("{0} = numero, {1} = nombre de contrats")] public string labelFormat = "Contrat {0} / {1}";
        [Tooltip("ajoute apres l'etiquette quand la menace est > 0 ; {0} = niveau")] public string threatFormat = " · menace {0}";
        [Tooltip("compteur pendant l'attente du contrat suivant ; {0} tues, {1} total, {2} secondes")] public string waitingFormat = "Terminé · {0} / {1} · contrat suivant dans {2} s";
        [Tooltip("texte quand la scene n'a aucun contrat")] public string noContractLabel = "Terrain d'essai";
        [Tooltip("titre quand aucun contrat n'est en cours")] public string idleTitle = "En attente";

        void Update()
        {
            if (hud == null || !hud.Visible) return;
            var cm = ContractManager.I;
            if (cm == null || cm.Count == 0)
            {
                UiFx.SetText(label, UiFx.Upper(noContractLabel));
                int alive = 0; foreach (var t in Targetable.All) if (t.Alive) alive++;
                UiFx.SetText(title, ""); UiFx.SetText(description, ""); UiFx.SetText(counter, $"Cibles et ennemis actifs : {alive}");
                if (bar != null) bar.Set(0f);
                return;
            }
            var c = cm.Current;
            string lab = string.Format(labelFormat, Mathf.Max(1, cm.Index + 1), cm.Count);
            if (Threat.Level > 0) lab += string.Format(threatFormat, Threat.Level);
            UiFx.SetText(label, UiFx.Upper(lab));
            if (c == null)
            {
                UiFx.SetText(title, UiFx.Upper(idleTitle)); UiFx.SetText(description, ""); UiFx.SetText(counter, "");
                if (bar != null) bar.Set(0f);
                return;
            }
            UiFx.SetText(title, UiFx.Upper(c.Title));
            UiFx.SetText(description, c.data != null ? c.data.description : "");
            int total = c.Total, killed = c.Killed;
            UiFx.SetText(counter, cm.Waiting ? string.Format(waitingFormat, killed, total, Mathf.CeilToInt(Mathf.Max(0f, cm.WaitLeft))) : $"{killed} / {total}");
            if (bar != null) bar.Set(total > 0 ? killed / (float)total : 0f);
        }
    }
}
