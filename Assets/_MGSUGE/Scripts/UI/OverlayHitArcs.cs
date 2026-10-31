using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Indicateurs de direction des coups recus (surimpression, §16.2) : arcs rouges autour du centre selon l'angle de
    /// provenance relatif a la camera. Chaque arc est un pivot au centre de l'ecran dont l'enfant est l'arc lui-meme.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Surimpression Coups recus")]
    public class OverlayHitArcs : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("pivots des arcs (enfants : l'image de l'arc)")] public RectTransform[] arcs;
        [Tooltip("duree d'affichage d'un arc (s)")] [Range(0.1f, 5f)] public float duration = 1.5f;

        void LateUpdate()
        {
            int n = 0;
            bool ready = hud != null && hud.player != null && hud.cam != null && hud.data != null && (GameManager.I == null || GameManager.I.State == GameState.Play);
            if (ready && arcs != null)
            {
                Vector3 f = hud.cam.transform.forward; f.y = 0f;
                var hits = hud.player.recentHits;
                for (int i = hits.Count - 1; i >= 0 && n < arcs.Length; i--)
                {
                    float a = 1f - (Time.time - hits[i].Key) / duration; if (a <= 0f) continue;
                    Vector3 from = -hits[i].Value; from.y = 0f;
                    float ang = Vector3.SignedAngle(f, from, Vector3.up);
                    var r = arcs[n++]; if (r == null) continue;
                    UiFx.SetActive(r.gameObject, true);
                    r.localRotation = Quaternion.Euler(0f, 0f, -ang);
                    var g = r.GetComponentInChildren<Graphic>();
                    if (g != null) g.color = UiFx.A(hud.data.threat, a);
                }
            }
            if (arcs != null) for (; n < arcs.Length; n++) if (arcs[n] != null) UiFx.SetActive(arcs[n].gameObject, false);
        }
    }
}
