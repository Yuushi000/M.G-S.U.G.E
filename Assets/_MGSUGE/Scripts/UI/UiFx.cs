using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Animation "+N C" sous le compteur de credits (§11.1).</summary>
    public static class HudCredits
    {
        public static long LastGain; public static float LastTime = -9f;
        public static void Gain(long c) { LastGain = c; LastTime = Time.unscaledTime; }
    }

    /// <summary>Outils communs de l'interface : formats francais, alpha, temps non affecte par la pause, « reduire les animations ».</summary>
    public static class UiFx
    {
        static readonly CultureInfo FR = CultureInfo.GetCultureInfo("fr-FR");
        /// <summary>Option « reduire les animations » (§16.3), recopiee depuis l'asset Interface par le HudController.</summary>
        public static bool ReduceMotion;
        public static float Now => Time.unscaledTime;

        /// <summary>Nombre entier avec separateur de milliers francais (espace fine insecable remplacee par une espace normale).</summary>
        public static string N(float v) => Fix(System.Math.Round((double)v).ToString("N0", FR)); // double : pas de depassement a forte reincarnation
        public static string N(long v) => Fix(v.ToString("N0", FR));
        /// <summary>Nombre a une decimale avec virgule (1,6).</summary>
        public static string D1(float v) => Fix(v.ToString("0.#", FR));
        public static string D2(float v) => Fix(v.ToString("0.0##", FR));
        static string Fix(string s) => s.Replace(' ', ' ').Replace(' ', ' ');
        public static string Upper(string s) => string.IsNullOrEmpty(s) ? "" : s.ToUpper(FR);
        /// <summary>« 1 M C », « 500 k C » (lignes du HUD des capacites non acquises).</summary>
        public static string ShortCredits(long c)
        {
            if (c >= 1000000 && c % 100000 == 0) return D1(c / 1000000f) + " M C";
            if (c >= 1000 && c % 1000 == 0) return N(c / 1000) + " k C";
            return MathX.Credits(c);
        }
        /// <summary>« 2 min 30 s » / « 45 s ».</summary>
        public static string Duration(float s)
        {
            int t = Mathf.CeilToInt(Mathf.Max(0f, s));
            return t >= 60 ? $"{t / 60} min {t % 60:00} s" : $"{t} s";
        }

        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static void SetAlpha(Graphic g, float a) { if (g == null) return; var c = g.color; c.a = a; g.color = c; }
        public static void SetText(Text t, string s) { if (t != null && t.text != s) t.text = s; }
        public static void SetColor(Graphic g, Color c) { if (g != null && g.color != c) g.color = c; }
        public static void SetActive(Component c, bool on) { if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }
        public static void SetActive(GameObject g, bool on) { if (g != null && g.activeSelf != on) g.SetActive(on); }
        public static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - t); }
        /// <summary>Clignotement on/off (periode en s).</summary>
        public static bool Blink(float period) => Mathf.Repeat(Time.unscaledTime, period) < period * 0.5f;

        /// <summary>Position ecran (pixels reels) d'un point du monde ; faux s'il est derriere la camera.</summary>
        public static bool ToScreen(Camera cam, Vector3 world, out Vector2 screen)
        {
            Vector3 sp = cam.WorldToScreenPoint(world); screen = sp;
            return sp.z > 0f;
        }
        /// <summary>Position ecran (pixels reels) -> position ancree au centre d'un canvas plein ecran (unites du canvas).</summary>
        public static Vector2 ScreenToCanvas(Canvas canvas, Vector2 screen)
        {
            float k = canvas != null ? Mathf.Max(1e-4f, canvas.scaleFactor) : 1f;
            return (screen - new Vector2(Screen.width, Screen.height) * 0.5f) / k;
        }
        /// <summary>Convertit une position ecran (pixels) en position locale d'un RectTransform plein ecran du canvas.</summary>
        public static Vector2 ScreenToLocal(RectTransform parent, Vector2 screen, Canvas canvas)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out Vector2 local);
            return local;
        }
    }
}
