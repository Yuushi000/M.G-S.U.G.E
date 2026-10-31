using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Armure et energie (bas centre, §16.1) : grand chiffre d'AP, barre d'AP (blanche, rouge sous 25 %, avec trainee),
    /// compteur « Territoire N » avec mini-barre bleue (bouclier de territoire, jalon 8), barre d'EN orange (rouge clignotante
    /// en surchauffe), etat de l'EN ou sa valeur, etiquette de forme (+ « · Reincarnation N »).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Armure et energie")]
    public class HudArmorEnergy : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Header("Armure")]
        [Tooltip("grand chiffre d'AP")] public Text apValue;
        public UiBar apBar;
        [Header("Territoire (jalon 8)")]
        [Tooltip("bloc « Territoire N » + mini-barre")] public GameObject territory;
        public Text territoryText;
        public UiBar territoryBar;
        [Header("Energie")]
        public UiBar enBar;
        [Tooltip("etat de l'EN (« SURCHAUFFE », « BOOST »...) ou sa valeur")] public Text enState;
        [Tooltip("etiquette de forme (« Chassis », « Vaisseau · degats −10 % »...)")] public Text form;
        [Tooltip("periode du clignotement en surchauffe (s)")] [Range(0.1f, 2f)] public float overheatBlink = 0.4f;

        /// <summary>Bouclier de territoire (renseigne par l'Extension du territoire, jalon 8).</summary>
        public static float TerritoryShield, TerritoryShieldMax;

        void Update()
        {
            if (hud == null || hud.player == null || hud.data == null || !hud.Visible) return;
            var p = hud.player; var d = hud.data;
            float apMax = Mathf.Max(1f, p.S.armor);
            bool crit = p.AP < apMax * p.frame.criticalArmor;
            UiFx.SetText(apValue, UiFx.N(Mathf.Max(0f, p.AP)));
            UiFx.SetColor(apValue, crit ? d.threat : d.bone);
            if (apBar != null) { apBar.Set(p.AP / apMax); apBar.SetTrail(p.apTrail / apMax); apBar.SetColor(crit ? d.threat : d.bone); }

            bool terr = TerritoryShieldMax > 0f && TerritoryShield > 0f;
            if (territory != null) UiFx.SetActive(territory, terr);
            if (terr)
            {
                UiFx.SetText(territoryText, "Territoire " + UiFx.N(TerritoryShield));
                if (territoryBar != null) territoryBar.Set(TerritoryShield / TerritoryShieldMax);
            }

            if (enBar != null)
            {
                enBar.Set(p.EN / Mathf.Max(1f, p.S.energy));
                enBar.SetColor(p.Overheated ? (UiFx.Blink(overheatBlink) ? d.threat : UiFx.A(d.threat, 0.45f)) : d.amber);
            }
            string st = p.EnergyState;
            UiFx.SetText(enState, string.IsNullOrEmpty(st) ? "EN " + UiFx.N(p.EN) : st);
            UiFx.SetColor(enState, p.Overheated ? d.threat : d.amber);
            string f = p.FormLabel;
            if (p.progress.reincarnation > 0) f += " · Réincarnation " + p.progress.reincarnation;
            UiFx.SetText(form, f);
            UiFx.SetColor(form, p.progress.reincarnation > 0 ? d.gold : p.IsShip ? d.ice : d.bone);
        }
    }
}
