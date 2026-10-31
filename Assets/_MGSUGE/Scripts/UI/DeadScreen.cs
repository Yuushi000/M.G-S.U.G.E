using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Ecran « Chassis detruit » (§16.3) : designation rouge « Signal perdu », grand titre, texte, bouton « Redeployer ».</summary>
    [AddComponentMenu("MGSUGE/UI/Ecran Chassis detruit")]
    public class DeadScreen : MonoBehaviour
    {
        [Tooltip("gestionnaire de jeu")] public GameManager game;
        [Tooltip("contenu de l'ecran (affiche dans l'etat Dead)")] public GameObject content;
        public Button redeployButton;
        bool wasOn;

        public void Redeploy() { if (game != null) game.Redeploy(); }

        void Update()
        {
            bool on = game != null && game.State == GameState.Dead;
            if (content != null && content.activeSelf != on) content.SetActive(on);
            if (on && !wasOn && redeployButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(redeployButton.gameObject);
            wasOn = on;
        }
    }
}
