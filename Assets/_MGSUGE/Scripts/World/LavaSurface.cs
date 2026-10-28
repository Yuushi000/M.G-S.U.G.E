using UnityEngine;

namespace MGSUGE
{
    /// <summary>Lave (§6.4) : plan de 2 450 x 6 200 m a -16 m, orange incandescent avec croute sombre mouvante (texture de bruit defilante).</summary>
    public class LavaSurface : MonoBehaviour
    {
        public WorldData world;
        public Material material;
        Material inst; Texture2D tex;
        void Start()
        {
            tex = new Texture2D(256, 256, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Lave_Croute" };
            var px = new Color32[256 * 256];
            for (int j = 0; j < 256; j++)
                for (int i = 0; i < 256; i++)
                {
                    float n = 0f, a = 0.5f, f = 4f / 256f;
                    for (int o = 0; o < 4; o++) { n += a * Mathf.PerlinNoise(i * f * Mathf.Pow(2, o) + 31.7f * o, j * f * Mathf.Pow(2, o) + 11.3f * o); a *= 0.5f; }
                    float crust = Mathf.Clamp01((n - 0.42f) * 4f);
                    byte g = (byte)(255 * (1f - crust));
                    px[j * 256 + i] = new Color32(g, g, g, 255);
                }
            tex.SetPixels32(px); tex.Apply(true);
            inst = new Material(material) { name = "Lave_Instance" };
            inst.SetTexture("_BaseMap", tex); inst.SetTexture("_EmissionMap", tex);
            inst.SetTextureScale("_BaseMap", new Vector2(6f, 15f));
            var mesh = MeshKit.Grid(world.lavaPlaneSize.x, world.lavaPlaneSize.y, 24, 60);
            var go = MeshKit.Spawn("Lave_Surface", mesh, inst, transform, Vector3.zero, Quaternion.identity);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            transform.position = new Vector3(world.lavaPlaneCenter.x, world.lavaLevel, world.lavaPlaneCenter.y);
            var light = new GameObject("Lave_Lumiere").AddComponent<Light>();
            light.type = LightType.Point; light.color = world.lavaLightColor; light.intensity = world.lavaLightIntensity; light.range = world.lavaLightRange;
            light.transform.position = world.lavaLightPosition; light.transform.SetParent(transform, true);
        }
        void Update()
        {
            if (inst == null) return;
            float t = Time.time;
            inst.SetTextureOffset("_BaseMap", new Vector2(Mathf.Sin(t * 0.03f) * 0.2f, t * 0.012f));
        }
    }
}
