using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Change les 7 couleurs de role du chassis sans dupliquer le maillage (§3.1 : variantes Corbeau, as, Titan, Archonte).
    /// Le role est lu dans le nom du materiau (MGSUGE_Vesper_[Role]_...). Les nuances du modele (blanc, gris moyen, noir)
    /// sont conservees en proportion de la luminance par rapport au materiau principal du role.
    /// </summary>
    public class PaletteApplier : MonoBehaviour
    {
        public PaletteData palette;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Emission = Shader.PropertyToID("_EmissionColor");

        void Start() { Apply(); }

        public static string RoleOf(string matName)
        {
            const string pre = "MGSUGE_Vesper_";
            int i = matName.IndexOf(pre);
            if (i < 0) return null;
            string rest = matName.Substring(i + pre.Length);
            int u = rest.IndexOfAny(new[] { '_', ' ' });
            return u < 0 ? rest : rest.Substring(0, u);
        }
        static float Lum(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        public void Apply()
        {
            if (palette == null || palette.useOriginal) return;
            var rends = GetComponentsInChildren<Renderer>(true);
            // couleur de reference de chaque role = materiau principal (le plus clair) du role
            var reference = new Dictionary<string, float>();
            foreach (var r in rends)
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !m.HasProperty(BaseColor)) continue;
                    string role = RoleOf(m.name); if (role == null) continue;
                    float l = Lum(m.GetColor(BaseColor).linear);
                    if (!reference.TryGetValue(role, out float v) || l > v) reference[role] = l;
                }
            var mpb = new MaterialPropertyBlock();
            foreach (var r in rends)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null || !m.HasProperty(BaseColor)) continue;
                    string role = RoleOf(m.name); if (role == null) continue;
                    Color target = palette.Get(role).linear;
                    float shade = reference.TryGetValue(role, out float refL) && refL > 1e-4f ? Mathf.Clamp(Lum(m.GetColor(BaseColor).linear) / refL, 0.05f, 1.5f) : 1f;
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor(BaseColor, (target * shade).gamma);
                    if (m.HasProperty(Emission))
                    {
                        Color e = m.GetColor(Emission).linear; float k = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
                        if (k > 0.01f) mpb.SetColor(Emission, (target * k).gamma);
                    }
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }
    }
}
