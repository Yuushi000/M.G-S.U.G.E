using UnityEngine;

namespace MGSUGE
{
    /// <summary>Mer (§6.4) : plan a -42 m anime par 4 sinusoides (amplitude cumulee 5,8 m), etendu au-dela du monde.</summary>
    public class SeaSurface : MonoBehaviour
    {
        public WorldData world;
        public Material material;
        public float size = 40000f;
        public int resolution = 128;
        Mesh mesh; Vector3[] baseV, v;
        static readonly Vector4[] Waves = // dirX, dirZ, longueur d'onde, part d'amplitude
        {
            new Vector4(1f, 0.3f, 900f, 0.38f), new Vector4(-0.4f, 1f, 620f, 0.27f),
            new Vector4(0.7f, -0.7f, 410f, 0.2f), new Vector4(-1f, -0.2f, 260f, 0.15f)
        };
        void Start()
        {
            mesh = MeshKit.Grid(size, size, resolution, resolution); mesh.MarkDynamic();
            baseV = mesh.vertices; v = (Vector3[])baseV.Clone();
            var go = MeshKit.Spawn("Mer_Surface", mesh, material, transform, Vector3.zero, Quaternion.identity);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            transform.position = new Vector3(0f, world.seaLevel, 0f);
        }
        void Update()
        {
            if (mesh == null) return;
            float t = Time.time;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = baseV[i]; float y = 0f;
                for (int k = 0; k < Waves.Length; k++)
                {
                    var w = Waves[k]; float kk = 2f * Mathf.PI / w.z;
                    y += Mathf.Sin((p.x * w.x + p.z * w.y) * kk + t * Mathf.Sqrt(9.81f * kk)) * world.seaWaveAmplitude * w.w;
                }
                v[i].y = y;
            }
            mesh.vertices = v; mesh.RecalculateNormals();
        }
    }
}
