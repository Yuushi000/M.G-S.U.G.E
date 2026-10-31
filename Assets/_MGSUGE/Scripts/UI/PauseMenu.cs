using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Menu pause (Echap / P, §16.3) : colonne de gauche avec Reprendre, Ameliorations, Commandes et les credits ; a droite
    /// l'onglet Ameliorations (§11.4) ou Commandes (4 groupes, §8). Le temps du monde est arrete, la souris liberee.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Menu pause")]
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("gestionnaire de jeu")] public GameManager game;
        [Tooltip("donnees de l'interface")] public UiData data;
        [Tooltip("contenu du menu (affiche dans l'etat Pause)")] public GameObject content;
        [Tooltip("credits sous les boutons")] public Text credits;
        [Header("Boutons de la colonne")]
        public Button resumeButton;
        public Button upgradesButton;
        public Button commandsButton;
        [Tooltip("trait sous l'onglet actif (Ameliorations, Commandes)")] public Graphic upgradesMark, commandsMark;
        [Header("Onglets")]
        public GameObject upgradesPanel;
        public GameObject commandsPanel;

        int lastVersion = -1;

        void Update()
        {
            bool on = game != null && game.State == GameState.Pause;
            if (content != null && content.activeSelf != on) content.SetActive(on);
            if (!on) { lastVersion = -1; return; }
            bool up = game.tab == GameManager.PauseTab.Upgrades;
            if (upgradesPanel != null && upgradesPanel.activeSelf != up) upgradesPanel.SetActive(up);
            if (commandsPanel != null && commandsPanel.activeSelf != !up) commandsPanel.SetActive(!up);
            if (data != null)
            {
                UiFx.SetColor(upgradesMark, up ? data.amber : UiFx.A(data.amber, 0f));
                UiFx.SetColor(commandsMark, !up ? data.amber : UiFx.A(data.amber, 0f));
            }
            if (game.player != null) UiFx.SetText(credits, MathX.Credits(game.player.progress.credits));
            if (lastVersion != game.MenuVersion)
            {
                bool first = lastVersion < 0;
                lastVersion = game.MenuVersion;
                var es = EventSystem.current;
                var b = up ? upgradesButton : commandsButton; if (b == null) b = resumeButton;
                if (es != null && first && b != null) es.SetSelectedGameObject(b.gameObject);
            }
        }
    }
}
