using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    public enum WeaponLine { Fusil, Missiles, Salve, Fragmentation, Lame, Kit, RayonArcane, Territoire, Sondes }

    /// <summary>Une ligne du panneau Armes (prefab MGSUGE_UI_LigneArme) : touche, nom, compteur, barre de recharge fine, eclat « pret ».</summary>
    [AddComponentMenu("MGSUGE/UI/HUD Ligne d'arme")]
    public class HudWeaponRow : MonoBehaviour
    {
        [Tooltip("arme ou capacite affichee")] public WeaponLine line;
        [Tooltip("couleur de la ligne (rose : armes et Rayon Arcane, violet : territoire, jaune : sondes)")] public Color accent = new Color32(0xFF, 0x4F, 0xD8, 0xFF);
        public Text key;
        public Text label;
        public Text count;
        [Tooltip("barre de recharge fine")] public UiBar reload;
        [Tooltip("fond de la barre de recharge")] public Image reloadBack;
        [Tooltip("eclat blanc-rose quand l'arme est de nouveau prete")] public Image flash;

        float lastCd; float flashTime = -9f;

        /// <summary>Met a jour la ligne. cd = part de recharge restante (0 = pret).</summary>
        public void Show(UiData d, string countText, float cd, bool owned, Color? countColor = null)
        {
            Color baseC = owned ? accent : UiFx.A(accent, 0.3f);
            UiFx.SetColor(key, owned ? d.BoneDim : d.Faint(d.bone));
            UiFx.SetColor(label, owned ? d.bone : UiFx.A(d.bone, 0.3f));
            UiFx.SetText(count, countText); UiFx.SetColor(count, countColor ?? baseC);
            if (reloadBack != null) UiFx.SetColor(reloadBack, UiFx.A(accent, owned ? 0.18f : 0.08f));
            if (reload != null) { reload.SetColor(baseC); reload.Set(cd > 0f ? 1f - cd : 0f); }
            if (lastCd > 0f && cd <= 0f && owned) flashTime = UiFx.Now;
            lastCd = cd;
            if (flash != null)
            {
                float k = 1f - (UiFx.Now - flashTime) / Mathf.Max(0.01f, d.readyFlashTime);
                bool on = k > 0f && !UiFx.ReduceMotion;
                UiFx.SetActive(flash, on);
                if (on) UiFx.SetAlpha(flash, k * 0.55f);
            }
        }
    }
}
