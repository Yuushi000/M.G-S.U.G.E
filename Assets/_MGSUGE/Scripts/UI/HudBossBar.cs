using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Barre de boss (haut centre, §16.1 et §14.1) : nom en violet (+ « niv. N »), distance, barre de vie rouge-rose, jauge
    /// d'impact orange (rouge clignotante en surcharge), ligne d'etat. Affiche le boss vivant le plus proche dans la portee.
    /// Les boss arrivent au jalon 7 (ils renseigneront le niveau et la ligne d'etat) : la barre reste cachee d'ici la.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Barre de boss")]
    public class HudBossBar : MonoBehaviour
    {
        public static HudBossBar I { get; private set; }
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("contenu affiche / cache")] public GameObject content;
        public Text bossName;
        public Text distance;
        public UiBar hp;
        public UiBar impact;
        [Tooltip("ligne d'etat (« Surcharge d'impact : degats ×1,6 », « Combat contre ... »)")] public Text state;
        [Tooltip("portee d'affichage (m)")] [Min(0f)] public float range = 2500f;
        [Tooltip("texte d'etat en surcharge")] public string overloadText = "Surcharge d'impact : dégâts ×1,6";
        /// <summary>Renseignes par les boss (jalon 7).</summary>
        public static int Level; public static string StateLine;
        public Targetable Current { get; private set; }

        void Awake() { I = this; }

        void Update()
        {
            if (hud == null || hud.player == null || hud.data == null) return;
            Current = null; float best = range;
            Vector3 p = hud.player.Center;
            foreach (var t in Targetable.All)
            {
                if (!t.isBoss || !t.Alive) continue;
                float dd = Vector3.Distance(p, t.Position);
                if (dd < best) { best = dd; Current = t; }
            }
            if (content != null) UiFx.SetActive(content, Current != null);
            if (Current == null) return;
            var d = hud.data;
            UiFx.SetText(bossName, Current.displayName + (Level > 0 ? $" · niv. {Level}" : ""));
            UiFx.SetText(distance, UiFx.N(best) + " m");
            if (hp != null) hp.Set(Current.HP / Mathf.Max(1f, Current.maxHP));
            if (impact != null)
            {
                impact.Set(Current.impactMax > 0f ? Current.impact / Current.impactMax : 0f);
                impact.SetColor(Current.Overloaded ? (UiFx.Blink(0.3f) ? d.threat : UiFx.A(d.threat, 0.35f)) : d.amber);
            }
            UiFx.SetText(state, Current.Overloaded ? overloadText : StateLine ?? "");
        }
    }
}
