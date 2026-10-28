using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>
    /// Construit le projet en un clic (menu MGSUGE) : couches, reglages du lecteur, materiaux, donnees (ScriptableObjects),
    /// import du chassis Vesper (FBX), prefab du joueur et scene World. Relancable sans risque : les assets existants sont mis a jour.
    /// </summary>
    public static class MGSUGEBuilder
    {
        const string Root = "Assets/_MGSUGE";
        const string ModelPath = Root + "/Art/Models/MGSUGE_Vesper_Mech.fbx";
        const string PalettePath = Root + "/Art/Models/MGSUGE_Vesper_Palette.json";
        const string PosesPath = Root + "/Art/Models/MGSUGE_Vesper_Poses.json";
        const string MatDir = Root + "/Art/Materials";
        const string DataDir = Root + "/Data";
        const string ScenePath = Root + "/Scenes/World.unity";
        const string PlayerPrefab = Root + "/Prefabs/MGSUGE_Player.prefab";

        [MenuItem("MGSUGE/1. Construire le projet (tout)", priority = 1)]
        public static void BuildAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("MGSUGE", "Reglages du projet", 0.05f);
                ProjectSettingsSetup();
                EditorUtility.DisplayProgressBar("MGSUGE", "Materiaux", 0.2f);
                var mats = BuildMaterials();
                EditorUtility.DisplayProgressBar("MGSUGE", "Donnees", 0.35f);
                var data = BuildData();
                EditorUtility.DisplayProgressBar("MGSUGE", "Import du chassis Vesper", 0.5f);
                ConfigureModel(mats);
                EditorUtility.DisplayProgressBar("MGSUGE", "Prefab du joueur", 0.65f);
                var prefab = BuildPlayerPrefab(data);
                EditorUtility.DisplayProgressBar("MGSUGE", "Scene World", 0.8f);
                BuildScene(data, mats, prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("[MGSUGE] Projet construit. Ouvrez la scene " + ScenePath + " et lancez Play.");
            }
            catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("MGSUGE", "Erreur pendant la construction :\n" + e.Message, "OK"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("MGSUGE/2. Ouvrir la scene World", priority = 2)]
        public static void OpenWorld() { if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("MGSUGE/3. Reimporter le chassis et les poses", priority = 3)]
        public static void ReimportModel()
        {
            var mats = BuildMaterials(); ConfigureModel(mats);
            var poses = LoadOrCreate<PoseLibrary>(DataDir + "/MGSUGE_Poses.asset");
            poses.LoadJson(File.ReadAllText(PosesPath)); EditorUtility.SetDirty(poses); AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ reglages
        static void ProjectSettingsSetup()
        {
            foreach (var d in new[] { Root + "/Art/Materials", Root + "/Data", Root + "/Scenes", Root + "/Prefabs" }) EnsureFolder(d);
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            void L(int i, string n) { var p = layers.GetArrayElementAtIndex(i); if (string.IsNullOrEmpty(p.stringValue) || p.stringValue == n) p.stringValue = n; else Debug.LogWarning($"[MGSUGE] Couche {i} deja utilisee ({p.stringValue})"); }
            L(Layers.Player, "Player"); L(Layers.World, "World"); L(Layers.Enemy, "Enemy");
            tm.ApplyModifiedProperties();
            PlayerSettings.productName = "M.G-S.U.G.E";
            PlayerSettings.companyName = "MGSUGE";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.studio.mgsuge");
            Physics.IgnoreLayerCollision(Layers.Player, Layers.Enemy, false);
            // ombres sur une grande distance (monde de 15 km)
            var assets = new List<RenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline != null) assets.Add(GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.names.Length; i++) { var a = QualitySettings.GetRenderPipelineAssetAt(i); if (a != null) assets.Add(a); }
            if (assets.Count == 0) Debug.LogError("[MGSUGE] Aucun pipeline URP actif : creez le projet avec le modele « Universal 3D ».");
            foreach (var a in assets) if (a is UniversalRenderPipelineAsset u) { u.shadowDistance = 1500f; EditorUtility.SetDirty(u); }
        }

        // ------------------------------------------------------------------ materiaux
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static Material Mat(string name, string shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[MGSUGE] Shader introuvable : " + shader); sh = Shader.Find("Universal Render Pipeline/Lit"); }
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else m.shader = sh;
            return m;
        }
        /// <summary>URP/Lit opaque. Couleurs donnees en sRGB (gamma) ; emission en lineaire HDR.</summary>
        static Material Lit(string name, Color baseGamma, float metallic, float smooth, Color emissionLinear = default, float alpha = 1f)
        {
            var m = Mat(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", new Color(baseGamma.r, baseGamma.g, baseGamma.b, alpha));
            m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            if (emissionLinear.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emissionLinear.gamma);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m); return m;
        }
        /// <summary>URP/Particles/Unlit en additif (flammes, tirs, eclairs, rayons).</summary>
        static Material Additive(string name, Color colorLinear)
        {
            var m = Mat(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetColor("_BaseColor", colorLinear.gamma);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
            if (m.HasProperty("_SrcBlendAlpha")) { m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.One); }
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m); return m;
        }

        [Serializable] class JCol { public float r, g, b, a = 1f; public Color C => new Color(r, g, b, a); }
        [Serializable] class JMat { public string name, role; public JCol baseColor, emissionColor; public float metallic, roughness, emissionStrength, alpha = 1f; }
        [Serializable] class JPal { public JMat[] materials; }

        public class Mats : Dictionary<string, Material> { }
        static Mats BuildMaterials()
        {
            EnsureFolder(MatDir);
            var d = new Mats();
            // chassis : un materiau par entree de la palette exportee de Blender (couleurs lineaires)
            var pal = JsonUtility.FromJson<JPal>(File.ReadAllText(PalettePath));
            foreach (var jm in pal.materials)
            {
                Color lin = jm.baseColor.C;
                Material m;
                if (jm.role == "Flame") m = Additive(jm.name, new Color(lin.r, lin.g, lin.b, 0.85f) * 2.2f);
                else if (jm.role == "Blade") m = Additive(jm.name, new Color(lin.r, lin.g, lin.b, 1f) * 3f);
                else
                {
                    Color e = jm.emissionStrength > 0.01f ? jm.emissionColor.C * Mathf.Min(jm.emissionStrength, 8f) * 0.6f : Color.clear;
                    m = Lit(jm.name, lin.gamma, Mathf.Clamp01(jm.metallic), 1f - Mathf.Clamp01(jm.roughness), e);
                }
                d[jm.name] = m;
            }
            // monde
            d["Terrain"] = Mat("MGSUGE_Terrain", "Universal Render Pipeline/Particles/Lit");
            d["Terrain"].SetColor("_BaseColor", Color.white); d["Terrain"].SetFloat("_Smoothness", 0.1f); EditorUtility.SetDirty(d["Terrain"]);
            d["Sea"] = Lit("MGSUGE_Mer", new Color(0.05f, 0.18f, 0.2f), 0.1f, 0.92f);
            d["Lava"] = Lit("MGSUGE_Lave", new Color(0.35f, 0.12f, 0.05f), 0f, 0.3f, new Color(1f, 0.32f, 0.06f) * 5f);
            var sky = Mat("MGSUGE_Ciel", "Skybox/Procedural");
            sky.SetFloat("_SunSize", 0.035f); sky.SetFloat("_SunSizeConvergence", 6f); sky.SetFloat("_AtmosphereThickness", 1.7f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.35f, 0.62f)); sky.SetColor("_GroundColor", new Color(0.18f, 0.08f, 0.12f)); sky.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(sky); d["Sky"] = sky;
            d["Planet"] = Mat("MGSUGE_Planete", "MGSUGE/SkyObject"); d["Planet"].SetColor("_BaseColor", new Color(0.72f, 0.5f, 0.42f)); d["Planet"].SetColor("_RimColor", new Color(0.62f, 0.35f, 0.95f));
            d["Moon"] = Mat("MGSUGE_Lune", "MGSUGE/SkyObject"); d["Moon"].SetColor("_BaseColor", new Color(0.62f, 0.58f, 0.66f)); d["Moon"].SetColor("_RimColor", new Color(0.2f, 0.15f, 0.3f));
            d["Ring"] = Mat("MGSUGE_Anneau_Planete", "MGSUGE/PlanetRing");
            d["Stars"] = Mat("MGSUGE_Etoiles", "MGSUGE/Stars");
            d["Structure"] = Lit("MGSUGE_Arco_Structure", Hex("#24222B"), 0.6f, 0.5f);
            d["StructureLight"] = Lit("MGSUGE_Arco_Structure_Claire", Hex("#4A4656"), 0.5f, 0.45f);
            d["AccentOrange"] = Lit("MGSUGE_Arco_Lumiere_Orange", Hex("#3A2410"), 0f, 0.5f, new Color(1f, 0.55f, 0.15f) * 3f);
            d["AccentRose"] = Lit("MGSUGE_Arco_Lumiere_Rose", Hex("#3A1030"), 0f, 0.5f, new Color(1f, 0.25f, 0.8f) * 3f);
            d["Beacon"] = Lit("MGSUGE_Arco_Balise", Hex("#FF8A2A"), 0f, 0.5f, new Color(1f, 0.55f, 0.15f) * 6f);
            d["RedLight"] = Lit("MGSUGE_Feu_Rouge", Hex("#FF2030"), 0f, 0.5f, new Color(1f, 0.05f, 0.08f) * 6f);
            d["Platform"] = Lit("MGSUGE_Plateforme_Helios", Hex("#55505E"), 0.4f, 0.45f);
            // effets
            d["Particles"] = Additive("MGSUGE_FX_Particules", Color.white);
            d["Flash"] = Additive("MGSUGE_FX_Eclair", Color.white);
            d["Beam"] = Additive("MGSUGE_FX_Rayon", Color.white);
            d["Projectile"] = Additive("MGSUGE_FX_Tir", Color.white);
            d["Trail"] = Additive("MGSUGE_FX_Trainee", Color.white);
            d["MissileBody"] = Lit("MGSUGE_Missile", Hex("#D8D0BC"), 0.2f, 0.5f, new Color(1f, 0.4f, 0.8f) * 0.6f);
            // cibles d'essai
            d["Sentinel"] = Lit("MGSUGE_Essai_Sentinelle", Hex("#2A2E39"), 0.5f, 0.5f, new Color(1f, 0.1f, 0.15f) * 0.6f);
            d["Relay"] = Lit("MGSUGE_Essai_Relais", Hex("#241C2A"), 0.4f, 0.5f, new Color(1f, 0.1f, 0.2f) * 1.5f);
            d["RelayRing"] = Lit("MGSUGE_Essai_Anneau", Hex("#FF4458"), 0f, 0.5f, new Color(1f, 0.15f, 0.2f) * 3f);
            AssetDatabase.SaveAssets();
            return d;
        }

        // ------------------------------------------------------------------ donnees
        public class Data
        {
            public FrameStatsData frame; public CameraData cam; public WeaponData weapons; public WorldData world;
            public ProgressionData progression; public PoseLibrary poses; public PaletteData player, corbeau;
        }
        static T LoadOrCreate<T>(string path, Action<T> init = null) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); init?.Invoke(a); AssetDatabase.CreateAsset(a, path); }
            return a;
        }
        static PaletteData Palette(string name, string armor, string frame, string accent, string visor, string glow, string flame, string blade)
            => LoadOrCreate<PaletteData>($"{DataDir}/{name}.asset", p =>
            {
                p.useOriginal = false; p.armor = Hex(armor); p.frame = Hex(frame); p.accent = Hex(accent); p.visor = Hex(visor);
                p.glow = Hex(glow); p.flame = Hex(flame); p.blade = Hex(blade);
            });
        static Data BuildData()
        {
            EnsureFolder(DataDir);
            var d = new Data
            {
                frame = LoadOrCreate<FrameStatsData>(DataDir + "/MGSUGE_FrameStats.asset"),
                cam = LoadOrCreate<CameraData>(DataDir + "/MGSUGE_Camera.asset"),
                weapons = LoadOrCreate<WeaponData>(DataDir + "/MGSUGE_Weapons.asset"),
                world = LoadOrCreate<WorldData>(DataDir + "/MGSUGE_World.asset", w => w.ResetZones()),
                progression = LoadOrCreate<ProgressionData>(DataDir + "/MGSUGE_Progression.asset"),
                poses = LoadOrCreate<PoseLibrary>(DataDir + "/MGSUGE_Poses.asset"),
                player = LoadOrCreate<PaletteData>(DataDir + "/MGSUGE_Palette_Joueur.asset", p => p.useOriginal = true),
                // palettes du cahier (§3.1)
                corbeau = Palette("MGSUGE_Palette_Corbeau", "#4A2A31", "#16171D", "#C92F3D", "#FF3346", "#FF5A6A", "#FF7A6A", "#FF4FD8"),
            };
            Palette("MGSUGE_Palette_As_A", "#9A4048", "#16171D", "#FF6A3A", "#FF3346", "#FF5A6A", "#FF7A3A", "#FF4FD8");
            Palette("MGSUGE_Palette_As_B", "#6A62B0", "#16171D", "#B58AFF", "#C07BFF", "#9A7BFF", "#B58AFF", "#FF4FD8");
            Palette("MGSUGE_Palette_Titan", "#6A6F7E", "#2A2E39", "#FFB040", "#FF4A2A", "#FFB040", "#FFC27A", "#FF4FD8");
            Palette("MGSUGE_Palette_Archonte", "#231830", "#16171D", "#B44DFF", "#F0D8FF", "#B44DFF", "#C07BFF", "#B44DFF");
            if (d.world.zones == null || d.world.zones.Count == 0) { d.world.ResetZones(); EditorUtility.SetDirty(d.world); }
            // chassis de 12 m (bassin a 7 m), valide le 28/10/2026 : mise a jour des donnees creees avec l'ancienne echelle
            if (Mathf.Abs(d.frame.frameClear - 5.7f) < 0.01f)
            { d.frame.frameClear = 7f; d.frame.frameBottomOffset = -4.7f; EditorUtility.SetDirty(d.frame); Debug.Log("[MGSUGE] Chassis passe a 12 m : bassin a 7 m du sol."); }
            d.poses.LoadJson(File.ReadAllText(PosesPath)); EditorUtility.SetDirty(d.poses);
            AssetDatabase.SaveAssets();
            return d;
        }

        // ------------------------------------------------------------------ modele
        static void ConfigureModel(Mats mats)
        {
            var imp = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (imp == null) throw new Exception("Modele introuvable : " + ModelPath);
            imp.globalScale = 1f; imp.useFileScale = true;
            imp.importAnimation = false; imp.animationType = ModelImporterAnimationType.None;
            imp.importBlendShapes = false; imp.importCameras = false; imp.importLights = false; imp.importVisibility = false;
            imp.importNormals = ModelImporterNormals.Import; imp.isReadable = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var kv in mats)
                if (kv.Key.StartsWith("MGSUGE_Vesper_"))
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ prefab du joueur
        static GameObject BuildPlayerPrefab(Data d)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var root = new GameObject("Joueur_Vesper");
            var visual = new GameObject("Visuel").transform; visual.SetParent(root.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
            inst.name = "MGSUGE_Vesper";
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Player;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = r.name.StartsWith("Flame_") || r.name == "Blade_Energy" ? ShadowCastingMode.Off : ShadowCastingMode.On;

            root.AddComponent<CharacterController>();
            var input = root.AddComponent<PlayerInputs>();
            var rig = root.AddComponent<FrameRig>(); rig.model = inst.transform; rig.poses = d.poses;
            var pc = root.AddComponent<PlayerController>();
            pc.frame = d.frame; pc.weaponData = d.weapons; pc.progression = d.progression; pc.world = d.world; pc.rig = rig; pc.input = input; pc.visual = visual;
            var lk = root.AddComponent<LockOn>(); lk.data = d.weapons; lk.player = pc;
            var wc = root.AddComponent<WeaponController>(); wc.data = d.weapons; wc.player = pc; wc.lockOn = lk; wc.rig = rig;
            var pr = root.AddComponent<ProbeController>(); pr.data = d.weapons; pr.player = pc; pr.lockOn = lk; pr.rig = rig;
            var th = root.AddComponent<ThrusterFX>(); th.player = pc; th.model = inst.transform;
            var pa = root.AddComponent<PaletteApplier>(); pa.palette = d.player;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ scene World
        static void BuildScene(Data d, Mats m, GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = m["Sky"];

            var gmGo = new GameObject("GameManager"); var gm = gmGo.AddComponent<GameManager>();

            var sunGo = new GameObject("Lumiere_Soleil"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            var fillGo = new GameObject("Lumiere_Remplissage"); var fill = fillGo.AddComponent<Light>(); fill.type = LightType.Directional;

            var world = new GameObject("Monde");
            var terrain = world.AddComponent<TerrainSystem>(); terrain.world = d.world; terrain.material = m["Terrain"];
            var seaGo = new GameObject("Mer"); seaGo.transform.SetParent(world.transform, false);
            var sea = seaGo.AddComponent<SeaSurface>(); sea.world = d.world; sea.material = m["Sea"];
            var lavaGo = new GameObject("Lave"); lavaGo.transform.SetParent(world.transform, false);
            var lava = lavaGo.AddComponent<LavaSurface>(); lava.world = d.world; lava.material = m["Lava"];
            var arcoGo = new GameObject("Arcologie_Vesta"); arcoGo.transform.SetParent(world.transform, false);
            var arco = arcoGo.AddComponent<Arcology>(); arco.world = d.world;
            arco.structure = m["Structure"]; arco.structureLight = m["StructureLight"]; arco.accentOrange = m["AccentOrange"]; arco.accentRose = m["AccentRose"];
            arco.beacon = m["Beacon"]; arco.redLight = m["RedLight"]; arco.platform = m["Platform"];
            var skyGo = new GameObject("Ciel"); skyGo.transform.SetParent(world.transform, false);
            var sky = skyGo.AddComponent<SkySystem>(); sky.world = d.world; sky.skybox = m["Sky"]; sky.planetMaterial = m["Planet"]; sky.moonMaterial = m["Moon"];
            sky.ringMaterial = m["Ring"]; sky.starMaterial = m["Stars"]; sky.sun = sun; sky.fill = fill;

            var volGo = new GameObject("Volume_Global"); var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true;
            string profPath = Root + "/Data/MGSUGE_PostProcess.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profPath);
            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, profPath);
                var tmap = prof.Add<Tonemapping>(true); tmap.mode.Override(TonemappingMode.ACES);
                var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.7f); bloom.threshold.Override(1.0f);
                foreach (var c in prof.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, prof); }
                AssetDatabase.SaveAssets();
            }
            vol.sharedProfile = prof;

            var fxGo = new GameObject("FX");
            var fx = fxGo.AddComponent<FxSystem>(); fx.particleMaterial = m["Particles"]; fx.flashMaterial = m["Flash"]; fx.beamMaterial = m["Beam"];
            var proj = fxGo.AddComponent<ProjectileSystem>(); proj.data = d.weapons; proj.material = m["Projectile"];
            var mis = fxGo.AddComponent<MissileSystem>(); mis.data = d.weapons; mis.bodyMaterial = m["MissileBody"]; mis.trailMaterial = m["Trail"];
            fxGo.AddComponent<SfxSystem>();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = d.world.heliosPosition + Vector3.up * (d.frame.frameClear + 0.5f);
            var pc = player.GetComponent<PlayerController>();

            var camGo = new GameObject("Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rig = camGo.AddComponent<CameraRig>(); rig.data = d.cam; rig.player = pc; rig.cam = cam;
            camGo.transform.position = player.transform.position + new Vector3(3f, 2f, -21f);
            pc.cam = rig;
            gm.player = pc;
            EditorUtility.SetDirty(pc);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pc);

            var hudGo = new GameObject("HUD"); var hud = hudGo.AddComponent<Hud>();
            hud.player = pc; hud.weapons = player.GetComponent<WeaponController>(); hud.probes = player.GetComponent<ProbeController>(); hud.lockOn = player.GetComponent<LockOn>(); hud.world = d.world;

            var range = new GameObject("Terrain_Essai").AddComponent<TestRange>();
            range.weaponData = d.weapons; range.world = d.world; range.sentinelMaterial = m["Sentinel"]; range.relayMaterial = m["Relay"]; range.relayRingMaterial = m["RelayRing"];
            range.frameModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath); range.hostilePalette = d.corbeau; range.poses = d.poses; range.frameClear = d.frame.frameClear;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'), leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
