using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Carte de hauteurs du monde (§6.3) : 513 x 513 points, cellule de 29,3 m. Source : fichier .raw exporte du prototype
    /// (StreamingAssets/MGSUGE) ou, a defaut, generateur provisoire. Calcule aussi le maillage a facettes d'une tuile.
    /// </summary>
    public class HeightField
    {
        public readonly WorldData world;
        public readonly int N;
        public readonly float[] H;
        public readonly bool FromHeightmap;
        readonly float half, cell;

        HeightField(WorldData w, float[] h, bool fromHm)
        {
            world = w; N = w.heightmapResolution; H = h; FromHeightmap = fromHm;
            half = w.worldSize * 0.5f; cell = w.worldSize / (N - 1);
        }

        public static HeightField Load(WorldData w)
        {
            int n = w.heightmapResolution;
            if (!string.IsNullOrEmpty(w.heightmapFile))
            {
                string path = Path.Combine(Application.streamingAssetsPath, "MGSUGE", w.heightmapFile);
                if (File.Exists(path))
                {
                    var h = TryLoadRaw(w, path, n);
                    if (h != null) return new HeightField(w, h, true);
                }
                Debug.LogWarning("[MGSUGE] Heightmap introuvable ou illisible : " + path + " -> terrain provisoire.");
            }
            return new HeightField(w, ProceduralTerrain.Generate(w, n), false);
        }

        static float[] TryLoadRaw(WorldData w, string path, int n)
        {
            var m = Regex.Match(Path.GetFileName(path), @"min(-?[0-9.]+)_max(-?[0-9.]+)");
            if (!m.Success) { Debug.LogWarning("[MGSUGE] Le nom du .raw doit contenir min<valeur>_max<valeur>."); return null; }
            float mn = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            float mx = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            byte[] b = File.ReadAllBytes(path);
            if (b.Length < n * n * 2) { Debug.LogWarning("[MGSUGE] Taille du .raw incorrecte."); return null; }
            var h = new float[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int si = w.heightmapFlipX ? n - 1 - i : i, sj = w.heightmapFlipZ ? n - 1 - j : j;
                    int k = (sj * n + si) * 2;
                    ushort u = (ushort)(b[k] | (b[k + 1] << 8));
                    h[j * n + i] = mn + (mx - mn) * (u / 65535f);
                }
            return h;
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

        // ------------------------------------------------------------------ couleurs de biome
        /// <summary>Couleur de biome du sol en (x, z) a la hauteur h (tuiles du terrain et carte de la mini-carte).</summary>
        public Color Biome(float x, float z, float h)
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

        // ------------------------------------------------------------------ maillage d'une tuile
        public int TileCount(int chunkCells) => Mathf.CeilToInt((N - 1) / (float)chunkCells);
        public Vector3 TileOrigin(int cx, int cz, int chunkCells) => new Vector3(-half + cx * chunkCells * cell, 0f, -half + cz * chunkCells * cell);

        /// <summary>Maillage d'affichage (facettes + couleurs) et maillage de collision d'une tuile, en coordonnees locales a son origine.</summary>
        public void BuildTile(int cx, int cz, int chunkCells, out Mesh render, out Mesh collision)
        {
            int cells = N - 1, cc = chunkCells;
            int i0 = cx * cc, j0 = cz * cc, i1 = Mathf.Min(i0 + cc, cells), j1 = Mathf.Min(j0 + cc, cells);
            Vector3 origin = TileOrigin(cx, cz, chunkCells);
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color32>(); var tris = new List<int>();
            for (int j = j0; j < j1; j++)
                for (int i = i0; i < i1; i++)
                {
                    Vector3 a = P(i, j) - origin, b = P(i + 1, j) - origin, c = P(i, j + 1) - origin, d = P(i + 1, j + 1) - origin;
                    AddTri(a, c, b, origin, verts, norms, cols, tris);
                    AddTri(b, c, d, origin, verts, norms, cols, tris);
                }
            render = new Mesh { name = $"Tuile_{cx}_{cz}" };
            render.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            render.SetVertices(verts); render.SetNormals(norms); render.SetColors(cols); render.SetTriangles(tris, 0); render.RecalculateBounds();

            var cverts = new List<Vector3>(); var ctris = new List<int>();
            int w = i1 - i0 + 1;
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) cverts.Add(P(i, j) - origin);
            for (int j = 0; j < j1 - j0; j++)
                for (int i = 0; i < i1 - i0; i++)
                {
                    int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
                    ctris.Add(a); ctris.Add(c); ctris.Add(b); ctris.Add(b); ctris.Add(c); ctris.Add(d);
                }
            collision = new Mesh { name = $"Tuile_{cx}_{cz}_Collision" };
            collision.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            collision.SetVertices(cverts); collision.SetTriangles(ctris, 0); collision.RecalculateBounds();
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

    /// <summary>
    /// Terrain du monde (objet « Terrain » de la scene World). Ses tuiles sont des enfants visibles dans la Hierarchy
    /// (composant TerrainTile) dont le maillage est calcule a l'ouverture de la scene, en edition comme en jeu.
    /// Fournit la hauteur du sol au gameplay (TerrainSystem.Ground). Sans terrain dans la scene, le sol est a y = 0.
    /// </summary>
    [ExecuteAlways, DefaultExecutionOrder(-200)]
    public class TerrainSystem : MonoBehaviour
    {
        public static TerrainSystem I { get; private set; }
        [Tooltip("donnees du monde : taille, heightmap, couleurs de biome")] public WorldData world;
        [Tooltip("materiau a couleurs de sommet utilise par les tuiles")] public Material material;
        [Tooltip("cellules par cote de tuile (64 -> 8 x 8 tuiles)")] public int chunkCells = 64;

        public HeightField Field { get; private set; }
        public bool FromHeightmap => Field != null && Field.FromHeightmap;

        void OnEnable() { I = this; EnsureLoaded(); }
        void OnDisable() { if (I == this) I = null; }

        public void EnsureLoaded() { if (Field == null && world != null) Field = HeightField.Load(world); }

        /// <summary>Relit la heightmap et recalcule toutes les tuiles (menu contextuel de l'Inspector).</summary>
        [ContextMenu("Recalculer le terrain")]
        public void Reload()
        {
            Field = world != null ? HeightField.Load(world) : null;
            foreach (var t in GetComponentsInChildren<TerrainTile>(true)) t.Rebuild();
        }

        public float Height(float x, float z) { EnsureLoaded(); return Field != null ? Field.Height(x, z) : 0f; }
        public float GroundH(float x, float z) { EnsureLoaded(); return Field != null ? Field.GroundH(x, z) : 0f; }
        public static float Ground(float x, float z) => I != null ? I.GroundH(x, z) : 0f;
    }
}
