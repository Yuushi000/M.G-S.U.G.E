using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Reticule de visee (surimpression, §16.2) : 4 traits et un point, qui s'ecartent avec la dispersion du tir.</summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Reticule")]
    public class OverlayReticle : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("traits : haut, bas, gauche, droite")] public RectTransform up, down, left, right;
        [Tooltip("point central")] public Graphic dot;
        [Tooltip("ecart de base entre le centre et les traits (px a 1080p)")] [Range(0f, 60f)] public float baseGap = 14f;
        [Tooltip("ecart ajoute pendant le tir continu (px)")] [Range(0f, 60f)] public float fireBloom = 10f;
        [Tooltip("multiplicateur de la dispersion reelle du fusil (±0,006 rad) convertie en pixels")] [Range(0f, 50f)] public float spreadScale = 4f;
        [Tooltip("vitesse d'ouverture / fermeture (1/s)")] [Range(1f, 40f)] public float rate = 12f;
        [Tooltip("opacite")] [Range(0f, 1f)] public float alpha = 0.8f;
        float bloom;

        void Update()
        {
            if (hud == null || hud.player == null || hud.cam == null || hud.data == null) return;
            var w = hud.weapons;
            bool firing = w != null && w.Cooldown01("rifle") > 0f;
            bloom = Mathf.Lerp(bloom, firing ? 1f : 0f, MathX.Damp(rate, Time.unscaledDeltaTime));
            float spread = w != null && w.data != null ? w.data.rifleSpread : 0.006f;
            float canvasH = ((RectTransform)transform).rect.height;
            float spreadPx = Mathf.Tan(spread) / Mathf.Tan(hud.cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * canvasH * 0.5f * spreadScale;
            float g = baseGap + spreadPx + bloom * fireBloom;
            if (up != null) up.anchoredPosition = new Vector2(0f, g);
            if (down != null) down.anchoredPosition = new Vector2(0f, -g);
            if (left != null) left.anchoredPosition = new Vector2(-g, 0f);
            if (right != null) right.anchoredPosition = new Vector2(g, 0f);
            Color c = UiFx.A(hud.data.bone, alpha);
            foreach (var r in new[] { up, down, left, right }) if (r != null) { var gr = r.GetComponent<Graphic>(); if (gr != null) UiFx.SetColor(gr, c); }
            UiFx.SetColor(dot, c);
        }
    }
}
