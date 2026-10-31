using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Ciel (§6.5) : les astres (planete, anneau, lune, etoiles) sont des objets de la scene sous « Astres », centres sur la camera
    /// (composant FollowCamera). Ce script gere le passage a l'espace entre 3 600 et 7 200 m : ciel qui s'assombrit,
    /// brouillard -72 % et virant au noir violace, etoiles plus visibles. Les valeurs de depart (brouillard, ciel) sont celles
    /// de la fenetre Lighting de la scene.
    /// </summary>
    public class SkySystem : MonoBehaviour
    {
        [Tooltip("donnees du monde : altitudes et couleurs du passage a l'espace")] public WorldData world;
        [Tooltip("camera dont l'altitude pilote le passage a l'espace")] public Camera targetCamera;
        [Tooltip("rendu des etoiles (son materiau est duplique pour regler le fondu)")] public Renderer stars;
        [Tooltip("exposition du ciel au sol et dans l'espace")] public Vector2 skyExposure = new Vector2(1.15f, 0.08f);

        Material skyInst, starInst;
        float fog0; Color fogColor0;

        void Start()
        {
            fog0 = RenderSettings.fogDensity; fogColor0 = RenderSettings.fogColor;
            if (RenderSettings.skybox != null) { skyInst = new Material(RenderSettings.skybox) { name = RenderSettings.skybox.name + " (jeu)" }; RenderSettings.skybox = skyInst; }
            if (stars != null) starInst = stars.material;
        }

        void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || world == null) return;
            float y = targetCamera.transform.position.y;
            float k = Mathf.Clamp01((y - world.spaceFogStart) / (world.spaceFogEnd - world.spaceFogStart));
            RenderSettings.fogDensity = fog0 * (1f - world.spaceFogReduction * k);
            RenderSettings.fogColor = Color.Lerp(fogColor0, world.spaceFogColor.gamma, k);
            if (skyInst != null && skyInst.HasProperty("_Exposure")) skyInst.SetFloat("_Exposure", Mathf.Lerp(skyExposure.x, skyExposure.y, k));
            if (starInst != null) starInst.SetFloat("_Fade", Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(y / world.spaceFogEnd)));
        }
    }
}
