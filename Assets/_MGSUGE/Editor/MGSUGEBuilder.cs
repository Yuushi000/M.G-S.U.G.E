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
    /// Constructeur du projet (menu MGSUGE). Cree une fois pour toutes, dans le projet : materiaux, maillages, donnees
    /// (ScriptableObjects), prefabs (joueur, ennemis, avant-postes, tours, effets) et les scenes World et Test, enregistrees
    /// avec une Hierarchy rangee (Managers, Lights, Player, World, Enemies, Contracts, Outposts, Bosses, UI).
    /// Ensuite, on ouvre les scenes par double-clic et on joue : rien n'est regenere au lancement.
    /// Relancer le constructeur REMPLACE les prefabs et les scenes (les donnees deja creees sont conservees).
    /// </summary>
    public static partial class MGSUGEBuilder
    {
        const string Root = "Assets/_MGSUGE";
        const string ModelPath = Root + "/Art/Models/MGSUGE_Vesper_Mech.fbx";
        const string PalettePath = Root + "/Art/Models/MGSUGE_Vesper_Palette.json";
        const string PosesPath = Root + "/Art/Models/MGSUGE_Vesper_Poses.json";
        const string MatDir = Root + "/Art/Materials";
        const string GenDir = Root + "/Art/Generated";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string WorldScenePath = SceneDir + "/World.unity";
        const string TestScenePath = SceneDir + "/Test.unity";
        const string PlayerPrefab = PrefabDir + "/Joueur/MGSUGE_Player.prefab";

        // ------------------------------------------------------------------ menus
        [MenuItem("MGSUGE/1. Construire tout (prefabs, donnees, scenes World et Test)", priority = 1)]
        public static void BuildAllMenu()
        {
            bool exists = File.Exists(WorldScenePath) || File.Exists(TestScenePath);
            if (exists && !EditorUtility.DisplayDialog("MGSUGE", "Les prefabs et les scenes World et Test vont etre regeneres.\n" +
                "Vos modifications dans ces scenes et ces prefabs seront perdues (les donnees de Data/ sont conservees).\n\nContinuer ?", "Construire", "Annuler")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll(true, true);
        }
        [MenuItem("MGSUGE/2. Ouvrir la scene World", priority = 20)]
        public static void OpenWorld() { if (File.Exists(WorldScenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(WorldScenePath); }
        [MenuItem("MGSUGE/3. Ouvrir la scene Test", priority = 21)]
        public static void OpenTest() { if (File.Exists(TestScenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(TestScenePath); }

        [MenuItem("MGSUGE/4. Test automatique (scene Test)", priority = 40)]
        public static void RunAutoTestTest() => RunAutoTest(TestScenePath);
        [MenuItem("MGSUGE/5. Test automatique (scene World)", priority = 41)]
        public static void RunAutoTestWorld() => RunAutoTest(WorldScenePath);
        static void RunAutoTest(string scene)
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (!File.Exists(scene)) { EditorUtility.DisplayDialog("MGSUGE", "Scene introuvable : " + scene + "\nLancez d'abord MGSUGE > 1. Construire tout.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scene);
            PlayerPrefs.SetInt(AutoTest.PrefKey, 1); PlayerPrefs.Save();
            Debug.Log("[MGSUGE] Test automatique : 1 a 2 minutes. Rapport et captures dans Logs/AutoTest du projet.");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("MGSUGE/Avance/Reconstruire seulement les prefabs et les donnees", priority = 60)]
        public static void BuildAssetsOnly()
        {
            if (!EditorUtility.DisplayDialog("MGSUGE", "Les prefabs vont etre regeneres (les scenes ne changent pas ; leurs instances suivront les prefabs).", "Construire", "Annuler")) return;
            BuildAll(false, false);
        }
        [MenuItem("MGSUGE/Avance/Reconstruire seulement la scene World", priority = 61)]
        public static void BuildWorldOnly()
        {
            if (!EditorUtility.DisplayDialog("MGSUGE", "La scene World va etre regeneree (vos modifications y seront perdues).", "Construire", "Annuler")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll(true, false, false);
        }
        [MenuItem("MGSUGE/Avance/Reconstruire seulement la scene Test", priority = 62)]
        public static void BuildTestOnly()
        {
            if (!EditorUtility.DisplayDialog("MGSUGE", "La scene Test va etre regeneree (vos modifications y seront perdues).", "Construire", "Annuler")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll(false, true, false);
        }
        [MenuItem("MGSUGE/Avance/Reimporter le chassis et les poses", priority = 63)]
        public static void ReimportModel()
        {
            var mats = BuildMaterials(); ConfigureModel(mats);
            var poses = LoadOrCreate<PoseLibrary>(DataDir + "/MGSUGE_Poses.asset");
            poses.LoadJson(File.ReadAllText(PosesPath)); EditorUtility.SetDirty(poses); AssetDatabase.SaveAssets();
        }

        /// <summary>Pour l'automatisation (ligne de commande) : construit tout sans boite de dialogue.</summary>
        public static void BuildAllBatch() => BuildAll(true, true);

        static void BuildAll(bool world, bool test, bool prefabs = true)
        {
            try
            {
                EditorUtility.DisplayProgressBar("MGSUGE", "Reglages du projet", 0.03f);
                ProjectSettingsSetup();
                EditorUtility.DisplayProgressBar("MGSUGE", "Materiaux", 0.08f);
                var mats = BuildMaterials();
                EditorUtility.DisplayProgressBar("MGSUGE", "Donnees", 0.12f);
                var data = BuildData();
                EditorUtility.DisplayProgressBar("MGSUGE", "Import du chassis Vesper", 0.18f);
                ConfigureModel(mats);
                EditorUtility.DisplayProgressBar("MGSUGE", "Maillages", 0.25f);
                var meshes = BuildMeshes(data);
                Prefabs pf;
                if (prefabs)
                {
                    EditorUtility.DisplayProgressBar("MGSUGE", "Prefabs", 0.35f);
                    pf = BuildPrefabs(data, mats, meshes);
                }
                else pf = LoadPrefabs();
                EditorUtility.DisplayProgressBar("MGSUGE", "Interface (polices, sprites, carte, prefabs UI)", 0.45f);
                BuildUiAssets(data);
                AssetDatabase.SaveAssets();
                // Une nouvelle scene decharge les assets non references : on recharge toutes les references apres chaque NewScene.
                if (world)
                {
                    EditorUtility.DisplayProgressBar("MGSUGE", "Scene World (terrain, arcologie, contrats, avant-postes)", 0.55f);
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    BuildWorldScene(scene, BuildData(), BuildMaterials(), LoadMeshes(), LoadPrefabs());
                }
                if (test)
                {
                    EditorUtility.DisplayProgressBar("MGSUGE", "Scene Test", 0.85f);
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    BuildTestScene(scene, BuildData(), BuildMaterials(), LoadMeshes(), LoadPrefabs());
                }
                UpdateBuildSettings();
                AssetDatabase.SaveAssets();
                if (world) EditorSceneManager.OpenScene(WorldScenePath);
                Debug.Log("[MGSUGE] Construction terminee. Scenes : " + WorldScenePath + " et " + TestScenePath + " (File > Build Profiles).");
            }
            catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("MGSUGE", "Erreur pendant la construction :\n" + e.Message, "OK"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static void UpdateBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in new[] { WorldScenePath, TestScenePath })
                if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            // les autres scenes deja listees (hors scene d'exemple du modele Unity) restent apres, desactivees si absentes
            foreach (var s in EditorBuildSettings.scenes)
                if (!list.Exists(x => x.path == s.path) && !s.path.EndsWith("SampleScene.unity") && File.Exists(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
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
            EnsureUrp();
            // ombres sur une grande distance (monde de 15 km)
            var assets = new List<RenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline != null) assets.Add(GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.names.Length; i++) { var a = QualitySettings.GetRenderPipelineAssetAt(i); if (a != null) assets.Add(a); }
            foreach (var a in assets) if (a is UniversalRenderPipelineAsset u) { u.shadowDistance = 1500f; u.supportsHDR = true; EditorUtility.SetDirty(u); }
        }

        /// <summary>
        /// Projet cree sans le modele « Universal 3D » : aucun pipeline n'est assigne et tous les materiaux URP s'affichent en magenta.
        /// On cree alors un asset URP (avec Universal Renderer) et on l'assigne dans Graphics.
        /// </summary>
        static void EnsureUrp()
        {
            bool any = GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset;
            for (int i = 0; i < QualitySettings.names.Length && !any; i++) any = QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset;
            if (any) return;
            string dir = Root + "/Settings"; EnsureFolder(dir);
            string assetPath = dir + "/MGSUGE_URP.asset", rendererPath = dir + "/MGSUGE_URP_Renderer.asset";
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rd == null) { rd = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(rd, rendererPath); }
                urp = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(urp, assetPath);
                AssetDatabase.SaveAssets();
            }
            GraphicsSettings.defaultRenderPipeline = urp;
            // les niveaux de qualite sans asset propre utilisent celui-ci ; on remplace ceux qui pointeraient vers un autre pipeline
            int cur = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) != null) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = urp; }
            QualitySettings.SetQualityLevel(cur, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[MGSUGE] Pipeline URP cree et assigne : " + assetPath);
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
            // ennemis (§13.1) : metal sombre, liserets rouges « menace »
            d["EnemyHull"] = Lit("MGSUGE_Ennemi_Coque", Hex("#2A2E39"), 0.55f, 0.5f);
            d["EnemyDark"] = Lit("MGSUGE_Ennemi_Sombre", Hex("#16171D"), 0.4f, 0.4f);
            d["EnemyGlow"] = Lit("MGSUGE_Ennemi_Lumiere", Hex("#FF4458"), 0f, 0.5f, new Color(1f, 0.12f, 0.18f) * 4f);
            d["EnemyCore"] = Lit("MGSUGE_Ennemi_Noyau", Hex("#241C2A"), 0.4f, 0.5f, new Color(1f, 0.1f, 0.2f) * 1.5f);
            d["EnemyEngine"] = Lit("MGSUGE_Ennemi_Reacteur", Hex("#FF7A3A"), 0f, 0.5f, new Color(1f, 0.45f, 0.15f) * 5f);
            d["Shell"] = Lit("MGSUGE_Obus", Hex("#FF8A2A"), 0f, 0.5f, new Color(1f, 0.5f, 0.15f) * 6f);
            d["Telegraph"] = Additive("MGSUGE_Telegraphe", new Color(1f, 0.32f, 0.12f, 0.55f));
            d["TelegraphFill"] = Additive("MGSUGE_Telegraphe_Remplissage", new Color(1f, 0.25f, 0.1f, 0.22f));
            // avant-postes (§13.3) et decors provisoires des contrats
            d["OutpostHull"] = Lit("MGSUGE_AvantPoste_Coque", Hex("#2B2730"), 0.5f, 0.45f);
            d["OutpostTrim"] = Lit("MGSUGE_AvantPoste_Liseret", Hex("#FF2A3A"), 0f, 0.5f, new Color(1f, 0.08f, 0.12f) * 4f);
            d["PylonGlow"] = Lit("MGSUGE_Pylone_Lumiere", Hex("#6A7BFF"), 0f, 0.5f, new Color(0.35f, 0.45f, 1f) * 4f);
            d["Wreck"] = Lit("MGSUGE_Epave_Coque", Hex("#3A3530"), 0.6f, 0.35f);
            d["TestGround"] = Lit("MGSUGE_Sol_Essai", Hex("#1E1A22"), 0.1f, 0.2f);
            // textures generees : point doux des particules, croute de la lave
            var dot = SoftDotTexture();
            d["Particles"].SetTexture("_BaseMap", dot); EditorUtility.SetDirty(d["Particles"]);
            var crust = LavaCrustTexture();
            d["Lava"].SetTexture("_BaseMap", crust); d["Lava"].SetTexture("_EmissionMap", crust);
            d["Lava"].SetTextureScale("_BaseMap", new Vector2(6f, 15f)); EditorUtility.SetDirty(d["Lava"]);
            AssetDatabase.SaveAssets();
            return d;
        }

        // ------------------------------------------------------------------ donnees
        public class Data
        {
            public FrameStatsData frame; public CameraData cam; public WeaponData weapons; public WorldData world;
            public ProgressionData progression; public PoseLibrary poses; public PaletteData player, corbeau;
            public SentinelData sentinel; public FrameEnemyData corbeauData; public TurretData turret; public RelayData relay; public FighterData fighter; public SpiderData spider;
            public CampaignData campaign; public OutpostData outposts; public PatrolData patrols;
            public ContractData[] contracts; public ContractData testContract;
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
            BuildGameplayData(d);
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


        // ------------------------------------------------------------------ donnees de jeu du jalon 5
        static void BuildGameplayData(Data d)
        {
            string ed = DataDir + "/Ennemis", cd = DataDir + "/Contrats";
            EnsureFolder(ed); EnsureFolder(cd);
            Color threat = Hex("#FF4458");
            d.sentinel = LoadOrCreate<SentinelData>(ed + "/MGSUGE_Ennemi_Sentinelle.asset", x =>
            { x.displayName = "Sentinelle"; x.credits = 120; x.maxHP = 650; x.radius = 3.8f; x.impactMax = 0; x.patrolRadius = 60; x.patrolSpeed = 14; x.shotColor = threat; });
            d.corbeauData = LoadOrCreate<FrameEnemyData>(ed + "/MGSUGE_Ennemi_Corbeau.asset", x =>
            { x.displayName = "Châssis hostile « Corbeau »"; x.credits = 900; x.maxHP = 4200; x.radius = 5f; x.impactMax = 2400; x.patrolRadius = 80; x.patrolSpeed = 12; x.shotColor = threat; x.shotLength = 7f; });
            d.turret = LoadOrCreate<TurretData>(ed + "/MGSUGE_Ennemi_Tourelle.asset", x =>
            { x.displayName = "Tourelle de défense"; x.credits = 250; x.maxHP = 1800; x.radius = 4.6f; x.impactMax = 0; x.shotColor = new Color(1f, 0.45f, 0.2f); });
            d.relay = LoadOrCreate<RelayData>(ed + "/MGSUGE_Ennemi_Relais.asset", x =>
            { x.displayName = "Relais de brouillage"; x.credits = 400; x.maxHP = 5200; x.radius = 7f; x.impactMax = 3800; });
            d.fighter = LoadOrCreate<FighterData>(ed + "/MGSUGE_Ennemi_Faucon.asset", x =>
            { x.displayName = "Chasseur « Faucon »"; x.credits = 300; x.maxHP = 1500; x.radius = 5f; x.impactMax = 0; x.aggroRange = 1100; x.contractAggroRange = 1100; x.patrolRadius = 400; x.patrolSpeed = 70; x.shotColor = threat; });
            d.spider = LoadOrCreate<SpiderData>(ed + "/MGSUGE_Ennemi_Araignee.asset", x =>
            { x.displayName = "Araignée de siège"; x.credits = 450; x.maxHP = 3000; x.radius = 7f; x.impactMax = 2000; x.patrolRadius = 40; x.patrolSpeed = 6; });
            d.campaign = LoadOrCreate<CampaignData>(DataDir + "/MGSUGE_Campagne.asset");
            d.outposts = LoadOrCreate<OutpostData>(DataDir + "/MGSUGE_AvantPostes.asset");
            d.patrols = LoadOrCreate<PatrolData>(DataDir + "/MGSUGE_Patrouilles.asset");
            ContractData C(int n, string file, string title, string loc, long reward, string desc, string note)
                => LoadOrCreate<ContractData>($"{cd}/MGSUGE_Contrat_{n}_{file}.asset", x => { x.title = title; x.location = loc; x.reward = reward; x.description = desc; x.enemiesNote = note; });
            d.contracts = new[]
            {
                C(1, "Lyra", "Relais de brouillage de Lyra", "Champ de quartz", 2600, "Trois relais brouillent les communications du champ de quartz. Détruisez-les.", "3 relais, 1 châssis, 4 sentinelles"),
                C(2, "Kessler", "Purge de la Porte de Kessler", "Cratère", 3000, "Des tourelles tiennent les pylônes du cratère, protégées par un châssis. Neutralisez la position.", "4 tourelles sur les pylônes, 1 châssis, 3 sentinelles"),
                C(3, "Thalassa", "Pillards de l'épave Thalassa", "Épave", 3200, "Deux châssis pillent la coque du Thalassa sous la couverture de tourelles et de sentinelles.", "2 châssis, tourelles sur la coque, 5 sentinelles"),
                C(4, "Orsk", "Raffinerie d'Orsk", "Raffinerie", 3400, "Des araignées de siège pilonnent la raffinerie depuis le sol, couvertes par une escadrille de chasseurs.", "2 araignées de siège, 2 relais, 3 chasseurs"),
                C(5, "Ascenseur", "Ascenseur orbital", "Crêtes", 3800, "Deux relais tiennent le pied de l'ascenseur et la station à 2 450 m. Passez en forme vaisseau (T) pour monter.", "2 relais (pied et station à 2 450 m), 1 châssis, 4 sentinelles entre 500 et 2 000 m"),
                C(6, "Helix", "Spatioport Hélix", "Spatioport", 4000, "Un escadron de chasseurs a pris le spatioport. Abattez-les et détruisez les châssis qui gardent les pistes.", "5 chasseurs, 2 châssis, 1 araignée"),
                C(7, "Ceinture", "Ceinture flottante", "Haute altitude (200 ; 800 ; -1 800)", 4400, "Relais sur les stations flottantes, tourelles sur les croiseurs. Volez entre les astéroïdes.", "2 relais sur stations flottantes, 3 tourelles sur croiseurs, 6 sentinelles, 2 chasseurs"),
            };
            d.testContract = LoadOrCreate<ContractData>($"{cd}/MGSUGE_Contrat_Essai.asset", x =>
            { x.title = "Contrat d'essai"; x.location = "Terrain d'essai"; x.reward = 2600; x.description = "Détruisez la petite garnison au sud. À chaque fin de contrat, la menace augmente (cycle d'un seul contrat)."; x.enemiesNote = "2 sentinelles, 1 tourelle, 1 relais"; });
            foreach (var o in new Object[] { d.sentinel, d.corbeauData, d.turret, d.relay, d.fighter, d.spider, d.campaign, d.outposts, d.patrols, d.testContract }) EditorUtility.SetDirty(o);
        }

        // ------------------------------------------------------------------ textures generees
        static Texture2D SaveTexture(string name, int n, Func<int, int, Color32> px, bool alphaIsTransparency, TextureWrapMode wrap)
        {
            EnsureFolder(GenDir);
            string path = $"{GenDir}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var c = new Color32[n * n];
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) c[j * n + i] = px(i, j);
            tex.SetPixels32(c); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.alphaIsTransparency = alphaIsTransparency; imp.wrapMode = wrap; imp.mipmapEnabled = true; imp.sRGBTexture = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D SoftDotTexture() => SaveTexture("MGSUGE_Point_Doux", 64, (i, j) =>
        {
            float c = 31.5f, d = Mathf.Sqrt((i - c) * (i - c) + (j - c) * (j - c)) / c;
            float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
            return new Color32(255, 255, 255, (byte)(a * 255f));
        }, true, TextureWrapMode.Clamp);
        static Texture2D LavaCrustTexture() => SaveTexture("MGSUGE_Lave_Croute", 256, (i, j) =>
        {
            float n = 0f, a = 0.5f, f = 4f / 256f;
            for (int o = 0; o < 4; o++) { n += a * Mathf.PerlinNoise(i * f * Mathf.Pow(2, o) + 31.7f * o, j * f * Mathf.Pow(2, o) + 11.3f * o); a *= 0.5f; }
            float crust = Mathf.Clamp01((n - 0.42f) * 4f);
            byte g = (byte)(255 * (1f - crust));
            return new Color32(g, g, g, 255);
        }, false, TextureWrapMode.Repeat);

        // ------------------------------------------------------------------ prefab du joueur
        static GameObject BuildPlayerPrefab(Data d)
        {
            EnsureFolder(PrefabDir + "/Joueur");
            // ancien emplacement (jalons 3-4) : deplace pour garder les references
            string old = PrefabDir + "/MGSUGE_Player.prefab";
            if (File.Exists(old) && !File.Exists(PlayerPrefab)) AssetDatabase.MoveAsset(old, PlayerPrefab);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var root = new GameObject("MGSUGE_Player");
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
            Transform F(string n) => MathX.FindDeep(inst.transform, n);
            wc.muzzle = F("Muzzle"); wc.muzzleL = F("MuzzleL"); wc.podOut = F("PodOut"); wc.pod = F("MissilePod");
            wc.blade = F("Blade"); wc.bladeGrip = F("BladeGrip"); wc.bladeEnergy = wc.blade != null ? MathX.FindDeep(wc.blade, "Blade_Energy") : null;
            var pr = root.AddComponent<ProbeController>(); pr.data = d.weapons; pr.player = pc; pr.lockOn = lk; pr.rig = rig;
            pr.probeSockets = new List<Transform>();
            for (int i = 1; i <= 6; i++) { var t = F("Probe_" + i); if (t != null) pr.probeSockets.Add(t); }
            var th = root.AddComponent<ThrusterFX>(); th.player = pc;
            th.flames = new List<Transform>();
            foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Flame_")) th.flames.Add(t);
            var pa = root.AddComponent<PaletteApplier>(); pa.palette = d.player;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ outils
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'), leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
        /// <summary>Cree un objet vide range sous parent.</summary>
        static Transform Group(string name, Transform parent = null, Vector3 pos = default)
        {
            var go = new GameObject(name); if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; return go.transform;
        }
        /// <summary>Objet visible : maillage + materiau (+ collision optionnelle), echelle et rotation locales.</summary>
        static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot, bool meshCollider = false, int layer = 0, bool shadows = true)
        {
            var go = new GameObject(name); go.layer = layer;
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            if (!shadows) { mr.shadowCastingMode = ShadowCastingMode.Off; }
            if (meshCollider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
        static void SetLayer(GameObject go, int layer) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
        static GameObject Instance(GameObject prefab, Transform parent, Vector3 worldPos, Quaternion rot, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(worldPos, rot);
            if (name != null) go.name = name;
            Rec(go);
            return go;
        }
        /// <summary>Enregistre les modifications faites par script sur une instance de prefab (sinon perdues a la sauvegarde).</summary>
        static void Rec(GameObject go)
        {
            if (go == null || !PrefabUtility.IsPartOfPrefabInstance(go)) return;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        static void Deactivate(GameObject go) { go.SetActive(false); Rec(go); }
    }
}
