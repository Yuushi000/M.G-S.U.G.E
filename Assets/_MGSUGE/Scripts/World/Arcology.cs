using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Arcologie Vesta (cahier §6.2), version low-poly de premier jalon : fleche centrale, anneaux, balise,
    /// gyroscopes, halo, 51 tours en 4 anneaux et plateforme Helios (point de depart).
    /// </summary>
    public class Arcology : MonoBehaviour
    {
        public WorldData world;
        public Material structure, structureLight, accentOrange, accentRose, beacon, redLight, platform;
        public int towerSeed = 4242;
        readonly List<Transform> spinning = new List<Transform>();
        readonly List<float> spinSpeed = new List<float>();
        readonly List<Renderer> blinkers = new List<Renderer>();
        Renderer beaconR; MaterialPropertyBlock mpb;

        void Awake()
        {
            float ground = world.centerHeight;
            var root = transform;
            // fleche centrale : 780 m, rayon 48 -> 16 m
            var spire = MeshKit.Spawn("Fleche_Centrale", MeshKit.Frustum(48f, 16f, 780f, 12), structure, root, new Vector3(0, ground, 0), Quaternion.identity, true, Layers.World);
            for (int i = 0; i < 6; i++)
            {
                float h = 110f + i * 115f, r = Mathf.Lerp(48f, 16f, h / 780f) + 3f;
                MeshKit.Spawn($"Fleche_Anneau_{i + 1}", MeshKit.Torus(r, 2.2f, 32, 6), accentOrange, spire.transform, new Vector3(0, h, 0), Quaternion.identity);
            }
            var b = MeshKit.Spawn("Fleche_Balise", MeshKit.Sphere(9f, 1), beacon, spire.transform, new Vector3(0, 792f, 0), Quaternion.identity);
            beaconR = b.GetComponent<Renderer>();
            // 3 gyroscopes (rayon 80 m, 470-530 m)
            for (int i = 0; i < 3; i++)
            {
                var g = MeshKit.Spawn($"Gyroscope_{i + 1}", MeshKit.Torus(80f, 3.5f, 40, 6), structureLight, root, new Vector3(0, ground + 470f + i * 30f, 0), Quaternion.Euler(i * 55f, i * 40f, 0));
                spinning.Add(g.transform); spinSpeed.Add(9f + i * 4f);
            }
            // halo : 430 m d'altitude, rayon 600 m, 16 modules, 4 rayons
            var halo = new GameObject("Halo").transform; halo.SetParent(root, false); halo.localPosition = new Vector3(0, ground + 430f, 0);
            MeshKit.Spawn("Halo_Anneau", MeshKit.Torus(600f, 7f, 96, 6), structure, halo, Vector3.zero, Quaternion.identity);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                MeshKit.Spawn($"Halo_Module_{i + 1}", MeshKit.Box(new Vector3(34, 18, 34)), i % 2 == 0 ? structureLight : structure, halo,
                    new Vector3(Mathf.Cos(a) * 600f, -9f, Mathf.Sin(a) * 600f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
            for (int i = 0; i < 4; i++)
                MeshKit.Spawn($"Halo_Rayon_{i + 1}", MeshKit.Box(new Vector3(6, 4, 1200)), structure, halo, new Vector3(0, -2, 0), Quaternion.Euler(0, i * 45f, 0));
            spinning.Add(halo); spinSpeed.Add(1.2f);
            // 51 tours : anneaux 190/330/470/640 m, 7/11/15/18 tours
            var rng = new System.Random(towerSeed);
            int[] counts = { 7, 11, 15, 18 }; float[] radii = { 190, 330, 470, 640 };
            Vector2[] hr = { new Vector2(240, 440), new Vector2(140, 300), new Vector2(70, 190), new Vector2(40, 110) };
            var inner = new List<Vector3>();
            Vector2 helios = new Vector2(world.heliosPosition.x, world.heliosPosition.z);
            for (int ring = 0; ring < 4; ring++)
                for (int i = 0; i < counts[ring]; i++)
                {
                    float a = (i + (ring % 2) * 0.5f) * Mathf.PI * 2f / counts[ring];
                    Vector3 p = new Vector3(Mathf.Cos(a) * radii[ring], ground, Mathf.Sin(a) * radii[ring]);
                    while (Vector2.Distance(new Vector2(p.x, p.z), helios) < 120f)   // laisse la plateforme degagee
                    {
                        a += 0.04f; p = new Vector3(Mathf.Cos(a) * radii[ring], ground, Mathf.Sin(a) * radii[ring]);
                    }
                    float h = Mathf.Lerp(hr[ring].x, hr[ring].y, (float)rng.NextDouble());
                    float w = Mathf.Lerp(26f, 40f, (float)rng.NextDouble());
                    var t = MeshKit.Spawn($"Tour_{ring + 1}_{i + 1}", MeshKit.Box(new Vector3(w, h, w)), structure, root, p, Quaternion.Euler(0, -a * Mathf.Rad2Deg + 45f, 0), false, Layers.World);
                    var bc = t.AddComponent<BoxCollider>(); bc.center = new Vector3(0, h / 2, 0); bc.size = new Vector3(w, h, w);
                    var acc = (ring + i) % 3 == 0 ? accentRose : accentOrange;
                    for (int k = 0; k < 4; k++)  // liseres lumineux aux aretes
                    {
                        float ka = k * 90f * Mathf.Deg2Rad;
                        MeshKit.Spawn("Liseret", MeshKit.Box(new Vector3(1.2f, h * 0.92f, 1.2f)), acc, t.transform,
                            new Vector3(Mathf.Cos(ka) * w * 0.5f, h * 0.04f, Mathf.Sin(ka) * w * 0.5f) * 1f, Quaternion.identity);
                    }
                    for (int f = 1; f < Mathf.FloorToInt(h / 40f); f++)  // bandeaux de fenetres
                        MeshKit.Spawn("Fenetres", MeshKit.Box(new Vector3(w + 0.6f, 2.2f, w + 0.6f)), acc, t.transform, new Vector3(0, f * 40f, 0), Quaternion.identity);
                    var ant = MeshKit.Spawn("Antenne", MeshKit.Frustum(1.2f, 0.4f, 24f, 6), structure, t.transform, new Vector3(0, h, 0), Quaternion.identity);
                    var red = MeshKit.Spawn("Feu_Rouge", MeshKit.Sphere(2.2f, 0), redLight, ant.transform, new Vector3(0, 25f, 0), Quaternion.identity);
                    blinkers.Add(red.GetComponent<Renderer>());
                    if (ring == 0) inner.Add(p + Vector3.up * h * 0.55f);
                }
            // passerelles entre les 7 tours interieures
            for (int i = 0; i < inner.Count; i++)
            {
                Vector3 a = inner[i], c = inner[(i + 1) % inner.Count];
                var mid = (a + c) / 2f; float len = Vector3.Distance(a, c);
                var pw = MeshKit.Spawn($"Passerelle_{i + 1}", MeshKit.Box(new Vector3(8, 4, len)), structureLight, root, mid - Vector3.up * 2f, Quaternion.LookRotation(c - a), false, Layers.World);
                var bc = pw.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 2, 0); bc.size = new Vector3(8, 4, len);
            }
            // plateforme Helios : disque de 42 m, dessus a 14 m
            var hp = world.heliosPosition;
            var plat = MeshKit.Spawn("Plateforme_Helios", MeshKit.Frustum(world.heliosRadius, world.heliosRadius, hp.y + 30f, 24), platform, root, new Vector3(hp.x, -30f, hp.z), Quaternion.identity, true, Layers.World);
            MeshKit.Spawn("Helios_Anneau", MeshKit.Torus(world.heliosRadius - 2f, 0.8f, 48, 4), accentOrange, plat.transform, new Vector3(0, hp.y + 30f + 0.2f, 0), Quaternion.identity);
            MeshKit.Spawn("Helios_Cercle", MeshKit.Torus(14f, 0.5f, 32, 4), accentRose, plat.transform, new Vector3(0, hp.y + 30f + 0.1f, 0), Quaternion.identity);
            mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            float dt = GameClock.WorldDt;
            for (int i = 0; i < spinning.Count; i++) spinning[i].Rotate(0f, spinSpeed[i] * dt, 0f, Space.Self);
            float t = Time.time;
            bool on = Mathf.Repeat(t, 1.6f) < 0.5f;
            foreach (var r in blinkers) if (r != null) r.enabled = on;
            if (beaconR != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
                beaconR.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.15f) * Mathf.Lerp(2f, 9f, k));
                beaconR.SetPropertyBlock(mpb);
            }
        }
    }
}
