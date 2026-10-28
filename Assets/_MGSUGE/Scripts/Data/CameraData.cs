using UnityEngine;

namespace MGSUGE
{
    /// <summary>Reglages de camera (cahier §7.5).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Camera", fileName = "MGSUGE_Camera")]
    public class CameraData : ScriptableObject
    {
        [Header("Forme chassis")]
        public float mechDistance = 21f;
        public float mechAssaultExtra = 5f;
        [Tooltip("recul supplementaire maximal selon la vitesse")] public float mechSpeedExtra = 2f;
        public float mechShoulder = 3.2f;
        public float mechElevation = 5.2f;
        public float mechLookHeight = 3.6f;
        public float pitchMin = -1.15f, pitchMax = 1.2f;

        [Header("Forme vaisseau")]
        public float shipDistance = 26f;
        public float shipElevation = 2.6f;
        public float shipLookHeight = 1.5f;
        [Tooltip("degres supplementaires au-dela de 150 m/s")] public float shipSpeedFov = 12f;
        public float shipFovSpeedStart = 150f;
        public float shipFovSpeedFull = 440f;

        [Header("Champ de vision (degres)")]
        public float fov = 72f;
        public float assaultFov = 10f, quickBoostFov = 6f, jumpFov = 4f;

        [Header("Souris et clavier")]
        [Tooltip("rad par pixel")] public float mouseSensitivity = 0.0023f;
        [Tooltip("rad/s avec les fleches")] public float arrowYaw = 1.8f, arrowPitch = 1.5f;

        [Header("Divers")]
        public float groundClearance = 2.5f;
        [Tooltip("decroissance des secousses par seconde")] public float shakeDecay = 3f;
        public float aimMin = 30f, aimMax = 1200f;
        [Tooltip("duree de remise a l'horizontale apres vaisseau -> chassis")] public float relevelTime = 0.6f;
        public float near = 1f, far = 32000f;

        [Header("Secousses")]
        public float shakeShot = 0.05f, shakeQuickBoost = 0.35f, shakeAssault = 0.5f, shakeLandingMax = 0.6f, shakeHit = 0.4f;
    }
}
