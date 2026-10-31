using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Chiffres de degats flottants au-dessus des ennemis touches (surimpression, §16.2), d'une autre couleur en surcharge.
    /// Copies du prefab MGSUGE_UI_ChiffreDegats creees au lancement (reserve).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Chiffres de degats")]
    public class OverlayDamageNumbers : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("prefab d'un chiffre")] public Text numberPrefab;
        [Tooltip("nombre de chiffres en reserve")] [Range(8, 120)] public int poolSize = 60;
        [Tooltip("montee du chiffre (m/s)")] [Range(0f, 30f)] public float rise = 6f;
        [Tooltip("taille normale / en surcharge (px a 1080p)")] public Vector2Int fontSizes = new Vector2Int(15, 19);
        readonly List<Text> pool = new List<Text>(); Canvas canvas;

        void Start()
        {
            canvas = GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas;
            if (numberPrefab == null) return;
            for (int i = 0; i < poolSize; i++) { var t = Instantiate(numberPrefab, transform); t.name = numberPrefab.name; t.gameObject.SetActive(false); pool.Add(t); }
        }

        void LateUpdate()
        {
            int n = 0;
            bool ready = hud != null && hud.cam != null && hud.data != null && (GameManager.I == null || GameManager.I.State == GameState.Play);
            if (ready)
            {
                float now = Time.time; var d = hud.data;
                var list = DamageNumbers.List;
                for (int i = list.Count - 1; i >= 0 && n < pool.Count; i--)
                {
                    var e = list[i];
                    float age = now - e.time; if (age > DamageNumbers.Life || age < 0f) continue;
                    if (!UiFx.ToScreen(hud.cam, e.pos + Vector3.up * age * rise, out Vector2 sp)) continue;
                    var t = pool[n++];
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t.rectTransform.anchoredPosition = UiFx.ScreenToCanvas(canvas, sp);
                    UiFx.SetText(t, UiFx.N(e.value));
                    t.fontSize = e.overload ? fontSizes.y : fontSizes.x;
                    t.color = UiFx.A(e.overload ? d.amber : d.bone, 1f - age / DamageNumbers.Life);
                }
            }
            for (; n < pool.Count; n++) if (pool[n].gameObject.activeSelf) pool[n].gameObject.SetActive(false);
        }
    }
}
