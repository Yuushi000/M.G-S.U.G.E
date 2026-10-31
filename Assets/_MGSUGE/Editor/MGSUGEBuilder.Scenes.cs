using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>Scenes World (monde complet, contrats, avant-postes) et Test (terrain plat, cibles et un ennemi de chaque type).</summary>
    public static partial class MGSUGEBuilder
    {
        class SceneRefs
        {
            public Transform managers, lights, player, world, enemies, contracts, outposts, bosses, ui, patrolsParent;
            public PlayerController pc; public GameManager gm; public ContractManager cm; public PatrolSpawner ps; public Transform spawn;
        }

        // ------------------------------------------------------------------ elements communs aux deux scenes
        static SceneRefs CommonScene(Data d, Mats m, Meshes me, Prefabs pf, Vector3 spawnPos, string startTitle, string startSub)
        {
            var r = new SceneRefs();
            r.managers = Group("Managers"); r.lights = Group("Lights"); r.player = Group("Player"); r.world = Group("World");
            r.enemies = Group("Enemies"); r.contracts = Group("Contracts"); r.outposts = Group("Outposts"); r.bosses = Group("Bosses"); r.ui = Group("UI");
            r.patrolsParent = Group("Patrouilles", r.enemies);
            Group("(jalon 7 : boss errants)", r.bosses);

            // atmosphere de la scene (fenetre Lighting) : ambiance tri-couleur, brouillard exponentiel carre, ciel
            var w = d.world;
            RenderSettings.skybox = m["Sky"];
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = w.hemiSky * w.hemiIntensity;
            RenderSettings.ambientEquatorColor = Color.Lerp(w.hemiSky, w.hemiGround, 0.5f) * w.hemiIntensity;
            RenderSettings.ambientGroundColor = w.hemiGround * w.hemiIntensity;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = w.fogColor.gamma; RenderSettings.fogDensity = w.fogDensity;

            // lumieres (§6.5)
            var sun = new GameObject("Lumiere_Soleil").AddComponent<Light>(); sun.transform.SetParent(r.lights, false);
            sun.type = LightType.Directional; sun.color = w.sunColor; sun.intensity = w.sunIntensity; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.LookRotation(-w.sunDirection.normalized);
            RenderSettings.sun = sun;
            var fill = new GameObject("Lumiere_Remplissage").AddComponent<Light>(); fill.transform.SetParent(r.lights, false);
            fill.type = LightType.Directional; fill.color = w.fillColor; fill.intensity = w.fillIntensity; fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.LookRotation(-w.fillDirection.normalized);
            var vol = new GameObject("Volume_Global").AddComponent<Volume>(); vol.transform.SetParent(r.lights, false);
            vol.isGlobal = true; vol.sharedProfile = PostProfile();

            // joueur et camera
            r.spawn = Group("Point_Depart", r.player); r.spawn.position = spawnPos;
            var player = (GameObject)PrefabUtility.InstantiatePrefab(pf.player, r.player);
            player.transform.position = spawnPos + Vector3.up * (d.frame.frameClear + 0.5f); Rec(player);
            r.pc = player.GetComponent<PlayerController>(); r.pc.spawnPoint = r.spawn;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r.pc);
            var camGo = new GameObject("Camera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(r.player, false);
            var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rig = camGo.AddComponent<CameraRig>(); rig.data = d.cam; rig.player = r.pc; rig.cam = cam;
            camGo.transform.position = player.transform.position + new Vector3(3f, 2f, -21f);
            r.pc.cam = rig; PrefabUtility.RecordPrefabInstancePropertyModifications(r.pc);

            // gestionnaires
            r.gm = new GameObject("GameManager").AddComponent<GameManager>(); r.gm.transform.SetParent(r.managers, false);
            r.gm.player = r.pc; r.gm.startTitle = startTitle; r.gm.startSubtitle = startSub;
            var fxGo = new GameObject("FX"); fxGo.transform.SetParent(r.managers, false);
            var fx = fxGo.AddComponent<FxSystem>(); fx.flashPrefab = pf.flash; fx.beamPrefab = pf.beam;
            fx.particles = BuildParticles(fxGo.transform, m);
            var proj = fxGo.AddComponent<ProjectileSystem>(); proj.data = d.weapons; proj.projectilePrefab = pf.projectile;
            var mis = fxGo.AddComponent<MissileSystem>(); mis.data = d.weapons; mis.missilePrefab = pf.missile;
            var sh = fxGo.AddComponent<ShellSystem>(); sh.shellPrefab = pf.shell; sh.telegraphPrefab = pf.telegraph;
            fxGo.AddComponent<SfxSystem>();
            var psGo = new GameObject("Patrouilles"); psGo.transform.SetParent(r.managers, false);
            r.ps = psGo.AddComponent<PatrolSpawner>(); r.ps.data = d.patrols; r.ps.player = r.pc; r.ps.world = d.world;
            r.ps.sentinelPrefab = pf.sentinel; r.ps.fighterPrefab = pf.fighter; r.ps.spawnParent = r.patrolsParent; r.ps.helios = r.spawn;
            r.gm.patrols = r.ps;
            var cmGo = new GameObject("Contrats"); cmGo.transform.SetParent(r.managers, false);
            r.cm = cmGo.AddComponent<ContractManager>(); r.cm.campaign = d.campaign; r.cm.player = r.pc;

            // interface : construite par BuildInterface a la fin de chaque scene (MGSUGEBuilder.UIScene.cs)
            return r;
        }

        static VolumeProfile PostProfile()
        {
            string profPath = DataDir + "/MGSUGE_PostProcess.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profPath);
            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, profPath);
                var tmap = prof.Add<Tonemapping>(true); tmap.mode.Override(TonemappingMode.ACES);
                var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.7f); bloom.threshold.Override(1.0f);
                foreach (var c in prof.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, prof); }
                AssetDatabase.SaveAssets();
            }
            return prof;
        }

        /// <summary>Nuage de particules additif partage (etincelles, poussiere, explosions), configure une fois dans la scene.</summary>
        static ParticleSystem BuildParticles(Transform parent, Mats m)
        {
            var go = new GameObject("Particules"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = true; main.maxParticles = 20000;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0f; main.startLifetime = 1f; main.gravityModifier = 0f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = m["Particles"]; rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = ShadowCastingMode.Off; rend.receiveShadows = false;
            return ps;
        }

        static void Sky(Transform world, Data d, Mats m, Meshes me, Camera cam)
        {
            var w = d.world;
            var skyGo = new GameObject("Ciel"); skyGo.transform.SetParent(world, false);
            var sky = skyGo.AddComponent<SkySystem>(); sky.world = w; sky.targetCamera = cam;
            var astres = Group("Astres", skyGo.transform);
            astres.gameObject.AddComponent<FollowCamera>().target = cam;
            var planet = Part("Planete", astres, me["Sphere_3"], m["Planet"], w.planetDirection.normalized * w.planetDistance, Vector3.one * w.planetRadius, Quaternion.identity, shadows: false);
            Part("Anneau_Planete", planet.transform, me["Anneau_Planete"], m["Ring"], Vector3.zero, Vector3.one / w.planetRadius,
                Quaternion.Euler(w.ringTilt.x * Mathf.Rad2Deg, 0f, w.ringTilt.y * Mathf.Rad2Deg), shadows: false);
            Part("Lune", astres, me["Sphere_1"], m["Moon"], w.moonDirection.normalized * w.moonDistance, Vector3.one * w.moonRadius, Quaternion.identity, shadows: false);
            var stars = Part("Etoiles", astres, me["Etoiles"], m["Stars"], Vector3.zero, Vector3.one, Quaternion.identity, shadows: false);
            sky.stars = stars.GetComponent<Renderer>();
        }

        // ------------------------------------------------------------------ scene World
        static void BuildWorldScene(UnityEngine.SceneManagement.Scene scene, Data d, Mats m, Meshes me, Prefabs pf)
        {
            EnsureFolder(SceneDir);
            var w = d.world;
            var r = CommonScene(d, m, me, pf, w.heliosPosition, "M.G-S.U.G.E", "Contrats · K : commandes · Échap : menu");
            var cam = r.pc.cam.cam;
            Sky(r.world, d, m, me, cam);

            // terrain : 64 tuiles visibles (maillage calcule depuis la heightmap a l'ouverture)
            var tGo = new GameObject("Terrain"); tGo.transform.SetParent(r.world, false);
            var ts = tGo.AddComponent<TerrainSystem>(); ts.world = w; ts.material = m["Terrain"]; ts.EnsureLoaded();
            var hf = ts.Field;
            int tiles = hf.TileCount(ts.chunkCells);
            for (int cz = 0; cz < tiles; cz++)
                for (int cx = 0; cx < tiles; cx++)
                {
                    var tile = new GameObject($"Tuile_{cx}_{cz}"); tile.layer = Layers.World;
                    tile.transform.SetParent(tGo.transform, false);
                    tile.transform.localPosition = hf.TileOrigin(cx, cz, ts.chunkCells);
                    var tt = tile.AddComponent<TerrainTile>();
                    tile.GetComponent<MeshRenderer>().sharedMaterial = m["Terrain"];
                    tt.terrain = ts; tt.tileX = cx; tt.tileZ = cz; tt.Rebuild();
                }
            // mer et lave
            var sea = Part("Mer", r.world, me["Mer_Grille"], m["Sea"], new Vector3(0f, w.seaLevel, 0f), Vector3.one, Quaternion.identity, shadows: false);
            sea.AddComponent<SeaSurface>().waveAmplitude = w.seaWaveAmplitude;
            var lava = Part("Lave", r.world, me["Lave_Grille"], m["Lava"], new Vector3(w.lavaPlaneCenter.x, w.lavaLevel, w.lavaPlaneCenter.y), Vector3.one, Quaternion.identity, shadows: false);
            lava.AddComponent<LavaSurface>();
            var ll = new GameObject("Lave_Lumiere").AddComponent<Light>(); ll.transform.SetParent(lava.transform, true);
            ll.type = LightType.Point; ll.color = w.lavaLightColor; ll.intensity = w.lavaLightIntensity; ll.range = w.lavaLightRange;
            ll.transform.position = w.lavaLightPosition;

            Arcology(r.world, d, m, me, pf);
            var zones = ZoneProps(r.world, d, m, me, hf);
            Contracts(r, d, pf, hf, zones);
            Outposts(r, d, pf, hf);

            r.gm.startOnTitle = true;
            BuildInterface(r.ui, d, LoadUiAssets(d), r.pc, r.gm);
            EditorSceneManager.SaveScene(scene, WorldScenePath);
        }

        // ------------------------------------------------------------------ arcologie Vesta (§6.2)
        static void Arcology(Transform world, Data d, Mats m, Meshes me, Prefabs pf)
        {
            var w = d.world; float ground = w.centerHeight; Quaternion I = Quaternion.identity;
            var root = Group("Arcologie_Vesta", world);
            // fleche centrale : 780 m, rayon 48 -> 16 m, 6 anneaux lumineux, balise orange pulsante
            var spire = Part("Fleche_Centrale", root, me["Fleche_Centrale"], m["Structure"], new Vector3(0f, ground, 0f), Vector3.one, I, true, Layers.World);
            for (int i = 0; i < 6; i++)
            {
                float h = 110f + i * 115f, rr = Mathf.Lerp(48f, 16f, h / 780f) + 3f;
                Part($"Anneau_{i + 1}", spire.transform, Torus(me, rr, 2.2f, 32, 6), m["AccentOrange"], new Vector3(0f, h, 0f), Vector3.one, I, false, Layers.World, false);
            }
            var beacon = Part("Balise", spire.transform, me["Sphere_1"], m["Beacon"], new Vector3(0f, 792f, 0f), Vector3.one * 9f, I, false, Layers.World, false);
            var pulse = beacon.AddComponent<EmissionPulse>(); pulse.target = beacon.GetComponent<Renderer>();
            // 3 gyroscopes de 80 a 132 m de rayon, a 470-530 m
            var gyros = Group("Gyroscopes", root);
            float[] gr = { 80f, 106f, 132f };
            for (int i = 0; i < 3; i++)
            {
                var g = Part($"Gyroscope_{i + 1}", gyros, Torus(me, gr[i], 3.5f, 40, 6), m["StructureLight"], new Vector3(0f, ground + 470f + i * 30f, 0f), Vector3.one, Quaternion.Euler(i * 55f, i * 40f, 0f), false, Layers.World);
                var rot = g.AddComponent<Rotator>(); rot.degreesPerSecond = 9f + i * 4f;
            }
            // halo de 430 m de rayon a 600 m d'altitude, 16 modules, 4 rayons, rotation lente
            var halo = Group("Halo", root, new Vector3(0f, ground + 600f, 0f));
            halo.gameObject.AddComponent<Rotator>().degreesPerSecond = 1.2f;
            Part("Halo_Anneau", halo, Torus(me, 430f, 7f, 96, 6), m["Structure"], Vector3.zero, Vector3.one, I, false, Layers.World);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                Part($"Halo_Module_{i + 1}", halo, me["Boite"], i % 2 == 0 ? m["StructureLight"] : m["Structure"],
                    new Vector3(Mathf.Cos(a) * 430f, -9f, Mathf.Sin(a) * 430f), new Vector3(34f, 18f, 34f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), false, Layers.World);
            }
            for (int i = 0; i < 4; i++)
                Part($"Halo_Rayon_{i + 1}", halo, me["Boite"], m["Structure"], new Vector3(0f, -2f, 0f), new Vector3(6f, 4f, 860f), Quaternion.Euler(0f, i * 45f, 0f), false, Layers.World);
            // 51 tours en 4 anneaux (190 / 330 / 470 / 640 m ; 7 / 11 / 15 / 18 tours)
            var towers = Group("Tours", root);
            var rng = new System.Random(4242);
            int[] counts = { 7, 11, 15, 18 }; float[] radii = { 190, 330, 470, 640 };
            Vector2[] hr = { new Vector2(240, 440), new Vector2(140, 300), new Vector2(70, 190), new Vector2(40, 110) };
            var inner = new List<Vector3>();
            Vector2 helios = new Vector2(w.heliosPosition.x, w.heliosPosition.z);
            for (int ring = 0; ring < 4; ring++)
                for (int i = 0; i < counts[ring]; i++)
                {
                    float a = (i + (ring % 2) * 0.5f) * Mathf.PI * 2f / counts[ring];
                    Vector3 p = new Vector3(Mathf.Cos(a) * radii[ring], ground, Mathf.Sin(a) * radii[ring]);
                    while (Vector2.Distance(new Vector2(p.x, p.z), helios) < 120f)
                    { a += 0.04f; p = new Vector3(Mathf.Cos(a) * radii[ring], ground, Mathf.Sin(a) * radii[ring]); }
                    float h = Mathf.Lerp(hr[ring].x, hr[ring].y, (float)rng.NextDouble());
                    float wd = Mathf.Lerp(26f, 40f, (float)rng.NextDouble());
                    var t = Instance(pf.tower, towers, p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 45f, 0f), $"Tour_{ring + 1}_{i + 1}");
                    var tw = t.GetComponent<ArcologyTower>();
                    tw.width = wd; tw.height = h; tw.accent = (ring + i) % 3 == 0 ? m["AccentRose"] : m["AccentOrange"];
                    tw.Layout();
                    var bl = t.GetComponentInChildren<Blinker>(); if (bl != null) { bl.phase = (float)rng.NextDouble() * 1.6f; PrefabUtility.RecordPrefabInstancePropertyModifications(bl); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tw);
                    foreach (var tr in t.GetComponentsInChildren<Transform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(tr);
                    foreach (var rd in t.GetComponentsInChildren<Renderer>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(rd);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t.GetComponent<BoxCollider>());
                    if (ring == 0) inner.Add(p + Vector3.up * h * 0.55f);
                }
            // passerelles entre les 7 tours interieures
            var walks = Group("Passerelles", root);
            for (int i = 0; i < inner.Count; i++)
            {
                Vector3 a = inner[i], c = inner[(i + 1) % inner.Count];
                var pw = Part($"Passerelle_{i + 1}", walks, me["Boite"], m["StructureLight"], (a + c) / 2f - Vector3.up * 2f, new Vector3(8f, 4f, Vector3.Distance(a, c)), Quaternion.LookRotation(c - a), false, Layers.World);
                var bc = pw.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 0.5f, 0f); bc.size = Vector3.one;
            }
            // plateforme Helios : disque de 42 m, dessus a 14 m (point de depart)
            var hp = w.heliosPosition;
            var plat = Group("Plateforme_Helios", root, new Vector3(hp.x, 0f, hp.z));
            Part("Socle", plat, me["Cylindre_24"], m["Platform"], new Vector3(0f, -30f, 0f), new Vector3(w.heliosRadius, hp.y + 30f, w.heliosRadius), I, true, Layers.World);
            Part("Anneau_Exterieur", plat, Torus(me, w.heliosRadius - 2f, 0.8f, 48, 4), m["AccentOrange"], new Vector3(0f, hp.y + 0.2f, 0f), Vector3.one, I, false, Layers.World, false);
            Part("Cercle_Central", plat, Torus(me, 14f, 0.5f, 32, 4), m["AccentRose"], new Vector3(0f, hp.y + 0.1f, 0f), Vector3.one, I, false, Layers.World, false);
        }

        // ------------------------------------------------------------------ decors provisoires des zones de contrat
        class ZoneAnchors
        {
            public List<Vector3> pylonTops = new List<Vector3>(), hullTops = new List<Vector3>(), cruiserDecks = new List<Vector3>(), stationTops = new List<Vector3>();
            public Vector3 elevatorFoot, anchorStation;
        }
        static ZoneAnchors ZoneProps(Transform world, Data d, Mats m, Meshes me, HeightField hf)
        {
            var z = new ZoneAnchors(); Quaternion I = Quaternion.identity;
            var root = Group("Zones (decors provisoires)", world);
            float G(float x, float zz) => hf.GroundH(x, zz);

            // Cratere de Kessler : 8 pylones de 90 m (rayon 480 m) a sommet bleu
            var kes = Group("Cratere_Kessler", root);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f; float x = 3200f + Mathf.Cos(a) * 480f, zz = 400f + Mathf.Sin(a) * 480f, g = G(x, zz);
                var py = Group($"Pylone_{i + 1}", kes); py.position = new Vector3(x, g, zz);
                Part("Fut", py, me["Cylindre_8"], m["Structure"], Vector3.zero, new Vector3(4f, 90f, 4f), I, true, Layers.World);
                Part("Sommet", py, me["Sphere_1"], m["PylonGlow"], new Vector3(0f, 92f, 0f), Vector3.one * 3f, I, false, Layers.World, false);
                z.pylonTops.Add(new Vector3(x, g + 90f + 2.6f, zz));
            }
            // Epave du Thalassa : coque de 130 x 80 x 520 m inclinee
            var tha = Group("Epave_Thalassa", root);
            var hull = Part("Coque", tha, me["Boite"], m["Wreck"], new Vector3(500f, G(500f, 3200f) - 28f, 3200f), new Vector3(130f, 80f, 520f), Quaternion.Euler(-6f, 30f, 5f), false, Layers.World);
            var hbc = hull.AddComponent<BoxCollider>(); hbc.center = new Vector3(0f, 0.5f, 0f); hbc.size = Vector3.one;
            foreach (var (xn, zn) in new[] { (-0.25f, -0.3f), (0.25f, 0.05f), (0f, 0.35f) })
                z.hullTops.Add(hull.transform.TransformPoint(new Vector3(xn, 1f, zn)) + Vector3.up * 2.6f);
            // Cretes de l'Ascenseur : base hexagonale, tour, cable jusqu'a 2 450 m, station orbitale Anchor
            var asc = Group("Cretes_Ascenseur", root);
            float eg = G(-3200f, 300f);
            Part("Base", asc, me["Cylindre_6"], m["Structure"], new Vector3(-3200f, eg - 6f, 300f), new Vector3(160f, 30f, 160f), I, true, Layers.World);
            Part("Tour", asc, me["Cylindre_12"], m["StructureLight"], new Vector3(-3200f, eg + 24f, 300f), new Vector3(22f, 140f, 22f), I, true, Layers.World);
            float cableBottom = eg + 164f;
            Part("Cable", asc, me["Cylindre_8"], m["Structure"], new Vector3(-3200f, cableBottom, 300f), new Vector3(5f, 2450f - cableBottom, 5f), I, true, Layers.World);
            var st = Group("Station_Anchor", asc, new Vector3(-3200f, 2450f, 300f));
            Part("Cylindre", st, me["Cylindre_24"], m["Structure"], new Vector3(0f, -40f, 0f), new Vector3(60f, 40f, 60f), I, true, Layers.World);
            Part("Plateau", st, me["Cylindre_24"], m["StructureLight"], Vector3.zero, new Vector3(80f, 4f, 80f), I, true, Layers.World);
            var ringGo = Part("Anneau_Tournant", st, Torus(me, 300f, 4f, 96, 6), m["Structure"], new Vector3(0f, 10f, 0f), Vector3.one, I, false, Layers.World);
            ringGo.AddComponent<Rotator>().degreesPerSecond = 3f;
            z.elevatorFoot = new Vector3(-3200f + 90f, eg + 24f + 30f, 300f);
            z.anchorStation = new Vector3(-3200f, 2450f + 4f + 22f, 300f);
            // 5 stations flottantes (cylindre de 28 m, plateau de 62 m) : le joueur peut s'y poser
            var sta = Group("Stations_Flottantes", root);
            Vector3[] stations = { new Vector3(-1400, 520, -1200), new Vector3(1800, 700, -2400), new Vector3(-600, 880, 2200), new Vector3(2700, 640, 1500), new Vector3(-2700, 760, -2500) };
            for (int i = 0; i < stations.Length; i++)
            {
                var s = Group($"Station_{i + 1}", sta); s.position = stations[i];
                Part("Cylindre", s, me["Cylindre_24"], m["Structure"], new Vector3(0f, -60f, 0f), new Vector3(28f, 56f, 28f), I, true, Layers.World);
                Part("Plateau", s, me["Cylindre_24"], m["StructureLight"], new Vector3(0f, -4f, 0f), new Vector3(62f, 4f, 62f), I, true, Layers.World);
                Part("Anneau", s, Torus(me, 60f, 0.8f, 48, 4), m["AccentOrange"], new Vector3(0f, 0.2f, 0f), Vector3.one, I, false, Layers.World, false);
                z.stationTops.Add(stations[i] + Vector3.up * 22f);
            }
            // Ceinture flottante (200 ; 800 ; -1 800) : 3 croiseurs (coque de 300 m) portant des tourelles
            var belt = Group("Ceinture_Flottante", root);
            Vector3[] cruisers = { new Vector3(-150, 760, -1650), new Vector3(420, 840, -1900), new Vector3(120, 900, -2150) };
            float[] yaws = { 20f, -35f, 70f };
            for (int i = 0; i < 3; i++)
            {
                var c = Group($"Croiseur_{i + 1}", belt); c.position = cruisers[i]; c.rotation = Quaternion.Euler(0f, yaws[i], 0f);
                var hb = Part("Coque", c, me["Boite"], m["EnemyHull"], Vector3.zero, new Vector3(60f, 24f, 300f), I, false, Layers.World);
                var cbc = hb.AddComponent<BoxCollider>(); cbc.center = new Vector3(0f, 0.5f, 0f); cbc.size = Vector3.one;
                Part("Pont", c, me["Boite"], m["Structure"], new Vector3(0f, 24f, -40f), new Vector3(30f, 14f, 80f), I, false, Layers.World);
                Part("Liseret", c, me["Boite"], m["OutpostTrim"], new Vector3(0f, 23.6f, 0f), new Vector3(60.4f, 0.6f, 300.4f), I, false, Layers.World, false);
                z.cruiserDecks.Add(c.TransformPoint(new Vector3(0f, 24f + 2.6f, 70f)));
            }
            return z;
        }

        // ------------------------------------------------------------------ 7 contrats (§13.4)
        static void Contracts(SceneRefs r, Data d, Prefabs pf, HeightField hf, ZoneAnchors z)
        {
            float G(float x, float zz) => hf.GroundH(x, zz);
            Vector3 Ground(float x, float zz, float h) => new Vector3(x, G(x, zz) + h, zz);
            Vector3 Ring(Vector3 c, float rad, int i, int n, float h, float phase = 0f)
            { float a = phase + i * Mathf.PI * 2f / n; float x = c.x + Mathf.Cos(a) * rad, zz = c.z + Mathf.Sin(a) * rad; return new Vector3(x, G(x, zz) + h, zz); }
            var spec = new List<(string obj, ContractData data, Vector3 loc, List<(GameObject, Vector3)> enemies)>();
            float cl = d.corbeauData.frameClear, sc = d.spider.bodyClear;

            var c1 = new Vector3(0, 0, -3200); var e1 = new List<(GameObject, Vector3)>();
            for (int i = 0; i < 3; i++) e1.Add((pf.relay, Ring(c1, 260f, i, 3, 40f)));
            e1.Add((pf.corbeau, Ground(c1.x, c1.z, cl)));
            for (int i = 0; i < 4; i++) e1.Add((pf.sentinel, Ring(c1, 160f, i, 4, 80f, 0.4f)));
            spec.Add(("Contrat_1_Lyra", d.contracts[0], Ground(c1.x, c1.z, 20f), e1));

            var c2 = new Vector3(3200, 0, 400); var e2 = new List<(GameObject, Vector3)>();
            for (int i = 0; i < 4; i++) e2.Add((pf.turret, z.pylonTops[i * 2]));
            e2.Add((pf.corbeau, Ground(c2.x, c2.z, cl)));
            for (int i = 0; i < 3; i++) e2.Add((pf.sentinel, Ring(c2, 200f, i, 3, 90f)));
            spec.Add(("Contrat_2_Kessler", d.contracts[1], Ground(c2.x, c2.z, 20f), e2));

            var c3 = new Vector3(500, 0, 3200); var e3 = new List<(GameObject, Vector3)>();
            e3.Add((pf.corbeau, Ground(c3.x - 160f, c3.z - 120f, cl))); e3.Add((pf.corbeau, Ground(c3.x + 170f, c3.z + 140f, cl)));
            foreach (var t in z.hullTops) e3.Add((pf.turret, t));
            for (int i = 0; i < 5; i++) e3.Add((pf.sentinel, Ring(c3, 260f, i, 5, 110f)));
            spec.Add(("Contrat_3_Thalassa", d.contracts[2], Ground(c3.x, c3.z, 20f), e3));

            var c4 = new Vector3(3500, 0, -3500); var e4 = new List<(GameObject, Vector3)>();
            e4.Add((pf.spider, Ground(c4.x - 200f, c4.z, sc))); e4.Add((pf.spider, Ground(c4.x + 200f, c4.z + 60f, sc)));
            e4.Add((pf.relay, Ring(c4, 300f, 0, 2, 45f, 0.8f))); e4.Add((pf.relay, Ring(c4, 300f, 1, 2, 45f, 0.8f)));
            for (int i = 0; i < 3; i++) e4.Add((pf.fighter, Ring(c4, 350f, i, 3, 210f)));
            spec.Add(("Contrat_4_Orsk", d.contracts[3], Ground(c4.x, c4.z, 20f), e4));

            var c5 = new Vector3(-3200, 0, 300); var e5 = new List<(GameObject, Vector3)>();
            e5.Add((pf.relay, z.elevatorFoot)); e5.Add((pf.relay, z.anchorStation));
            e5.Add((pf.corbeau, Ground(c5.x + 220f, c5.z - 120f, cl)));
            float[] alts = { 500f, 1000f, 1500f, 2000f };
            for (int i = 0; i < 4; i++) { float a = i * 1.6f; e5.Add((pf.sentinel, new Vector3(c5.x + Mathf.Cos(a) * 70f, alts[i], c5.z + Mathf.Sin(a) * 70f))); }
            spec.Add(("Contrat_5_Ascenseur", d.contracts[4], Ground(c5.x, c5.z, 20f), e5));

            var c6 = new Vector3(-3500, 0, 3500); var e6 = new List<(GameObject, Vector3)>();
            for (int i = 0; i < 5; i++) e6.Add((pf.fighter, Ring(c6, 320f, i, 5, 220f)));
            e6.Add((pf.corbeau, Ground(c6.x - 120f, c6.z, cl))); e6.Add((pf.corbeau, Ground(c6.x + 140f, c6.z - 60f, cl)));
            e6.Add((pf.spider, Ground(c6.x, c6.z + 200f, sc)));
            spec.Add(("Contrat_6_Helix", d.contracts[5], Ground(c6.x, c6.z, 20f), e6));

            var c7 = new Vector3(200, 800, -1800); var e7 = new List<(GameObject, Vector3)>();
            e7.Add((pf.relay, z.stationTops[0])); e7.Add((pf.relay, z.stationTops[1]));
            foreach (var t in z.cruiserDecks) e7.Add((pf.turret, t));
            for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; e7.Add((pf.sentinel, c7 + new Vector3(Mathf.Cos(a) * 220f, (i % 2 == 0 ? 40f : -40f), Mathf.Sin(a) * 220f))); }
            e7.Add((pf.fighter, c7 + new Vector3(-300f, 120f, 0f))); e7.Add((pf.fighter, c7 + new Vector3(300f, 140f, 0f)));
            spec.Add(("Contrat_7_Ceinture", d.contracts[6], c7, e7));

            foreach (var (obj, data, loc, enemies) in spec)
            {
                var root = Group(obj, r.contracts);
                root.position = loc;
                var ct = root.gameObject.AddComponent<Contract>(); ct.data = data;
                var lieu = Group("Lieu", root); lieu.position = loc; ct.location = lieu;
                var count = new Dictionary<string, int>();
                foreach (var (prefab, pos) in enemies)
                {
                    string n = prefab.name.Replace("MGSUGE_Ennemi_", "");
                    count[n] = count.TryGetValue(n, out int k) ? k + 1 : 1;
                    var face = loc - pos; face.y = 0f;
                    var e = Instance(prefab, root, pos, face.sqrMagnitude > 1f ? Quaternion.LookRotation(face) : Quaternion.identity, $"{n}_{count[n]}");
                    Deactivate(e);
                }
                r.cm.contracts.Add(ct);
            }
        }

        // ------------------------------------------------------------------ 36 avant-postes (§13.3), graine 90210
        static void Outposts(SceneRefs r, Data d, Prefabs pf, HeightField hf)
        {
            var od = d.outposts; var w = d.world;
            var rng = new System.Random(od.seed);
            var placed = new List<Vector3>();
            float slope = od.maxSlope, lim = w.wallStart - 150f;
            Vector2 helios = new Vector2(w.heliosPosition.x, w.heliosPosition.z);
            int attempts = 0;
            while (placed.Count < od.count && attempts < 60000)
            {
                attempts++;
                if (attempts % 15000 == 0) { slope *= 1.5f; Debug.LogWarning($"[MGSUGE] Avant-postes : terrain plat insuffisant, tolerance portee a {slope:0} m."); }
                float x = ((float)rng.NextDouble() * 2f - 1f) * lim, z = ((float)rng.NextDouble() * 2f - 1f) * lim;
                if (Vector2.Distance(new Vector2(x, z), helios) < od.minDistanceFromHelios) continue;
                if (hf.Height(x, z) < w.seaLevel + 4f || w.InLavaRect(x, z)) continue;
                bool inZone = false;
                foreach (var zn in w.zones) if (Vector2.Distance(new Vector2(x, z), zn.center) < zn.radius) { inZone = true; break; }
                if (inZone) continue;
                bool near = false;
                foreach (var p in placed) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)) < od.minSpacing) { near = true; break; }
                if (near) continue;
                float hmin = float.MaxValue, hmax = float.MinValue;
                for (int k = 0; k <= 8; k++)
                {
                    float a = k * Mathf.PI / 4f, rad = k == 8 ? 0f : 45f;
                    float h = hf.Height(x + Mathf.Cos(a) * rad, z + Mathf.Sin(a) * rad);
                    hmin = Mathf.Min(hmin, h); hmax = Mathf.Max(hmax, h);
                }
                if (hmax - hmin > slope) continue;
                placed.Add(new Vector3(x, hf.GroundH(x, z), z));
            }
            if (placed.Count < od.count) Debug.LogWarning($"[MGSUGE] Seulement {placed.Count} avant-postes places sur {od.count}.");
            for (int i = 0; i < placed.Count; i++)
            {
                int type = i % 5;
                var yaw = (float)rng.NextDouble() * 360f;
                Instance(pf.outposts[type], r.outposts, placed[i], Quaternion.Euler(0f, yaw, 0f), $"AvantPoste_{i + 1:00}_{OutpostNames[type]}");
            }
        }

        // ------------------------------------------------------------------ scene Test
        static void BuildTestScene(UnityEngine.SceneManagement.Scene scene, Data d, Mats m, Meshes me, Prefabs pf)
        {
            EnsureFolder(SceneDir);
            var r = CommonScene(d, m, me, pf, Vector3.zero, "Terrain d'essai", "Cibles devant · ennemis tout autour · contrat au sud · K : commandes");
            Quaternion I = Quaternion.identity;
            Sky(r.world, d, m, me, r.pc.cam.cam);
            // sol plat a y = 0 (sans objet Terrain, le sol du gameplay est a 0)
            var sol = Part("Sol_Essai", r.world, me["Boite"], m["TestGround"], new Vector3(0f, -10f, 0f), new Vector3(8000f, 10f, 8000f), I, false, Layers.World);
            var sbc = sol.AddComponent<BoxCollider>(); sbc.center = new Vector3(0f, 0.5f, 0f); sbc.size = Vector3.one;
            Part("Plateforme_Depart", r.world, me["Cylindre_24"], m["Platform"], new Vector3(0f, -0.4f, 0f), new Vector3(42f, 0.5f, 42f), I, false, Layers.World);
            Part("Plateforme_Anneau", r.world, Torus(me, 40f, 0.8f, 48, 4), m["AccentOrange"], new Vector3(0f, 0.2f, 0f), Vector3.one, I, false, Layers.World, false);

            // cibles d'entrainement (ne tirent pas) devant le point de depart
            var targets = Group("Cibles_Essai", r.enemies);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var t = Instance(pf.dummySentinel, targets, new Vector3(Mathf.Cos(a) * 300f, 170f + (i % 3) * 50f, 560f + Mathf.Sin(a) * 160f), I, $"Cible_Sentinelle_{i + 1}");
                var dm = t.GetComponent<TargetDummy>(); dm.orbitRadius = 40f + i * 9f; dm.orbitSpeed = (i % 2 == 0 ? 1f : -1f) * (0.25f + i * 0.05f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(dm);
            }
            Instance(pf.dummyRelay, targets, new Vector3(-220f, 70f, 560f), I, "Cible_Relais_1");
            Instance(pf.dummyRelay, targets, new Vector3(220f, 70f, 560f), I, "Cible_Relais_2");
            var c1 = Instance(pf.dummyCorbeau, targets, new Vector3(-150f, d.frame.frameClear, 560f), I, "Cible_Corbeau_1");
            var c2 = Instance(pf.dummyCorbeau, targets, new Vector3(170f, d.frame.frameClear, 670f), I, "Cible_Corbeau_2");
            var d2 = c2.GetComponent<TargetDummy>(); d2.orbitSpeed = -0.35f; PrefabUtility.RecordPrefabInstancePropertyModifications(d2);

            // un ennemi de chaque type, a 900-2 000 m, chacun dans une direction (ils attaquent quand on s'approche)
            var foes = Group("Ennemis_Essai", r.enemies);
            Instance(pf.sentinel, foes, new Vector3(900f, 80f, -150f), I, "Sentinelle");
            Instance(pf.corbeau, foes, new Vector3(-900f, d.corbeauData.frameClear, -150f), Quaternion.Euler(0f, 90f, 0f), "Corbeau");
            var socle = Part("Socle_Tourelle", r.world, me["Cylindre_8"], m["Structure"], new Vector3(0f, 0f, -1100f), new Vector3(8f, 12f, 8f), I, true, Layers.World);
            Instance(pf.turret, foes, new Vector3(0f, 12f + 2.6f, -1100f), I, "Tourelle");
            Instance(pf.relay, foes, new Vector3(1100f, 45f, 650f), I, "Relais");
            Instance(pf.spider, foes, new Vector3(-1100f, d.spider.bodyClear, 700f), Quaternion.Euler(0f, 120f, 0f), "Araignee");
            Instance(pf.fighter, foes, new Vector3(0f, 220f, 2000f), Quaternion.Euler(0f, 180f, 0f), "Faucon");

            // contrat d'essai (cycle d'un seul contrat : la menace monte a chaque victoire)
            var ct = Group("Contrat_Essai", r.contracts, new Vector3(0f, 0f, -2300f));
            var contract = ct.gameObject.AddComponent<Contract>(); contract.data = d.testContract;
            var lieu = Group("Lieu", ct, new Vector3(0f, 20f, 0f)); contract.location = lieu;
            var e1 = Instance(pf.sentinel, ct, new Vector3(-120f, 70f, -2300f), I, "Sentinelle_1"); Deactivate(e1);
            var e2 = Instance(pf.sentinel, ct, new Vector3(120f, 90f, -2300f), I, "Sentinelle_2"); Deactivate(e2);
            Part("Socle_Contrat", r.world, me["Cylindre_8"], m["Structure"], new Vector3(0f, 0f, -2380f), new Vector3(8f, 12f, 8f), I, true, Layers.World);
            var e3 = Instance(pf.turret, ct, new Vector3(0f, 14.6f, -2380f), I, "Tourelle_1"); Deactivate(e3);
            var e4 = Instance(pf.relay, ct, new Vector3(0f, 45f, -2220f), I, "Relais_1"); Deactivate(e4);
            r.cm.contracts.Add(contract);

            // avant-poste d'essai (s'active a moins de 1 700 m)
            Instance(pf.outposts[0], r.outposts, new Vector3(1900f, 0f, -1500f), Quaternion.Euler(0f, 30f, 0f), "AvantPoste_Essai_Patrouille");

            // patrouilles errantes desactivees par defaut dans la scene Test (cocher le composant pour les essayer)
            r.ps.enabled = false;
            r.gm.startOnTitle = false; // la scene Test demarre directement en mode normal
            BuildInterface(r.ui, d, LoadUiAssets(d), r.pc, r.gm);
            EditorSceneManager.SaveScene(scene, TestScenePath);
        }
    }
}
