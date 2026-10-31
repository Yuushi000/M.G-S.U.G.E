using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Verrouillage (surimpression, §16.2) : cadre a coins autour de la cible verrouillee et sa distance, losanges orange sur les
    /// cibles des missiles, petits reperes rouges sur les autres ennemis visibles.
    /// Les losanges et reperes sont des copies du prefab MGSUGE_UI_Marque creees au lancement (reserve).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Verrouillage")]
    public class OverlayTargets : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("cadre de verrouillage (les coins sont ses enfants ancres aux angles)")] public RectTransform lockFrame;
        [Tooltip("couleur des coins du cadre")] public Graphic[] lockCorners;
        [Tooltip("distance a cote du cadre")] public Text lockDistance;
        [Tooltip("prefab d'une marque (losange ou repere)")] public Image markPrefab;
        [Tooltip("parent des marques")] public RectTransform markParent;
        [Tooltip("sprite losange (contour)")] public Sprite diamondSprite;
        [Tooltip("sprite carre plein (repere)")] public Sprite squareSprite;
        [Header("Reglages")]
        [Tooltip("nombre de marques en reserve")] [Range(8, 200)] public int poolSize = 64;
        [Tooltip("distance maximale des reperes (m)")] [Min(0f)] public float maxDistance = 1600f;
        [Tooltip("taille d'un losange (px a 1080p)")] [Range(4f, 40f)] public float diamondSize = 16f;
        [Tooltip("taille d'un repere (px)")] [Range(1f, 20f)] public float markSize = 4f;
        [Tooltip("taille mini / maxi du cadre (px)")] public Vector2 frameSize = new Vector2(26f, 260f);
        [Tooltip("marge du cadre autour de la sphere de la cible")] [Range(1f, 4f)] public float frameScale = 2.4f;

        readonly List<Image> pool = new List<Image>(); int used; Canvas canvas;

        void Start()
        {
            canvas = GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas;
            if (markPrefab == null || markParent == null) return;
            for (int i = 0; i < poolSize; i++) { var m = Instantiate(markPrefab, markParent); m.name = markPrefab.name; m.gameObject.SetActive(false); pool.Add(m); }
        }

        void LateUpdate()
        {
            used = 0;
            bool ready = hud != null && hud.player != null && hud.cam != null && hud.data != null;
            bool show = ready && (GameManager.I == null || GameManager.I.State == GameState.Play);
            Targetable target = show && hud.lockOn != null ? hud.lockOn.Target : null;
            if (show)
            {
                var d = hud.data; var p = hud.player; var cam = hud.cam; var wd = p.weaponData;
                Transform ct = cam.transform;
                foreach (var t in Targetable.All)
                {
                    if (!t.Alive || !t.lockable || t == target) continue;
                    float dist = Vector3.Distance(p.Center, t.Position);
                    if (dist > maxDistance || !UiFx.ToScreen(cam, t.Position, out Vector2 sp)) continue;
                    bool inCone = t.missileTarget && wd != null && dist < wd.missileRange && Vector3.Angle(ct.forward, t.Position - ct.position) * Mathf.Deg2Rad < wd.missileCone;
                    if (inCone) Mark(sp, diamondSprite, diamondSize, d.amber);
                    else Mark(sp, squareSprite, markSize, UiFx.A(d.threat, 0.7f));
                }
            }
            for (int i = used; i < pool.Count; i++) if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);

            Vector2 tp = Vector2.zero;
            bool lockOn = target != null && UiFx.ToScreen(hud.cam, target.Position, out tp);
            if (lockFrame != null) UiFx.SetActive(lockFrame.gameObject, lockOn);
            if (!lockOn) return;
            float k = canvas != null ? canvas.scaleFactor : 1f;
            float camDist = Vector3.Distance(hud.cam.transform.position, target.Position);
            float sizePx = target.radius * frameScale / Mathf.Max(1f, camDist * Mathf.Tan(hud.cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Screen.height * 0.5f / k;
            sizePx = Mathf.Clamp(sizePx, frameSize.x, frameSize.y);
            lockFrame.anchoredPosition = UiFx.ScreenToCanvas(canvas, tp);
            lockFrame.sizeDelta = new Vector2(sizePx, sizePx);
            Color c = hud.data.threat;
            if (lockCorners != null) foreach (var g in lockCorners) UiFx.SetColor(g, c);
            UiFx.SetText(lockDistance, UiFx.N(Vector3.Distance(hud.player.Center, target.Position)) + " m");
            UiFx.SetColor(lockDistance, c);
        }

        void Mark(Vector2 screen, Sprite s, float size, Color c)
        {
            if (used >= pool.Count) return;
            var m = pool[used++];
            if (!m.gameObject.activeSelf) m.gameObject.SetActive(true);
            if (m.sprite != s) m.sprite = s;
            m.color = c;
            m.rectTransform.anchoredPosition = UiFx.ScreenToCanvas(canvas, screen);
            m.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
