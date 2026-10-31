using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Arc rose a l'ecran au moment de la frappe de la lame a plasma (repris du HUD de travail du jalon 4).</summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Arc de lame")]
    public class OverlayBladeArc : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("image de l'arc")] public Image arc;
        [Tooltip("duree (s)")] [Range(0.05f, 1f)] public float duration = 0.3f;

        void LateUpdate()
        {
            if (arc == null) return;
            float k = hud != null && hud.weapons != null && hud.data != null ? (Time.time - hud.weapons.BladeArcTime) / duration : 9f;
            bool on = k >= 0f && k < 1f;
            arc.enabled = on;
            if (on) arc.color = UiFx.A(hud.data.rose, 1f - k);
        }
    }
}
