using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Armes (bas droite, §16.1) : 9 lignes — Fusil a impulsions (CLIC G, ∞), Missiles (CLIC D, ×N), Salve (C, ×N ou « 3/5 »),
    /// Fragmentation (V, ×N), Lame a plasma (F, portee), Kit de reparation (H, ×N), Rayon Arcane (G : « 1 M C », « PRÊT »,
    /// N s, « ACTIF »), Extension du territoire (B : « 500 k C », « PRÊT », duree, « ACTIF »), Sondes laser (AUTO, N/5).
    /// Les lignes sont des copies du prefab MGSUGE_UI_LigneArme, enfants de cet objet.
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/HUD Armes")]
    public class HudWeapons : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Tooltip("lignes de haut en bas")] public HudWeaponRow[] rows = new HudWeaponRow[9];
        [Header("Textes")]
        public string readyText = "PRÊT";
        public string activeText = "ACTIF";

        /// <summary>Etat des competences speciales (renseigne au jalon 8) : recharge restante et duree active restante (s).</summary>
        public static float ArcaneCooldown, ArcaneActive, DomainCooldown, DomainActive, DomainCooldownMax = 300f, ArcaneCooldownMax = 60f;

        void Update()
        {
            if (hud == null || hud.player == null || hud.data == null || !hud.Visible) return;
            var d = hud.data; var p = hud.player; var S = p.S; var w = hud.weapons; var pr = p.progress;
            foreach (var r in rows)
            {
                if (r == null) continue;
                switch (r.line)
                {
                    case WeaponLine.Fusil: r.Show(d, "∞", w != null ? w.Cooldown01("rifle") : 0f, true); break;
                    case WeaponLine.Missiles: r.Show(d, "×" + S.missileCount, w != null ? w.Cooldown01("missile") : 0f, true); break;
                    case WeaponLine.Salve:
                        string sc = w != null && w.SalvoLeft > 0 ? $"{S.salvoCount - w.SalvoLeft}/{S.salvoCount}" : "×" + S.salvoCount;
                        r.Show(d, sc, w != null ? w.Cooldown01("salvo") : 0f, true); break;
                    case WeaponLine.Fragmentation: r.Show(d, "×" + S.fragMinis, w != null ? w.Cooldown01("frag") : 0f, true); break;
                    case WeaponLine.Lame: r.Show(d, UiFx.N(S.bladeRange) + " m", w != null ? w.Cooldown01("blade") : 0f, true); break;
                    case WeaponLine.Kit: r.Show(d, "×" + p.kits, 0f, p.kits > 0, d.bone); break;
                    case WeaponLine.RayonArcane:
                        if (!pr.arcane) r.Show(d, UiFx.ShortCredits(p.progression.arcaneCost), 0f, false);
                        else if (ArcaneActive > 0f) r.Show(d, activeText, 0f, true);
                        else if (ArcaneCooldown > 0f) r.Show(d, UiFx.N(Mathf.Ceil(ArcaneCooldown)) + " s", ArcaneCooldown / Mathf.Max(1f, ArcaneCooldownMax), true);
                        else r.Show(d, readyText, 0f, true);
                        break;
                    case WeaponLine.Territoire:
                        if (!pr.domain) r.Show(d, UiFx.ShortCredits(p.progression.domainCost), 0f, false);
                        else if (DomainActive > 0f) r.Show(d, activeText, 0f, true);
                        else if (DomainCooldown > 0f) r.Show(d, UiFx.Duration(DomainCooldown), DomainCooldown / Mathf.Max(1f, DomainCooldownMax), true);
                        else r.Show(d, readyText, 0f, true);
                        break;
                    case WeaponLine.Sondes:
                        int n = hud.probes != null ? hud.probes.Owned : pr.probes;
                        int max = p.weaponData != null ? p.weaponData.probeMax : 5;
                        bool fighting = hud.probes != null && hud.probes.Fighting && UiFx.Blink(0.5f);
                        r.Show(d, $"{n}/{max}", 0f, n > 0, fighting ? Color.white : (Color?)null);
                        break;
                }
            }
        }
    }
}
