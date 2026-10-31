using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Racine de l'interface en jeu (objet UI/HUD). Regroupe les references partagees par les elements du HUD et de la
    /// surimpression et les affiche selon l'etat du jeu : caches sur l'ecran titre et l'ecran « Chassis detruit ».
    /// </summary>
    [DefaultExecutionOrder(50)]
    [AddComponentMenu("MGSUGE/UI/HUD (racine)")]
    public class HudController : MonoBehaviour
    {
        public static HudController I { get; private set; }
        [Header("Donnees")]
        [Tooltip("couleurs, polices, durees (Data/MGSUGE_Interface)")] public UiData data;
        [Tooltip("donnees du monde (noms des secteurs, taille de la carte)")] public WorldData world;
        [Header("Joueur")]
        public PlayerController player;
        public WeaponController weapons;
        public ProbeController probes;
        public LockOn lockOn;
        [Tooltip("camera de jeu (projection des marqueurs)")] public Camera cam;
        [Header("Groupes affiches selon l'etat")]
        [Tooltip("HUD (bords de l'ecran)")] public CanvasGroup hudGroup;
        [Tooltip("surimpression (reticule, marqueurs, chiffres de degats)")] public CanvasGroup overlayGroup;
        [Tooltip("opacite du HUD pendant le menu pause")] [Range(0f, 1f)] public float pauseOpacity = 0f;
        [Tooltip("opacite du HUD hors pause (le Rayon Arcane la baisse a 20 % au jalon 8)")] [Range(0f, 1f)] public float opacity = 1f;

        void Awake()
        {
            I = this;
            if (data != null) UiFx.ReduceMotion = data.reduceMotion;
            if (cam == null && player != null && player.cam != null) cam = player.cam.cam;
        }
        void OnValidate() { if (data != null) UiFx.ReduceMotion = data.reduceMotion; }

        void LateUpdate()
        {
            if (data != null) UiFx.ReduceMotion = data.reduceMotion;
            var gm = GameManager.I;
            GameState s = gm != null ? gm.State : GameState.Play;
            bool show = s == GameState.Play || s == GameState.Pause;
            float a = s == GameState.Pause ? pauseOpacity : opacity;
            if (hudGroup != null) { hudGroup.alpha = show ? a : 0f; }
            if (overlayGroup != null) { overlayGroup.alpha = s == GameState.Play ? opacity : 0f; }
        }

        public bool Visible => GameManager.I == null || GameManager.I.State == GameState.Play || GameManager.I.State == GameState.Pause;
    }
}
