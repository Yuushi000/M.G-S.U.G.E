using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>Maillages a facettes generes une fois et enregistres dans Art/Generated/MGSUGE_Maillages.asset.</summary>
    public static partial class MGSUGEBuilder
    {
        const string MeshAssetPath = GenDir + "/MGSUGE_Maillages.asset";

        public class Meshes : Dictionary<string, Mesh> { }

        static Dictionary<string, Mesh> meshCache;
        /// <summary>Met a jour (ou cree) le maillage nomme dans l'asset commun, en gardant sa reference.</summary>
        static Mesh StoreMesh(string name, Mesh src)
        {
            if (meshCache == null)
            {
                EnsureFolder(GenDir);
                if (AssetDatabase.LoadMainAssetAtPath(MeshAssetPath) == null)
                    AssetDatabase.CreateAsset(new Mesh { name = "MGSUGE_Maillages" }, MeshAssetPath);
                meshCache = new Dictionary<string, Mesh>();
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(MeshAssetPath)) if (o is Mesh mm && mm.name != "MGSUGE_Maillages") meshCache[mm.name] = mm;
            }
            src.name = name;
            if (meshCache.TryGetValue(name, out var dst))
            {
                dst.Clear();
                dst.indexFormat = src.indexFormat;
                dst.vertices = src.vertices; dst.normals = src.normals; dst.uv = src.uv; dst.colors = src.colors;
                dst.subMeshCount = 1; dst.SetIndices(src.GetIndices(0), src.GetTopology(0), 0);
                dst.bounds = src.bounds;
                EditorUtility.SetDirty(dst);
                Object.DestroyImmediate(src);
                return dst;
            }
            AssetDatabase.AddObjectToAsset(src, MeshAssetPath);
            meshCache[name] = src;
            return src;
        }

        /// <summary>Recharge les maillages deja enregistres (apres l'ouverture d'une nouvelle scene).</summary>
        static Meshes LoadMeshes()
        {
            meshCache = null;
            var m = new Meshes();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(MeshAssetPath)) if (o is Mesh mm && mm.name != "MGSUGE_Maillages") m[mm.name] = mm;
            return m;
        }

        static Meshes BuildMeshes(Data d)
        {
            meshCache = null;
            var m = new Meshes();
            void Add(string n, Mesh mesh) => m[n] = StoreMesh(n, mesh);
            // primitives unitaires (pose sur y = 0 sauf mention)
            Add("Boite", MeshKit.Box(Vector3.one));
            Add("Cylindre_6", MeshKit.Frustum(1f, 1f, 1f, 6));
            Add("Cylindre_8", MeshKit.Frustum(1f, 1f, 1f, 8));
            Add("Cylindre_12", MeshKit.Frustum(1f, 1f, 1f, 12));
            Add("Cylindre_24", MeshKit.Frustum(1f, 1f, 1f, 24));
            Add("Cone_6", MeshKit.Frustum(1f, 1f / 3f, 1f, 6));
            Add("Sphere_0", MeshKit.Sphere(1f, 0)); Add("Sphere_1", MeshKit.Sphere(1f, 1)); Add("Sphere_2", MeshKit.Sphere(1f, 2)); Add("Sphere_3", MeshKit.Sphere(1f, 3));
            Add("Octaedre_Sentinelle", MeshKit.Octahedron(1f, 1.3f));
            Add("Octaedre_Relais", MeshKit.Octahedron(1f, 1.4f));
            Add("Disque", MeshKit.Annulus(0f, 1f, 48));
            Add("Telegraphe_Anneau", MeshKit.Annulus(0.92f, 1f, 64));
            // effets
            Add("Tir_Fuseau", MeshKit.Spindle(0.28f, 1f, 6));
            Add("Missile_Cone", MeshKit.ConeZ(0.32f, 2.4f, 6));
            // ennemis
            Add("Tore_Sentinelle", MeshKit.Torus(4.6f, 0.35f, 24, 4));
            Add("Tore_Relais_1", MeshKit.Torus(11f, 0.6f, 32, 4));
            Add("Tore_Relais_2", MeshKit.Torus(13f, 0.6f, 32, 4));
            Add("Faucon_Fuselage", FighterHull());
            // monde
            Add("Fleche_Centrale", MeshKit.Frustum(48f, 16f, 780f, 12));
            Add("Mer_Grille", MeshKit.Grid(40000f, 40000f, 128, 128));
            Add("Lave_Grille", MeshKit.Grid(d.world.lavaPlaneSize.x, d.world.lavaPlaneSize.y, 24, 60));
            Add("Anneau_Planete", MeshKit.Annulus(d.world.ringInner, d.world.ringOuter, 128));
            Add("Etoiles", Stars(d.world));
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Tore aux dimensions exactes (anneaux de l'arcologie, du halo, des gyroscopes...).</summary>
        static Mesh Torus(Meshes m, float R, float r, int seg, int side)
        {
            string n = $"Tore_{R:0.#}_{r:0.#}_{seg}_{side}".Replace(',', '.');
            if (!m.TryGetValue(n, out var mesh)) m[n] = mesh = StoreMesh(n, MeshKit.Torus(R, r, seg, side));
            return mesh;
        }

        /// <summary>Fuselage du chasseur « Faucon » : fleche a facettes de 14 m, ailes en delta de 12 m, derives.</summary>
        static Mesh FighterHull()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c) { int k = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2); }
            void Both(Vector3 a, Vector3 b, Vector3 c) { Tri(a, b, c); Tri(a, c, b); }
            Vector3 nose = new Vector3(0, 0, 7.5f), tailT = new Vector3(0, 1.4f, -6f), tailB = new Vector3(0, -1f, -6f);
            Vector3 l = new Vector3(-1.6f, 0.2f, -2f), r = new Vector3(1.6f, 0.2f, -2f);
            // coque
            Tri(nose, l, tailT); Tri(nose, tailT, r); Tri(nose, tailB, l); Tri(nose, r, tailB);
            Tri(l, tailB, tailT); Tri(r, tailT, tailB);
            // ailes en delta (double face)
            Both(new Vector3(-1.2f, 0f, 1.5f), new Vector3(-6.5f, -0.3f, -4.5f), new Vector3(-1.2f, 0f, -4.8f));
            Both(new Vector3(1.2f, 0f, 1.5f), new Vector3(1.2f, 0f, -4.8f), new Vector3(6.5f, -0.3f, -4.5f));
            // derives
            Both(new Vector3(-0.8f, 0.8f, -3.5f), new Vector3(-1.6f, 3.2f, -6.2f), new Vector3(-0.8f, 0.8f, -6f));
            Both(new Vector3(0.8f, 0.8f, -3.5f), new Vector3(0.8f, 0.8f, -6f), new Vector3(1.6f, 3.2f, -6.2f));
            var mesh = new Mesh { name = "Faucon" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>6 500 petites et 600 grosses etoiles (teintes orangee 15 %, bleutee 25 %, blanche) sur une sphere de 15 km.</summary>
        static Mesh Stars(WorldData w)
        {
            int n = w.smallStars + w.bigStars;
            var v = new Vector3[n]; var c = new Color[n]; var idx = new int[n];
            var rng = new System.Random(w.seed + 1);
            for (int i = 0; i < n; i++)
            {
                Vector3 dd;
                do { dd = new Vector3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1); } while (dd.sqrMagnitude > 1f || dd.sqrMagnitude < 0.01f);
                dd.Normalize(); if (dd.y < -0.15f) dd.y = -dd.y;
                v[i] = dd * 15000f;
                double r = rng.NextDouble();
                Color col = r < 0.15 ? new Color(1f, 0.75f, 0.5f) : r < 0.40 ? new Color(0.65f, 0.78f, 1f) : Color.white;
                float b = i < w.smallStars ? 0.35f + (float)rng.NextDouble() * 0.4f : 0.9f + (float)rng.NextDouble() * 0.6f;
                c[i] = col * b; c[i].a = 1f; idx[i] = i;
            }
            var mesh = new Mesh { name = "Etoiles" };
            mesh.vertices = v; mesh.colors = c; mesh.SetIndices(idx, MeshTopology.Points, 0); mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 40000f);
            return mesh;
        }
    }
}
