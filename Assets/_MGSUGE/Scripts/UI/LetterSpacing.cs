using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Espacement des lettres pour un Text uGUI (titres en majuscules « espacement large », §4.5) ; anime par le grand message.
    /// Respecte l'alignement gauche / centre / droite du Text, ligne par ligne. Le texte riche n'est pas gere.
    /// Placer ce composant AVANT un eventuel Shadow / Outline dans l'Inspector.
    /// </summary>
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("MGSUGE/UI/Espacement des lettres")]
    public class LetterSpacing : BaseMeshEffect
    {
        [Tooltip("espace ajoute entre deux lettres (unites du canvas, px a 1080p)")] [Range(-10f, 120f)] public float spacing = 6f;
        Text txt;
        readonly List<int> col = new List<int>(), lineLen = new List<int>(), lineOf = new List<int>();

        public void SetSpacing(float s)
        {
            if (Mathf.Approximately(s, spacing)) return;
            spacing = s; if (graphic != null) graphic.SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); if (graphic != null) graphic.SetVerticesDirty(); }
#endif

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0 || Mathf.Approximately(spacing, 0f)) return;
            if (txt == null) txt = GetComponent<Text>();
            string s = txt.text; if (string.IsNullOrEmpty(s)) return;
            int quads = vh.currentVertCount / 4;

            // colonne de chaque caractere dans sa ligne
            col.Clear(); lineOf.Clear(); lineLen.Clear();
            int c = 0, line = 0;
            var visible = new List<int>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '\n') { lineLen.Add(c); c = 0; line++; continue; }
                col.Add(c); lineOf.Add(line);
                if (!char.IsWhiteSpace(ch)) visible.Add(col.Count - 1);
                c++;
            }
            lineLen.Add(c);
            // Unity 2019.1+ ne genere pas de quad pour les espaces ; les versions plus anciennes en generent un par caractere
            bool perVisible = quads == visible.Count;
            if (!perVisible && quads != col.Count) return;

            float align = 0f;
            switch (txt.alignment)
            {
                case TextAnchor.UpperCenter: case TextAnchor.MiddleCenter: case TextAnchor.LowerCenter: align = 0.5f; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight: align = 1f; break;
            }
            UIVertex v = default;
            for (int q = 0; q < quads; q++)
            {
                int k = perVisible ? visible[q] : q;
                float off = col[k] * spacing - align * (lineLen[lineOf[k]] - 1) * spacing;
                for (int j = 0; j < 4; j++)
                {
                    vh.PopulateUIVertex(ref v, q * 4 + j);
                    v.position.x += off;
                    vh.SetUIVertex(v, q * 4 + j);
                }
            }
        }
    }
}
