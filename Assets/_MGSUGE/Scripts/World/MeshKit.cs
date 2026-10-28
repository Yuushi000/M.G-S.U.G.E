using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>Petites formes procedurales a facettes (style low-poly du prototype) pour le decor et les effets.</summary>
    public static class MeshKit
    {
        class B
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
            {
                Vector3 nn = Vector3.Cross(b - a, c - a).normalized; int k = v.Count;
                v.Add(a); v.Add(b); v.Add(c); n.Add(nn); n.Add(nn); n.Add(nn); uv.Add(ua); uv.Add(ub); uv.Add(uc);
                t.Add(k); t.Add(k + 1); t.Add(k + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(a, b, c, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)); Tri(a, c, d, new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0)); }
            public Mesh Make(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
            }
        }

        /// <summary>Tronc de cone vertical (base en y=0), facettes.</summary>
        public static Mesh Frustum(float r0, float r1, float h, int sides = 8, bool caps = true)
        {
            var b = new B();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = p0 * r0, b1 = p1 * r0, t0 = p0 * r1 + Vector3.up * h, t1 = p1 * r1 + Vector3.up * h;
                b.Quad(b0, t0, t1, b1);
                if (caps)
                {
                    b.Tri(Vector3.up * h, t1, t0);
                    b.Tri(Vector3.zero, b0, b1);
                }
            }
            return b.Make("Frustum");
        }
        /// <summary>Tore horizontal (anneau) de rayon R et de section r.</summary>
        public static Mesh Torus(float R, float r, int seg = 32, int side = 6)
        {
            var b = new B();
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    Vector3 P(int ii, int jj)
                    {
                        float a = ii * Mathf.PI * 2 / seg, c = jj * Mathf.PI * 2 / side;
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        return dir * (R + r * Mathf.Cos(c)) + Vector3.up * r * Mathf.Sin(c);
                    }
                    b.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j));
                }
            return b.Make("Torus");
        }
        /// <summary>Boite centree en x/z, posee sur y=0.</summary>
        public static Mesh Box(Vector3 size)
        {
            var b = new B(); float x = size.x / 2, z = size.z / 2, y = size.y;
            Vector3 v000 = new Vector3(-x, 0, -z), v100 = new Vector3(x, 0, -z), v010 = new Vector3(-x, y, -z), v110 = new Vector3(x, y, -z);
            Vector3 v001 = new Vector3(-x, 0, z), v101 = new Vector3(x, 0, z), v011 = new Vector3(-x, y, z), v111 = new Vector3(x, y, z);
            b.Quad(v000, v010, v110, v100); b.Quad(v101, v111, v011, v001); b.Quad(v001, v011, v010, v000);
            b.Quad(v100, v110, v111, v101); b.Quad(v010, v011, v111, v110); b.Quad(v001, v000, v100, v101);
            return b.Make("Box");
        }
        /// <summary>Anneau plat (disque perce) dans le plan XZ ; uv.x = rayon normalise.</summary>
        public static Mesh Annulus(float rIn, float rOut, int seg = 96)
        {
            var b = new B();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                b.Tri(d0 * rIn, d0 * rOut, d1 * rOut, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1));
                b.Tri(d0 * rIn, d1 * rOut, d1 * rIn, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1));
            }
            return b.Make("Annulus");
        }
        /// <summary>Sphere a facettes (icosaedre subdivise).</summary>
        public static Mesh Sphere(float r, int subdiv = 2)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var vs = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            var fs = new List<int[]> { new[]{0,11,5},new[]{0,5,1},new[]{0,1,7},new[]{0,7,10},new[]{0,10,11},new[]{1,5,9},new[]{5,11,4},new[]{11,10,2},new[]{10,7,6},new[]{7,1,8},
                new[]{3,9,4},new[]{3,4,2},new[]{3,2,6},new[]{3,6,8},new[]{3,8,9},new[]{4,9,5},new[]{2,4,11},new[]{6,2,10},new[]{8,6,7},new[]{9,8,1} };
            for (int s = 0; s < subdiv; s++)
            {
                var nf = new List<int[]>(); var cache = new Dictionary<long, int>();
                int Mid(int a, int c)
                {
                    long key = a < c ? ((long)a << 32) | (uint)c : ((long)c << 32) | (uint)a;
                    if (cache.TryGetValue(key, out int idx)) return idx;
                    vs.Add((vs[a] + vs[c]) * 0.5f); cache[key] = vs.Count - 1; return vs.Count - 1;
                }
                foreach (var f in fs)
                {
                    int a = Mid(f[0], f[1]), bb = Mid(f[1], f[2]), c = Mid(f[2], f[0]);
                    nf.Add(new[] { f[0], a, c }); nf.Add(new[] { f[1], bb, a }); nf.Add(new[] { f[2], c, bb }); nf.Add(new[] { a, bb, c });
                }
                fs = nf;
            }
            var b = new B();
            foreach (var f in fs) b.Tri(vs[f[0]].normalized * r, vs[f[2]].normalized * r, vs[f[1]].normalized * r);
            return b.Make("Sphere");
        }
        /// <summary>Octaedre (cristaux, noyaux, cibles).</summary>
        public static Mesh Octahedron(float r, float hScale = 1f)
        {
            var b = new B();
            Vector3 top = Vector3.up * r * hScale, bot = Vector3.down * r * hScale;
            Vector3[] e = { new Vector3(r, 0, 0), new Vector3(0, 0, r), new Vector3(-r, 0, 0), new Vector3(0, 0, -r) };
            for (int i = 0; i < 4; i++) { b.Tri(top, e[(i + 1) % 4], e[i]); b.Tri(bot, e[i], e[(i + 1) % 4]); }
            return b.Make("Octahedron");
        }
        /// <summary>Cone le long de +Z (flammes, projectiles), base en z=0.</summary>
        public static Mesh ConeZ(float r, float len, int sides = 6)
        {
            var b = new B(); Vector3 tip = Vector3.forward * len;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * r, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * r;
                b.Tri(p0, p1, tip); b.Tri(Vector3.zero, p1, p0);
            }
            return b.Make("ConeZ");
        }
        /// <summary>Fuseau allonge le long de +Z centre (tirs laser).</summary>
        public static Mesh Spindle(float r, float len, int sides = 6)
        {
            var b = new B(); Vector3 f = Vector3.forward * len * 0.5f, k = -f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * r, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * r;
                b.Tri(p0, p1, f); b.Tri(k, p1, p0);
            }
            return b.Make("Spindle");
        }
        /// <summary>Grille plane XZ subdivisee, centree (mer, lave).</summary>
        public static Mesh Grid(float sizeX, float sizeZ, int nx, int nz)
        {
            var m = new Mesh { name = "Grid" };
            var v = new Vector3[(nx + 1) * (nz + 1)]; var uv = new Vector2[v.Length]; var t = new int[nx * nz * 6];
            for (int j = 0; j <= nz; j++)
                for (int i = 0; i <= nx; i++)
                {
                    v[j * (nx + 1) + i] = new Vector3(-sizeX / 2 + sizeX * i / nx, 0, -sizeZ / 2 + sizeZ * j / nz);
                    uv[j * (nx + 1) + i] = new Vector2(i / (float)nx, j / (float)nz);
                }
            int k = 0;
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d;
                }
            if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = v; m.uv = uv; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        public static GameObject Spawn(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Quaternion rot, bool collider = false, int layer = 0)
        {
            var go = new GameObject(name); go.layer = layer;
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
