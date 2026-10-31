using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Vignette plein ecran (§16.1) : bords rouges lors des coups recus et sous 25 % d'AP (pulsation).</summary>
    [AddComponentMenu("MGSUGE/UI/HUD Vignette")]
    public class HudVignette : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("image plein ecran (degrade rouge sur les bords)")] public Image image;
        [Tooltip("duree du flash apres un coup (s)")] [Range(0.05f, 2f)] public float hitTime = 0.5f;
        [Tooltip("opacite maximale")] [Range(0f, 1f)] public float maxAlpha = 0.55f;
        [Tooltip("opacite de base sous 25 % d'AP")] [Range(0f, 1f)] public float criticalAlpha = 0.3f;
        [Tooltip("amplitude de la pulsation sous 25 % d'AP")] [Range(0f, 1f)] public float criticalPulse = 0.2f;
        [Tooltip("vitesse de la pulsation (rad/s)")] [Range(0f, 20f)] public float pulseSpeed = 6f;

        void Update()
        {
            if (hud == null || hud.player == null || image == null) return;
            var p = hud.player;
            float a = 0f;
            foreach (var h in p.recentHits) a = Mathf.Max(a, 1f - (Time.time - h.Key) / hitTime);
            bool crit = p.AP > 0f && p.AP < p.S.armor * p.frame.criticalArmor;
            if (crit) a = Mathf.Max(a, criticalAlpha + criticalPulse * (UiFx.ReduceMotion ? 0f : Mathf.Sin(Time.unscaledTime * pulseSpeed)));
            if (!hud.Visible) a = 0f;
            image.enabled = a > 0.001f;
            if (image.enabled) UiFx.SetAlpha(image, Mathf.Clamp01(a) * maxAlpha);
        }
    }
}
