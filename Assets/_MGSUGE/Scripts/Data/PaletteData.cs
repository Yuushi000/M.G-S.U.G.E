using UnityEngine;

namespace MGSUGE
{
    /// <summary>Palette de couleurs par role (Armor, Frame, Accent, Visor, Glow, Flame, Blade) pour les variantes de chassis.</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Palette de chassis", fileName = "MGSUGE_Palette")]
    public class PaletteData : ScriptableObject
    {
        [Tooltip("vide = couleurs d'origine du modele (joueur)")] public bool useOriginal = true;
        public Color armor = new Color32(0xD8, 0xD0, 0xBC, 0xFF);
        public Color frame = new Color32(0x2A, 0x2E, 0x39, 0xFF);
        public Color accent = new Color32(0xFF, 0x9A, 0x2E, 0xFF);
        public Color visor = new Color32(0xFF, 0xB2, 0x4A, 0xFF);
        public Color glow = new Color32(0x8F, 0xE3, 0xFF, 0xFF);
        public Color flame = new Color32(0xFF, 0xC2, 0x7A, 0xFF);
        public Color blade = new Color32(0xFF, 0x4F, 0xD8, 0xFF);
        public Color Get(string role)
        {
            switch (role)
            {
                case "Armor": return armor; case "Frame": return frame; case "Accent": return accent;
                case "Visor": return visor; case "Glow": return glow; case "Flame": return flame; default: return blade;
            }
        }
    }
}
