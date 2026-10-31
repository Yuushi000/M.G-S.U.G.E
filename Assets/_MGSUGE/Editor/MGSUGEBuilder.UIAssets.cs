using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MGSUGE.EditorTools
{
    /// <summary>Interface (jalon 6) : polices, sprites generes, carte du monde, asset Interface et prefabs d'elements repetes.</summary>
    public static partial class MGSUGEBuilder
    {
        const string FontDir = Root + "/Art/Fonts";
        const string UiArtDir = Root + "/Art/UI";

        class UiAssets
        {
            public UiData data;
            public Font tReg, tSemi, tBold, text, textSemi, mono;
            public Sprite disc, ring, diamond, diamondFill, triangle, gradientLeft, vignette, arc, bladeArc;
            public Texture2D map;
            public GameObject feedLine, weaponRow, upgradeRow, damageNumber, mark, blip, commandLine;
        }

        // ------------------------------------------------------------------ polices et donnees
        static Font LoadFont(string file)
        {
            string path = $"{FontDir}/{file}";
            var f = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (f == null) { AssetDatabase.ImportAsset(path); f = AssetDatabase.LoadAssetAtPath<Font>(path); }
            if (f == null) Debug.LogWarning("[MGSUGE] Police introuvable : " + path);
            return f;
        }

        static UiAssets BuildUiAssets(Data d)
        {
            EnsureFolder(UiArtDir);
            var a = new UiAssets();
            a.tReg = LoadFont("SairaExtraCondensed-Regular.ttf"); a.tSemi = LoadFont("SairaExtraCondensed-SemiBold.ttf"); a.tBold = LoadFont("SairaExtraCondensed-ExtraBold.ttf");
            a.text = LoadFont("Saira-Regular.ttf"); a.textSemi = LoadFont("Saira-SemiBold.ttf"); a.mono = LoadFont("IBMPlexMono-Regular.ttf");
            a.data = LoadOrCreate<UiData>(DataDir + "/MGSUGE_Interface.asset");
            var u = a.data;
            if (u.titleRegular == null) u.titleRegular = a.tReg; if (u.titleSemiBold == null) u.titleSemiBold = a.tSemi; if (u.titleExtraBold == null) u.titleExtraBold = a.tBold;
            if (u.text == null) u.text = a.text; if (u.textSemiBold == null) u.textSemiBold = a.textSemi; if (u.mono == null) u.mono = a.mono;
            EditorUtility.SetDirty(u);

            // sprites blancs (teintes par la couleur de l'Image)
            a.disc = SaveSprite("MGSUGE_UI_Disque", 256, 256, (x, y) => Disc(x, y, 256, 127f));
            a.ring = SaveSprite("MGSUGE_UI_Anneau", 512, 512, (x, y) => Ring(x, y, 512, 252f, 3.2f));
            a.diamond = SaveSprite("MGSUGE_UI_Losange", 64, 64, (x, y) => { float m = Mathf.Abs(x - 31.5f) + Mathf.Abs(y - 31.5f); return AA(Mathf.Abs(m - 26f), 3.2f); });
            a.diamondFill = SaveSprite("MGSUGE_UI_Losange_Plein", 64, 64, (x, y) => { float m = Mathf.Abs(x - 31.5f) + Mathf.Abs(y - 31.5f); return AA(Mathf.Max(0f, m - 28f), 0.8f); });
            a.triangle = SaveSprite("MGSUGE_UI_Triangle", 64, 64, (x, y) =>
            {
                // pointe vers le haut
                float fy = y / 63f, fx = Mathf.Abs(x - 31.5f) / 31.5f;
                float edge = (1f - fy) * 0.62f - fx; // > 0 a l'interieur
                return fy < 0.04f || fy > 0.98f ? 0f : Mathf.Clamp01(edge * 40f + 0.5f);
            });
            a.gradientLeft = SaveSprite("MGSUGE_UI_Degrade_Gauche", 256, 8, (x, y) => { float t = x / 255f; float k = 1f - t; return k * k * (3f - 2f * k); });
            a.vignette = SaveSprite("MGSUGE_UI_Vignette", 256, 256, (x, y) =>
            {
                float dx = Mathf.Abs(x - 127.5f) / 127.5f, dy = Mathf.Abs(y - 127.5f) / 127.5f;
                float e = Mathf.Max(dx, dy); float k = Mathf.Clamp01((e - 0.72f) / 0.28f);
                return k * k;
            });
            a.arc = SaveSprite("MGSUGE_UI_Arc", 256, 64, (x, y) =>
            {
                // arc d'anneau (rayon 300, centre sous l'image) : indicateur de coup
                float cx = 127.5f, cy = -250f; float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float band = AA(Mathf.Abs(r - 290f), 6f);
                float side = Mathf.Clamp01((1f - Mathf.Abs(x - cx) / 128f) * 3f);
                return band * side;
            });
            a.bladeArc = SaveSprite("MGSUGE_UI_Arc_Lame", 512, 128, (x, y) =>
            {
                float cx = 255.5f, cy = 400f; float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float band = AA(Mathf.Abs(r - 340f), 5f);
                float side = Mathf.Clamp01((1f - Mathf.Abs(x - cx) / 256f) * 2.5f);
                return band * side;
            });
            a.map = WorldMap(d);

            BuildUiPrefabs(a);
            return a;
        }

        static float AA(float dist, float half) => Mathf.Clamp01(half - dist + 0.5f) * Mathf.Clamp01(half + 0.5f);
        static float Disc(int x, int y, int n, float r) { float c = (n - 1) * 0.5f; float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)); return Mathf.Clamp01(r - d + 0.5f); }
        static float Ring(int x, int y, int n, float r, float w) { float c = (n - 1) * 0.5f; float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)); return Mathf.Clamp01(w * 0.5f - Mathf.Abs(d - r) + 0.5f); }

        /// <summary>Sprite blanc genere (alpha = f(x, y)) ; reecrit a chaque construction.</summary>
        static Sprite SaveSprite(string name, int w, int h, Func<int, int, float> alpha)
        {
            string path = $"{UiArtDir}/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Carte du monde pre-calculee pour la mini-carte : couleurs de biome eclaircies, ombrage du relief, mer, lave.</summary>
        static Texture2D WorldMap(Data d)
        {
            const int n = 512;
            string path = $"{UiArtDir}/MGSUGE_Carte_Monde.png";
            var w = d.world; var hf = HeightField.Load(w);
            float size = w.worldSize, half = size * 0.5f, cell = size / n;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            Color sea = new Color(0.07f, 0.16f, 0.24f), seaDeep = new Color(0.03f, 0.07f, 0.12f), lava = new Color(0.85f, 0.30f, 0.08f);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -half + (i + 0.5f) * cell, z = -half + (j + 0.5f) * cell;
                    float h = hf.Height(x, z);
                    float hx = hf.Height(x + cell, z) - hf.Height(x - cell, z), hz = hf.Height(x, z + cell) - hf.Height(x, z - cell);
                    Vector3 nrm = new Vector3(-hx, 2f * cell, -hz).normalized;
                    float shade = Mathf.Clamp01(Vector3.Dot(nrm, new Vector3(-0.5f, 0.75f, 0.45f).normalized)) * 0.75f + 0.45f;
                    Color c = hf.Biome(x, z, h);
                    c = new Color(Mathf.Pow(c.r, 0.6f), Mathf.Pow(c.g, 0.6f), Mathf.Pow(c.b, 0.6f)) * 1.15f * shade;
                    if (h < w.seaLevel) c = Color.Lerp(sea, seaDeep, Mathf.Clamp01((w.seaLevel - h) / 120f));
                    else if (w.InLavaRect(x, z) && h < w.lavaLevel + 2f) c = lava;
                    float contour = Mathf.Repeat(h, 150f);
                    if (h > w.seaLevel && (contour < 3f)) c *= 0.82f;
                    c.a = 1f;
                    px[j * n + i] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.sRGBTexture = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed; imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ outils de construction uGUI
        static RectTransform UiRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        static RectTransform UiStretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
        static Text UiText(string name, Transform parent, Font font, int size, Color color, TextAnchor align, string text,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box, bool shadow = true, bool wrap = false)
        {
            var rt = UiRect(name, parent, anchor, pivot, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = align; t.text = text; t.raycastTarget = false; t.supportRichText = false;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            if (shadow) { var s = rt.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0f, 0f, 0f, 0.85f); s.effectDistance = new Vector2(1.5f, -1.5f); }
            return t;
        }
        static Image UiImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = UiRect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return img;
        }
        /// <summary>Barre fine : fond + remplissage (+ trainee facultative), composant UiBar.</summary>
        static UiBar UiBarAt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color back, Color fill, bool trail = false)
        {
            var rt = UiRect(name, parent, anchor, pivot, pos, size);
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = back; bg.raycastTarget = false;
            var bar = rt.gameObject.AddComponent<UiBar>();
            if (trail)
            {
                var tr = UiStretch("Trainee", rt); var ti = tr.gameObject.AddComponent<Image>(); ti.color = new Color(1f, 1f, 1f, 0.45f); ti.raycastTarget = false;
                bar.trail = tr;
            }
            var f = UiStretch("Remplissage", rt); var fi = f.gameObject.AddComponent<Image>(); fi.color = fill; fi.raycastTarget = false;
            bar.fill = f; bar.fillImage = fi; bar.Set(1f); bar.SetTrail(1f);
            return bar;
        }
        static Button UiButton(string name, Transform parent, string label, Font font, int size, Color textColor, Color normal, Color highlight,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = UiRect(name, parent, anchor, pivot, pos, box);
            var img = rt.gameObject.AddComponent<Image>(); img.color = Color.white; img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.transition = Selectable.Transition.ColorTint;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal; cb.highlightedColor = highlight; cb.selectedColor = highlight; cb.pressedColor = new Color(highlight.r, highlight.g, highlight.b, Mathf.Min(1f, highlight.a + 0.25f));
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.35f); cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
            b.colors = cb;
            var t = UiText("Texte", rt, font, size, textColor, align, label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, box - new Vector2(28f, 0f), shadow: false);
            return b;
        }
        static void OnClick(Button b, UnityEngine.Events.UnityAction call) => UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, call);

        // ------------------------------------------------------------------ prefabs des elements repetes
        static void BuildUiPrefabs(UiAssets a)
        {
            var u = a.data;
            string dir = PrefabDir + "/UI"; EnsureFolder(dir);
            Vector2 TL = new Vector2(0f, 1f), C = new Vector2(0.5f, 0.5f);

            // ligne du fil de messages
            {
                var t = UiText("MGSUGE_UI_LigneMessage", null, a.text, 15, u.bone, TextAnchor.MiddleLeft, "Message", TL, TL, Vector2.zero, new Vector2(900f, 22f));
                a.feedLine = Save(t.gameObject, $"{dir}/MGSUGE_UI_LigneMessage.prefab");
            }
            // ligne d'arme
            {
                var root = UiRect("MGSUGE_UI_LigneArme", null, TL, TL, Vector2.zero, new Vector2(330f, 24f));
                var row = root.gameObject.AddComponent<HudWeaponRow>();
                row.flash = UiImage("Eclat", root, null, new Color(1f, 0.85f, 0.97f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(274f, 24f));
                row.flash.gameObject.SetActive(false);
                row.key = UiText("Touche", root, a.mono, 11, u.BoneDim, TextAnchor.MiddleLeft, "CLIC G", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(60f, 20f));
                row.label = UiText("Nom", root, a.text, 13, u.bone, TextAnchor.MiddleLeft, "Fusil à impulsions", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, 1f), new Vector2(190f, 20f));
                row.count = UiText("Compteur", root, a.mono, 13, u.rose, TextAnchor.MiddleRight, "∞", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 1f), new Vector2(80f, 20f));
                var bar = UiBarAt("Recharge", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(62f, 1f), new Vector2(258f, 1.5f), UiFx.A(u.rose, 0.18f), u.rose);
                row.reload = bar; row.reloadBack = bar.GetComponent<Image>(); bar.Set(0f);
                a.weaponRow = Save(root.gameObject, $"{dir}/MGSUGE_UI_LigneArme.prefab");
            }
            // ligne d'amelioration
            {
                var root = UiRect("MGSUGE_UI_LigneAmelioration", null, TL, TL, Vector2.zero, new Vector2(1440f, 44f));
                root.gameObject.AddComponent<CanvasGroup>();
                var row = root.gameObject.AddComponent<UpgradeRow>();
                var bg = UiImage("Fond", root, null, new Color(1f, 1f, 1f, 0.025f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1440f, 44f));
                row.border = UiImage("Bordure", root, null, u.amber, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 44f));
                row.title = UiText("Nom", root, a.textSemi, 15, u.bone, TextAnchor.UpperLeft, "Module", TL, TL, new Vector2(14f, -3f), new Vector2(520f, 22f));
                row.description = UiText("Description", root, a.text, 12, u.BoneDim, TextAnchor.UpperLeft, "Effet par niveau", TL, TL, new Vector2(14f, -23f), new Vector2(540f, 18f));
                row.pips = new Image[5];
                for (int i = 0; i < 5; i++) row.pips[i] = UiImage($"Case_{i + 1}", root, null, UiFx.A(u.bone, 0.12f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(580f + i * 26f, 0f), new Vector2(22f, 12f));
                row.value = UiText("Valeur", root, a.mono, 13, u.BoneDim, TextAnchor.MiddleLeft, "0 → 0", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(730f, 0f), new Vector2(430f, 22f));
                var buy = UiButton("Acheter", root, "900 C", a.mono, 14, u.bone, new Color(1f, 1f, 1f, 0.07f), UiFx.A(u.amber, 0.45f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(240f, 36f), TextAnchor.MiddleCenter);
                row.buyButton = buy; row.buyLabel = buy.GetComponentInChildren<Text>();
                OnClick(buy, row.Buy);
                var sel = UiRect("Selecteur_DIVIN", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(240f, 36f));
                row.divineSelector = sel.gameObject;
                row.prevButton = UiButton("Moins", sel, "◀", a.mono, 16, u.gold, UiFx.A(u.divineViolet, 0.35f), UiFx.A(u.gold, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(48f, 36f), TextAnchor.MiddleCenter);
                row.divineLevel = UiText("Niveau", sel, a.mono, 15, u.gold, TextAnchor.MiddleCenter, "5", C, C, Vector2.zero, new Vector2(120f, 30f));
                row.nextButton = UiButton("Plus", sel, "▶", a.mono, 16, u.gold, UiFx.A(u.divineViolet, 0.35f), UiFx.A(u.gold, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(48f, 36f), TextAnchor.MiddleCenter);
                OnClick(row.prevButton, row.Prev); OnClick(row.nextButton, row.Next);
                sel.gameObject.SetActive(false);
                a.upgradeRow = Save(root.gameObject, $"{dir}/MGSUGE_UI_LigneAmelioration.prefab");
            }
            // ligne de commande (§8)
            {
                var root = UiRect("MGSUGE_UI_LigneCommande", null, TL, TL, Vector2.zero, new Vector2(700f, 40f));
                root.gameObject.AddComponent<CanvasGroup>();
                UiText("Touche", root, a.mono, 13, u.amber, TextAnchor.UpperLeft, "Touche", TL, TL, Vector2.zero, new Vector2(150f, 20f));
                UiText("Action", root, a.textSemi, 14, u.bone, TextAnchor.UpperLeft, "Action", TL, TL, new Vector2(160f, 0f), new Vector2(530f, 20f));
                UiText("Detail", root, a.text, 12, u.BoneDim, TextAnchor.UpperLeft, "", TL, TL, new Vector2(160f, -20f), new Vector2(540f, 18f), wrap: true);
                a.commandLine = Save(root.gameObject, $"{dir}/MGSUGE_UI_LigneCommande.prefab");
            }
            // chiffre de degats, marque de verrouillage, point de la mini-carte
            {
                var t = UiText("MGSUGE_UI_ChiffreDegats", null, a.mono, 15, u.bone, TextAnchor.MiddleCenter, "115", C, C, Vector2.zero, new Vector2(120f, 24f));
                a.damageNumber = Save(t.gameObject, $"{dir}/MGSUGE_UI_ChiffreDegats.prefab");
                var m = UiImage("MGSUGE_UI_Marque", null, a.diamond, u.amber, C, C, Vector2.zero, new Vector2(16f, 16f));
                a.mark = Save(m.gameObject, $"{dir}/MGSUGE_UI_Marque.prefab");
                var b = UiImage("MGSUGE_UI_PointCarte", null, null, u.threat, C, C, Vector2.zero, new Vector2(5f, 5f));
                a.blip = Save(b.gameObject, $"{dir}/MGSUGE_UI_PointCarte.prefab");
            }
        }
    }
}
