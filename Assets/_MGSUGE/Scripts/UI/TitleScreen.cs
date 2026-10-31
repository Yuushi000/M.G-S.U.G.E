using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGSUGE
{
    public enum TitleLayout { UneLigne, DeuxLignes, PleinEtContour }

    /// <summary>
    /// Ecran titre (§16.3) : degrade sombre a gauche laissant voir le monde a droite, designation « Chassis transformable
    /// VSP-07 « Vesper » » en orange mono, nom du jeu en tres grand (3 mises en page proposees, §4.6), texte de presentation,
    /// boutons « Mode normal » (orange) et « Mode DIVIN » (or / violet), note, rappel des touches. Entree = mode normal.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Ecran titre")]
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("gestionnaire de jeu (lance la partie)")] public GameManager game;
        [Tooltip("donnees de l'interface")] public UiData data;
        [Tooltip("contenu de l'ecran (affiche dans l'etat Title)")] public GameObject content;
        [Header("Nom du jeu (§4.6 : trois propositions)")]
        [Tooltip("mise en page affichee")] public TitleLayout layout = TitleLayout.DeuxLignes;
        [Tooltip("objets des trois mises en page, dans l'ordre de l'enumeration")] public GameObject[] layouts = new GameObject[3];
        [Tooltip("contours qui scintillent en rose")] public Outline[] shimmer;
        [Tooltip("vitesse du scintillement (rad/s)")] [Range(0f, 20f)] public float shimmerSpeed = 3.2f;
        [Header("Boutons")]
        public Button normalButton;
        public Button divineButton;
        [Header("Messages")]
        [Tooltip("« Ce jeu se joue au clavier et a la souris. » (affiche sur les appareils tactiles)")] public GameObject touchWarning;

        int lastVersion = -1;
        /// <summary>Ecran titre present dans la scene (il gere alors la touche Entree).</summary>
        public static TitleScreen I { get; private set; }
        void OnEnable() { I = this; }
        void OnDisable() { if (I == this) I = null; }

#if UNITY_EDITOR
        void OnValidate() { UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyLayout(); }; }
#endif
        void Awake() { ApplyLayout(); }

        public void ApplyLayout()
        {
            if (layouts == null) return;
            for (int i = 0; i < layouts.Length; i++) if (layouts[i] != null && layouts[i].activeSelf != (i == (int)layout)) layouts[i].SetActive(i == (int)layout);
        }

        public void StartNormal() { if (game != null && game.State == GameState.Title) game.StartGame(false); }
        public void StartDivine() { if (game != null && game.State == GameState.Title) game.StartGame(true); }

        void Update()
        {
            bool on = game != null && game.State == GameState.Title;
            if (content != null && content.activeSelf != on) content.SetActive(on);
            if (!on) { lastVersion = -1; return; }
            if (lastVersion != game.MenuVersion)
            {
                lastVersion = game.MenuVersion;
                if (normalButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(normalButton.gameObject);
            }
            // Entree : lance le mode normal (ou le bouton DIVIN s'il a le focus)
            var inp = game.player != null ? game.player.input : null;
            if (inp != null && inp.PressedRaw(Cmd.Start))
            {
                var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (divineButton != null && sel == divineButton.gameObject) StartDivine(); else StartNormal();
                return;
            }
            if (touchWarning != null && touchWarning.activeSelf != Application.isMobilePlatform) touchWarning.SetActive(Application.isMobilePlatform);
            if (shimmer != null && data != null)
            {
                float k = UiFx.ReduceMotion ? 0.85f : 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * shimmerSpeed);
                foreach (var o in shimmer) if (o != null) o.effectColor = UiFx.A(data.rose, k);
            }
        }
    }
}
