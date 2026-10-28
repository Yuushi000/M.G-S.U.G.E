using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    [Serializable]
    public class ZoneInfo
    {
        public string name;
        public Vector2 center;      // (x ; z)
        public float radius;
        public bool edge;           // zone de bord (Glacier, Caldeira, Marais, Erg)
        [Tooltip("couleur de base du sol (RVB lineaire)")] public Color ground = new Color(0.11f, 0.085f, 0.10f);
    }

    /// <summary>Le monde (cahier §6) : dimensions, zones, terrain, mer, lave, ciel, lumieres.</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Monde", fileName = "MGSUGE_World")]
    public class WorldData : ScriptableObject
    {
        [Header("6.1 Dimensions")]
        public float worldSize = 15000f;
        public float playableLimit = 7440f;
        public float seaLevel = -42f;
        public float floorMin = -43.4f;
        public float lavaLevel = -16f;
        [Tooltip("rectangle de lave : x > lavaMinX et |z| < lavaHalfZ")] public float lavaMinX = 5050f;
        public float lavaHalfZ = 3100f;
        public Vector2 lavaPlaneCenter = new Vector2(6275f, 0f);
        public Vector2 lavaPlaneSize = new Vector2(2450f, 6200f);
        public float ceiling = 12000f;
        public float spaceY = 8000f;
        public float viewDistance = 32000f;

        [Header("4.2 Depart : plateforme Helios")]
        public Vector3 heliosPosition = new Vector3(0f, 14f, 440f);
        public float heliosRadius = 42f;
        public long startCredits = 2000;

        [Header("6.3 Terrain (heightmap 513 x 513)")]
        public int heightmapResolution = 513;
        [Tooltip("fichier .raw 16 bits (ordre Windows) dans StreamingAssets/MGSUGE. Le min et le max sont lus dans le nom : heightmap_513_min<min>_max<max>.raw")]
        public string heightmapFile = "";
        public bool heightmapFlipX = false, heightmapFlipZ = false;
        [Tooltip("hauteur imposee en (0;0) par le generateur provisoire")] public float centerHeight = 10f;
        public float wallStart = 6750f, wallEnd = 7400f, wallHeight = 520f;
        [Tooltip("variation aleatoire de couleur par bloc de 23 m")] public float colorVariation = 0.18f;
        public float colorBlock = 23f;
        public Color rockColor = new Color(0.16f, 0.15f, 0.16f);
        public Color snowColor = new Color(0.62f, 0.62f, 0.68f);
        public Color seabedColor = new Color(0.03f, 0.05f, 0.06f);
        public Vector2 rockHeight = new Vector2(190f, 420f), snowHeight = new Vector2(560f, 760f);
        public int seed = 90210;

        [Header("6.2 Zones")]
        public List<ZoneInfo> zones = new List<ZoneInfo>();

        [Header("6.2 Noms de secteur selon l'altitude")]
        public float orbitSectorAlt = 6200f, highAtmoAlt = 3200f, anchorAlt = 1900f, beltMinAlt = 520f, beltMinAboveGround = 300f;

        [Header("6.5 Lumieres et atmosphere")]
        public Vector3 sunDirection = new Vector3(0.62f, 0.13f, -0.77f);
        public Color sunColor = new Color32(0xFF, 0x9A, 0x66, 0xFF);
        public float sunIntensity = 2.3f;
        public Vector3 fillDirection = new Vector3(-0.6f, 0.35f, 0.7f);
        public Color fillColor = new Color32(0x8C, 0x90, 0xFF, 0xFF);
        public float fillIntensity = 0.45f;
        public Color hemiSky = new Color32(0xA7, 0x7F, 0xB0, 0xFF);
        public Color hemiGround = new Color32(0x2A, 0x18, 0x24, 0xFF);
        public float hemiIntensity = 0.72f;
        public Color lavaLightColor = new Color32(0xFF, 0x6A, 0x2A, 0xFF);
        public float lavaLightIntensity = 2.4f, lavaLightRange = 3200f;
        public Vector3 lavaLightPosition = new Vector3(6100f, 220f, 0f);
        [Tooltip("RVB lineaire, deja multiplie par 1,25")] public Color fogColor = new Color(0.625f, 0.25f, 0.2875f);
        public float fogDensity = 0.00019f;
        public float spaceFogStart = 3600f, spaceFogEnd = 7200f;
        [Range(0, 1)] public float spaceFogReduction = 0.72f;
        public Color spaceFogColor = new Color(0.025f, 0.015f, 0.045f);

        [Header("6.5 Ciel")]
        public Color skyZenith = new Color(0.03f, 0.026f, 0.085f);
        public Color skyMid = new Color(0.24f, 0.075f, 0.24f);
        public Color skyHorizonSun = new Color(1.05f, 0.36f, 0.12f);
        public Color skyHorizonOpposite = new Color(0.62f, 0.17f, 0.30f);
        public int smallStars = 6500, bigStars = 600;
        public Vector3 planetDirection = new Vector3(-0.62f, 0.36f, -0.70f);
        public float planetRadius = 2700f, planetDistance = 12500f;
        public float ringInner = 3500f, ringOuter = 6300f;
        public Vector2 ringTilt = new Vector2(1.22f, 0.35f);
        public Vector3 moonDirection = new Vector3(0.7f, 0.5f, 0.5f);
        public float moonRadius = 420f, moonDistance = 12500f;

        [Header("6.4 Mer")]
        public float seaWaveAmplitude = 5.8f;
        public Color seaColor = new Color(0.02f, 0.06f, 0.07f);

        /// <summary>Remplit les 13 zones du cahier (§6.2) avec les couleurs de biome (§6.3).</summary>
        public void ResetZones()
        {
            zones = new List<ZoneInfo>
            {
                Z("Arcologie Vesta", 0, 0, 950, false, 0.11f, 0.11f, 0.13f),
                Z("Champ de quartz de Lyra", 0, -3200, 1100, false, 0.07f, 0.10f, 0.15f),
                Z("Cratère de Kessler", 3200, 400, 1050, false, 0.17f, 0.09f, 0.065f),
                Z("Crêtes de l'Ascenseur", -3200, 300, 1100, false, 0.055f, 0.055f, 0.07f),
                Z("Épave du Thalassa", 500, 3200, 1100, false, 0.20f, 0.16f, 0.12f),
                Z("Raffinerie d'Orsk", 3500, -3500, 950, false, 0.09f, 0.085f, 0.08f),
                Z("Réseau Tessera", -3500, -3500, 1000, false, 0.12f, 0.13f, 0.11f),
                Z("Archipel de Néréide", 3500, 3500, 1150, false, 0.12f, 0.15f, 0.11f),
                Z("Spatioport Hélix", -3500, 3500, 1000, false, 0.13f, 0.13f, 0.14f),
                Z("Glacier de Borée", 0, -6300, 1800, true, 0.42f, 0.47f, 0.58f),
                Z("Caldeira d'Ignis", 6300, 0, 1800, true, 0.05f, 0.042f, 0.045f),
                Z("Marais de Nyx", 0, 6300, 1800, true, 0.04f, 0.085f, 0.075f),
                Z("Erg de Sel", -6300, 0, 1800, true, 0.42f, 0.32f, 0.21f),
            };
        }
        static ZoneInfo Z(string n, float x, float z, float r, bool edge, float cr, float cg, float cb)
            => new ZoneInfo { name = n, center = new Vector2(x, z), radius = r, edge = edge, ground = new Color(cr, cg, cb) };

        public bool InLavaRect(float x, float z) => x > lavaMinX && Mathf.Abs(z) < lavaHalfZ;

        /// <summary>Nom de secteur affiche en haut a droite (§6.2).</summary>
        public string SectorName(Vector3 p, float groundHeight, string hangar = null)
        {
            if (!string.IsNullOrEmpty(hangar)) return "Hangar · " + hangar;
            if (p.y > orbitSectorAlt) return "Orbite haute · bataille orbitale";
            if (p.y > highAtmoAlt) return "Haute atmosphère";
            if (p.y > anchorAlt) return "Station orbitale Anchor";
            if (p.y > beltMinAlt && p.y < anchorAlt && p.y - groundHeight > beltMinAboveGround) return "Ceinture flottante";
            ZoneInfo best = null; float bestD = float.MaxValue;
            var xz = new Vector2(p.x, p.z);
            foreach (var z in zones)
                if (z.edge && Vector2.Distance(xz, z.center) < z.radius) return z.name;
            foreach (var z in zones)
            {
                if (z.edge) continue;
                float d = Vector2.Distance(xz, z.center);
                if (d < bestD) { bestD = d; best = z; }
            }
            return best != null ? best.name : "";
        }
    }
}
