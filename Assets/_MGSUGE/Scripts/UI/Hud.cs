using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>Animation "+N C" sous le compteur de credits (§11.1).</summary>
    public static class HudCredits
    {
        public static long LastGain; public static float LastTime = -9f;
        public static void Gain(long c) { LastGain = c; LastTime = Time.unscaledTime; }
    }

    /// <summary>
    /// HUD de travail (IMGUI) couvrant les besoins des jalons 3-4 : armure/energie, vitesse/altitude, armes, cible verrouillee,
    /// losanges des missiles, fil de messages, credits/secteur, chiffres de degats, vignette, menus pause/ameliorations/commandes,
    /// ecran "Chassis detruit". Le HUD final en traits fins (UI Toolkit, polices Saira / IBM Plex Mono) arrive au jalon 6.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public PlayerController player;
        public WeaponController weapons;
        public ProbeController probes;
        public LockOn lockOn;
        public WorldData world;

        static readonly Color Bone = new Color32(0xF1, 0xE8, 0xDA, 0xFF), Amber = new Color32(0xFF, 0xA7, 0x33, 0xFF),
            Threat = new Color32(0xFF, 0x44, 0x58, 0xFF), Ice = new Color32(0x86, 0xD8, 0xFF, 0xFF), Rose = new Color32(0xFF, 0x4F, 0xD8, 0xFF),
            Violet = new Color32(0xC0, 0x7B, 0xFF, 0xFF), Gold = new Color32(0xFF, 0xD2, 0x7A, 0xFF), ProbeY = new Color32(0xFF, 0xD8, 0x4A, 0xFF),
            Bg = new Color32(0x0B, 0x06, 0x10, 0xC8);
        static readonly CultureInfo FR = CultureInfo.GetCultureInfo("fr-FR");
        GUIStyle st; float s; float shownCredits; Vector2 scroll;
        Camera cam;

        void Start() { shownCredits = player != null ? player.progress.credits : 0; }

        // ------------------------------------------------------------------ outils de dessin
        void Text(float x, float y, float w, float h, string t, Color c, int size, TextAnchor a = TextAnchor.UpperLeft, bool bold = false)
        {
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s)); st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            var r = new Rect(x * s, y * s, w * s, h * s);
            st.normal.textColor = new Color(0, 0, 0, c.a * 0.85f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), t, st);
            st.normal.textColor = c; GUI.Label(r, t, st);
        }
        void Box(float x, float y, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(x * s, y * s, w * s, h * s), Texture2D.whiteTexture); GUI.color = Color.white; }
        void BoxPx(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Bar(float x, float y, float w, float h, float k, Color fill, Color back) { Box(x, y, w, h, back); Box(x, y, w * Mathf.Clamp01(k), h, fill); }
        void FrameRect(Rect r, Color c, float t = 1.5f)
        { BoxPx(new Rect(r.x, r.y, r.width, t), c); BoxPx(new Rect(r.x, r.yMax - t, r.width, t), c); BoxPx(new Rect(r.x, r.y, t, r.height), c); BoxPx(new Rect(r.xMax - t, r.y, t, r.height), c); }
        static string N(float v) => Mathf.RoundToInt(v).ToString("N0", FR).Replace(' ', ' ').Replace(' ', ' ');
        bool ToScreen(Vector3 w, out Vector2 p)
        {
            var sp = cam.WorldToScreenPoint(w); p = new Vector2(sp.x, Screen.height - sp.y);
            return sp.z > 0f;
        }

        void OnGUI()
        {
            if (player == null) return;
            if (cam == null) cam = Camera.main; if (cam == null) return;
            if (st == null) st = new GUIStyle(GUI.skin.label) { richText = false, wordWrap = false };
            s = Screen.height / 1080f;
            float W = Screen.width / s;
            var gm = GameManager.I;
            if (gm != null && gm.State == GameState.Dead) { DeadScreen(W); return; }

            DamageNums();
            Targets(W);
            Reticle(W);
            ArmorEnergy(W);
            WeaponsPanel(W);
            TopLeft();
            TopRight(W);
            BigMessage(W);
            Vignette();
            if (weapons != null && Time.time - weapons.BladeArcTime < 0.3f) BladeArc(W);
            if (gm != null && gm.State == GameState.Pause) PauseMenu(W, gm);
        }

        void Reticle(float W)
        {
            float cx = W / 2f, cy = 540f;
            Color c = new Color(Bone.r, Bone.g, Bone.b, 0.8f);
            float g = 14f;
            Box(cx - g - 12f, cy - 0.75f, 12f, 1.5f, c); Box(cx + g, cy - 0.75f, 12f, 1.5f, c);
            Box(cx - 0.75f, cy - g - 12f, 1.5f, 12f, c); Box(cx - 0.75f, cy + g, 1.5f, 12f, c);
            Box(cx - 1.5f, cy - 1.5f, 3f, 3f, c);
            // vitesse et altitude a droite du reticule
            Text(cx + 60f, cy - 24f, 200f, 24f, N(player.SpeedKmh) + " km/h", Bone, 16);
            Text(cx + 60f, cy, 200f, 24f, N(player.Altitude) + " m", new Color(Bone.r, Bone.g, Bone.b, 0.62f), 14);
            if (player.IsShip) // ligne d'horizon artificielle
            {
                Vector3 f = player.ShipRotation * Vector3.forward, u = player.ShipRotation * Vector3.up;
                float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, f), u, f);
                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(-roll, new Vector2(cx * s, cy * s));
                Box(cx - 160f, cy + Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * 200f, 110f, 1f, new Color(Ice.r, Ice.g, Ice.b, 0.5f));
                Box(cx + 50f, cy + Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * 200f, 110f, 1f, new Color(Ice.r, Ice.g, Ice.b, 0.5f));
                GUI.matrix = m;
            }
        }

        void Targets(float W)
        {
            var target = lockOn != null ? lockOn.Target : null;
            Transform ct = cam.transform;
            foreach (var t in Targetable.All)
            {
                if (!t.Alive || !t.lockable) continue;
                float dist = Vector3.Distance(player.Center, t.Position);
                if (dist > 1600f || !ToScreen(t.Position, out Vector2 p)) continue;
                bool inCone = t.missileTarget && dist < player.weaponData.missileRange && Vector3.Angle(ct.forward, t.Position - ct.position) * Mathf.Deg2Rad < player.weaponData.missileCone;
                if (inCone && t != target) // losange orange
                {
                    Matrix4x4 m = GUI.matrix; GUIUtility.RotateAroundPivot(45f, p);
                    FrameRect(new Rect(p.x - 7f * s, p.y - 7f * s, 14f * s, 14f * s), Amber, 1.5f); GUI.matrix = m;
                }
                else if (t != target) BoxPx(new Rect(p.x - 2f, p.y - 2f, 4f, 4f), new Color(Threat.r, Threat.g, Threat.b, 0.7f));
            }
            if (target == null) return;
            if (ToScreen(target.Position, out Vector2 tp))
            {
                float d = Vector3.Distance(cam.transform.position, target.Position);
                float size = Mathf.Clamp(target.radius * 2.4f / (d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Screen.height * 0.5f, 26f * s, 260f * s);
                var r = new Rect(tp.x - size / 2f, tp.y - size / 2f, size, size);
                float c = size * 0.28f, th = 2f;
                foreach (var corner in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax - c, r.yMin), new Vector2(r.xMin, r.yMax - th), new Vector2(r.xMax - c, r.yMax - th) })
                    BoxPx(new Rect(corner.x, corner.y, c, th), Threat);
                foreach (var corner in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax - th, r.yMin), new Vector2(r.xMin, r.yMax - c), new Vector2(r.xMax - th, r.yMax - c) })
                    BoxPx(new Rect(corner.x, corner.y, th, c), Threat);
                Text(tp.x / s + size / s / 2f + 6f, tp.y / s - 10f, 160f, 20f, N(Vector3.Distance(player.Center, target.Position)) + " m", Threat, 13);
            }
            // bandeau de la cible verrouillee (haut centre)
            float x = W / 2f - 220f, y = 70f;
            string extra = target.Overloaded ? " · surcharge" : "";
            Text(x, y, 440f, 24f, target.displayName + extra, Threat, 17, TextAnchor.MiddleCenter, true);
            Bar(x, y + 28f, 440f, 6f, target.HP / target.maxHP, new Color(1f, 0.3f, 0.45f), new Color(0, 0, 0, 0.5f));
            if (target.impactMax > 0f)
            {
                bool blink = target.Overloaded && Mathf.Repeat(Time.time, 0.3f) < 0.15f;
                Bar(x, y + 37f, 440f, 3f, target.impact / target.impactMax, target.Overloaded ? (blink ? Threat : Amber) : Amber, new Color(0, 0, 0, 0.5f));
            }
            Text(x, y + 42f, 440f, 20f, N(Vector3.Distance(player.Center, target.Position)) + " m", new Color(Bone.r, Bone.g, Bone.b, 0.6f), 12, TextAnchor.MiddleCenter);
        }

        void ArmorEnergy(float W)
        {
            float cx = W / 2f, y = 930f;
            float apMax = player.S.armor;
            bool crit = player.AP < apMax * player.frame.criticalArmor;
            Text(cx - 250f, y - 52f, 500f, 50f, N(player.AP), crit ? Threat : Bone, 40, TextAnchor.LowerCenter, true);
            Box(cx - 250f, y, 500f, 7f, new Color(0, 0, 0, 0.45f));
            Box(cx - 250f, y, 500f * Mathf.Clamp01(player.apTrail / apMax), 7f, new Color(1f, 1f, 1f, 0.45f));
            Box(cx - 250f, y, 500f * Mathf.Clamp01(player.AP / apMax), 7f, crit ? Threat : Bone);
            bool blink = player.Overheated && Mathf.Repeat(Time.time, 0.4f) < 0.2f;
            Bar(cx - 250f, y + 14f, 500f, 5f, player.EN / player.S.energy, player.Overheated ? (blink ? Threat : new Color(0.6f, 0.1f, 0.1f)) : Amber, new Color(0, 0, 0, 0.45f));
            string st2 = player.EnergyState;
            Text(cx - 250f, y + 22f, 250f, 22f, "EN " + N(player.EN) + (string.IsNullOrEmpty(st2) ? "" : " · " + st2), player.Overheated ? Threat : Amber, 13);
            string form = player.FormLabel;
            if (player.progress.reincarnation > 0) form += " · Réincarnation " + player.progress.reincarnation;
            Text(cx, y + 22f, 250f, 22f, form, player.IsShip ? Ice : Bone, 13, TextAnchor.UpperRight);
            Text(cx - 250f, y + 42f, 500f, 20f, $"Kits de réparation : {player.kits}", new Color(Bone.r, Bone.g, Bone.b, 0.55f), 12, TextAnchor.UpperCenter);
        }

        void WeaponsPanel(float W)
        {
            if (weapons == null) return;
            float x = W - 330f, y = 800f;
            var S = player.S;
            Line(ref y, x, "CLIC G", "Fusil à impulsions", "∞", weapons.Cooldown01("rifle"), Rose, true);
            Line(ref y, x, "CLIC D", "Missiles", "×" + S.missileCount, weapons.Cooldown01("missile"), Rose, true);
            Line(ref y, x, "C", "Salve de missiles", weapons.SalvoLeft > 0 ? $"{S.salvoCount - weapons.SalvoLeft}/{S.salvoCount}" : "×" + S.salvoCount, weapons.Cooldown01("salvo"), Rose, true);
            Line(ref y, x, "V", "Fragmentation", "×" + S.fragMinis, weapons.Cooldown01("frag"), Rose, true);
            Line(ref y, x, "F", "Lame à plasma", N(S.bladeRange) + " m", weapons.Cooldown01("blade"), Rose, true);
            Line(ref y, x, "H", "Kit de réparation", "×" + player.kits, 0f, Bone, player.kits > 0);
            var pr = player.progress;
            Line(ref y, x, "G", "Rayon Arcane", pr.arcane ? "PRÊT" : "1 M C", 0f, Rose, pr.arcane);
            Line(ref y, x, "B", "Extension du territoire", pr.domain ? "PRÊT" : "500 k C", 0f, Violet, pr.domain);
            int n = probes != null ? probes.Owned : 0;
            bool blink = probes != null && probes.Fighting && Mathf.Repeat(Time.time, 0.5f) < 0.25f;
            Line(ref y, x, "AUTO", "Sondes laser", $"{n}/5", 0f, blink ? Color.white : ProbeY, n > 0);
        }
        void Line(ref float y, float x, string key, string name, string count, float cd, Color c, bool on)
        {
            Color col = on ? c : new Color(c.r, c.g, c.b, 0.3f);
            Text(x, y, 60f, 18f, key, new Color(Bone.r, Bone.g, Bone.b, on ? 0.6f : 0.25f), 11);
            Text(x + 62f, y, 190f, 18f, name, on ? Bone : new Color(Bone.r, Bone.g, Bone.b, 0.3f), 13);
            Text(x + 250f, y, 70f, 18f, count, col, 13, TextAnchor.UpperRight);
            Box(x + 62f, y + 18f, 258f, 1.5f, new Color(col.r, col.g, col.b, 0.18f));
            if (cd > 0f) Box(x + 62f, y + 18f, 258f * (1f - cd), 1.5f, col);
            y += 25f;
        }

        void TopLeft()
        {
            Text(40f, 34f, 600f, 22f, "TERRAIN D'ESSAI · JALON 4", Amber, 14, TextAnchor.UpperLeft, true);
            int alive = 0, total = 0; foreach (var t in Targetable.All) { total++; if (t.Alive) alive++; }
            Text(40f, 56f, 600f, 20f, $"Détruisez les cibles d'entraînement · actives : {alive} / {total}", new Color(Bone.r, Bone.g, Bone.b, 0.7f), 13);
            Box(40f, 78f, 300f, 1.5f, Amber);
            float y = 92f, now = Time.unscaledTime;
            foreach (var m in Msg.Feed)
            {
                float age = now - m.time; if (age > Msg.Duration) continue;
                float a = Mathf.Clamp01((Msg.Duration - age) / 0.5f) * Mathf.Clamp01(age / 0.15f);
                Color c = m.kind == MsgKind.Danger ? Threat : m.kind == MsgKind.Warning ? Amber : m.kind == MsgKind.Good ? Ice : Bone;
                c.a = a;
                Text(40f - (1f - Mathf.Clamp01(age / 0.15f)) * 30f, y, 900f, 22f, m.text, c, 15);
                y += 24f;
            }
        }

        void TopRight(float W)
        {
            long cr = player.progress.credits;
            shownCredits = Mathf.Lerp(shownCredits, cr, MathX.Damp(8f, Time.unscaledDeltaTime));
            if (Mathf.Abs(shownCredits - cr) < 1f) shownCredits = cr;
            Text(W - 440f, 30f, 400f, 30f, MathX.Credits((long)Mathf.Round(shownCredits)), Bone, 22, TextAnchor.UpperRight, true);
            float age = Time.unscaledTime - HudCredits.LastTime;
            if (age < 1.25f) Text(W - 440f, 58f, 400f, 20f, "+" + MathX.Credits(HudCredits.LastGain), new Color(Ice.r, Ice.g, Ice.b, 1f - age / 1.25f), 14, TextAnchor.UpperRight);
            float gh = TerrainSystem.Ground(player.Center.x, player.Center.z);
            Text(W - 640f, 80f, 600f, 20f, world != null ? world.SectorName(player.Center, gh) : "", new Color(Bone.r, Bone.g, Bone.b, 0.7f), 13, TextAnchor.UpperRight);
            if (player.progress.divine) Text(W - 440f, 100f, 400f, 20f, "Mode DIVIN", Gold, 13, TextAnchor.UpperRight, true);
        }

        void BigMessage(float W)
        {
            float age = Time.unscaledTime - Msg.BigTime;
            if (age > Msg.BigDuration || string.IsNullOrEmpty(Msg.BigTitle)) return;
            float a = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((Msg.BigDuration - age) / 0.6f);
            string spaced = string.Join(" ", Msg.BigTitle.ToUpperInvariant().ToCharArray());
            Text(0f, 280f, W, 70f, spaced, new Color(Bone.r, Bone.g, Bone.b, a), 52, TextAnchor.MiddleCenter, true);
            Text(0f, 350f, W, 30f, Msg.BigSub, new Color(Amber.r, Amber.g, Amber.b, a), 18, TextAnchor.MiddleCenter);
        }

        void DamageNums()
        {
            float now = Time.time;
            foreach (var d in DamageNumbers.List)
            {
                float age = now - d.time; if (age > DamageNumbers.Life) continue;
                if (!ToScreen(d.pos + Vector3.up * age * 6f, out Vector2 p)) continue;
                Color c = d.overload ? Amber : Bone; c.a = 1f - age / DamageNumbers.Life;
                Text(p.x / s - 60f, p.y / s - 12f, 120f, 24f, N(d.value), c, d.overload ? 17 : 14, TextAnchor.MiddleCenter, d.overload);
            }
        }

        void Vignette()
        {
            float hitA = 0f;
            foreach (var h in player.recentHits) hitA = Mathf.Max(hitA, 1f - (Time.time - h.Key) / 0.5f);
            bool crit = player.AP < player.S.armor * player.frame.criticalArmor && player.AP > 0f;
            if (crit) hitA = Mathf.Max(hitA, 0.25f + 0.2f * Mathf.Sin(Time.time * 6f));
            if (hitA <= 0f) return;
            Color c = new Color(1f, 0.1f, 0.15f, hitA * 0.35f);
            float t = 40f * s;
            BoxPx(new Rect(0, 0, Screen.width, t), c); BoxPx(new Rect(0, Screen.height - t, Screen.width, t), c);
            BoxPx(new Rect(0, 0, t, Screen.height), c); BoxPx(new Rect(Screen.width - t, 0, t, Screen.height), c);
            // indicateurs de direction des coups recus
            Vector3 f = cam.transform.forward; f.y = 0f;
            foreach (var h in player.recentHits)
            {
                float a = 1f - (Time.time - h.Key) / 1.5f; if (a <= 0f) continue;
                Vector3 from = -h.Value; from.y = 0f;
                float ang = Vector3.SignedAngle(f, from, Vector3.up);
                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, new Vector2(Screen.width / 2f, Screen.height / 2f));
                BoxPx(new Rect(Screen.width / 2f - 40f * s, Screen.height / 2f - 150f * s, 80f * s, 4f * s), new Color(1f, 0.2f, 0.25f, a));
                GUI.matrix = m;
            }
        }

        void BladeArc(float W)
        {
            float k = (Time.time - weapons.BladeArcTime) / 0.3f;
            Color c = new Color(Rose.r, Rose.g, Rose.b, 1f - k);
            for (int i = 0; i < 24; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / 23f) * Mathf.Deg2Rad;
                float x = W / 2f + Mathf.Sin(a) * 300f, y = 540f - Mathf.Cos(a) * 120f + 60f;
                Box(x - 6f, y - 2f, 12f, 4f, c);
            }
        }

        // ------------------------------------------------------------------ menus
        void PauseMenu(float W, GameManager gm)
        {
            Box(0, 0, W, 1080f, new Color(0.04f, 0.02f, 0.06f, 0.78f));
            Text(80f, 80f, 400f, 40f, "PAUSE", Bone, 34, TextAnchor.UpperLeft, true);
            Text(80f, 126f, 400f, 24f, MathX.Credits(player.progress.credits), Amber, 18);
            if (Button(80f, 180f, 260f, "Reprendre")) gm.SetState(GameState.Play);
            if (Button(80f, 230f, 260f, "Améliorations")) gm.tab = GameManager.PauseTab.Upgrades;
            if (Button(80f, 280f, 260f, "Commandes")) gm.tab = GameManager.PauseTab.Commands;
            if (gm.tab == GameManager.PauseTab.Upgrades) Upgrades(W, gm); else Commands(W);
        }
        bool Button(float x, float y, float w, string label)
        {
            st.fontSize = Mathf.RoundToInt(16 * s);
            var bs = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(16 * s), alignment = TextAnchor.MiddleLeft };
            return GUI.Button(new Rect(x * s, y * s, w * s, 40f * s), "  " + label, bs);
        }
        static readonly string[] ModNames = { "Propulseurs", "Réservoir d'EN", "Générateur", "Quick boost et saut", "Blindage", "Fusil à impulsions", "Lance-missiles", "Salve de missiles (C)", "Fragmentation (V)", "Lame à plasma", "Forme vaisseau" };
        static readonly string[] ModDesc = { "Vitesses +12 % par niveau (montée +8 %)", "EN maximale +25 %", "Recharge +22 %, surchauffe −12 %", "Quick boost +12 %, saut +8 %, coût −9 %", "AP maximale +16 %",
            "Dégâts +16 %, délai entre tirs −5 %", "+1 missile, dégâts des missiles +12 %", "+1 missile dans la salve, dégâts +6 %", "+1 mini-missile, dégâts +12 %", "Dégâts +18 %, portée de charge +8 %", "Vitesse max +10 %, glissement +5 %" };
        void Upgrades(float W, GameManager gm)
        {
            float x = 420f, y = 80f;
            Text(x, y, 900f, 34f, "Améliorations", Bone, 28, TextAnchor.UpperLeft, true);
            Text(x, y + 40f, 1100f, 22f, "Les crédits viennent des ennemis détruits, des avant-postes neutralisés, des contrats et des boss. Chaque module a cinq niveaux ; les effets s'appliquent immédiatement.", new Color(Bone.r, Bone.g, Bone.b, 0.7f), 13);
            y += 80f;
            var pr = player.progress; var pd = player.progression;
            for (int i = 0; i < ModNames.Length; i++)
            {
                int lvl = pr.levels[i];
                Text(x, y, 300f, 20f, ModNames[i], Bone, 15, TextAnchor.UpperLeft, true);
                Text(x, y + 20f, 420f, 18f, ModDesc[i], new Color(Bone.r, Bone.g, Bone.b, 0.6f), 12);
                for (int k = 0; k < pd.maxLevel; k++) Box(x + 440f + k * 26f, y + 8f, 22f, 12f, k < lvl ? Amber : new Color(1, 1, 1, 0.12f));
                string label = lvl >= pd.maxLevel ? "Maximum" : MathX.Credits(pd.levelCosts[lvl]);
                GUI.enabled = lvl < pd.maxLevel && pr.credits >= (lvl < pd.maxLevel ? pd.levelCosts[lvl] : long.MaxValue);
                if (Button(x + 600f, y, 200f, label)) gm.Buy((UpgradeModule)i);
                GUI.enabled = true;
                y += 46f;
            }
            Box(x, y + 4f, 3f, 40f, ProbeY);
            Text(x + 12f, y + 4f, 420f, 20f, $"Sonde laser ({pr.probes} / {player.weaponData.probeMax})", Bone, 15, TextAnchor.UpperLeft, true);
            Text(x + 12f, y + 24f, 500f, 18f, "Un drone du dos se détache et mitraille la cible verrouillée", new Color(Bone.r, Bone.g, Bone.b, 0.6f), 12);
            GUI.enabled = pr.probes < player.weaponData.probeMax && pr.credits >= player.weaponData.probeCost;
            if (Button(x + 600f, y + 4f, 200f, pr.probes >= player.weaponData.probeMax ? "Maximum" : MathX.Credits(player.weaponData.probeCost))) gm.BuyProbe();
            GUI.enabled = true;
            Text(x, y + 60f, 900f, 20f, "Rayon Arcane, Extension du territoire et Réincarnation : jalon 8.  Touches de test : F1 +1 M C · F2 sonde · F3 tout +1 niveau · F4 plein", new Color(Bone.r, Bone.g, Bone.b, 0.45f), 12);
        }
        static readonly string[][] Cmds =
        {
            new[] { "CHÂSSIS", "Z Q S D : se déplacer", "Souris / flèches : viser", "Espace : saut boost (maintenir : monter)", "Ctrl : saut boost vers le bas (maintenir : descendre)", "Maj : boost (glisse au sol, plane en l'air)", "E : quick boost", "R : assault boost (R ou S pour arrêter)" },
            new[] { "VAISSEAU", "T : changer de forme", "Souris : rotation libre (looping)", "Z / S : avancer / reculer", "Q / D : glisser sur le côté", "Espace / Ctrl : monter / descendre", "Maj : postcombustion", "E : tonneau d'esquive" },
            new[] { "COMBAT", "Clic gauche : fusil à impulsions", "Clic droit : missiles multi-cibles", "C : salve de missiles", "V : missile à fragmentation", "F : lame à plasma", "X : changer de cible", "H : kit de réparation", "G : Rayon Arcane · B : Extension du territoire" },
            new[] { "INTERFACE", "Échap ou P : menu et pause", "U : améliorations", "K : commandes", "M : zoom de la mini-carte", "N : couper ou rétablir le son" },
        };
        void Commands(float W)
        {
            float x = 420f, y = 80f;
            Text(x, y, 900f, 34f, "Commandes", Bone, 28, TextAnchor.UpperLeft, true);
            y += 60f;
            for (int g = 0; g < Cmds.Length; g++)
            {
                float gx = x + (g % 2) * 520f, gy = y + (g / 2) * 260f;
                Text(gx, gy, 500f, 22f, Cmds[g][0], Amber, 15, TextAnchor.UpperLeft, true);
                for (int i = 1; i < Cmds[g].Length; i++) Text(gx, gy + 6f + i * 24f, 500f, 22f, Cmds[g][i], Bone, 14);
            }
        }
        void DeadScreen(float W)
        {
            Box(0, 0, W, 1080f, new Color(0.05f, 0.01f, 0.02f, 0.82f));
            Text(0f, 340f, W, 24f, "SIGNAL PERDU", Threat, 16, TextAnchor.MiddleCenter, true);
            Text(0f, 370f, W, 80f, "Châssis détruit", Bone, 60, TextAnchor.MiddleCenter, true);
            Text(0f, 460f, W, 26f, "Le Vesper est récupéré et réparé sur la plateforme Hélios. Crédits, améliorations et progression du contrat sont conservés.", new Color(Bone.r, Bone.g, Bone.b, 0.75f), 15, TextAnchor.MiddleCenter);
            if (Button(W / 2f - 120f, 530f, 240f, "Redéployer")) GameManager.I.Redeploy();
        }
    }
}
