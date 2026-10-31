using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>Ligne d'horizon artificielle (surimpression, §16.2), affichee en forme vaisseau : suit le roulis et le tangage.</summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Horizon")]
    public class OverlayHorizon : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("conteneur tourne selon le roulis (les deux traits sont ses enfants)")] public RectTransform pivot;
        [Tooltip("traits gauche et droit")] public Graphic[] lines;
        [Tooltip("decalage vertical par radian de tangage (px a 1080p)")] [Range(0f, 600f)] public float pitchScale = 200f;
        [Tooltip("opacite")] [Range(0f, 1f)] public float alpha = 0.5f;
        [Tooltip("afficher aussi en chassis")] public bool showInMech;

        void LateUpdate()
        {
            bool on = hud != null && hud.player != null && hud.data != null && (hud.player.IsShip || showInMech) && (GameManager.I == null || GameManager.I.State == GameState.Play);
            if (pivot != null) UiFx.SetActive(pivot.gameObject, on);
            if (!on || pivot == null) return;
            Quaternion q = hud.player.IsShip ? hud.player.ShipRotation : hud.player.transform.rotation;
            Vector3 f = q * Vector3.forward, u = q * Vector3.up;
            float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, f), u, f);
            float pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f));
            pivot.localRotation = Quaternion.Euler(0f, 0f, roll);
            pivot.anchoredPosition = new Vector2(0f, -pitch * pitchScale);
            if (lines != null) foreach (var l in lines) UiFx.SetColor(l, UiFx.A(hud.data.ice, alpha));
        }
    }
}
