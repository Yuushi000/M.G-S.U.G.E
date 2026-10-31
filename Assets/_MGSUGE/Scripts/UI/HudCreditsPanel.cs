using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Haut droite (§16.1, §11.1) : compteur « N C » qui glisse vers la nouvelle valeur, « +N C » qui s'efface en 1,25 s,
    /// nom du secteur et mention « Mode DIVIN » en or.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Credits et secteur")]
    public class HudCreditsPanel : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        public Text credits;
        [Tooltip("« +N C » sous le compteur")] public Text gain;
        public Text sector;
        [Tooltip("mention « Mode DIVIN »")] public Text divine;
        [Tooltip("texte de la mention DIVIN")] public string divineText = "Mode DIVIN";
        float shown = -1f;

        void Update()
        {
            if (hud == null || hud.player == null || hud.data == null || !hud.Visible) return;
            var d = hud.data; var pr = hud.player.progress;
            long cr = pr.credits;
            if (shown < 0f || UiFx.ReduceMotion) shown = cr;
            shown = Mathf.Lerp(shown, cr, MathX.Damp(d.creditCounterRate, Time.unscaledDeltaTime));
            if (Mathf.Abs(shown - cr) < 1f) shown = cr;
            UiFx.SetText(credits, MathX.Credits((long)Mathf.Round(shown)));
            float age = UiFx.Now - HudCredits.LastTime;
            if (gain != null)
            {
                bool on = age < d.creditGainDuration && HudCredits.LastGain != 0;
                UiFx.SetActive(gain, on);
                if (on)
                {
                    UiFx.SetText(gain, (HudCredits.LastGain > 0 ? "+" : "−") + MathX.Credits(System.Math.Abs(HudCredits.LastGain)));
                    UiFx.SetColor(gain, UiFx.A(HudCredits.LastGain > 0 ? d.ice : d.amber, 1f - age / d.creditGainDuration));
                }
            }
            if (sector != null && hud.world != null)
            {
                Vector3 p = hud.player.Center;
                UiFx.SetText(sector, hud.world.SectorName(p, TerrainSystem.Ground(p.x, p.z)));
            }
            if (divine != null) { UiFx.SetActive(divine, pr.divine); if (pr.divine) UiFx.SetText(divine, divineText); }
        }
    }
}
