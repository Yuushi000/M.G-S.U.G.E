using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Generateur PROVISOIRE de la carte de hauteurs, utilise tant que la heightmap n'a pas ete exportee du HTML (§6.3).
    /// Il reproduit les grandes formes decrites dans le cahier (centre a 10 m, cratere, cretes, glacier, volcans, mur de 520 m)
    /// mais pas le relief exact du prototype. A remplacer par le fichier .raw des que possible.
    /// </summary>
    public static class ProceduralTerrain
    {
        static int seedX, seedZ;
        static float Hash(int x, int z)
        {
            unchecked
            {
                int h = x * 374761393 + z * 668265263 + seedX * 1274126177 + seedZ;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }
        static float Noise(float x, float z)
        {
            int xi = Mathf.FloorToInt(x), zi = Mathf.FloorToInt(z);
            float xf = x - xi, zf = z - zi;
            float u = xf * xf * (3 - 2 * xf), v = zf * zf * (3 - 2 * zf);
            float a = Hash(xi, zi), b = Hash(xi + 1, zi), c = Hash(xi, zi + 1), d = Hash(xi + 1, zi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) * 2f - 1f;
        }
        public static float Fbm(float x, float z, int oct = 5)
        {
            float s = 0, a = 0.5f, f = 1f, n = 0;
            for (int i = 0; i < oct; i++) { s += Noise(x * f, z * f) * a; n += a; a *= 0.5f; f *= 2.03f; }
            return s / n;
        }
        static float Ridged(float x, float z)
        {
            float s = 0, a = 0.5f, f = 1f;
            for (int i = 0; i < 5; i++) { float r = 1f - Mathf.Abs(Noise(x * f, z * f)); s += r * r * a; a *= 0.5f; f *= 2.1f; }
            return s;
        }
        static float W(Vector2 p, Vector2 c, float r) // poids d'une zone : 1 au centre (jusqu'a 0,6 R), 0 au bord
        {
            float d = Vector2.Distance(p, c);
            return 1f - Mathf.Clamp01((d - r * 0.6f) / (r * 0.4f));
        }
        static float Cone(Vector2 p, Vector2 c, float r, float h)
        {
            float d = Vector2.Distance(p, c) / r;
            if (d >= 1f) return -1e9f;
            float cone = h * Mathf.Pow(1f - d, 1.35f);
            if (d < 0.12f) cone -= (0.12f - d) / 0.12f * h * 0.12f; // cratere sommital
            return cone;
        }
        public static readonly Vector2[] Volcanoes = { new Vector2(6450, -950), new Vector2(5900, 1100), new Vector2(6900, 2000), new Vector2(6700, -2300) };
        static readonly float[] VolcanoR = { 560, 400, 320, 300 };
        static readonly float[] VolcanoH = { 560, 380, 300, 260 };

        public static float Height(WorldData w, float x, float z)
        {
            var p = new Vector2(x, z);
            float h = 22f + Fbm(x / 1700f, z / 1700f) * 55f + Fbm(x / 400f, z / 400f, 3) * 8f;
            // zones centrales
            h = Mathf.Lerp(h, 16f + Fbm(x / 600f, z / 600f) * 14f, W(p, new Vector2(0, -3200), 1100));                 // Lyra
            float dk = Vector2.Distance(p, new Vector2(3200, 400));                                                        // Kessler
            float crater = dk < 560f ? -110f + Mathf.Pow(dk / 560f, 2.2f) * 175f : 65f * Mathf.Exp(-(dk - 560f) / 180f);
            h = Mathf.Lerp(h, crater, W(p, new Vector2(3200, 400), 1050));
            h = Mathf.Lerp(h, 70f + Ridged(x / 700f, z / 700f) * 230f, W(p, new Vector2(-3200, 300), 1100));             // Cretes
            h = Mathf.Lerp(h, 14f + Mathf.Sin(x / 95f + Fbm(x / 300f, z / 300f) * 3f) * 7f, W(p, new Vector2(500, 3200), 1100)); // Thalassa
            h = Mathf.Lerp(h, 18f + Fbm(x / 500f, z / 500f) * 3f, W(p, new Vector2(3500, -3500), 950));                 // Orsk
            h = Mathf.Lerp(h, 90f + Fbm(x / 500f, z / 500f) * 6f, W(p, new Vector2(-3500, -3500), 1000));               // Tessera
            h = Mathf.Lerp(h, -70f + Mathf.Max(0f, Fbm(x / 260f, z / 260f) + 0.15f) * 220f, W(p, new Vector2(3500, 3500), 1150)); // Nereide
            h = Mathf.Lerp(h, 12f + Fbm(x / 500f, z / 500f) * 2f, W(p, new Vector2(-3500, 3500), 1000));                // Helix
            // zones de bord
            h = Mathf.Lerp(h, 110f + (Fbm(x / 900f, z / 900f) * 0.5f + 0.5f) * 160f, W(p, new Vector2(0, -6300), 1800)); // Boree
            float cal = -30f + Fbm(x / 500f, z / 500f) * 6f;                                                              // Ignis
            for (int i = 0; i < Volcanoes.Length; i++) cal = Mathf.Max(cal, Cone(p, Volcanoes[i], VolcanoR[i], VolcanoH[i]));
            h = Mathf.Lerp(h, cal, W(p, new Vector2(6300, 0), 1800));
            h = Mathf.Lerp(h, -40.5f + Fbm(x / 300f, z / 300f) * 3.5f, W(p, new Vector2(0, 6300), 1800));             // Nyx
            h = Mathf.Lerp(h, 18f + Mathf.Abs(Mathf.Sin(x / 140f + Fbm(x / 800f, z / 800f) * 4f)) * 26f, W(p, new Vector2(-6300, 0), 1800)); // Erg
            // arcologie : plaine reguliere, exactement 10 m au centre
            float dc = p.magnitude;
            float arco = w.centerHeight + (dc > 350f ? Fbm(x / 500f, z / 500f) * 3f * Mathf.Clamp01((dc - 350f) / 400f) : 0f);
            h = Mathf.Lerp(h, arco, W(p, Vector2.zero, 1150));
            // mur montagneux
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float wall = Mathf.Clamp01((edge - w.wallStart) / (w.wallEnd - w.wallStart));
            wall = wall * wall * (3 - 2 * wall);
            h += wall * (w.wallHeight + Ridged(x / 500f, z / 500f) * 120f);
            return Mathf.Max(h, w.floorMin - 80f);   // le fond du cratere de Kessler descend a -110 m
        }

        public static float[] Generate(WorldData w, int n)
        {
            seedX = w.seed; seedZ = w.seed * 7 + 13;
            var H = new float[n * n];
            float half = w.worldSize * 0.5f, cell = w.worldSize / (n - 1);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                    H[j * n + i] = Height(w, -half + i * cell, -half + j * cell);
            return H;
        }

        public static float ColorNoise(int bx, int bz) { return Hash(bx * 31 + 7, bz * 17 + 3) * 2f - 1f; }
    }
}
