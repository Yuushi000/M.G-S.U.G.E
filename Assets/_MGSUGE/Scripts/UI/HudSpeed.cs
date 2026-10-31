using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>A droite du reticule (§16.1) : vitesse en km/h et altitude en m (hauteur au-dessus de la mer).</summary>
    [AddComponentMenu("MGSUGE/UI/HUD Vitesse et altitude")]
    public class HudSpeed : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        public Text speed;
        public Text altitude;

        void Update()
        {
            if (hud == null || hud.player == null || !hud.Visible) return;
            UiFx.SetText(speed, UiFx.N(hud.player.SpeedKmh) + " km/h");
            UiFx.SetText(altitude, UiFx.N(hud.player.Altitude) + " m");
        }
    }
}
