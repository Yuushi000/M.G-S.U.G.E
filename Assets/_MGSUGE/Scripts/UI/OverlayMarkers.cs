using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Marqueurs (surimpression, §16.2) : objectif du contrat en cours (losange orange + « OBJECTIF · N m ») et boss le plus
    /// proche (violet), ramenes au bord de l'ecran quand ils sont hors champ.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Marqueurs")]
    public class OverlayMarkers : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Header("Objectif")]
        public RectTransform objective;
        public Graphic objectiveIcon;
        public Text objectiveText;
        [Tooltip("format ; {0} = distance")] public string objectiveFormat = "OBJECTIF · {0} m";
        [Header("Boss")]
        public RectTransform boss;
        public Graphic bossIcon;
        public Text bossText;
        [Tooltip("portee d'affichage du marqueur de boss (m)")] [Min(0f)] public float bossRange = 6000f;
        [Header("Reglages")]
        [Tooltip("marge au bord de l'ecran (px a 1080p)")] [Range(0f, 300f)] public float edgeMargin = 120f;
        Canvas canvas;

        void Start() { canvas = GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas; }

        void LateUpdate()
        {
            bool ready = hud != null && hud.player != null && hud.cam != null && hud.data != null && (GameManager.I == null || GameManager.I.State == GameState.Play);
            var d = ready ? hud.data : null;
            // objectif
            var cm = ContractManager.I;
            bool obj = ready && cm != null && cm.Current != null && !cm.Waiting;
            if (objective != null) UiFx.SetActive(objective.gameObject, obj);
            if (obj)
            {
                var e = cm.Current.NearestAlive(hud.player.Center);
                Vector3 wp = e != null ? e.transform.position : cm.Current.LocationPoint;
                objective.anchoredPosition = Place(wp);
                UiFx.SetColor(objectiveIcon, d.amber); UiFx.SetColor(objectiveText, d.amber);
                UiFx.SetText(objectiveText, string.Format(objectiveFormat, UiFx.N(Vector3.Distance(hud.player.Center, wp))));
            }
            // boss
            Targetable b = null; float best = bossRange;
            if (ready) foreach (var t in Targetable.All)
                {
                    if (!t.isBoss || !t.Alive) continue;
                    float dd = Vector3.Distance(hud.player.Center, t.Position);
                    if (dd < best) { best = dd; b = t; }
                }
            if (boss != null) UiFx.SetActive(boss.gameObject, b != null);
            if (b != null)
            {
                boss.anchoredPosition = Place(b.Position);
                UiFx.SetColor(bossIcon, d.violet); UiFx.SetColor(bossText, d.violet);
                UiFx.SetText(bossText, b.displayName + " · " + UiFx.N(best) + " m");
            }
        }

        /// <summary>Position du marqueur dans le canvas, ramenee au bord si hors champ ou derriere.</summary>
        Vector2 Place(Vector3 world)
        {
            Vector3 sp = hud.cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            Vector2 p = new Vector2(sp.x, sp.y);
            if (behind) p = new Vector2(Screen.width - p.x, Screen.height - p.y);
            float k = canvas != null ? canvas.scaleFactor : 1f, m = edgeMargin * k;
            bool off = behind || p.x < m || p.x > Screen.width - m || p.y < m || p.y > Screen.height - m;
            if (off)
            {
                Vector2 ctr = new Vector2(Screen.width, Screen.height) * 0.5f, dl = p - ctr;
                if (dl.sqrMagnitude < 1f) dl = Vector2.down;
                float kx = (Screen.width * 0.5f - m) / Mathf.Max(1e-3f, Mathf.Abs(dl.x)), ky = (Screen.height * 0.5f - m) / Mathf.Max(1e-3f, Mathf.Abs(dl.y));
                p = ctr + dl * Mathf.Min(kx, ky);
            }
            return UiFx.ScreenToCanvas(canvas, p);
        }
    }
}
