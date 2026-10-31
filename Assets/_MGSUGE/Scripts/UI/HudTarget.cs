using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Cible verrouillee (haut centre, sous la barre de boss si presente, §16.1) : nom en rouge, barre de vie, jauge d'impact,
    /// distance (+ « · surcharge »).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Cible verrouillee")]
    public class HudTarget : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("contenu affiche / cache")] public GameObject content;
        public Text targetName;
        public UiBar hp;
        public UiBar impact;
        public Text distance;
        [Tooltip("barre de boss : quand elle est visible, ce bloc descend")] public HudBossBar bossBar;
        [Tooltip("position verticale sans boss (px a 1080p, depuis le haut)")] public float normalY = -70f;
        [Tooltip("position verticale sous la barre de boss")] public float belowBossY = -150f;
        [Tooltip("ajoute a la distance en surcharge")] public string overloadSuffix = " · surcharge";
        RectTransform rt;

        void Update()
        {
            if (hud == null || hud.data == null || !hud.Visible) return;
            var t = hud.lockOn != null ? hud.lockOn.Target : null;
            bool on = t != null && t.Alive && (bossBar == null || bossBar.Current != t);
            if (content != null) UiFx.SetActive(content, on);
            if (!on) return;
            var d = hud.data;
            if (rt == null) rt = (RectTransform)transform;
            bool boss = bossBar != null && bossBar.content != null && bossBar.content.activeSelf;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, boss ? belowBossY : normalY);
            UiFx.SetText(targetName, t.displayName);
            if (hp != null) hp.Set(t.HP / Mathf.Max(1f, t.maxHP));
            if (impact != null)
            {
                UiFx.SetActive(impact, t.impactMax > 0f);
                impact.Set(t.impactMax > 0f ? t.impact / t.impactMax : 0f);
                impact.SetColor(t.Overloaded ? (UiFx.Blink(0.3f) ? d.threat : UiFx.A(d.threat, 0.35f)) : d.amber);
            }
            float dist = hud.player != null ? Vector3.Distance(hud.player.Center, t.Position) : 0f;
            UiFx.SetText(distance, UiFx.N(dist) + " m" + (t.Overloaded ? overloadSuffix : ""));
        }
    }
}
