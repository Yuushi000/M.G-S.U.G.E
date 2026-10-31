using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Alerte de missiles entrants (surimpression, §16.2) : texte rouge clignotant et chevrons qui pointent vers chaque missile
    /// guide sur le joueur (sur un cercle autour du centre).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Missiles entrants")]
    public class OverlayMissileAlert : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        public Text label;
        [Tooltip("pivots des chevrons (enfant : l'image du chevron, au-dessus du centre)")] public RectTransform[] chevrons;
        [Tooltip("texte pour un missile")] public string oneText = "MISSILE ENTRANT";
        [Tooltip("texte pour plusieurs ; {0} = nombre")] public string manyText = "MISSILES ENTRANTS ×{0}";
        [Tooltip("periode du clignotement (s)")] [Range(0.1f, 2f)] public float blink = 0.35f;

        void LateUpdate()
        {
            var ms = MissileSystem.I;
            bool ready = hud != null && hud.player != null && hud.cam != null && hud.data != null && ms != null && (GameManager.I == null || GameManager.I.State == GameState.Play);
            int n = ready ? ms.IncomingCount : 0;
            if (label != null)
            {
                UiFx.SetActive(label, n > 0);
                if (n > 0)
                {
                    UiFx.SetText(label, n == 1 ? oneText : string.Format(manyText, n));
                    UiFx.SetColor(label, UiFx.A(hud.data.threat, UiFx.ReduceMotion || UiFx.Blink(blink) ? 1f : 0.35f));
                }
            }
            int k = 0;
            if (n > 0 && chevrons != null)
            {
                Vector3 f = hud.cam.transform.forward; f.y = 0f;
                foreach (var pos in ms.IncomingPositions)
                {
                    if (k >= chevrons.Length) break;
                    var r = chevrons[k++]; if (r == null) continue;
                    Vector3 to = pos - hud.player.Center; to.y = 0f;
                    float ang = Vector3.SignedAngle(f, to, Vector3.up);
                    UiFx.SetActive(r.gameObject, true);
                    r.localRotation = Quaternion.Euler(0f, 0f, -ang);
                    var g = r.GetComponentInChildren<Graphic>(); if (g != null) g.color = hud.data.threat;
                }
            }
            if (chevrons != null) for (; k < chevrons.Length; k++) if (chevrons[k] != null) UiFx.SetActive(chevrons[k].gameObject, false);
        }
    }
}
