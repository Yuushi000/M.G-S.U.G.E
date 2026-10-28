using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Terrain du monde : carte de hauteurs 513 x 513 (cellule de 29,3 m), maillage a facettes avec couleur de sommet par biome (§6.3).
    /// Source : fichier .raw exporte du prototype (StreamingAssets/MGSUGE) ou, a defaut, generateur provisoire.
    /// Fournit aussi la hauteur du sol pour le gameplay (groundH).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class TerrainSystem : MonoBehaviour
    {
        public static TerrainSystem I { get; private set; }
        public WorldData world;
        public Material material;
        [Tooltip("cellules par cote de tuile (64 -> 8 x 8 tuiles)")] public int chunkCells = 64;

        public float[] H { get; private set; }
        public int N { get; private set; }
        public bool FromHeightmap { get; private set; }
        float half, cell;

        void Awake()
        {
            I = this;
            Load();
            Build();
        }

        public void Load()
        {
            N = world.heightmapResolution;
            half = world.worldSize * 0.5f; cell = world.worldSize / (N - 1);
            FromHeightmap = false;
            if (!string.IsNullOrEmpty(world.heightmapFile))
            {
                string path = Path.Combine(Application.streamingAssetsPath, "MGSUGE", world.heightmapFile);
                if (File.Exists(path) && TryLoadRaw(path)) FromHeightmap = true;
                else Debug.LogWarning("[MGSUGE] Heightmap introuvable ou illisible : " + path + " -> terrain provisoire.");
            }
            if (!FromHeightmap) H = ProceduralTerrain.Generate(world, N);
        }

        bool TryLoadRaw(string path)
        {
            var m = Regex.Match(Path.GetFileName(path), @"min(-?[0-9.]+)_max(-?[0-9.]+)");
            if (!m.Success) { Debug.LogWarning("[MGSUGE] Le nom du .raw doit contenir min<valeur>_max<valeur>."); return false; }
            float mn = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            float mx = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            byte[] b = File.ReadAllBytes(path);
            if (b.Length < N * N * 2) { Debug.LogWarning("[MGSUGE] Taille du .raw incorrecte."); return false; }
            H = new float[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    int si = world.heightmapFlipX ? N - 1 - i : i, sj = world.heightmapFlipZ ? N - 1 - j : j;
                    int k = (sj * N + si) * 2;
                    ushort u = (ushort)(b[k] | (b[k + 1] << 8));
                    H[j * N + i] = mn + (mx - mn) * (u / 65535f);
                }
            return true;
        }

        // ------------------------------------------------------------------ hauteurs
        public float Height(float x, float z)
        {
            float fx = Mathf.Clamp((x + half) / cell, 0, N - 1.001f), fz = Mathf.Clamp((z + half) / cell, 0, N - 1.001f);
            int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
            float a = H[j * N + i], b = H[j * N + i + 1], c = H[(j + 1) * N + i], d = H[(j + 1) * N + i + 1];
            // meme decoupe en triangles que le maillage
            return u + v <= 1f ? a + (b - a) * u + (c - a) * v : d + (c - d) * (1 - u) + (b - d) * (1 - v);
        }
        /// <summary>Plancher utilise par les collisions : max(terrain, -43,4), et max(terrain, -16) dans le rectangle de lave.</summary>
        public float GroundH(float x, float z)
        {
            float h = Mathf.Max(Height(x, z), world.floorMin);
            if (world.InLavaRect(x, z)) h = Mathf.Max(h, world.lavaLevel);
            return h;
        }
        public static float Ground(float x, float z) => I != null ? I.GroundH(x, z) : 0f;

        // ------------------------------------------------------------------ couleurs de biome
        Color Biome(float x, float z, float h)
        {
            Color c = new Color(0.11f, 0.085f, 0.10f);
            float best = 0f;
            var p = new Vector2(x, z);
            foreach (var zn in world.zones)
            {
                float d = Vector2.Distance(p, zn.center);
                float wgt = 1f - Mathf.Clamp01((d - zn.radius * 0.7f) / (zn.radius * 0.3f));
                if (wgt > best) { best = wgt; c = Color.Lerp(new Color(0.11f, 0.085f, 0.10f), zn.ground, wgt); }
            }
            bool glacier = Vector2.Distance(p, new Vector2(0, -6300)) < 1800f;
            bool lava = world.InLavaRect(x, z);
            if (lava && h < world.lavaLevel + 2f) c = new Color(0.05f, 0.042f, 0.045f);
            float rock = Mathf.Clamp01((h - world.rockHeight.x) / (world.rockHeight.y - world.rockHeight.x));
            if (!glacier) c = Color.Lerp(c, world.rockColor, rock);
            float snow = Mathf.Clamp01((h - world.snowHeight.x) / (world.snowHeight.y - world.snowHeight.x));
            if (!glacier) c = Color.Lerp(c, world.snowColor, snow);
            if (h < world.seaLevel) c = Color.Lerp(c, world.seabedColor, Mathf.Clamp01((world.seaLevel - h) / 6f));
            float n = ProceduralTerrain.ColorNoise(Mathf.FloorToInt(x / world.colorBlock), Mathf.FloorToInt(z / world.colorBlock));
            return c * (1f + n * world.colorVariation);
        }

        // ------------------------------------------------------------------ maillages
        void Build()
        {
            int cells = N - 1, cc = chunkCells, chunks = Mathf.CeilToInt(cells / (float)cc);
            var root = new GameObject("Terrain_Tuiles").transform; root.SetParent(transform, false);
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color32>(); var tris = new List<int>();
            var cverts = new List<Vector3>(); var ctris = new List<int>();
            for (int cz = 0; cz < chunks; cz++)
                for (int cx = 0; cx < chunks; cx++)
                {
                    int i0 = cx * cc, j0 = cz * cc, i1 = Mathf.Min(i0 + cc, cells), j1 = Mathf.Min(j0 + cc, cells);
                    Vector3 origin = new Vector3(-half + i0 * cell, 0f, -half + j0 * cell);
                    verts.Clear(); norms.Clear(); cols.Clear(); tris.Clear(); cverts.Clear(); ctris.Clear();
                    for (int j = j0; j < j1; j++)
                        for (int i = i0; i < i1; i++)
                        {
                            Vector3 a = P(i, j) - origin, b = P(i + 1, j) - origin, c = P(i, j + 1) - origin, d = P(i + 1, j + 1) - origin;
                            AddTri(a, c, b, origin, verts, norms, cols, tris);
                            AddTri(b, c, d, origin, verts, norms, cols, tris);
                        }
                    int w = i1 - i0 + 1;
                    for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) cverts.Add(P(i, j) - origin);
                    for (int j = 0; j < j1 - j0; j++)
                        for (int i = 0; i < i1 - i0; i++)
                        {
                            int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
                            ctris.Add(a); ctris.Add(c); ctris.Add(b); ctris.Add(b); ctris.Add(c); ctris.Add(d);
                        }
                    var go = new GameObject($"Tuile_{cx}_{cz}");
                    go.layer = Layers.World;
                    go.transform.SetParent(root, false); go.transform.localPosition = origin;
                    var mesh = new Mesh { name = go.name };
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
                    mesh.RecalculateBounds();
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material;
                    var cm = new Mesh { name = go.name + "_Collision" };
                    cm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    cm.SetVertices(cverts); cm.SetTriangles(ctris, 0); cm.RecalculateBounds();
                    go.AddComponent<MeshCollider>().sharedMesh = cm;
                }
        }
        Vector3 P(int i, int j) => new Vector3(-half + i * cell, H[j * N + i], -half + j * cell);
        void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector3 origin, List<Vector3> v, List<Vector3> n, List<Color32> col, List<int> t)
        {
            Vector3 nn = Vector3.Cross(b - a, c - a).normalized;
            Vector3 ctr = (a + b + c) / 3f + origin;
            Color32 cc = Biome(ctr.x, ctr.z, ctr.y);
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(nn); n.Add(nn); n.Add(nn);
            col.Add(cc); col.Add(cc); col.Add(cc);
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
        }
    }
}
