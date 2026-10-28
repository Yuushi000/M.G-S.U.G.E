using UnityEngine;
using UnityEngine.Rendering;

namespace MGSUGE
{
    /// <summary>
    /// Ciel crepusculaire, lumieres, brouillard et passage a l'espace (cahier §6.5).
    /// Les astres (planete a anneaux, lune, etoiles) suivent la camera pour rester "a l'infini".
    /// </summary>
    public class SkySystem : MonoBehaviour
    {
        public WorldData world;
        public Material skybox, planetMaterial, moonMaterial, ringMaterial, starMaterial;
        public Light sun, fill;
        Transform anchor; Material skyInst, starInst;
        Camera cam;

        void Awake()
        {
            if (skybox != null) { skyInst = new Material(skybox); RenderSettings.skybox = skyInst; }
            Vector3 sd = world.sunDirection.normalized;
            if (sun != null)
            {
                sun.type = LightType.Directional; sun.color = world.sunColor; sun.intensity = world.sunIntensity;
                sun.transform.rotation = Quaternion.LookRotation(-sd); sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }
            if (fill != null)
            {
                fill.type = LightType.Directional; fill.color = world.fillColor; fill.intensity = world.fillIntensity;
                fill.transform.rotation = Quaternion.LookRotation(-world.fillDirection.normalized); fill.shadows = LightShadows.None;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = world.hemiSky * world.hemiIntensity;
            RenderSettings.ambientEquatorColor = Color.Lerp(world.hemiSky, world.hemiGround, 0.5f) * world.hemiIntensity;
            RenderSettings.ambientGroundColor = world.hemiGround * world.hemiIntensity;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = world.fogColor.gamma;
            RenderSettings.fogDensity = world.fogDensity;
            BuildCelestials();
        }

        void BuildCelestials()
        {
            anchor = new GameObject("Ciel_Astres").transform; anchor.SetParent(transform, false);
            if (planetMaterial != null)
            {
                var p = MeshKit.Spawn("Planete", MeshKit.Sphere(world.planetRadius, 3), planetMaterial, anchor, world.planetDirection.normalized * world.planetDistance, Quaternion.identity);
                p.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                if (ringMaterial != null)
                {
                    var r = MeshKit.Spawn("Anneau_Planete", MeshKit.Annulus(world.ringInner, world.ringOuter, 128), ringMaterial, p.transform, Vector3.zero,
                        Quaternion.Euler(world.ringTilt.x * Mathf.Rad2Deg, 0f, world.ringTilt.y * Mathf.Rad2Deg));
                    r.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            if (moonMaterial != null)
            {
                var m = MeshKit.Spawn("Lune", MeshKit.Sphere(world.moonRadius, 1), moonMaterial, anchor, world.moonDirection.normalized * world.moonDistance, Quaternion.identity);
                m.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (starMaterial != null)
            {
                starInst = new Material(starMaterial);
                int n = world.smallStars + world.bigStars;
                var v = new Vector3[n]; var c = new Color[n]; var idx = new int[n];
                var rng = new System.Random(world.seed + 1);
                for (int i = 0; i < n; i++)
                {
                    Vector3 d;
                    do { d = new Vector3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1); } while (d.sqrMagnitude > 1f || d.sqrMagnitude < 0.01f);
                    d.Normalize(); if (d.y < -0.15f) d.y = -d.y; // surtout au-dessus de l'horizon
                    v[i] = d * 15000f;
                    double r = rng.NextDouble();
                    Color col = r < 0.15 ? new Color(1f, 0.75f, 0.5f) : r < 0.40 ? new Color(0.65f, 0.78f, 1f) : Color.white;
                    float b = i < world.smallStars ? 0.35f + (float)rng.NextDouble() * 0.4f : 0.9f + (float)rng.NextDouble() * 0.6f;
                    c[i] = col * b; c[i].a = 1f; idx[i] = i;
                }
                var mesh = new Mesh { name = "Etoiles" };
                mesh.vertices = v; mesh.colors = c; mesh.SetIndices(idx, MeshTopology.Points, 0); mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 40000f);
                var s = MeshKit.Spawn("Etoiles", mesh, starInst, anchor, Vector3.zero, Quaternion.identity);
                s.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            if (anchor != null) anchor.position = cp;
            // passage a l'espace : ciel noir etoile, brouillard -72 %
            float k = Mathf.Clamp01((cp.y - world.spaceFogStart) / (world.spaceFogEnd - world.spaceFogStart));
            RenderSettings.fogDensity = world.fogDensity * (1f - world.spaceFogReduction * k);
            RenderSettings.fogColor = Color.Lerp(world.fogColor, world.spaceFogColor, k).gamma;
            if (skyInst != null) skyInst.SetFloat("_Exposure", Mathf.Lerp(1.15f, 0.08f, k));
            if (starInst != null) starInst.SetFloat("_Fade", Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(cp.y / world.spaceFogEnd)));
        }
    }
}
