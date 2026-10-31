using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Une ligne du menu Ameliorations (prefab MGSUGE_UI_LigneAmelioration, §11.4) : nom et description a gauche, jauge a
    /// 5 cases (remplies en orange), valeur actuelle « → » valeur suivante, bouton d'achat avec le prix (« Maximum » au
    /// niveau 5, desactive si les credits manquent). En mode DIVIN : selecteur ◀ [niveau] ▶ gratuit.
    /// Les lignes speciales ont une bordure coloree (Rayon Arcane rose, Territoire violet, Sonde jaune, Reincarnation or).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Ligne d'amelioration")]
    public class UpgradeRow : MonoBehaviour
    {
        [Header("Ligne")]
        [Tooltip("module ou achat unique")] public UpgradeKind kind;
        [Tooltip("module (si Kind = Module)")] public UpgradeModule module;
        [Header("Elements")]
        public Text title;
        public Text description;
        [Tooltip("5 cases de niveau")] public Image[] pips = new Image[5];
        [Tooltip("valeur actuelle → suivante")] public Text value;
        public Button buyButton;
        public Text buyLabel;
        [Tooltip("bordure coloree des lignes speciales")] public Image border;
        [Tooltip("selecteur du mode DIVIN")] public GameObject divineSelector;
        public Button prevButton, nextButton;
        public Text divineLevel;
        [Header("Textes")]
        public string maxText = "Maximum";
        public string ownedText = "Acquis";
        public string lockedText = "Verrouillé";

        GameManager game; UiData data;

        public void Bind(GameManager g, UiData d) { game = g; data = d; }
        public void Buy()
        {
            if (game == null) game = GameManager.I;
            if (game == null) return;
            if (kind == UpgradeKind.Module) game.Buy(module); else game.BuySpecial(kind);
        }
        public void Prev() { if (game == null) game = GameManager.I; game?.DivineStep(kind, module, -1); }
        public void Next() { if (game == null) game = GameManager.I; game?.DivineStep(kind, module, +1); }

        Color Accent()
        {
            switch (kind)
            {
                case UpgradeKind.RayonArcane: return data.rose;
                case UpgradeKind.Territoire: case UpgradeKind.TerritoireII: return data.divineViolet;
                case UpgradeKind.Sonde: return data.probe;
                case UpgradeKind.Reincarnation: return data.gold;
                default: return data.amber;
            }
        }

        public void Refresh()
        {
            if (game == null) game = GameManager.I;
            if (game == null || game.player == null || data == null) return;
            var p = game.player; var pr = p.progress; var pd = p.progression;
            bool divine = pr.divine;
            int level = 0, max = 0; long cost = 0; bool owned = false, locked = false;
            string val = "";
            switch (kind)
            {
                case UpgradeKind.Module:
                    level = pr.Level(module); max = pd.maxLevel; cost = level < max ? pd.levelCosts[level] : 0;
                    val = ModuleValue(p, level, max); break;
                case UpgradeKind.Sonde:
                    level = pr.probes; max = p.weaponData.probeMax; cost = p.weaponData.probeCost;
                    val = $"{level} / {max}"; break;
                case UpgradeKind.RayonArcane: owned = pr.arcane; level = owned ? 1 : 0; max = 1; cost = pd.arcaneCost; break;
                case UpgradeKind.Territoire: owned = pr.domain; level = owned ? 1 : 0; max = 1; cost = pd.domainCost; break;
                case UpgradeKind.TerritoireII: owned = pr.domain2; level = owned ? 1 : 0; max = 1; cost = pd.domain2Cost; locked = !game.Domain2Unlocked && !owned; break;
                case UpgradeKind.Reincarnation:
                    level = pr.reincarnation; max = divine ? pd.divineMaxReincarnation : int.MaxValue; cost = pd.reincarnationCost;
                    val = level > 0 ? $"Niveau {level} · ×{UiFx.N(Mathf.Pow(pd.reincarnationFactor, level))}" + $" → ×{UiFx.N(Mathf.Pow(pd.reincarnationFactor, level + 1))}" : $"×1 → ×{UiFx.N(pd.reincarnationFactor)}";
                    break;
            }
            Color acc = Accent();
            if (border != null) { UiFx.SetActive(border, kind != UpgradeKind.Module); UiFx.SetColor(border, locked ? UiFx.A(acc, 0.3f) : acc); }
            UiFx.SetColor(title, locked ? UiFx.A(data.bone, 0.35f) : data.bone);
            UiFx.SetColor(description, UiFx.A(data.bone, locked ? 0.25f : data.boneDim));
            bool showPips = kind == UpgradeKind.Module || kind == UpgradeKind.Sonde;
            if (pips != null)
                for (int i = 0; i < pips.Length; i++)
                {
                    if (pips[i] == null) continue;
                    UiFx.SetActive(pips[i], showPips);
                    if (showPips) UiFx.SetColor(pips[i], i < level ? data.amber : UiFx.A(data.bone, 0.12f));
                }
            if (kind == UpgradeKind.RayonArcane || kind == UpgradeKind.Territoire || kind == UpgradeKind.TerritoireII) val = owned ? ownedText : locked ? lockedText : "";
            UiFx.SetText(value, val);
            UiFx.SetColor(value, owned ? acc : data.BoneDim);

            if (buyButton != null) UiFx.SetActive(buyButton, !divine);
            if (divineSelector != null) UiFx.SetActive(divineSelector, divine);
            if (divine)
            {
                string lv = max == 1 ? (level > 0 ? "Oui" : "Non") : level.ToString();
                UiFx.SetText(divineLevel, lv);
                UiFx.SetColor(divineLevel, data.gold);
                if (prevButton != null) prevButton.interactable = level > 0;
                if (nextButton != null) nextButton.interactable = level < max;
                return;
            }
            bool maxed = kind == UpgradeKind.Module || kind == UpgradeKind.Sonde ? level >= max : owned;
            string label = maxed ? (kind == UpgradeKind.Module || kind == UpgradeKind.Sonde ? maxText : ownedText) : locked ? lockedText : MathX.Credits(cost);
            UiFx.SetText(buyLabel, label);
            if (buyButton != null) buyButton.interactable = !maxed && !locked && pr.credits >= cost;
        }

        /// <summary>Valeur actuelle et du niveau suivant (« → ») pour chaque module (§11.2).</summary>
        string ModuleValue(PlayerController p, int level, int max)
        {
            var now = p.S;
            PlayerStats next = null;
            if (level < max)
            {
                var copy = new PlayerProgress { levels = (int[])p.progress.levels.Clone(), reincarnation = p.progress.reincarnation, probes = p.progress.probes };
                copy.levels[(int)module] = level + 1;
                next = PlayerStats.Compute(p.frame, p.weaponData, p.progression, copy);
            }
            string F(PlayerStats s)
            {
                switch (module)
                {
                    case UpgradeModule.Propulseurs: return UiFx.N(s.groundBoost * 3.6f) + " km/h";
                    case UpgradeModule.ReservoirEN: return UiFx.N(s.energy) + " EN";
                    case UpgradeModule.Generateur: return UiFx.N(s.regen) + " EN/s · " + UiFx.D2(s.overheat) + " s";
                    case UpgradeModule.QuickBoostSaut: return UiFx.N(s.quickSpeed * 3.6f) + " km/h · " + UiFx.N(s.quickCost) + " EN";
                    case UpgradeModule.Blindage: return UiFx.N(s.armor) + " AP";
                    case UpgradeModule.Fusil: return UiFx.N(s.rifleDamage) + " · " + UiFx.D2(s.rifleInterval) + " s";
                    case UpgradeModule.LanceMissiles: return s.missileCount + " missiles · " + UiFx.N(s.missileDamage);
                    case UpgradeModule.Salve: return s.salvoCount + " missiles";
                    case UpgradeModule.Fragmentation: return s.fragMinis + " mini-missiles";
                    case UpgradeModule.Lame: return UiFx.N(s.bladeDamage) + " · " + UiFx.N(s.bladeRange) + " m";
                    case UpgradeModule.FormeVaisseau: return UiFx.N(s.shipMax * 3.6f) + " km/h";
                }
                return "";
            }
            return next != null ? F(now) + " → " + F(next) : F(now);
        }
    }
}
