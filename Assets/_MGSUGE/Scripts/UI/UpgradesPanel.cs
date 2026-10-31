using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Onglet Ameliorations du menu pause (§11.4) : titre, texte d'introduction, 11 modules et les lignes speciales
    /// (Sonde laser, Rayon Arcane, Extension du territoire I et II, Reincarnation). En mode DIVIN, texte dedie et fleches.
    /// Les lignes sont des copies du prefab MGSUGE_UI_LigneAmelioration, enfants de cet objet.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Onglet Ameliorations")]
    public class UpgradesPanel : MonoBehaviour
    {
        [Tooltip("gestionnaire de jeu (achats)")] public GameManager game;
        [Tooltip("donnees de l'interface")] public UiData data;
        [Tooltip("lignes du menu")] public UpgradeRow[] rows;
        [Tooltip("texte d'introduction du mode normal")] public GameObject normalIntro;
        [Tooltip("texte « Mode DIVIN : tout est debloque... »")] public GameObject divineIntro;

        void OnEnable() { if (rows != null) foreach (var r in rows) if (r != null) r.Bind(game, data); }

        void Update()
        {
            if (game == null || game.player == null) return;
            bool divine = game.player.progress.divine;
            if (normalIntro != null && normalIntro.activeSelf == divine) normalIntro.SetActive(!divine);
            if (divineIntro != null && divineIntro.activeSelf != divine) divineIntro.SetActive(divine);
            if (rows != null) foreach (var r in rows) if (r != null) { r.Bind(game, data); r.Refresh(); }
        }
    }
}
