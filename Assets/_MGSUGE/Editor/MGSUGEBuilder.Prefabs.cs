using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>Prefabs : effets, ennemis, cibles d'essai, tour de l'arcologie, avant-postes.</summary>
    public static partial class MGSUGEBuilder
    {
        public class Prefabs
        {
            public GameObject player, projectile, missile, flash, beam, shell, telegraph;
            public GameObject sentinel, corbeau, turret, relay, fighter, spider;
            public GameObject dummySentinel, dummyRelay, dummyCorbeau;
            public GameObject tower, outpostBuilding;
            public GameObject[] outposts = new GameObject[5];
        }
        static readonly string[] OutpostNames = { "Patrouille", "Aerien", "Blinde", "Siege", "Mixte" };

        static string P(string folder, string name) { EnsureFolder($"{PrefabDir}/{folder}"); return $"{PrefabDir}/{folder}/{name}.prefab"; }
        static GameObject Save(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
        static GameObject LoadPrefab(string folder, string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{folder}/{name}.prefab");

        static Prefabs LoadPrefabs()
        {
            var p = new Prefabs
            {
                player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab),
                projectile = LoadPrefab("FX", "MGSUGE_Tir"), missile = LoadPrefab("FX", "MGSUGE_Missile"), flash = LoadPrefab("FX", "MGSUGE_Eclair"),
                beam = LoadPrefab("FX", "MGSUGE_Rayon"), shell = LoadPrefab("FX", "MGSUGE_Obus"), telegraph = LoadPrefab("FX", "MGSUGE_Telegraphe"),
                sentinel = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Sentinelle"), corbeau = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Corbeau"),
                turret = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Tourelle"), relay = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Relais"),
                fighter = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Faucon"), spider = LoadPrefab("Ennemis", "MGSUGE_Ennemi_Araignee"),
                dummySentinel = LoadPrefab("Cibles", "MGSUGE_Cible_Sentinelle"), dummyRelay = LoadPrefab("Cibles", "MGSUGE_Cible_Relais"), dummyCorbeau = LoadPrefab("Cibles", "MGSUGE_Cible_Corbeau"),
                tower = LoadPrefab("Monde", "MGSUGE_Tour"), outpostBuilding = LoadPrefab("AvantPostes", "MGSUGE_AvantPoste_Batiment"),
            };
            for (int i = 0; i < 5; i++) p.outposts[i] = LoadPrefab("AvantPostes", "MGSUGE_AvantPoste_" + OutpostNames[i]);
            return p;
        }

        static Prefabs BuildPrefabs(Data d, Mats m, Meshes me)
        {
            var p = new Prefabs();
            p.player = BuildPlayerPrefab(d);
            BuildFxPrefabs(p, m, me);
            BuildEnemyPrefabs(p, d, m, me);
            BuildDummyPrefabs(p, d, m, me);
            p.tower = BuildTowerPrefab(m, me);
            BuildOutpostPrefabs(p, d, m, me);
            AssetDatabase.SaveAssets();
            return p;
        }

        // ------------------------------------------------------------------ effets
        static void BuildFxPrefabs(Prefabs p, Mats m, Meshes me)
        {
            var go = new GameObject("MGSUGE_Tir");
            Part("Fuseau", go.transform, me["Tir_Fuseau"], m["Projectile"], Vector3.zero, Vector3.one, Quaternion.identity, shadows: false).GetComponent<MeshRenderer>().receiveShadows = false;
            p.projectile = Save(go, P("FX", "MGSUGE_Tir"));

            go = new GameObject("MGSUGE_Missile");
            Part("Corps", go.transform, me["Missile_Cone"], m["MissileBody"], Vector3.zero, Vector3.one, Quaternion.identity, shadows: false);
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = m["Trail"]; tr.time = 0.45f; tr.minVertexDistance = 2f;
            tr.widthCurve = AnimationCurve.Linear(0f, 0.9f, 1f, 0f);
            tr.shadowCastingMode = ShadowCastingMode.Off; tr.receiveShadows = false;
            p.missile = Save(go, P("FX", "MGSUGE_Missile"));

            go = new GameObject("MGSUGE_Eclair");
            go.AddComponent<MeshFilter>().sharedMesh = me["Sphere_1"];
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m["Flash"]; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            p.flash = Save(go, P("FX", "MGSUGE_Eclair"));

            go = new GameObject("MGSUGE_Rayon");
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = m["Beam"]; lr.positionCount = 2; lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.startWidth = lr.endWidth = 0.35f;
            p.beam = Save(go, P("FX", "MGSUGE_Rayon"));

            go = new GameObject("MGSUGE_Obus");
            Part("Corps", go.transform, me["Sphere_1"], m["Shell"], Vector3.zero, Vector3.one * 1.3f, Quaternion.identity, shadows: false);
            var st = go.AddComponent<TrailRenderer>();
            st.sharedMaterial = m["Trail"]; st.time = 0.6f; st.minVertexDistance = 3f; st.widthCurve = AnimationCurve.Linear(0f, 1.4f, 1f, 0f);
            st.startColor = new Color(1f, 0.55f, 0.2f, 1f); st.endColor = new Color(1f, 0.3f, 0.1f, 0f); st.shadowCastingMode = ShadowCastingMode.Off;
            p.shell = Save(go, P("FX", "MGSUGE_Obus"));

            go = new GameObject("MGSUGE_Telegraphe");
            var tg = go.AddComponent<Telegraph>();
            tg.ring = Part("Anneau", go.transform, me["Telegraphe_Anneau"], m["Telegraph"], Vector3.zero, Vector3.one, Quaternion.identity, shadows: false).transform;
            tg.fill = Part("Remplissage", go.transform, me["Disque"], m["TelegraphFill"], Vector3.up * 0.1f, Vector3.one, Quaternion.identity, shadows: false).transform;
            p.telegraph = Save(go, P("FX", "MGSUGE_Telegraphe"));
        }

        // ------------------------------------------------------------------ ennemis (§13.1)
        static GameObject EnemyRoot<T>(string name, out T enemy, WeaponData weapons) where T : Enemy
        {
            var go = new GameObject(name);
            go.AddComponent<Targetable>();
            enemy = go.AddComponent<T>();
            enemy.weaponData = weapons;
            return go;
        }

        static void BuildEnemyPrefabs(Prefabs p, Data d, Mats m, Meshes me)
        {
            Quaternion I = Quaternion.identity;
            // Sentinelle : noyau octaedrique de 3,8 m, anneau, oeil rouge
            var go = EnemyRoot<SentinelEnemy>("MGSUGE_Ennemi_Sentinelle", out var se, d.weapons); se.data = d.sentinel;
            Part("Noyau", go.transform, me["Octaedre_Sentinelle"], m["EnemyHull"], Vector3.zero, Vector3.one * 3.8f, I);
            se.ring = Part("Anneau", go.transform, me["Tore_Sentinelle"], m["EnemyGlow"], Vector3.zero, Vector3.one, Quaternion.Euler(12f, 0f, 0f)).transform;
            Part("Oeil", go.transform, me["Sphere_1"], m["EnemyGlow"], new Vector3(0f, 0f, 3.1f), Vector3.one * 0.9f, I);
            se.muzzle = Group("Bouche", go.transform, new Vector3(0f, 0f, 4.2f));
            SetLayer(go, Layers.Enemy); p.sentinel = Save(go, P("Ennemis", "MGSUGE_Ennemi_Sentinelle"));

            // Relais : noyau de 7 m, 2 anneaux
            go = EnemyRoot<RelayEnemy>("MGSUGE_Ennemi_Relais", out var re, d.weapons); re.data = d.relay;
            re.core = Part("Noyau", go.transform, me["Octaedre_Relais"], m["EnemyCore"], Vector3.zero, Vector3.one * 7f, I).transform;
            re.ringA = Part("Anneau_1", go.transform, me["Tore_Relais_1"], m["EnemyGlow"], Vector3.zero, Vector3.one, Quaternion.Euler(20f, 0f, 0f)).transform;
            re.ringB = Part("Anneau_2", go.transform, me["Tore_Relais_2"], m["EnemyGlow"], Vector3.zero, Vector3.one, Quaternion.Euler(-35f, 40f, 0f)).transform;
            SetLayer(go, Layers.Enemy); p.relay = Save(go, P("Ennemis", "MGSUGE_Ennemi_Relais"));

            // Tourelle : socle, tete pivotante, canon double ; le pivot de l'objet est le centre de la sphere de touche
            go = EnemyRoot<TurretEnemy>("MGSUGE_Ennemi_Tourelle", out var tu, d.weapons); tu.data = d.turret;
            Part("Socle", go.transform, me["Cylindre_8"], m["EnemyDark"], new Vector3(0f, -2.5f, 0f), new Vector3(3.4f, 2.4f, 3.4f), I);
            tu.head = Group("Tete", go.transform, new Vector3(0f, 0.2f, 0f));
            Part("Tete_Corps", tu.head, me["Boite"], m["EnemyHull"], new Vector3(0f, -1f, 0f), new Vector3(4.2f, 2.6f, 4.6f), I);
            Part("Tete_Liseret", tu.head, me["Boite"], m["EnemyGlow"], new Vector3(0f, 1.6f, 0f), new Vector3(4.3f, 0.25f, 4.7f), I);
            tu.barrel = Group("Canon", tu.head, new Vector3(0f, 0.4f, 1.8f));
            Part("Tube_G", tu.barrel, me["Boite"], m["EnemyDark"], new Vector3(-0.8f, -0.3f, 2.5f), new Vector3(0.6f, 0.6f, 5f), I);
            Part("Tube_D", tu.barrel, me["Boite"], m["EnemyDark"], new Vector3(0.8f, -0.3f, 2.5f), new Vector3(0.6f, 0.6f, 5f), I);
            tu.muzzle = Group("Bouche", tu.barrel, new Vector3(0f, 0f, 5.4f));
            SetLayer(go, Layers.Enemy); p.turret = Save(go, P("Ennemis", "MGSUGE_Ennemi_Tourelle"));

            // Chasseur « Faucon »
            go = EnemyRoot<FighterEnemy>("MGSUGE_Ennemi_Faucon", out var fe, d.weapons); fe.data = d.fighter;
            Part("Fuselage", go.transform, me["Faucon_Fuselage"], m["EnemyHull"], Vector3.zero, Vector3.one, I);
            Part("Cockpit", go.transform, me["Sphere_1"], m["EnemyGlow"], new Vector3(0f, 0.7f, 2.2f), new Vector3(0.7f, 0.45f, 1.6f), I);
            Part("Reacteur", go.transform, me["Sphere_1"], m["EnemyEngine"], new Vector3(0f, 0.2f, -6.1f), new Vector3(0.8f, 0.8f, 0.5f), I, shadows: false);
            fe.muzzle = Group("Bouche", go.transform, new Vector3(0f, 0f, 8f));
            SetLayer(go, Layers.Enemy); p.fighter = Save(go, P("Ennemis", "MGSUGE_Ennemi_Faucon"));

            // Araignee de siege : corps, mortier, 8 pattes (pivot a la hanche)
            go = EnemyRoot<SpiderEnemy>("MGSUGE_Ennemi_Araignee", out var sp, d.weapons); sp.data = d.spider;
            sp.body = Group("Corps", go.transform);
            Part("Abdomen", sp.body, me["Sphere_1"], m["EnemyHull"], new Vector3(0f, 0.5f, -2.5f), new Vector3(5.2f, 3.2f, 6.5f), I);
            Part("Thorax", sp.body, me["Boite"], m["EnemyDark"], new Vector3(0f, -0.6f, 2.2f), new Vector3(5f, 2.4f, 4.5f), I);
            Part("Liseret", sp.body, me["Boite"], m["EnemyGlow"], new Vector3(0f, 1.85f, 2.2f), new Vector3(5.1f, 0.25f, 4.6f), I);
            var mortar = Group("Mortier_Pivot", sp.body, new Vector3(0f, 2f, 1.5f));
            mortar.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            Part("Mortier_Tube", mortar, me["Cylindre_8"], m["EnemyDark"], Vector3.zero, new Vector3(0.9f, 4f, 0.9f), Quaternion.Euler(90f, 0f, 0f));
            sp.mortar = Group("Bouche", mortar, new Vector3(0f, 0f, 4.4f));
            var legs = new List<Transform>();
            for (int i = 0; i < 8; i++)
            {
                bool left = i < 4; int k = i % 4; float z = 3.2f - k * 2.1f;
                var hip = Group($"Patte_{i + 1}", go.transform, new Vector3(left ? -2.4f : 2.4f, 0.2f, z));
                hip.localRotation = Quaternion.Euler(0f, (left ? -90f : 90f) + (k - 1.5f) * 22f * (left ? -1f : 1f), 0f);
                var upper = Group("Femur", hip); upper.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                Part("Femur_Piece", upper, me["Boite"], m["EnemyHull"], new Vector3(0f, -0.4f, 2.6f), new Vector3(0.8f, 0.8f, 5.6f), I);
                var lower = Group("Tibia", upper, new Vector3(0f, 0f, 5.2f)); lower.localRotation = Quaternion.Euler(95f, 0f, 0f);
                Part("Tibia_Piece", lower, me["Boite"], m["EnemyDark"], new Vector3(0f, -0.3f, 3.7f), new Vector3(0.6f, 0.6f, 7.6f), I);
                legs.Add(hip);
            }
            sp.legs = legs.ToArray();
            SetLayer(go, Layers.Enemy); p.spider = Save(go, P("Ennemis", "MGSUGE_Ennemi_Araignee"));

            // Chassis hostile « Corbeau » : meme modele que le joueur, palette hostile, anime par FrameRig
            go = EnemyRoot<FrameEnemy>("MGSUGE_Ennemi_Corbeau", out var co, d.weapons); co.data = d.corbeauData;
            var visual = Group("Visuel", go.transform);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), visual);
            model.name = "MGSUGE_Vesper_Hostile";
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Flame_") || t.name == "Blade_Energy") Deactivate(t.gameObject);
            var nose = MathX.FindDeep(model.transform, "Nose"); if (nose != null) { nose.localScale = Vector3.zero; Rec(nose.gameObject); }
            var rig = go.AddComponent<FrameRig>(); rig.poses = d.poses; rig.model = model.transform;
            co.rig = rig; co.muzzle = MathX.FindDeep(model.transform, "Muzzle"); co.podOut = MathX.FindDeep(model.transform, "PodOut");
            var pa = go.AddComponent<PaletteApplier>(); pa.palette = d.corbeau;
            SetLayer(go, Layers.Enemy); p.corbeau = Save(go, P("Ennemis", "MGSUGE_Ennemi_Corbeau"));
        }

        // ------------------------------------------------------------------ cibles d'entrainement (scene Test)
        static void BuildDummyPrefabs(Prefabs p, Data d, Mats m, Meshes me)
        {
            Quaternion I = Quaternion.identity;
            GameObject Dummy(string name, string label, float hp, float r, float impact, long credits, out TargetDummy dm)
            {
                var go = new GameObject(name);
                var t = go.AddComponent<Targetable>();
                t.displayName = label; t.maxHP = hp; t.HP = hp; t.radius = r; t.impactMax = impact; t.credits = credits; t.weaponData = d.weapons;
                dm = go.AddComponent<TargetDummy>();
                return go;
            }
            var g = Dummy("MGSUGE_Cible_Sentinelle", "Sentinelle d'essai", 650f, 3.8f, 0f, 120, out var dm1);
            dm1.orbitRadius = 60f; dm1.orbitSpeed = 0.3f;
            Part("Noyau", g.transform, me["Octaedre_Sentinelle"], m["Sentinel"], Vector3.zero, Vector3.one * 3.8f, I);
            Part("Anneau", g.transform, me["Tore_Sentinelle"], m["RelayRing"], Vector3.zero, Vector3.one, I);
            SetLayer(g, Layers.Enemy); p.dummySentinel = Save(g, P("Cibles", "MGSUGE_Cible_Sentinelle"));

            g = Dummy("MGSUGE_Cible_Relais", "Relais d'essai", 5200f, 7f, 3800f, 400, out var dm2);
            dm2.orbitRadius = 0.01f; dm2.orbitSpeed = 0.5f; dm2.bobAmplitude = 3f;
            Part("Noyau", g.transform, me["Octaedre_Relais"], m["Relay"], Vector3.zero, Vector3.one * 7f, I);
            Part("Anneau_1", g.transform, me["Tore_Relais_1"], m["RelayRing"], Vector3.zero, Vector3.one, Quaternion.Euler(20f, 0f, 0f));
            Part("Anneau_2", g.transform, me["Tore_Relais_2"], m["RelayRing"], Vector3.zero, Vector3.one, Quaternion.Euler(-35f, 40f, 0f));
            SetLayer(g, Layers.Enemy); p.dummyRelay = Save(g, P("Cibles", "MGSUGE_Cible_Relais"));

            g = Dummy("MGSUGE_Cible_Corbeau", "Châssis « Corbeau » (essai)", 4200f, 5f, 2400f, 900, out var dm3);
            dm3.orbitRadius = 70f; dm3.orbitSpeed = 0.35f; dm3.followGround = true; dm3.groundClear = d.frame.frameClear;
            var visual = Group("Visuel", g.transform);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), visual);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Flame_") || t.name == "Blade_Energy") Deactivate(t.gameObject);
            var nose = MathX.FindDeep(model.transform, "Nose"); if (nose != null) { nose.localScale = Vector3.zero; Rec(nose.gameObject); }
            var rig = g.AddComponent<FrameRig>(); rig.poses = d.poses; rig.model = model.transform;
            g.AddComponent<PaletteApplier>().palette = d.corbeau;
            SetLayer(g, Layers.Enemy); p.dummyCorbeau = Save(g, P("Cibles", "MGSUGE_Cible_Corbeau"));
        }

        // ------------------------------------------------------------------ tour de l'arcologie
        static GameObject BuildTowerPrefab(Mats m, Meshes me)
        {
            Quaternion I = Quaternion.identity;
            var go = new GameObject("MGSUGE_Tour"); go.layer = Layers.World;
            var tw = go.AddComponent<ArcologyTower>();
            tw.box = go.AddComponent<BoxCollider>();
            tw.accent = m["AccentOrange"];
            tw.body = Part("Corps", go.transform, me["Boite"], m["Structure"], Vector3.zero, Vector3.one, I, false, Layers.World).transform;
            tw.edges = new Transform[4];
            for (int k = 0; k < 4; k++) tw.edges[k] = Part($"Liseret_{k + 1}", go.transform, me["Boite"], m["AccentOrange"], Vector3.zero, Vector3.one, I, false, Layers.World, false).transform;
            tw.windowBands = new Transform[11];
            for (int f = 0; f < 11; f++) tw.windowBands[f] = Part($"Fenetres_{f + 1}", go.transform, me["Boite"], m["AccentOrange"], Vector3.zero, Vector3.one, I, false, Layers.World, false).transform;
            var ant = Group("Antenne", go.transform); ant.gameObject.layer = Layers.World; tw.antenna = ant;
            Part("Mat", ant, me["Cone_6"], m["Structure"], Vector3.zero, new Vector3(1.2f, 24f, 1.2f), I, false, Layers.World);
            var red = Part("Feu_Rouge", ant, me["Sphere_0"], m["RedLight"], new Vector3(0f, 25f, 0f), Vector3.one * 2.2f, I, false, Layers.World, false);
            var bl = red.AddComponent<Blinker>(); bl.target = red.GetComponent<Renderer>();
            tw.Layout();
            return Save(go, P("Monde", "MGSUGE_Tour"));
        }

        // ------------------------------------------------------------------ avant-postes (§13.3)
        static void BuildOutpostPrefabs(Prefabs p, Data d, Mats m, Meshes me)
        {
            Quaternion I = Quaternion.identity;
            // batiment commun : bloc de coque 34 x 12 x 22 avec lisere rouge, tour de 36 m + bloc + feu rouge, 4 murets bas
            var b = new GameObject("MGSUGE_AvantPoste_Batiment"); b.layer = Layers.World;
            var hull = Part("Coque", b.transform, me["Boite"], m["OutpostHull"], Vector3.zero, new Vector3(34f, 12f, 22f), I, false, Layers.World);
            var bc = hull.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 0.5f, 0f); bc.size = Vector3.one;
            Part("Liseret", b.transform, me["Boite"], m["OutpostTrim"], new Vector3(0f, 10.6f, 0f), new Vector3(34.6f, 0.7f, 22.6f), I, false, Layers.World, false);
            var tower = Part("Tour", b.transform, me["Boite"], m["OutpostHull"], new Vector3(12f, 0f, 16f), new Vector3(6f, 36f, 6f), I, false, Layers.World);
            var tc = tower.AddComponent<BoxCollider>(); tc.center = new Vector3(0f, 0.5f, 0f); tc.size = Vector3.one;
            Part("Tour_Bloc", b.transform, me["Boite"], m["OutpostHull"], new Vector3(12f, 36f, 16f), new Vector3(10f, 5f, 10f), I, false, Layers.World);
            Part("Tour_Liseret", b.transform, me["Boite"], m["OutpostTrim"], new Vector3(12f, 39.5f, 16f), new Vector3(10.3f, 0.5f, 10.3f), I, false, Layers.World, false);
            var red = Part("Feu_Rouge", b.transform, me["Sphere_0"], m["RedLight"], new Vector3(12f, 43f, 16f), Vector3.one * 1.8f, I, false, Layers.World, false);
            var bl = red.AddComponent<Blinker>(); bl.target = red.GetComponent<Renderer>();
            Vector3[] wallPos = { new Vector3(0f, 0f, -26f), new Vector3(0f, 0f, 30f), new Vector3(-30f, 0f, 2f), new Vector3(30f, 0f, 2f) };
            Vector3[] wallSize = { new Vector3(50f, 3f, 1.5f), new Vector3(50f, 3f, 1.5f), new Vector3(1.5f, 3f, 46f), new Vector3(1.5f, 3f, 46f) };
            for (int i = 0; i < 4; i++)
            {
                var w = Part($"Muret_{i + 1}", b.transform, me["Boite"], m["OutpostHull"], wallPos[i], wallSize[i], I, false, Layers.World);
                var wc = w.AddComponent<BoxCollider>(); wc.center = new Vector3(0f, 0.5f, 0f); wc.size = Vector3.one;
            }
            p.outpostBuilding = Save(b, P("AvantPostes", "MGSUGE_AvantPoste_Batiment"));

            // 5 garnisons ; positions relatives au centre du batiment (y = hauteur au-dessus du sol)
            Vector3 roof = new Vector3(-6f, 12f + 2.6f, -2f), towerTop = new Vector3(12f, 41f + 2.6f, 16f);
            var garrisons = new (GameObject prefab, Vector3 pos)[][]
            {
                new[] { (p.sentinel, new Vector3(-35f, 60f, 25f)), (p.sentinel, new Vector3(35f, 70f, -20f)), (p.sentinel, new Vector3(0f, 80f, 40f)), (p.turret, roof) },
                new[] { (p.fighter, new Vector3(-60f, 200f, 0f)), (p.fighter, new Vector3(60f, 220f, 0f)), (p.sentinel, new Vector3(0f, 70f, 35f)), (p.turret, towerTop) },
                new[] { (p.corbeau, new Vector3(-55f, 7f, 40f)), (p.sentinel, new Vector3(-30f, 65f, -30f)), (p.sentinel, new Vector3(30f, 75f, 30f)), (p.turret, roof) },
                new[] { (p.spider, new Vector3(60f, 5f, -45f)), (p.turret, roof), (p.turret, towerTop), (p.sentinel, new Vector3(0f, 70f, -35f)) },
                new[] { (p.spider, new Vector3(-60f, 5f, -45f)), (p.fighter, new Vector3(0f, 210f, 60f)), (p.sentinel, new Vector3(-30f, 65f, 30f)), (p.sentinel, new Vector3(30f, 75f, 30f)) },
            };
            for (int i = 0; i < 5; i++)
            {
                var root = new GameObject("MGSUGE_AvantPoste_" + OutpostNames[i]);
                var op = root.AddComponent<Outpost>(); op.data = d.outposts; op.type = (OutpostType)i;
                var bi = (GameObject)PrefabUtility.InstantiatePrefab(p.outpostBuilding, root.transform); bi.name = "Batiment"; Rec(bi);
                var gar = Group("Garnison", root.transform);
                int n = 0;
                foreach (var (prefab, pos) in garrisons[i])
                {
                    var e = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gar);
                    e.transform.localPosition = pos; e.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    e.name = $"{prefab.name.Replace("MGSUGE_Ennemi_", "")}_{++n}";
                    Deactivate(e);
                }
                p.outposts[i] = Save(root, P("AvantPostes", "MGSUGE_AvantPoste_" + OutpostNames[i]));
            }
        }
    }
}
