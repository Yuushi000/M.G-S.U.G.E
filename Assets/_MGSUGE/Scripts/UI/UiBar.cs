using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Barre fine du HUD : un fond et un remplissage (enfant etire de 0 a k par ses ancres), plus une trainee facultative
    /// qui rattrape lentement la valeur (barre d'AP, §7.6). Les couleurs se reglent sur les Image enfants.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Barre")]
    public class UiBar : MonoBehaviour
    {
        [Tooltip("remplissage (RectTransform enfant, ancres etirees)")] public RectTransform fill;
        [Tooltip("trainee facultative (blanche, derriere le remplissage)")] public RectTransform trail;
        [Tooltip("image du remplissage (pour changer sa couleur)")] public Image fillImage;
        [Tooltip("valeur affichee a l'ouverture de la scene")] [Range(0f, 1f)] public float value = 1f;
        float shown = -1f, shownTrail = -1f;

        void OnValidate() { Set(value); }

        public void Set(float k)
        {
            k = Mathf.Clamp01(k); value = k;
            if (fill != null && !Mathf.Approximately(shown, k)) { shown = k; fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(k, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero; }
        }
        public void SetTrail(float k)
        {
            if (trail == null) return; k = Mathf.Clamp01(k);
            if (Mathf.Approximately(shownTrail, k)) return; shownTrail = k;
            trail.anchorMin = Vector2.zero; trail.anchorMax = new Vector2(k, 1f); trail.offsetMin = trail.offsetMax = Vector2.zero;
        }
        public void SetColor(Color c) { if (fillImage != null && fillImage.color != c) fillImage.color = c; }
    }
}
