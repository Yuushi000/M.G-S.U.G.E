using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Fil de messages (haut gauche, sous l'objectif, §16.1) : 5 messages au maximum, apparition par glissement, disparition
    /// apres 3,8 s. Couleurs : normal (os), avertissement (orange), danger (rouge), bonne nouvelle (bleu glace).
    /// Les lignes sont des copies du prefab MGSUGE_UI_LigneMessage, enfants de cet objet.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Fil de messages")]
    public class HudFeed : MonoBehaviour
    {
        [Tooltip("racine du HUD (couleurs, durees)")] public HudController hud;
        [Tooltip("lignes de texte, de haut en bas")] public Text[] lines = new Text[5];
        [Tooltip("hauteur d'une ligne (px a 1080p)")] [Range(10f, 60f)] public float lineHeight = 24f;

        void Update()
        {
            if (hud == null || hud.data == null || !hud.Visible) return;
            var d = hud.data;
            Msg.Duration = d.messageDuration; Msg.MaxLines = Mathf.Min(d.maxMessages, lines.Length);
            Msg.BigMin = d.bigMinDuration; Msg.BigMax = Mathf.Max(d.bigMinDuration, d.bigMaxDuration);
            float now = UiFx.Now;
            int n = 0;
            for (int i = 0; i < Msg.Feed.Count && n < lines.Length; i++)
            {
                var m = Msg.Feed[i];
                float age = now - m.time; if (age > d.messageDuration) continue;
                var t = lines[n]; if (t == null) { n++; continue; }
                float slide = UiFx.ReduceMotion || d.messageSlideTime <= 0f ? 1f : UiFx.EaseOut(age / d.messageSlideTime);
                float a = Mathf.Clamp01((d.messageDuration - age) / Mathf.Max(0.01f, d.messageFadeTime)) * (UiFx.ReduceMotion ? 1f : Mathf.Clamp01(slide * 1.5f));
                Color c = m.kind == MsgKind.Danger ? d.threat : m.kind == MsgKind.Warning ? d.amber : m.kind == MsgKind.Good ? d.ice : d.bone;
                c.a *= a;
                UiFx.SetActive(t, true);
                UiFx.SetText(t, m.text); UiFx.SetColor(t, c);
                t.rectTransform.anchoredPosition = new Vector2(-(1f - slide) * d.messageSlideDistance, -n * lineHeight);
                n++;
            }
            for (; n < lines.Length; n++) if (lines[n] != null) UiFx.SetActive(lines[n], false);
        }
    }
}
