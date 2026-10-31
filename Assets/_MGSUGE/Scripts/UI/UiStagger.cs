using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Apparition echelonnee des lignes d'un menu a chaque ouverture (§16.3) : fondu + leger glissement, ligne apres ligne.
    /// Desactivee par l'option « reduire les animations ». Fonctionne pendant la pause (temps non affecte).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Apparition echelonnee")]
    public class UiStagger : MonoBehaviour
    {
        [Tooltip("donnees de l'interface (pas et duree)")] public UiData data;
        [Tooltip("lignes dans l'ordre d'apparition (chacune avec un CanvasGroup)")] public CanvasGroup[] items;
        [Tooltip("glissement horizontal de depart (px a 1080p)")] [Range(-100f, 100f)] public float slide = -18f;
        [Tooltip("duree maximale de l'echelonnement complet (s) : les longues listes apparaissent plus vite")] [Range(0.1f, 3f)] public float maxTotal = 0.6f;
        Vector2[] home; float t0;

        void Awake()
        {
            if (items == null) return;
            home = new Vector2[items.Length];
            for (int i = 0; i < items.Length; i++) if (items[i] != null) home[i] = ((RectTransform)items[i].transform).anchoredPosition;
        }
        void OnEnable() { t0 = Time.unscaledTime; Apply(); }
        void Update() { Apply(); }

        void Apply()
        {
            if (items == null || home == null) return;
            float step = data != null ? data.staggerStep : 0.035f, dur = data != null ? data.staggerTime : 0.22f;
            if (items.Length > 1) step = Mathf.Min(step, maxTotal / (items.Length - 1));
            float t = Time.unscaledTime - t0;
            for (int i = 0; i < items.Length; i++)
            {
                var g = items[i]; if (g == null) continue;
                float k = UiFx.ReduceMotion ? 1f : UiFx.EaseOut((t - i * step) / Mathf.Max(0.01f, dur));
                g.alpha = k;
                ((RectTransform)g.transform).anchoredPosition = home[i] + new Vector2((1f - k) * slide, 0f);
            }
        }
    }
}
