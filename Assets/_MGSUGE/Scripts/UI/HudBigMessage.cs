using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Grand message (centre haut, a 30 % de la hauteur, §16.1) : titre en tres grandes majuscules + sous-titre, apparition
    /// par espacement de lettres, duree 2,5 a 5 s. Alimente par Msg.Big(titre, sous-titre, duree).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Grand message")]
    public class HudBigMessage : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        public Text title;
        [Tooltip("espacement anime du titre")] public LetterSpacing spacing;
        public Text subtitle;
        [Tooltip("couleur du sous-titre : accent orange")] public bool amberSubtitle = true;

        void Update()
        {
            if (hud == null || hud.data == null) return;
            var d = hud.data;
            float age = UiFx.Now - Msg.BigTime;
            bool on = age >= 0f && age <= Msg.BigDuration && !string.IsNullOrEmpty(Msg.BigTitle) && hud.Visible;
            UiFx.SetActive(title, on); UiFx.SetActive(subtitle, on && !string.IsNullOrEmpty(Msg.BigSub));
            if (!on) return;
            float appear = UiFx.ReduceMotion ? 1f : UiFx.EaseOut(age / d.bigAppearTime);
            float a = Mathf.Clamp01(appear * 1.4f) * Mathf.Clamp01((Msg.BigDuration - age) / d.bigFadeTime);
            UiFx.SetText(title, UiFx.Upper(Msg.BigTitle));
            UiFx.SetColor(title, UiFx.A(d.bone, a));
            if (spacing != null) spacing.SetSpacing(Mathf.Lerp(d.bigSpacingFrom, d.bigSpacingTo, appear));
            UiFx.SetText(subtitle, Msg.BigSub);
            UiFx.SetColor(subtitle, UiFx.A(amberSubtitle ? d.amber : d.bone, a));
        }
    }
}
