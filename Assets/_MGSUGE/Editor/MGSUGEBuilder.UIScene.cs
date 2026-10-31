using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>
    /// Interface du jalon 6 enregistree dans les scenes (groupe UI de la Hierarchy) : EventSystem, canvas Surimpression,
    /// HUD et Ecrans (titre, pause, ameliorations, commandes, chassis detruit). Tout est modifiable ensuite dans l'Inspector.
    /// </summary>
    public static partial class MGSUGEBuilder
    {
        [MenuItem("MGSUGE/6. Mettre a jour l'interface (scenes World et Test)", priority = 42)]
        public static void UpdateInterfaceMenu()
        {
            if (!EditorUtility.DisplayDialog("MGSUGE", "L'interface (groupe UI) des scenes World et Test va etre reconstruite.\n" +
                "Le reste des scenes (terrain, ennemis, contrats, avant-postes, vos reglages) n'est pas modifie.\n\nContinuer ?", "Mettre a jour", "Annuler")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("MGSUGE", "Polices, sprites, carte, prefabs d'interface", 0.2f);
                var data = BuildData();
                var ui = BuildUiAssets(data);
                AssetDatabase.SaveAssets();
                foreach (var path in new[] { WorldScenePath, TestScenePath })
                {
                    if (!File.Exists(path)) continue;
                    EditorUtility.DisplayProgressBar("MGSUGE", "Interface de " + Path.GetFileNameWithoutExtension(path), path == WorldScenePath ? 0.5f : 0.8f);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    data = BuildData(); ui = LoadUiAssets(data);
                    ReplaceInterface(scene, data, ui, path == WorldScenePath);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                if (File.Exists(WorldScenePath)) EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
                Debug.Log("[MGSUGE] Interface mise a jour dans les scenes World et Test.");
            }
            catch (System.Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("MGSUGE", "Erreur pendant la mise a jour :\n" + e.Message, "OK"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        /// <summary>Recharge les assets d'interface deja construits (apres l'ouverture d'une scene, qui decharge les references).</summary>
        static UiAssets LoadUiAssets(Data d)
        {
            var a = new UiAssets();
            a.tReg = LoadFont("SairaExtraCondensed-Regular.ttf"); a.tSemi = LoadFont("SairaExtraCondensed-SemiBold.ttf"); a.tBold = LoadFont("SairaExtraCondensed-ExtraBold.ttf");
            a.text = LoadFont("Saira-Regular.ttf"); a.textSemi = LoadFont("Saira-SemiBold.ttf"); a.mono = LoadFont("IBMPlexMono-Regular.ttf");
            a.data = AssetDatabase.LoadAssetAtPath<UiData>(DataDir + "/MGSUGE_Interface.asset");
            Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiArtDir}/{n}.png");
            a.disc = S("MGSUGE_UI_Disque"); a.ring = S("MGSUGE_UI_Anneau"); a.diamond = S("MGSUGE_UI_Losange"); a.diamondFill = S("MGSUGE_UI_Losange_Plein");
            a.triangle = S("MGSUGE_UI_Triangle"); a.gradientLeft = S("MGSUGE_UI_Degrade_Gauche"); a.vignette = S("MGSUGE_UI_Vignette"); a.arc = S("MGSUGE_UI_Arc"); a.bladeArc = S("MGSUGE_UI_Arc_Lame");
            a.map = AssetDatabase.LoadAssetAtPath<Texture2D>($"{UiArtDir}/MGSUGE_Carte_Monde.png");
            GameObject P(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/UI/{n}.prefab");
            a.feedLine = P("MGSUGE_UI_LigneMessage"); a.weaponRow = P("MGSUGE_UI_LigneArme"); a.upgradeRow = P("MGSUGE_UI_LigneAmelioration");
            a.commandLine = P("MGSUGE_UI_LigneCommande"); a.damageNumber = P("MGSUGE_UI_ChiffreDegats"); a.mark = P("MGSUGE_UI_Marque"); a.blip = P("MGSUGE_UI_PointCarte");
            return a;
        }

        /// <summary>Remplace le contenu du groupe UI d'une scene deja construite et branche camera de titre et GameManager.</summary>
        static void ReplaceInterface(UnityEngine.SceneManagement.Scene scene, Data d, UiAssets a, bool world)
        {
            Transform uiRoot = null;
            foreach (var go in scene.GetRootGameObjects()) if (go.name == "UI") uiRoot = go.transform;
            if (uiRoot == null) uiRoot = Group("UI");
            for (int i = uiRoot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(uiRoot.GetChild(i).gameObject);
            var pc = Object.FindAnyObjectByType<PlayerController>();
            var gm = Object.FindAnyObjectByType<GameManager>();
            if (pc == null || gm == null) { Debug.LogError("[MGSUGE] Joueur ou GameManager absent de la scene " + scene.name + " : relancez MGSUGE > 1. Construire tout."); return; }
            gm.startOnTitle = world; EditorUtility.SetDirty(gm);
            BuildInterface(uiRoot, d, a, pc, gm);
        }

        static readonly string[] ModuleTitles = { "Propulseurs", "Réservoir d'EN", "Générateur", "Quick boost et saut", "Blindage", "Fusil à impulsions", "Lance-missiles", "Salve de missiles (C)", "Fragmentation (V)", "Lame à plasma", "Forme vaisseau" };
        static readonly string[] ModuleDescriptions =
        {
            "Vitesses de marche, boost, assault boost et montée +12 % par niveau (montée +8 %)", "EN maximale +25 %", "Recharge d'EN +22 %, durée de surchauffe −12 %",
            "Vitesse du quick boost +12 %, du saut +8 % ; coût −9 %", "AP maximale +16 %", "Dégâts +16 %, délai entre tirs −5 %",
            "+1 missile par volée, dégâts de tous les missiles +12 %", "+1 missile dans la salve, dégâts +6 %", "+1 mini-missile, dégâts +12 %",
            "Dégâts +18 %, portée de charge +8 %", "Vitesse max +10 % (et postcombustion), glissement latéral +5 %"
        };
        static readonly string[][][] CommandGroups =
        {
            new[] { new[] { "Châssis" },
                new[] { "ZQSD", "Se déplacer", "" },
                new[] { "Souris", "Viser et orienter la caméra", "Les flèches tournent aussi la caméra" },
                new[] { "Espace", "Saut boost", "Impulsion verticale à chaque pression, au sol comme en l'air ; maintenir pour continuer à monter aux propulseurs" },
                new[] { "Ctrl", "Saut boost vers le bas", "Même impulsion qu'Espace, vers le bas ; maintenir pour descendre" },
                new[] { "Maj", "Boost (maintenir)", "Glisse au sol, plane en l'air" },
                new[] { "E", "Quick boost", "Esquive instantanée dans la direction de déplacement" },
                new[] { "R", "Assault boost", "Charge vers le point visé ; R ou S pour arrêter" } },
            new[] { new[] { "Vaisseau" },
                new[] { "T", "Changer de forme", "Châssis ↔ vaisseau ; vitesse accrue, dégâts −10 %" },
                new[] { "Souris", "Rotation libre", "Looping complet sans limite ; l'assiette se remet à niveau après un moment sans bouger la souris" },
                new[] { "Z/S", "Avancer / reculer", "Sans touche, le vaisseau reste en vol stationnaire" },
                new[] { "Q/D", "Glisser sur le côté", "" },
                new[] { "Espace / Ctrl", "Monter / descendre", "Selon l'orientation du vaisseau" },
                new[] { "Maj", "Postcombustion", "Consomme de l'EN" },
                new[] { "E", "Tonneau d'esquive", "" } },
            new[] { new[] { "Combat" },
                new[] { "Clic gauche", "Fusil à impulsions", "Maintenu. Tirs laser roses ; anticipe la trajectoire de la cible verrouillée" },
                new[] { "Clic droit", "Missiles multi-cibles", "Se répartissent sur les cibles marquées d'un losange orange" },
                new[] { "C", "Salve de missiles", "Un missile toutes les 0,33 s ; 5 au départ, 10 au niveau maximum" },
                new[] { "V", "Missile à fragmentation", "Éclate près du point visé ; mini-missiles à tête chercheuse 1,5× plus rapides, 75 % des dégâts" },
                new[] { "F", "Lame à plasma", "Charge vers la cible verrouillée si elle est à portée" },
                new[] { "X", "Changer de cible", "Permet aussi de cibler les as des flottes (jamais verrouillés automatiquement) ; les attaquer les rend hostiles 25 s" },
                new[] { "H", "Kit de réparation", "Rend 40 % de l'armure ; 3 kits par sortie" },
                new[] { "G", "Rayon Arcane", "Compétence spéciale à 1 000 000 C ; détruit tous les ennemis dans un rayon de 4,5 km ; recharge 60 s" },
                new[] { "B", "Extension du territoire", "500 000 C ; centre : l'ennemi verrouillé (à moins de 1 km) ; sphère de 2 km scellée ; attaques +1 000 %, armure +1 000 % des PV ; recharge 5 min (2 min 30 s avec l'amélioration)" },
                new[] { "Auto", "Sondes laser", "250 000 C chacune, 5 au maximum ; encerclent la cible verrouillée et tirent des lasers jaunes (dégâts du fusil à impulsions)" } },
            new[] { new[] { "Interface" },
                new[] { "Échap ou P", "Menu et pause", "" },
                new[] { "U", "Améliorations", "Ouvre le menu sur l'onglet Améliorations" },
                new[] { "K", "Commandes", "Ouvre cette liste" },
                new[] { "M", "Zoom de la mini-carte", "600 m → 1 800 m → 4 500 m, puis retour à 600 m" },
                new[] { "N", "Couper ou rétablir le son", "Messages « Son coupé » / « Son rétabli »" },
                new[] { "Entrée", "Écran titre", "Lance le mode normal" } },
        };

        static Canvas UiCanvas(string name, Transform parent, int order, bool raycaster)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order; c.pixelPerfect = false;
            var cs = go.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; cs.matchWidthOrHeight = 1f;
            if (raycaster) go.AddComponent<GraphicRaycaster>();
            return c;
        }
        static T Inst<T>(GameObject prefab, Transform parent, string name) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name; return go.GetComponent<T>();
        }
        static void RecAll(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true)) if (c != null) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }

        /// <summary>Construit EventSystem, Surimpression, HUD et Ecrans sous le groupe UI.</summary>
        static void BuildInterface(Transform ui, Data d, UiAssets a, PlayerController pc, GameManager gm)
        {
            var u = a.data;
            var cam = pc.cam != null ? pc.cam.cam : Camera.main;
            Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), BL = Vector2.zero, BR = new Vector2(1f, 0f), C = new Vector2(0.5f, 0.5f),
                T = new Vector2(0.5f, 1f), B = new Vector2(0.5f, 0f), L = new Vector2(0f, 0.5f), R = new Vector2(1f, 0.5f);
            Color dim = u.BoneDim;

            // camera de l'ecran titre (sur l'objet Camera)
            if (cam != null)
            {
                var tc = cam.GetComponent<TitleCamera>(); if (tc == null) tc = cam.gameObject.AddComponent<TitleCamera>();
                tc.player = pc; tc.cam = cam; tc.rig = pc.cam; EditorUtility.SetDirty(tc);
            }

            // EventSystem (navigation clavier / souris des menus)
            var esGo = new GameObject("EventSystem"); esGo.transform.SetParent(ui, false);
            esGo.AddComponent<EventSystem>();
            var modType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (modType != null)
            {
                var mod = esGo.AddComponent(modType);
                modType.GetMethod("AssignDefaultActions")?.Invoke(mod, null);
            }
            else esGo.AddComponent<StandaloneInputModule>();

            // ================================================================ HUD
            var hudCanvas = UiCanvas("HUD", ui, 10, false);
            var hudRoot = (RectTransform)hudCanvas.transform;
            var hud = hudCanvas.gameObject.AddComponent<HudController>();
            hud.data = u; hud.world = d.world; hud.player = pc; hud.weapons = pc.GetComponent<WeaponController>(); hud.probes = pc.GetComponent<ProbeController>();
            hud.lockOn = pc.GetComponent<LockOn>(); hud.cam = cam;
            hud.hudGroup = hudCanvas.gameObject.AddComponent<CanvasGroup>(); hud.hudGroup.interactable = false; hud.hudGroup.blocksRaycasts = false;

            // vignette
            var vig = UiStretch("Vignette", hudRoot); var vigImg = vig.gameObject.AddComponent<Image>(); vigImg.sprite = a.vignette; vigImg.color = new Color(1f, 0.1f, 0.15f, 0f); vigImg.raycastTarget = false; vigImg.enabled = false;
            var hv = vig.gameObject.AddComponent<HudVignette>(); hv.hud = hud; hv.image = vigImg;

            // objectif (haut gauche)
            var objRt = UiRect("Objectif", hudRoot, TL, TL, new Vector2(40f, -30f), new Vector2(760f, 130f));
            var obj = objRt.gameObject.AddComponent<HudObjective>(); obj.hud = hud;
            obj.label = UiText("Etiquette", objRt, a.mono, 13, u.amber, TextAnchor.UpperLeft, "CONTRAT 1 / 7", TL, TL, Vector2.zero, new Vector2(600f, 20f));
            obj.title = UiText("Titre", objRt, a.tSemi, 32, u.bone, TextAnchor.UpperLeft, "RELAIS DE BROUILLAGE DE LYRA", TL, TL, new Vector2(0f, -16f), new Vector2(760f, 40f));
            var lsT = obj.title.gameObject.AddComponent<LetterSpacing>(); lsT.spacing = 2f; MoveFirst(lsT);
            obj.description = UiText("Description", objRt, a.text, 13, UiFx.A(u.bone, 0.7f), TextAnchor.UpperLeft, "Description du contrat", TL, TL, new Vector2(0f, -56f), new Vector2(560f, 40f), wrap: true);
            obj.counter = UiText("Compteur", objRt, a.mono, 14, u.bone, TextAnchor.UpperLeft, "0 / 8", TL, TL, new Vector2(0f, -98f), new Vector2(560f, 20f));
            obj.bar = UiBarAt("Barre", objRt, TL, TL, new Vector2(0f, -122f), new Vector2(300f, 2f), UiFx.A(u.amber, 0.2f), u.amber);

            // fil de messages
            var feedRt = UiRect("Fil_Messages", hudRoot, TL, TL, new Vector2(40f, -172f), new Vector2(900f, 130f));
            var feed = feedRt.gameObject.AddComponent<HudFeed>(); feed.hud = hud; feed.lines = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                var t = Inst<Text>(a.feedLine, feedRt, $"Ligne_{i + 1}");
                t.rectTransform.anchoredPosition = new Vector2(0f, -i * 24f); t.text = ""; t.gameObject.SetActive(false);
                RecAll(t.gameObject); feed.lines[i] = t;
            }

            // credits et secteur (haut droite)
            var crRt = UiRect("Credits_Secteur", hudRoot, TR, TR, new Vector2(-40f, -26f), new Vector2(600f, 100f));
            var cr = crRt.gameObject.AddComponent<HudCreditsPanel>(); cr.hud = hud;
            cr.credits = UiText("Credits", crRt, a.mono, 22, u.bone, TextAnchor.UpperRight, "2 000 C", TR, TR, Vector2.zero, new Vector2(500f, 30f));
            cr.gain = UiText("Gain", crRt, a.mono, 14, u.ice, TextAnchor.UpperRight, "+120 C", TR, TR, new Vector2(0f, -30f), new Vector2(500f, 20f)); cr.gain.gameObject.SetActive(false);
            cr.sector = UiText("Secteur", crRt, a.text, 13, UiFx.A(u.bone, 0.7f), TextAnchor.UpperRight, "Arcologie Vesta", TR, TR, new Vector2(0f, -52f), new Vector2(600f, 20f));
            cr.divine = UiText("Mode_DIVIN", crRt, a.tSemi, 16, u.gold, TextAnchor.UpperRight, "Mode DIVIN", TR, TR, new Vector2(0f, -72f), new Vector2(400f, 22f)); cr.divine.gameObject.SetActive(false);

            // barre de boss (haut centre)
            var bossRt = UiRect("Barre_Boss", hudRoot, T, T, new Vector2(0f, -22f), new Vector2(560f, 76f));
            var boss = bossRt.gameObject.AddComponent<HudBossBar>(); boss.hud = hud;
            var bossC = UiStretch("Contenu", bossRt); boss.content = bossC.gameObject;
            boss.bossName = UiText("Nom", bossC, a.tSemi, 20, u.violet, TextAnchor.UpperLeft, "BOSS", TL, TL, Vector2.zero, new Vector2(400f, 26f));
            boss.distance = UiText("Distance", bossC, a.mono, 12, dim, TextAnchor.UpperRight, "0 m", TR, TR, new Vector2(0f, -6f), new Vector2(160f, 18f));
            boss.hp = UiBarAt("Vie", bossC, TL, TL, new Vector2(0f, -30f), new Vector2(560f, 6f), new Color(0f, 0f, 0f, 0.5f), new Color(1f, 0.3f, 0.45f));
            boss.impact = UiBarAt("Impact", bossC, TL, TL, new Vector2(0f, -39f), new Vector2(560f, 3f), new Color(0f, 0f, 0f, 0.5f), u.amber);
            boss.state = UiText("Etat", bossC, a.text, 12, dim, TextAnchor.UpperCenter, "", T, T, new Vector2(0f, -46f), new Vector2(560f, 18f));
            bossC.gameObject.SetActive(false);

            // cible verrouillee (haut centre, sous la barre de boss si presente)
            var tgRt = UiRect("Cible_Verrouillee", hudRoot, T, T, new Vector2(0f, -70f), new Vector2(440f, 64f));
            var tg = tgRt.gameObject.AddComponent<HudTarget>(); tg.hud = hud; tg.bossBar = boss;
            var tgC = UiStretch("Contenu", tgRt); tg.content = tgC.gameObject;
            tg.targetName = UiText("Nom", tgC, a.tSemi, 19, u.threat, TextAnchor.UpperCenter, "Cible", T, T, Vector2.zero, new Vector2(440f, 24f));
            tg.hp = UiBarAt("Vie", tgC, T, T, new Vector2(0f, -28f), new Vector2(440f, 6f), new Color(0f, 0f, 0f, 0.5f), new Color(1f, 0.3f, 0.45f));
            tg.impact = UiBarAt("Impact", tgC, T, T, new Vector2(0f, -37f), new Vector2(440f, 3f), new Color(0f, 0f, 0f, 0.5f), u.amber);
            tg.distance = UiText("Distance", tgC, a.mono, 12, UiFx.A(u.bone, 0.6f), TextAnchor.UpperCenter, "0 m", T, T, new Vector2(0f, -43f), new Vector2(440f, 18f));
            tgC.gameObject.SetActive(false);

            // vitesse et altitude (a droite du reticule)
            var spRt = UiRect("Vitesse_Altitude", hudRoot, C, L, new Vector2(60f, 0f), new Vector2(220f, 48f));
            var sp = spRt.gameObject.AddComponent<HudSpeed>(); sp.hud = hud;
            sp.speed = UiText("Vitesse", spRt, a.mono, 16, u.bone, TextAnchor.LowerLeft, "0 km/h", L, new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(220f, 22f));
            sp.altitude = UiText("Altitude", spRt, a.mono, 14, dim, TextAnchor.UpperLeft, "56 m", L, new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(220f, 20f));

            // armure et energie (bas centre)
            var aeRt = UiRect("Armure_Energie", hudRoot, B, B, new Vector2(0f, 96f), new Vector2(500f, 110f));
            var ae = aeRt.gameObject.AddComponent<HudArmorEnergy>(); ae.hud = hud;
            ae.apValue = UiText("AP", aeRt, a.mono, 40, u.bone, TextAnchor.LowerCenter, "8 200", T, T, new Vector2(0f, 0f), new Vector2(500f, 52f));
            ae.apBar = UiBarAt("Barre_AP", aeRt, T, T, new Vector2(0f, -56f), new Vector2(500f, 7f), new Color(0f, 0f, 0f, 0.45f), u.bone, trail: true);
            var terr = UiRect("Territoire", aeRt, TR, new Vector2(1f, 0f), new Vector2(0f, -2f), new Vector2(200f, 18f)); ae.territory = terr.gameObject;
            ae.territoryText = UiText("Texte", terr, a.mono, 12, u.ice, TextAnchor.MiddleRight, "Territoire 0", R, R, new Vector2(0f, 4f), new Vector2(200f, 16f));
            ae.territoryBar = UiBarAt("Barre", terr, BR, BR, Vector2.zero, new Vector2(120f, 2f), UiFx.A(u.ice, 0.2f), u.ice);
            terr.gameObject.SetActive(false);
            ae.enBar = UiBarAt("Barre_EN", aeRt, T, T, new Vector2(0f, -70f), new Vector2(500f, 5f), new Color(0f, 0f, 0f, 0.45f), u.amber);
            ae.enState = UiText("Etat_EN", aeRt, a.mono, 13, u.amber, TextAnchor.UpperLeft, "EN 1 000", TL, TL, new Vector2(0f, -79f), new Vector2(250f, 20f));
            ae.form = UiText("Forme", aeRt, a.text, 13, u.bone, TextAnchor.UpperRight, "Châssis", TR, TR, new Vector2(0f, -79f), new Vector2(300f, 20f));

            // armes (bas droite)
            var wRt = UiRect("Armes", hudRoot, BR, BR, new Vector2(-40f, 40f), new Vector2(330f, 9 * 25f));
            var wp = wRt.gameObject.AddComponent<HudWeapons>(); wp.hud = hud; wp.rows = new HudWeaponRow[9];
            string[,] lines = { { "CLIC G", "Fusil à impulsions" }, { "CLIC D", "Missiles" }, { "C", "Salve de missiles" }, { "V", "Fragmentation" }, { "F", "Lame à plasma" },
                                { "H", "Kit de réparation" }, { "G", "Rayon Arcane" }, { "B", "Extension du territoire" }, { "AUTO", "Sondes laser" } };
            for (int i = 0; i < 9; i++)
            {
                var row = Inst<HudWeaponRow>(a.weaponRow, wRt, "Ligne_" + lines[i, 1].Replace(' ', '_'));
                var rrt = (RectTransform)row.transform; rrt.anchoredPosition = new Vector2(0f, -i * 25f);
                row.line = (WeaponLine)i; row.key.text = lines[i, 0]; row.label.text = lines[i, 1];
                row.accent = i == 7 ? u.divineViolet : i == 8 ? u.probe : i == 5 ? u.bone : u.rose;
                RecAll(row.gameObject); wp.rows[i] = row;
            }

            // mini-carte (bas gauche)
            var mmRt = UiRect("Mini_Carte", hudRoot, BL, BL, new Vector2(40f, 58f), new Vector2(u.minimapDiameter, u.minimapDiameter));
            var mm = mmRt.gameObject.AddComponent<Minimap>(); mm.hud = hud;
            var disc = UiImage("Disque", mmRt, a.disc, new Color(0.04f, 0.02f, 0.06f, 0.55f), C, C, Vector2.zero, Vector2.zero);
            var drt = disc.rectTransform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.sizeDelta = Vector2.zero;
            var mask = disc.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true; mm.disc = drt;
            var mapRt = UiRect("Carte", drt, C, C, Vector2.zero, Vector2.zero);
            mapRt.anchorMin = new Vector2(-0.21f, -0.21f); mapRt.anchorMax = new Vector2(1.21f, 1.21f); mapRt.offsetMin = mapRt.offsetMax = Vector2.zero;
            var map = mapRt.gameObject.AddComponent<RawImage>(); map.texture = a.map; map.color = new Color(1f, 1f, 1f, 0.85f); map.raycastTarget = false; mm.map = map;
            foreach (var k in new[] { 1f / 3f, 2f / 3f })
            {
                var ring = UiImage(k < 0.5f ? "Repere_1" : "Repere_2", drt, a.ring, UiFx.A(u.bone, 0.16f), C, C, Vector2.zero, Vector2.zero);
                ring.rectTransform.anchorMin = new Vector2(0.5f - k * 0.5f, 0.5f - k * 0.5f); ring.rectTransform.anchorMax = new Vector2(0.5f + k * 0.5f, 0.5f + k * 0.5f); ring.rectTransform.sizeDelta = Vector2.zero;
            }
            var blips = UiRect("Points", drt, C, C, Vector2.zero, Vector2.zero); mm.blipParent = blips;
            mm.blipPrefab = a.blip.GetComponent<Image>(); mm.diamondSprite = a.diamondFill; mm.dotSprite = a.disc; mm.squareSprite = null;
            var arrow = UiImage("Joueur", drt, a.triangle, u.bone, C, C, Vector2.zero, new Vector2(12f, 14f)); mm.playerArrow = arrow.rectTransform;
            var edge = UiImage("Bord", mmRt, a.ring, UiFx.A(u.bone, 0.45f), C, C, Vector2.zero, Vector2.zero);
            edge.rectTransform.anchorMin = Vector2.zero; edge.rectTransform.anchorMax = Vector2.one; edge.rectTransform.sizeDelta = Vector2.zero;
            mm.rangeText = UiText("Portee", mmRt, a.mono, 11, dim, TextAnchor.UpperLeft, "Portée 600 m", BL, TL, new Vector2(0f, -6f), new Vector2(200f, 16f));
            mm.zoomText = UiText("Zoom", mmRt, a.mono, 11, UiFx.A(u.bone, 0.4f), TextAnchor.UpperRight, "M · zoom", BR, TR, new Vector2(0f, -6f), new Vector2(120f, 16f));

            // grand message (centre haut, 30 %)
            var bigRt = UiRect("Grand_Message", hudRoot, new Vector2(0.5f, 0.7f), C, Vector2.zero, new Vector2(1800f, 120f));
            var big = bigRt.gameObject.AddComponent<HudBigMessage>(); big.hud = hud;
            big.title = UiText("Titre", bigRt, a.tBold, 72, u.bone, TextAnchor.MiddleCenter, "M.G-S.U.G.E", C, C, new Vector2(0f, 14f), new Vector2(1800f, 90f));
            big.spacing = big.title.gameObject.AddComponent<LetterSpacing>(); big.spacing.spacing = u.bigSpacingTo; MoveFirst(big.spacing);
            big.subtitle = UiText("Sous_Titre", bigRt, a.text, 20, u.amber, TextAnchor.MiddleCenter, "", C, C, new Vector2(0f, -48f), new Vector2(1800f, 30f));
            big.title.gameObject.SetActive(false); big.subtitle.gameObject.SetActive(false);

            // ================================================================ SURIMPRESSION
            var ovCanvas = UiCanvas("Surimpression", ui, 0, false);
            var ov = (RectTransform)ovCanvas.transform;
            hud.overlayGroup = ovCanvas.gameObject.AddComponent<CanvasGroup>(); hud.overlayGroup.interactable = false; hud.overlayGroup.blocksRaycasts = false;

            var retRt = UiStretch("Reticule", ov); var ret = retRt.gameObject.AddComponent<OverlayReticle>(); ret.hud = hud;
            ret.up = UiImage("Haut", retRt, null, u.bone, C, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1.5f, 12f)).rectTransform;
            ret.down = UiImage("Bas", retRt, null, u.bone, C, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1.5f, 12f)).rectTransform;
            ret.left = UiImage("Gauche", retRt, null, u.bone, C, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(12f, 1.5f)).rectTransform;
            ret.right = UiImage("Droite", retRt, null, u.bone, C, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(12f, 1.5f)).rectTransform;
            ret.dot = UiImage("Point", retRt, null, u.bone, C, C, Vector2.zero, new Vector2(3f, 3f));

            var horRt = UiStretch("Horizon", ov); var hor = horRt.gameObject.AddComponent<OverlayHorizon>(); hor.hud = hud;
            var piv = UiRect("Pivot", horRt, C, C, Vector2.zero, new Vector2(10f, 10f)); hor.pivot = piv;
            hor.lines = new Graphic[] {
                UiImage("Trait_Gauche", piv, null, UiFx.A(u.ice, 0.5f), C, new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(110f, 1f)),
                UiImage("Trait_Droit", piv, null, UiFx.A(u.ice, 0.5f), C, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(110f, 1f)) };
            piv.gameObject.SetActive(false);

            var tRt = UiStretch("Verrouillage", ov); var ot = tRt.gameObject.AddComponent<OverlayTargets>(); ot.hud = hud;
            ot.markParent = UiStretch("Marques", tRt); ot.markPrefab = a.mark.GetComponent<Image>(); ot.diamondSprite = a.diamond; ot.squareSprite = null;
            var frame = UiRect("Cadre", tRt, C, C, Vector2.zero, new Vector2(60f, 60f)); ot.lockFrame = frame;
            var corners = new List<Graphic>();
            foreach (var cn in new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) })
            {
                string nm = (cn.y > 0.5f ? "Haut" : "Bas") + "_" + (cn.x > 0.5f ? "Droite" : "Gauche");
                var h = UiImage("Coin_" + nm + "_H", frame, null, u.threat, cn, cn, Vector2.zero, new Vector2(16f, 2f));
                var v = UiImage("Coin_" + nm + "_V", frame, null, u.threat, cn, cn, Vector2.zero, new Vector2(2f, 16f));
                corners.Add(h); corners.Add(v);
            }
            ot.lockCorners = corners.ToArray();
            ot.lockDistance = UiText("Distance", frame, a.mono, 13, u.threat, TextAnchor.MiddleLeft, "0 m", R, L, new Vector2(8f, 0f), new Vector2(140f, 20f));
            frame.gameObject.SetActive(false);

            var mkRt = UiStretch("Marqueurs", ov); var mk = mkRt.gameObject.AddComponent<OverlayMarkers>(); mk.hud = hud;
            var objM = UiRect("Objectif", mkRt, C, C, Vector2.zero, new Vector2(20f, 20f)); mk.objective = objM;
            var objIcon = UiImage("Losange", objM, a.diamond, u.amber, C, C, Vector2.zero, new Vector2(22f, 22f)); mk.objectiveIcon = objIcon;
            UiImage("Centre", objM, null, u.amber, C, C, Vector2.zero, new Vector2(4f, 4f));
            mk.objectiveText = UiText("Texte", objM, a.mono, 12, u.amber, TextAnchor.UpperCenter, "OBJECTIF · 0 m", C, T, new Vector2(0f, -16f), new Vector2(220f, 18f));
            objM.gameObject.SetActive(false);
            var bossM = UiRect("Boss", mkRt, C, C, Vector2.zero, new Vector2(20f, 20f)); mk.boss = bossM;
            mk.bossIcon = UiImage("Losange", bossM, a.diamond, u.violet, C, C, Vector2.zero, new Vector2(28f, 28f));
            mk.bossText = UiText("Texte", bossM, a.mono, 12, u.violet, TextAnchor.UpperCenter, "BOSS", C, T, new Vector2(0f, -20f), new Vector2(260f, 18f));
            bossM.gameObject.SetActive(false);

            var hitRt = UiStretch("Coups_Recus", ov); var hits = hitRt.gameObject.AddComponent<OverlayHitArcs>(); hits.hud = hud;
            hits.arcs = new RectTransform[6];
            for (int i = 0; i < 6; i++)
            {
                var p = UiRect($"Arc_{i + 1}", hitRt, C, C, Vector2.zero, new Vector2(10f, 10f));
                UiImage("Image", p, a.arc, u.threat, C, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(150f, 38f));
                p.gameObject.SetActive(false); hits.arcs[i] = p;
            }

            var msRt = UiStretch("Missiles_Entrants", ov); var ms = msRt.gameObject.AddComponent<OverlayMissileAlert>(); ms.hud = hud;
            ms.label = UiText("Alerte", msRt, a.tSemi, 22, u.threat, TextAnchor.MiddleCenter, "MISSILE ENTRANT", C, C, new Vector2(0f, 190f), new Vector2(600f, 30f));
            var lsM = ms.label.gameObject.AddComponent<LetterSpacing>(); lsM.spacing = 4f; MoveFirst(lsM);
            ms.label.gameObject.SetActive(false);
            ms.chevrons = new RectTransform[6];
            for (int i = 0; i < 6; i++)
            {
                var p = UiRect($"Chevron_{i + 1}", msRt, C, C, Vector2.zero, new Vector2(10f, 10f));
                UiImage("Image", p, a.triangle, u.threat, C, new Vector2(0.5f, 0f), new Vector2(0f, 168f), new Vector2(18f, 16f));
                p.gameObject.SetActive(false); ms.chevrons[i] = p;
            }

            var dmgRt = UiStretch("Chiffres_Degats", ov); var dmg = dmgRt.gameObject.AddComponent<OverlayDamageNumbers>(); dmg.hud = hud; dmg.numberPrefab = a.damageNumber.GetComponent<Text>();

            var bl = UiImage("Arc_Lame", ov, a.bladeArc, u.rose, C, C, new Vector2(0f, -20f), new Vector2(640f, 160f)); bl.enabled = false;
            var ba = bl.gameObject.AddComponent<OverlayBladeArc>(); ba.hud = hud; ba.arc = bl;

            // ================================================================ ECRANS
            var scCanvas = UiCanvas("Ecrans", ui, 20, true);
            var sc = (RectTransform)scCanvas.transform;
            BuildTitleScreen(sc, a, gm);
            BuildPauseMenu(sc, a, gm);
            BuildDeadScreen(sc, a, gm);
        }

        /// <summary>L'espacement des lettres doit passer avant l'ombre (ordre des composants dans l'Inspector).</summary>
        static void MoveFirst(Component c)
        {
            if (c.GetComponent<Shadow>() != null) UnityEditorInternal.ComponentUtility.MoveComponentUp(c);
        }

        static Button MenuButton(string name, Transform parent, string label, UiAssets a, Vector2 pos, Vector2 size)
        {
            var u = a.data;
            return UiButton(name, parent, label, a.textSemi, 17, u.bone, new Color(1f, 1f, 1f, 0.06f), UiFx.A(u.amber, 0.35f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
        }

        static void BuildTitleScreen(RectTransform sc, UiAssets a, GameManager gm)
        {
            var u = a.data;
            Vector2 TL = new Vector2(0f, 1f), C = new Vector2(0.5f, 0.5f), BL = Vector2.zero;
            var root = UiStretch("Ecran_Titre", sc);
            var ts = root.gameObject.AddComponent<TitleScreen>(); ts.game = gm; ts.data = u;
            var content = UiStretch("Contenu", root); ts.content = content.gameObject;
            var grt = UiStretch("Degrade", content); grt.anchorMax = new Vector2(0.62f, 1f); grt.offsetMin = grt.offsetMax = Vector2.zero;
            var grad = grt.gameObject.AddComponent<Image>(); grad.sprite = a.gradientLeft; grad.color = UiFx.A(u.background, 0.92f); grad.raycastTarget = false;
            var area = UiRect("Colonne", content, TL, TL, new Vector2(110f, -150f), new Vector2(900f, 820f));
            UiText("Designation", area, a.mono, 16, u.amber, TextAnchor.UpperLeft, "Châssis transformable VSP-07 « Vesper »", TL, TL, Vector2.zero, new Vector2(800f, 24f));

            // trois mises en page du nom (§4.6)
            ts.layouts = new GameObject[3];
            var shimmer = new List<Outline>();
            Text Fill(string n, Transform p, string s, Vector2 pos, TextAnchor al, Vector2 pivot)
            {
                var t = UiText(n, p, a.tBold, 150, u.bone, al, s, TL, pivot, pos, new Vector2(900f, 150f));
                var ls = t.gameObject.AddComponent<LetterSpacing>(); ls.spacing = 10f; MoveFirst(ls);
                return t;
            }
            Text Contour(string n, Transform p, string s, Vector2 pos, TextAnchor al, Vector2 pivot)
            {
                var t = UiText(n, p, a.tBold, 150, u.background, al, s, TL, pivot, pos, new Vector2(900f, 150f), shadow: false); // lettres sombres + contour rose
                var ls = t.gameObject.AddComponent<LetterSpacing>(); ls.spacing = 10f;
                var o = t.gameObject.AddComponent<Outline>(); o.effectColor = u.rose; o.effectDistance = new Vector2(2.5f, -2.5f); o.useGraphicAlpha = false;
                shimmer.Add(o); return t;
            }
            var l1 = UiRect("Nom_Une_Ligne", area, TL, TL, new Vector2(0f, -34f), new Vector2(900f, 170f)); ts.layouts[0] = l1.gameObject;
            Fill("Nom", l1, "M.G-S.U.G.E", Vector2.zero, TextAnchor.UpperLeft, TL);
            var l2 = UiRect("Nom_Deux_Lignes", area, TL, TL, new Vector2(0f, -34f), new Vector2(900f, 300f)); ts.layouts[1] = l2.gameObject;
            Fill("Ligne_Pleine", l2, "M.G", Vector2.zero, TextAnchor.UpperLeft, TL);
            Contour("Ligne_Contour", l2, "S.U.G.E", new Vector2(0f, -128f), TextAnchor.UpperLeft, TL);
            var l3 = UiRect("Nom_Plein_Et_Contour", area, TL, TL, new Vector2(0f, -34f), new Vector2(900f, 170f)); ts.layouts[2] = l3.gameObject;
            var f3 = Fill("Partie_Pleine", l3, "M.G-", new Vector2(300f, 0f), TextAnchor.UpperRight, new Vector2(1f, 1f)); f3.rectTransform.sizeDelta = new Vector2(300f, 150f);
            var c3 = Contour("Partie_Contour", l3, "S.U.G.E", new Vector2(306f, 0f), TextAnchor.UpperLeft, TL); c3.rectTransform.sizeDelta = new Vector2(600f, 150f);
            ts.shimmer = shimmer.ToArray(); ts.layout = TitleLayout.DeuxLignes;
            l1.gameObject.SetActive(false); l3.gameObject.SetActive(false);

            UiText("Presentation", area, a.text, 16, UiFx.A(u.bone, 0.82f), TextAnchor.UpperLeft,
                "Un monde ouvert de 15 km sous un ciel crépusculaire : arcologie, raffinerie, spatioport, archipel de plateformes, puis glacier, caldeira, marais et désert aux confins. " +
                "Sept boss rivaux rôdent et se battent entre eux, plus forts à chaque retour. À 8 000 m d'altitude, deux flottes et leurs as se livrent une guerre sans fin, " +
                "sous le regard du Titan « Prométhée » et de l'Archonte « Zénith », le boss ultime. Chacun peut déployer son extension du territoire.",
                TL, TL, new Vector2(0f, -350f), new Vector2(640f, 140f), wrap: true);
            var bn = UiButton("Bouton_Mode_Normal", area, "Mode normal", a.tSemi, 26, u.background, UiFx.A(u.amber, 0.88f), u.amber, TL, TL, new Vector2(0f, -500f), new Vector2(300f, 56f), TextAnchor.MiddleCenter);
            var bd = UiButton("Bouton_Mode_DIVIN", area, "Mode DIVIN", a.tSemi, 26, u.gold, UiFx.A(u.divineViolet, 0.55f), UiFx.A(u.divineViolet, 0.9f), TL, TL, new Vector2(320f, -500f), new Vector2(300f, 56f), TextAnchor.MiddleCenter);
            var bdo = bd.gameObject.AddComponent<Outline>(); bdo.effectColor = UiFx.A(u.gold, 0.8f); bdo.effectDistance = new Vector2(1f, -1f);
            OnClick(bn, ts.StartNormal); OnClick(bd, ts.StartDivine);
            ts.normalButton = bn; ts.divineButton = bd;
            UiText("Note", area, a.text, 14, UiFx.A(u.bone, 0.62f), TextAnchor.UpperLeft,
                "Normal : on gagne ses crédits et on achète tout. DIVIN : tout est débloqué, et des flèches ◀ ▶ dans les améliorations règlent le niveau de chaque module.",
                TL, TL, new Vector2(0f, -574f), new Vector2(640f, 44f), wrap: true);
            UiText("Touches", area, a.mono, 13, UiFx.A(u.bone, 0.5f), TextAnchor.UpperLeft,
                "ZQSD déplacement · Souris viser · Espace / Ctrl saut boost · Maj boost · E quick boost · R assault boost\nT transformation · Clic G fusil · Clic D missiles · C salve · V fragmentation · F lame · X cible · H kit\nÉchap menu · U améliorations · K commandes · M mini-carte · N son · Entrée : jouer en mode normal",
                TL, TL, new Vector2(0f, -640f), new Vector2(900f, 70f), wrap: true);
            var touch = UiText("Avertissement_Tactile", area, a.textSemi, 16, u.threat, TextAnchor.UpperLeft, "Ce jeu se joue au clavier et à la souris.", TL, TL, new Vector2(0f, -726f), new Vector2(640f, 24f));
            ts.touchWarning = touch.gameObject; touch.gameObject.SetActive(false);

            // navigation clavier entre les deux boutons
            var nn = bn.navigation; nn.mode = Navigation.Mode.Explicit; nn.selectOnRight = bd; nn.selectOnDown = bd; bn.navigation = nn;
            var nd = bd.navigation; nd.mode = Navigation.Mode.Explicit; nd.selectOnLeft = bn; nd.selectOnUp = bn; bd.navigation = nd;
        }

        static void BuildPauseMenu(RectTransform sc, UiAssets a, GameManager gm)
        {
            var u = a.data;
            Vector2 TL = new Vector2(0f, 1f);
            var root = UiStretch("Menu_Pause", sc);
            var pm = root.gameObject.AddComponent<PauseMenu>(); pm.game = gm; pm.data = u;
            var content = UiStretch("Contenu", root); pm.content = content.gameObject;
            var bg = content.gameObject.AddComponent<Image>(); bg.color = new Color(u.background.r, u.background.g, u.background.b, 0.93f); bg.raycastTarget = true;

            // colonne de gauche
            var col = UiRect("Colonne", content, TL, TL, new Vector2(80f, -80f), new Vector2(280f, 400f));
            var title = UiText("Titre", col, a.tBold, 44, u.bone, TextAnchor.UpperLeft, "PAUSE", TL, TL, Vector2.zero, new Vector2(280f, 54f));
            var lsP = title.gameObject.AddComponent<LetterSpacing>(); lsP.spacing = 8f; MoveFirst(lsP);
            pm.resumeButton = MenuButton("Reprendre", col, "Reprendre", a, new Vector2(0f, -90f), new Vector2(260f, 42f));
            pm.upgradesButton = MenuButton("Ameliorations", col, "Améliorations", a, new Vector2(0f, -140f), new Vector2(260f, 42f));
            pm.commandsButton = MenuButton("Commandes", col, "Commandes", a, new Vector2(0f, -190f), new Vector2(260f, 42f));
            OnClick(pm.resumeButton, gm.Resume); OnClick(pm.upgradesButton, gm.ShowUpgrades); OnClick(pm.commandsButton, gm.ShowCommands);
            pm.upgradesMark = UiImage("Onglet_Actif", pm.upgradesButton.transform, null, u.amber, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 42f));
            pm.commandsMark = UiImage("Onglet_Actif", pm.commandsButton.transform, null, u.amber, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 42f));
            UiText("Credits_Titre", col, a.mono, 12, u.BoneDim, TextAnchor.UpperLeft, "CRÉDITS", TL, TL, new Vector2(0f, -256f), new Vector2(260f, 18f));
            pm.credits = UiText("Credits", col, a.mono, 22, u.amber, TextAnchor.UpperLeft, "2 000 C", TL, TL, new Vector2(0f, -276f), new Vector2(260f, 30f));
            // navigation verticale de la colonne
            Selectable[] colSel = { pm.resumeButton, pm.upgradesButton, pm.commandsButton };
            for (int i = 0; i < colSel.Length; i++)
            {
                var n = colSel[i].navigation; n.mode = Navigation.Mode.Explicit;
                n.selectOnUp = colSel[(i + colSel.Length - 1) % colSel.Length]; n.selectOnDown = colSel[(i + 1) % colSel.Length];
                colSel[i].navigation = n;
            }

            // onglet Ameliorations (§11.4)
            var up = UiRect("Onglet_Ameliorations", content, TL, TL, new Vector2(420f, -80f), new Vector2(1440f, 940f));
            pm.upgradesPanel = up.gameObject;
            var upp = up.gameObject.AddComponent<UpgradesPanel>(); upp.game = gm; upp.data = u;
            var stag = up.gameObject.AddComponent<UiStagger>(); stag.data = u;
            var items = new List<CanvasGroup>();
            var hdr = UiRect("Entete", up, TL, TL, Vector2.zero, new Vector2(1440f, 110f)); items.Add(hdr.gameObject.AddComponent<CanvasGroup>());
            var ut = UiText("Titre", hdr, a.tBold, 40, u.bone, TextAnchor.UpperLeft, "AMÉLIORATIONS", TL, TL, Vector2.zero, new Vector2(900f, 50f));
            var lsU = ut.gameObject.AddComponent<LetterSpacing>(); lsU.spacing = 6f; MoveFirst(lsU);
            upp.normalIntro = UiText("Introduction", hdr, a.text, 14, UiFx.A(u.bone, 0.7f), TextAnchor.UpperLeft,
                "Les crédits viennent des ennemis détruits, des avant-postes neutralisés, des contrats et des boss. Chaque module a cinq niveaux ; les effets s'appliquent immédiatement.",
                TL, TL, new Vector2(0f, -56f), new Vector2(1100f, 40f), wrap: true).gameObject;
            var di = UiText("Introduction_DIVIN", hdr, a.text, 14, u.gold, TextAnchor.UpperLeft,
                "Mode DIVIN : tout est débloqué. Les flèches ◀ ▶ règlent le niveau de chaque module, gratuitement.",
                TL, TL, new Vector2(0f, -56f), new Vector2(1100f, 40f), wrap: true);
            upp.divineIntro = di.gameObject; di.gameObject.SetActive(false);
            var rows = new List<UpgradeRow>();
            float y = -112f;
            UpgradeRow AddRow(string name, UpgradeKind kind, UpgradeModule mod, string t, string desc)
            {
                var r = Inst<UpgradeRow>(a.upgradeRow, up, name);
                ((RectTransform)r.transform).anchoredPosition = new Vector2(0f, y); y -= 48f;
                r.kind = kind; r.module = mod; r.title.text = t; r.description.text = desc;
                RecAll(r.gameObject); rows.Add(r); items.Add(r.GetComponent<CanvasGroup>());
                return r;
            }
            for (int i = 0; i < ModuleTitles.Length; i++) AddRow("Module_" + ((UpgradeModule)i), UpgradeKind.Module, (UpgradeModule)i, ModuleTitles[i], ModuleDescriptions[i]);
            y -= 8f;
            AddRow("Sonde_Laser", UpgradeKind.Sonde, 0, "Sonde laser", "Un drone du dos se détache, encercle la cible verrouillée et tire des lasers jaunes (dégâts du fusil). 5 au maximum");
            AddRow("Rayon_Arcane", UpgradeKind.RayonArcane, 0, "Rayon Arcane (G)", "Détruit tous les ennemis dans un rayon de 4,5 km ; recharge 60 s");
            AddRow("Extension_Territoire", UpgradeKind.Territoire, 0, "Extension du territoire (B)", "Sphère de 2 km scellée autour de l'ennemi verrouillé : attaques +1 000 %, armure +1 000 % des PV ; recharge 5 min");
            AddRow("Extension_Territoire_II", UpgradeKind.TerritoireII, 0, "Extension du territoire II", "Recharge 5 min → 2 min 30 s. Condition : extension I achetée et Archonte « Zénith » vaincu");
            AddRow("Reincarnation", UpgradeKind.Reincarnation, 0, "Réincarnation", "PV maximaux et dégâts du fusil, des missiles et de la lame ×5 à chaque achat (cumulatif). Sauts, boosts et vitesses inchangés");
            upp.rows = rows.ToArray(); stag.items = items.ToArray();
            // navigation verticale des boutons d'achat ; gauche = colonne
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i].buyButton; var n = b.navigation; n.mode = Navigation.Mode.Explicit;
                n.selectOnUp = i > 0 ? rows[i - 1].buyButton : null; n.selectOnDown = i < rows.Count - 1 ? rows[i + 1].buyButton : null; n.selectOnLeft = pm.upgradesButton;
                b.navigation = n;
                foreach (var sel in new Selectable[] { rows[i].prevButton, rows[i].nextButton })
                {
                    var ns = sel.navigation; ns.mode = Navigation.Mode.Explicit;
                    ns.selectOnUp = i > 0 ? (sel == rows[i].prevButton ? rows[i - 1].prevButton : (Selectable)rows[i - 1].nextButton) : null;
                    ns.selectOnDown = i < rows.Count - 1 ? (sel == rows[i].prevButton ? rows[i + 1].prevButton : (Selectable)rows[i + 1].nextButton) : null;
                    ns.selectOnLeft = sel == rows[i].prevButton ? (Selectable)pm.upgradesButton : rows[i].prevButton;
                    ns.selectOnRight = sel == rows[i].prevButton ? rows[i].nextButton : null;
                    sel.navigation = ns;
                }
                RecAll(rows[i].gameObject);
            }
            var ub = pm.upgradesButton.navigation; ub.selectOnRight = rows[0].buyButton; pm.upgradesButton.navigation = ub;

            // onglet Commandes (§8 : 4 groupes)
            var cmd = UiRect("Onglet_Commandes", content, TL, TL, new Vector2(420f, -80f), new Vector2(1440f, 940f));
            pm.commandsPanel = cmd.gameObject;
            var cst = cmd.gameObject.AddComponent<UiStagger>(); cst.data = u;
            var citems = new List<CanvasGroup>();
            var ch = UiRect("Entete", cmd, TL, TL, Vector2.zero, new Vector2(1440f, 60f)); citems.Add(ch.gameObject.AddComponent<CanvasGroup>());
            var ct = UiText("Titre", ch, a.tBold, 40, u.bone, TextAnchor.UpperLeft, "COMMANDES", TL, TL, Vector2.zero, new Vector2(900f, 50f));
            var lsC = ct.gameObject.AddComponent<LetterSpacing>(); lsC.spacing = 6f; MoveFirst(lsC);
            float[] colX = { 0f, 0f, 730f, 730f };
            float leftY = -74f, rightY = -74f;
            for (int g = 0; g < CommandGroups.Length; g++)
            {
                var grp = CommandGroups[g];
                bool right = g >= 2; float gy = right ? rightY : leftY;
                var gh = UiText("Groupe_" + grp[0][0], cmd, a.tSemi, 20, u.amber, TextAnchor.UpperLeft, UiFx.Upper(grp[0][0]), TL, TL, new Vector2(colX[g], gy), new Vector2(700f, 26f));
                var lsG = gh.gameObject.AddComponent<LetterSpacing>(); lsG.spacing = 3f; MoveFirst(lsG);
                citems.Add(gh.gameObject.AddComponent<CanvasGroup>());
                gy -= 30f;
                for (int i = 1; i < grp.Length; i++)
                {
                    var line = (GameObject)PrefabUtility.InstantiatePrefab(a.commandLine, cmd);
                    line.name = $"{grp[0][0]}_{i:00}_{grp[i][1]}";
                    var lrt = (RectTransform)line.transform; lrt.anchoredPosition = new Vector2(colX[g], gy);
                    var texts = line.GetComponentsInChildren<Text>(true);
                    texts[0].text = grp[i][0]; texts[1].text = grp[i][1]; texts[2].text = grp[i][2];
                    int detailLines = Mathf.CeilToInt(grp[i][2].Length / 92f); // ~92 caracteres par ligne a 540 px
                    gy -= detailLines == 0 ? 28f : 24f + 17f * detailLines;
                    RecAll(line); citems.Add(line.GetComponent<CanvasGroup>());
                }
                gy -= 16f;
                if (right) rightY = gy; else leftY = gy;
            }
            cst.items = citems.ToArray();
            cmd.gameObject.SetActive(false);
            content.gameObject.SetActive(false);
        }

        static void BuildDeadScreen(RectTransform sc, UiAssets a, GameManager gm)
        {
            var u = a.data;
            Vector2 C = new Vector2(0.5f, 0.5f);
            var root = UiStretch("Chassis_Detruit", sc);
            var ds = root.gameObject.AddComponent<DeadScreen>(); ds.game = gm;
            var content = UiStretch("Contenu", root); ds.content = content.gameObject;
            var bg = content.gameObject.AddComponent<Image>(); bg.color = new Color(0.05f, 0.01f, 0.02f, 0.82f); bg.raycastTarget = true;
            var des = UiText("Designation", content, a.mono, 16, u.threat, TextAnchor.MiddleCenter, "SIGNAL PERDU", C, C, new Vector2(0f, 190f), new Vector2(800f, 24f));
            var lsD = des.gameObject.AddComponent<LetterSpacing>(); lsD.spacing = 6f; MoveFirst(lsD);
            var t = UiText("Titre", content, a.tBold, 96, u.bone, TextAnchor.MiddleCenter, "CHÂSSIS DÉTRUIT", C, C, new Vector2(0f, 110f), new Vector2(1600f, 110f));
            var ls = t.gameObject.AddComponent<LetterSpacing>(); ls.spacing = 8f; MoveFirst(ls);
            UiText("Texte", content, a.text, 16, UiFx.A(u.bone, 0.75f), TextAnchor.MiddleCenter,
                "Le Vesper est récupéré et réparé sur la plateforme Hélios. Crédits, améliorations et progression du contrat sont conservés.",
                C, C, new Vector2(0f, 30f), new Vector2(1400f, 26f));
            var b = UiButton("Bouton_Redeployer", content, "Redéployer", a.tSemi, 24, u.background, UiFx.A(u.amber, 0.88f), u.amber, C, C, new Vector2(0f, -50f), new Vector2(280f, 54f), TextAnchor.MiddleCenter);
            OnClick(b, ds.Redeploy); ds.redeployButton = b;
            content.gameObject.SetActive(false);
        }
    }
}
