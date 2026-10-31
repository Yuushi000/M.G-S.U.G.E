using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Mer (§6.4) : l'objet « Mer » de la scene porte une grille (MeshFilter) placee au niveau de la mer ; en jeu, ce script
    /// anime une copie du maillage avec 4 sinusoides (amplitude cumulee 5,8 m).
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class SeaSurface : MonoBehaviour
    {
        [Tooltip("amplitude cumulee des vagues (m)")] public float waveAmplitude = 5.8f;
        [Tooltip("vagues : direction X, direction Z, longueur d'onde (m), part de l'amplitude")]
        public Vector4[] waves =
        {
            new Vector4(1f, 0.3f, 900f, 0.38f), new Vector4(-0.4f, 1f, 620f, 0.27f),
            new Vector4(0.7f, -0.7f, 410f, 0.2f), new Vector4(-1f, -0.2f, 260f, 0.15f)
        };
        Mesh mesh; Vector3[] baseV, v;

        void Start()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf.sharedMesh == null) return;
            mesh = Instantiate(mf.sharedMesh); mesh.name = mf.sharedMesh.name + " (anime)"; mesh.MarkDynamic();
            mf.sharedMesh = mesh;
            baseV = mesh.vertices; v = (Vector3[])baseV.Clone();
        }
        void Update()
        {
            if (mesh == null) return;
            float t = Time.time;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = baseV[i]; float y = 0f;
                for (int k = 0; k < waves.Length; k++)
                {
                    var w = waves[k]; float kk = 2f * Mathf.PI / Mathf.Max(1f, w.z);
                    y += Mathf.Sin((p.x * w.x + p.z * w.y) * kk + t * Mathf.Sqrt(9.81f * kk)) * waveAmplitude * w.w;
                }
                v[i].y = y;
            }
            mesh.vertices = v; mesh.RecalculateNormals();
        }
    }
}
