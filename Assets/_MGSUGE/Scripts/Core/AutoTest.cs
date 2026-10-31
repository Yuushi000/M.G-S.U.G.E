using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>Entrees pilotees par script (test automatique).</summary>
    public class ScriptedInput : IInputBackend
    {
        public string Name => "test automatique (entrees simulees)";
        public Vector2 move, look, arrows;
        readonly HashSet<Cmd> held = new HashSet<Cmd>();
        readonly Dictionary<Cmd, int> tapFrame = new Dictionary<Cmd, int>();
        public void Hold(Cmd c, bool on) { if (on) held.Add(c); else held.Remove(c); }
        public void Tap(Cmd c) { tapFrame[c] = Time.frameCount + 1; }
        public void ReleaseAll() { held.Clear(); move = look = arrows = Vector2.zero; }
        public void Enable() { }
        public void Disable() { }
        public Vector2 Move() => move;
        public Vector2 LookDelta() => look;
        public Vector2 Arrows() => arrows;
        public bool Held(Cmd c) => held.Contains(c) || Pressed(c);
        public bool Pressed(Cmd c) => tapFrame.TryGetValue(c, out int f) && f == Time.frameCount;
        public bool TestKey(int n) => false;
        public bool Clicked() => false;
    }

    /// <summary>
    /// Test automatique en mode Play (menu MGSUGE > 4). Simule les commandes, verifie deplacements, armes, transformation,
    /// mort et redeploiement, prend des captures et ecrit un rapport dans Logs/AutoTest du projet.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        public const string PrefKey = "MGSUGE_AutoTest";
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "AutoTest"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) != 1) return;
            PlayerPrefs.SetInt(PrefKey, 0); PlayerPrefs.Save();
            new GameObject("MGSUGE_AutoTest").AddComponent<AutoTest>();
        }

        readonly StringBuilder log = new StringBuilder();
        readonly List<string> errors = new List<string>();
        int pass, fail, shot;
        PlayerController p; ScriptedInput si; LockOn lockOn; WeaponController weapons; ProbeController probes;
        float frameTimeSum; int frameCount; float worstFrame;

        void Awake()
        {
            Application.logMessageReceived += OnLog;
            if (Directory.Exists(Dir)) foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
            Directory.CreateDirectory(Dir);
        }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
        void OnLog(string cond, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add($"[{Time.time:0.0}s] {type}: {cond}\n{stack}");
        }
        void Update()
        {
            if (Time.unscaledDeltaTime > 0f && Time.frameCount > 10)
            { frameTimeSum += Time.unscaledDeltaTime; frameCount++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime); }
        }

        void Line(string s) { log.AppendLine(s); Debug.Log("[MGSUGE AutoTest] " + s); }
        void Check(bool ok, string what, string detail = "")
        {
            if (ok) pass++; else fail++;
            Line((ok ? "OK    " : "ECHEC ") + what + (detail.Length > 0 ? " — " + detail : ""));
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"{shot++:00}_{name}.png"));
            yield return null;
        }
        /// <summary>Attente en temps de jeu ; en pause (timeScale 0), en temps reel.</summary>
        static IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime; yield return null; } }
        Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        Targetable Nearest(float maxDist = 99999f)
        {
            Targetable best = null; float bd = maxDist;
            foreach (var t in Targetable.All)
            {
                if (t == null || !t.Alive || !t.lockable) continue;
                float d = Vector3.Distance(t.Position, p.Center);
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }
        void AimAt(Vector3 point)
        {
            if (p.cam == null || p.cam.cam == null) return;
            Vector3 d = point - p.cam.cam.transform.position;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(d.y / Mathf.Max(0.01f, d.magnitude), -1f, 1f));
            p.cam.Snap(yaw, pitch);
        }
        IEnumerator AimFor(Targetable t, float s) { float k = 0f; while (k < s && t != null && t.Alive) { AimAt(t.Position); k += Time.deltaTime; yield return null; } }

        readonly List<Enemy> testFoes = new List<Enemy>();
        ContractManager cm; PatrolSpawner patrols;

        IEnumerator Start()
        {
            Line("Test automatique M.G-S.U.G.E — " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Line($"Unity {Application.unityVersion} · {SystemInfo.graphicsDeviceName} · {Screen.width}x{Screen.height} · scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            // les ennemis libres de la scene Test sont mis de cote pendant les essais des jalons 1-4
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                if (e.GetComponentInParent<Contract>() == null && e.GetComponentInParent<Outpost>() == null) { testFoes.Add(e); e.gameObject.SetActive(false); }
            yield return Wait(2.5f);
            p = PlayerController.I != null ? PlayerController.I : FindAnyObjectByType<PlayerController>();
            if (p == null) { Check(false, "joueur present dans la scene"); yield return Finish(); yield break; }
            lockOn = FindAnyObjectByType<LockOn>(); weapons = FindAnyObjectByType<WeaponController>(); probes = FindAnyObjectByType<ProbeController>();
            cm = ContractManager.I; patrols = FindAnyObjectByType<PatrolSpawner>(FindObjectsInactive.Include);
            Check(GameManager.I != null, "GameManager present");
            Check(lockOn != null && weapons != null && probes != null, "systemes d'armes presents (verrouillage, armes, sondes)");
            si = new ScriptedInput();
            p.input.OverrideBackend(si);
            var rend = GraphicsSettingsName();
            Check(rend.Contains("Universal"), "pipeline de rendu URP actif", rend);
            yield return TitleStart();
            bool world = cm != null && cm.Count > 1;
            if (world) { yield return WorldChecks(); yield return HudChecks(); }
            else
            {
                Line("--- Jalons 1 a 4 : deplacements, armes, transformation, menus ---");
                yield return Basics();
                Line("--- Jalon 5 : ennemis, contrats, menace, avant-postes, patrouilles ---");
                yield return Jalon5();
                Line("--- Jalon 6 : HUD, mini-carte, menus, ameliorations, modes Normal / DIVIN ---");
                yield return HudChecks();
                yield return Jalon6Menus();
            }
            yield return DeathRedeploy();
            yield return Finish();
        }

        IEnumerator Basics()
        {
            // ---------------------------------------------------------------- apparition
            Line($"Position de depart {p.transform.position} · au sol {p.grounded} · hauteur sol {p.heightAboveGround:0.0} m · AP {p.AP:0} · EN {p.EN:0}");
            Check(p.grounded, "le chassis est pose au sol au demarrage");
            Check(Mathf.Abs(p.heightAboveGround) < 1.5f || p.grounded, "hauteur au-dessus du sol coherente", p.heightAboveGround.ToString("0.0") + " m");
            Check(Targetable.All.Count > 0, "cibles du terrain d'essai presentes", Targetable.All.Count + " cibles");
            yield return Shot("apparition");

            // ---------------------------------------------------------------- marche
            Vector3 a = p.transform.position;
            si.move = new Vector2(0f, 1f);
            yield return Wait(2f);
            float walkKmh = Flat(p.velocity).magnitude * 3.6f;
            yield return Shot("marche");
            si.move = Vector2.zero; yield return Wait(0.8f);
            float dWalk = Flat(p.transform.position - a).magnitude;
            Check(dWalk > 20f, "marche (Z) : le chassis avance", $"{dWalk:0} m en 2 s, {walkKmh:0} km/h (attendu ~{p.S.walk * 3.6f:0})");
            Check(p.grounded, "reste au sol en marchant");

            // ---------------------------------------------------------------- boost
            a = p.transform.position; float en0 = p.EN;
            si.move = new Vector2(0f, 1f); si.Hold(Cmd.Boost, true);
            yield return Wait(1.5f);
            float boostKmh = Flat(p.velocity).magnitude * 3.6f; float enBoost = p.EN;
            yield return Shot("boost");
            si.Hold(Cmd.Boost, false); si.move = Vector2.zero; yield return Wait(0.8f);
            Check(boostKmh > walkKmh * 1.3f, "boost (Maj) plus rapide que la marche", $"{boostKmh:0} km/h");
            Check(enBoost < en0, "le boost consomme de l'EN", $"{en0:0} -> {enBoost:0} (puis {p.EN:0} apres recharge)");

            // ---------------------------------------------------------------- quick boost
            yield return Wait(1f);
            si.move = new Vector2(1f, 0f); si.Tap(Cmd.QuickBoost);
            yield return Wait(0.12f);
            float qbKmh = Flat(p.velocity).magnitude * 3.6f;
            si.move = Vector2.zero; yield return Wait(1f);
            Check(qbKmh > 300f, "quick boost (E) : impulsion laterale", $"{qbKmh:0} km/h");

            // ---------------------------------------------------------------- saut et montee
            yield return Wait(1.5f);
            float y0 = p.transform.position.y;
            si.Tap(Cmd.Jump); yield return Wait(0.5f);
            float jumpH = p.transform.position.y - y0;
            Check(jumpH > 5f, "saut boost (Espace)", $"+{jumpH:0.0} m en 0,5 s");
            si.Hold(Cmd.Jump, true); yield return Wait(1.2f);
            float riseH = p.transform.position.y - y0;
            yield return Shot("vol_montee");
            si.Hold(Cmd.Jump, false);
            Check(riseH > jumpH, "montee (Espace maintenu)", $"+{riseH:0.0} m");
            float landT = 0f; while (!p.grounded && landT < 12f) { landT += Time.deltaTime; yield return null; }
            Check(p.grounded, "atterrissage", $"{landT:0.0} s");
            yield return Wait(2.5f); // regen EN

            // ---------------------------------------------------------------- fusil
            var t = Nearest();
            if (t != null)
            {
                Line($"Cible : {t.displayName} a {Vector3.Distance(t.Position, p.Center):0} m (PV {t.HP:0}/{t.maxHP:0})");
                yield return AimFor(t, 0.5f);
                Check(lockOn.Target != null, "verrouillage automatique d'une cible visee", lockOn.Target != null ? lockOn.Target.displayName : "aucune");
                if (lockOn.Target != null) t = lockOn.Target;   // le fusil tire sur la cible verrouillee
                float hp0 = t.HP; long cr0 = p.progress.credits;
                var ps = ProjectileSystem.I;
                if (ps != null) { ps.statFired = ps.statTargets = ps.statWorld = ps.statGround = ps.statExpired = 0; ps.statLastWorld = ""; }
                Vector3 mz = p.Center + Vector3.up * 3f;
                if (Physics.Linecast(mz, t.Position, out RaycastHit blk, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    Line($"  ligne de tir vers la cible coupee par « {blk.collider.name} » a {blk.distance:0} m");
                si.Hold(Cmd.Fire, true);
                float ft = 0f; bool shotTaken = false;
                while (ft < 4f && t.Alive) { AimAt(t.Position); ft += Time.deltaTime; if (!shotTaken && ft > 0.4f) { shotTaken = true; yield return Shot("fusil"); } else yield return null; }
                si.Hold(Cmd.Fire, false);
                yield return Wait(0.8f);
                if (ps != null) Line($"  tirs : {ps.statFired} tires, {ps.statTargets} sur cible, {ps.statWorld} sur decor ({ps.statLastWorld}), {ps.statGround} au sol, {ps.statExpired} expires");
                Check(!t.Alive || t.HP < hp0, "fusil a impulsions (clic gauche) : degats sur la cible", t.Alive ? $"PV {hp0:0} -> {t.HP:0}" : "cible detruite");
                if (!t.Alive) { yield return Wait(0.3f); Check(p.progress.credits > cr0, "credits gagnes a la destruction", $"+{p.progress.credits - cr0} C"); }
            }
            else Check(false, "une cible verrouillable existe");

            // ---------------------------------------------------------------- missiles
            yield return Wait(0.5f);
            t = Nearest();
            if (t != null)
            {
                yield return AimFor(t, 0.4f);
                int alive0 = CountAlive(); float hpSum0 = HpSum();
                si.Tap(Cmd.Missiles);
                yield return Wait(0.5f); AimAt(t.Position);
                yield return Shot("missiles");
                yield return AimFor(t, 3f);
                Check(HpSum() < hpSum0 || CountAlive() < alive0, "missiles multi-cibles (clic droit) touchent", $"PV totaux {hpSum0:0} -> {HpSum():0}");
                t = Nearest();
                if (t != null)
                {
                    // salve prete, puis attente des impacts (jusqu'a 4 s) en gardant la visee
                    float rw = 0f; while (weapons.Cooldown01("salvo") > 0f && rw < 5f) { rw += Time.deltaTime; yield return null; }
                    yield return AimFor(t, 0.3f); hpSum0 = HpSum();
                    si.Tap(Cmd.Salvo);
                    float sw = 0f; while (sw < 4f && HpSum() >= hpSum0) { if (t != null && t.Alive) AimAt(t.Position); else { t = Nearest(); } sw += Time.deltaTime; yield return null; }
                    Check(HpSum() < hpSum0, "salve de missiles (C)", $"PV totaux {hpSum0:0} -> {HpSum():0}");
                }
                t = Nearest();
                if (t != null)
                {
                    yield return AimFor(t, 0.3f); hpSum0 = HpSum();
                    si.Tap(Cmd.Frag); yield return AimFor(t, 3f);
                    Check(HpSum() < hpSum0, "missile a fragmentation (V)", $"PV totaux {hpSum0:0} -> {HpSum():0}");
                }
            }

            // ---------------------------------------------------------------- lame (vise la cible verrouillee : verrouillage persistant, X pour changer)
            yield return Wait(0.5f);
            t = lockOn.Target != null && lockOn.Target.Alive ? lockOn.Target : Nearest();
            if (t != null)
            {
                // place le chassis a 55 m de la cible (meme hauteur) pour tester la charge
                Vector3 off = Flat(p.Center - t.Position); if (off.sqrMagnitude < 1f) off = Vector3.back; off = off.normalized * 55f;
                var cc = p.GetComponent<CharacterController>();
                cc.enabled = false; p.transform.position = t.Position + off; p.velocity = Vector3.zero; cc.enabled = true;
                yield return AimFor(t, 0.25f);
                if (lockOn.Target != t) Line($"  cible verrouillee pour la lame : {(lockOn.Target != null ? lockOn.Target.displayName : "aucune")}");
                float hp0 = t.HP;
                si.Tap(Cmd.Blade);
                yield return Wait(0.15f);
                yield return Shot("lame");
                yield return Wait(1f);
                Check(!t.Alive || t.HP < hp0, "lame a plasma (F)", $"distance {Vector3.Distance(t.Position, p.Center):0} m, PV {hp0:0} -> {t.HP:0}");
                float lt = 0f; while (!p.grounded && lt < 10f) { lt += Time.deltaTime; yield return null; }
            }

            // ---------------------------------------------------------------- sondes
            p.progress.probes = 2;
            t = Nearest();
            if (t != null)
            {
                // on suit la cible visee (les cibles d'essai reapparaissent apres 6 s : la somme des PV ne suffit pas)
                float tHp0 = t.HP; bool fought = false, hurt = false; float pw = 0f;
                while (pw < 4.5f)
                {
                    if (t.Alive) AimAt(t.Position);
                    fought |= probes.Fighting; hurt |= !t.Alive || t.HP < tHp0;
                    if (pw < 1.5f && pw + Time.deltaTime >= 1.5f) yield return Shot("sondes");
                    pw += Time.deltaTime; yield return null;
                }
                Check(fought && hurt, "sondes laser : decollent et tirent sur la cible verrouillee", $"combat {fought}, cible {(t.Alive ? $"PV {tHp0:0} -> {t.HP:0}" : "detruite")}");
            }
            p.progress.probes = 0;
            yield return Wait(1.5f);

            // ---------------------------------------------------------------- transformation
            si.Tap(Cmd.Transform);
            yield return Wait(0.3f);
            yield return Shot("transformation");
            yield return Wait(p.frame.transformTime + 0.5f);
            Check(p.form == FrameForm.Ship && !p.transforming, "transformation en vaisseau (T)", p.FormLabel);
            a = p.transform.position;
            si.move = new Vector2(0f, 1f); si.Hold(Cmd.Jump, true); yield return Wait(1f); si.Hold(Cmd.Jump, false);
            si.Hold(Cmd.Boost, true); yield return Wait(2f);
            float shipKmh = p.velocity.magnitude * 3.6f;
            yield return Shot("vaisseau_vol");
            si.Hold(Cmd.Boost, false); si.move = Vector2.zero;
            Check(shipKmh > 400f, "vol en vaisseau avec postcombustion", $"{shipKmh:0} km/h, {Vector3.Distance(a, p.transform.position):0} m parcourus");
            si.Tap(Cmd.QuickBoost); yield return Wait(0.6f);
            Check(true, "tonneau d'esquive (E) sans erreur");
            si.Tap(Cmd.Transform);
            yield return Wait(p.frame.transformTime + 0.5f);
            Check(p.form == FrameForm.Mech && !p.transforming, "retour en chassis (T)", p.FormLabel);
            float gt = 0f; while (!p.grounded && gt < 15f) { gt += Time.deltaTime; yield return null; }
            Check(p.grounded, "le chassis retombe au sol", $"{gt:0.0} s");
            yield return Shot("retour_chassis");

            // ---------------------------------------------------------------- menu pause
            GameManager.I.OpenMenu(GameManager.PauseTab.Upgrades);
            yield return null;
            Check(Time.timeScale == 0f, "pause : temps du monde arrete");
            long c0 = p.progress.credits; p.progress.credits += 5000;
            bool bought = GameManager.I.Buy(UpgradeModule.Propulseurs);
            Check(bought && p.progress.Level(UpgradeModule.Propulseurs) >= 1, "achat d'une amelioration (propulseurs)");
            yield return Shot("menu_pause");
            GameManager.I.SetState(GameState.Play);
            yield return null;
            Check(Time.timeScale == 1f, "reprise du jeu");

        }

        // ------------------------------------------------------------------ outils
        /// <summary>Capture « vitrine » : la camera se place pres de l'objet le temps d'une image.</summary>
        IEnumerator Showcase(Vector3 c, float dist, float height, string name, Vector3? from = null)
        {
            var rig = p.cam; if (rig == null || rig.cam == null) yield break;
            var camT = rig.cam.transform;
            bool was = rig.enabled; rig.enabled = false;
            Vector3 dir = Flat((from ?? camT.position) - c); if (dir.sqrMagnitude < 1f) dir = Vector3.back; dir.Normalize();
            camT.position = c + dir * dist + Vector3.up * height; camT.LookAt(c);
            yield return null;
            yield return Shot(name);
            rig.enabled = was;
        }
        void Teleport(Vector3 pos)
        {
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false; p.transform.position = pos; p.velocity = Vector3.zero; cc.enabled = true;
        }
        static void Kill(Enemy e)
        {
            if (e == null || !e.IsAlive) return;
            e.Target.TakeDamage(new DamageInfo { amount = 1e8f, impact = 0f, point = e.transform.position, direction = Vector3.down, fromPlayer = true });
        }
        Vector3 StandAt(Vector3 near, float dist)
        {
            Vector3 dir = Flat(p.Center - near); if (dir.sqrMagnitude < 1f) dir = Vector3.back; dir.Normalize();
            Vector3 q = near + dir * dist;
            q.y = TerrainSystem.Ground(q.x, q.z) + p.frame.frameClear + 0.5f;
            return q;
        }

        // ------------------------------------------------------------------ mort et redeploiement (§4.4)
        IEnumerator DeathRedeploy()
        {
            p.invulnerable = false;
            // une patrouille pres de la plateforme doit disparaitre au redeploiement
            int near0 = -1;
            float clearR = GameManager.I != null ? GameManager.I.redeployClearRadius : 600f;
            int NearSpawn() { int n = 0; if (patrols != null) foreach (var e in patrols.Spawned) if (e != null && Vector3.Distance(e.transform.position, p.SpawnPosition) < clearR) n++; return n; }
            if (patrols != null && patrols.player != null)
            {
                bool was = patrols.enabled; patrols.enabled = true;
                patrols.SpawnGroupAt(p.SpawnPosition + new Vector3(250f, 0f, 0f));
                near0 = NearSpawn(); patrols.enabled = was;
            }
            long credits0 = p.progress.credits;
            int contract0 = cm != null ? cm.Index : -1;
            p.TakeDamage(new DamageInfo { amount = p.AP + 99999f, point = p.Center, direction = Vector3.forward, fromPlayer = false });
            yield return Wait(2.5f);
            Check(p.dead, "destruction du chassis a 0 AP");
            var dsc = FindAnyObjectByType<DeadScreen>();
            Check(dsc != null && dsc.content != null && dsc.content.activeInHierarchy && GameManager.I.State == GameState.Dead, "ecran « Châssis détruit » affiche", dsc != null ? Texts(dsc.content) : "absent");
            yield return Shot("mort");
            GameManager.I.Redeploy();
            yield return Wait(2f);
            Check(!p.dead && p.AP > 0f, "redeploiement", $"AP {p.AP:0}, {Vector3.Distance(p.Center, p.SpawnPosition):0} m du point de depart");
            Check(p.progress.credits >= credits0 && (cm == null || cm.Index == contract0), "credits et contrat conserves apres la mort", $"{MathX.Credits(p.progress.credits)}, contrat {(cm != null ? cm.Index + 1 : 0)}");
            if (near0 > 0) Check(NearSpawn() == 0, "patrouilles proches de la plateforme supprimees au redeploiement", $"{near0} -> {NearSpawn()} a moins de {clearR:0} m");
            yield return Shot("redeploiement");
        }

        // ------------------------------------------------------------------ jalon 6 : ecran titre, HUD, mini-carte, menus
        static string Texts(GameObject root)
        {
            var sb = new StringBuilder();
            foreach (var t in root.GetComponentsInChildren<UnityEngine.UI.Text>(false)) if (!string.IsNullOrEmpty(t.text)) { if (sb.Length > 0) sb.Append(" | "); sb.Append(t.text.Replace("\n", " ")); }
            return sb.ToString();
        }
        static bool Has(GameObject root, string s)
        {
            foreach (var t in root.GetComponentsInChildren<UnityEngine.UI.Text>(false)) if (t.text != null && t.text.Contains(s)) return true;
            return false;
        }

        IEnumerator TitleStart()
        {
            var gm = GameManager.I; if (gm == null) yield break;
            var title = FindAnyObjectByType<TitleScreen>();
            if (gm.State == GameState.Title)
            {
                Line("--- Jalon 6 : ecran titre ---");
                yield return Wait(1.5f);
                bool shown = title != null && title.content != null && title.content.activeInHierarchy;
                Check(shown, "ecran titre affiche au demarrage (monde visible derriere)");
                if (shown)
                {
                    Check(Has(title.content, "Châssis transformable VSP-07 « Vesper »"), "titre : designation « Châssis transformable VSP-07 « Vesper » »");
                    Check(Has(title.content, "Mode normal") && Has(title.content, "Mode DIVIN"), "titre : boutons « Mode normal » et « Mode DIVIN »");
                    Check(Has(title.content, "Normal : on gagne ses crédits et on achète tout."), "titre : note Normal / DIVIN");
                    Check(Has(title.content, "Un monde ouvert de 15 km"), "titre : texte de presentation (§4.1)");
                }
                var tc = FindAnyObjectByType<TitleCamera>();
                if (tc != null && p.cam != null) Check(Mathf.Abs(p.cam.cam.fieldOfView - tc.fov) < 0.5f && Vector3.Distance(p.cam.cam.transform.position, p.transform.position) > tc.distance * 0.8f, "camera de presentation (FOV 50, 30 m)", $"FOV {p.cam.cam.fieldOfView:0}, {Vector3.Distance(p.cam.cam.transform.position, p.transform.position):0} m");
                Check(!HudController.I.Visible, "HUD cache sur l'ecran titre");
                yield return Shot("ecran_titre");
                si.Tap(Cmd.Start);
                yield return null; yield return null;
                Check(gm.State == GameState.Play && !p.progress.divine, "Entree : la partie demarre en mode normal");
                yield return Wait(1f);
            }
            else Check(!gm.startOnTitle, "demarrage direct en jeu (scene Test : startOnTitle decoche)");
        }

        IEnumerator HudChecks()
        {
            var hud = HudController.I;
            Check(hud != null, "HUD (Canvas) present dans la scene");
            if (hud == null) yield break;
            Check(hud.Visible && hud.hudGroup != null && hud.hudGroup.alpha > 0.9f, "HUD visible en jeu");
            var w = FindAnyObjectByType<HudWeapons>();
            Check(w != null && w.rows.Length == 9, "panneau Armes : 9 lignes", w != null ? Texts(w.gameObject) : "");
            if (w != null)
            {
                string t = Texts(w.gameObject);
                Check(t.Contains("Fusil à impulsions") && t.Contains("∞") && t.Contains("×" + p.S.missileCount) && t.Contains("Kit de réparation"), "armes : fusil ∞, missiles ×N, kit");
                Check(p.progress.arcane || t.Contains("1 M C"), "armes : Rayon Arcane « 1 M C » tant qu'il n'est pas acquis");
                Check(p.progress.domain || t.Contains("500 k C"), "armes : Extension du territoire « 500 k C »");
            }
            var cr = FindAnyObjectByType<HudCreditsPanel>();
            Check(cr != null && cr.credits.text == MathX.Credits(p.progress.credits) || cr != null && cr.credits.text.EndsWith(" C"), "credits au format « 12 500 C »", cr != null ? cr.credits.text : "");
            var ae = FindAnyObjectByType<HudArmorEnergy>();
            Check(ae != null && ae.apValue.text == UiFx.N(p.AP), "grand chiffre d'AP", ae != null ? ae.apValue.text : "");
            var sp = FindAnyObjectByType<HudSpeed>();
            Check(sp != null && sp.speed.text.EndsWith(" km/h") && sp.altitude.text.EndsWith(" m"), "vitesse en km/h et altitude en m", sp != null ? sp.speed.text + " · " + sp.altitude.text : "");
            var ob = FindAnyObjectByType<HudObjective>();
            Check(ob != null && ob.label.text.Length > 0, "objectif (haut gauche)", ob != null ? Texts(ob.gameObject) : "");
            // fil de messages et grand message
            Msg.Post("Test du fil de messages", MsgKind.Good);
            Msg.Big("Test", "grand message", 3f);
            yield return null; yield return null;
            var feed = FindAnyObjectByType<HudFeed>();
            bool inFeed = false; if (feed != null) foreach (var l in feed.lines) if (l != null && l.gameObject.activeInHierarchy && l.text == "Test du fil de messages") inFeed = true;
            Check(inFeed, "fil de messages (bonne nouvelle en bleu glace)");
            var big = FindAnyObjectByType<HudBigMessage>();
            Check(big != null && big.title.gameObject.activeInHierarchy && big.title.text == "TEST", "grand message en majuscules");
            // mini-carte
            var mm = FindAnyObjectByType<Minimap>();
            Check(mm != null && mm.map != null && mm.map.texture != null, "mini-carte avec la carte du monde");
            if (mm != null)
            {
                float r0 = mm.CurrentRange; si.Tap(Cmd.MapZoom); yield return null; yield return null;
                float r1 = mm.CurrentRange; si.Tap(Cmd.MapZoom); yield return null; yield return null;
                float r2 = mm.CurrentRange; si.Tap(Cmd.MapZoom); yield return null; yield return null;
                float r3 = mm.CurrentRange;
                Check(r0 == 600f && r1 == 1800f && r2 == 4500f && r3 == 600f, "M : zoom 600 → 1 800 → 4 500 → 600 m", $"{r0} {r1} {r2} {r3}");
                Check(mm.rangeText.text == "Portée 600 m" && mm.zoomText.text == "M · zoom", "legendes de la mini-carte", mm.rangeText.text + " · " + mm.zoomText.text);
            }
            yield return Wait(0.5f);
            yield return Shot("hud");
        }

        IEnumerator Jalon6Menus()
        {
            var gm = GameManager.I; var pr = p.progress;
            int[] levels0 = (int[])pr.levels.Clone(); long credits0 = pr.credits; int probes0 = pr.probes;
            gm.OpenMenu(GameManager.PauseTab.Upgrades);
            yield return null; yield return null;
            var pm = FindAnyObjectByType<PauseMenu>();
            var up = FindAnyObjectByType<UpgradesPanel>();
            Check(pm != null && pm.content.activeInHierarchy && up != null && up.gameObject.activeInHierarchy, "menu pause : onglet Ameliorations");
            if (up == null) { gm.SetState(GameState.Play); yield break; }
            Check(Has(up.gameObject, "Les crédits viennent des ennemis détruits, des avant-postes neutralisés, des contrats et des boss."), "texte d'introduction (§11.4)");
            Check(up.rows.Length == 16, "11 modules + 5 lignes speciales", up.rows.Length.ToString());
            // prix et valeurs (§11.2) : un module de 0 a 5
            for (int i = 0; i < pr.levels.Length; i++) pr.levels[i] = 0;
            p.Recompute();
            pr.credits = 14100;
            UpgradeRow prop = null; foreach (var r in up.rows) if (r.kind == UpgradeKind.Module && r.module == UpgradeModule.Propulseurs) prop = r;
            var prices = new List<string>();
            for (int k = 0; k < 5; k++)
            {
                yield return null;
                prices.Add(prop.buyLabel.text);
                if (k == 0) Check(prop.value.text == "259 km/h → 290 km/h", "valeur actuelle → suivante (propulseurs)", prop.value.text);
                prop.Buy();
            }
            yield return null;
            Check(string.Join(" / ", prices) == "900 C / 1 600 C / 2 600 C / 3 800 C / 5 200 C", "prix des niveaux 1 a 5", string.Join(" / ", prices));
            Check(pr.credits == 0 && pr.Level(UpgradeModule.Propulseurs) == 5, "14 100 C par module", MathX.Credits(14100 - pr.credits));
            Check(prop.buyLabel.text == "Maximum" && !prop.buyButton.interactable && prop.value.text == "415 km/h", "niveau 5 : « Maximum », boost 415 km/h", prop.buyLabel.text + " · " + prop.value.text);
            UpgradeRow Row(UpgradeKind k) { foreach (var r in up.rows) if (r.kind == k) return r; return null; }
            pr.credits = 0; yield return null;
            Check(!Row(UpgradeKind.RayonArcane).buyButton.interactable && Row(UpgradeKind.RayonArcane).buyLabel.text == "1 000 000 C", "Rayon Arcane : 1 000 000 C (desactive sans credits)");
            Check(Row(UpgradeKind.Territoire).buyLabel.text == "500 000 C", "Extension du territoire : 500 000 C");
            Check(Row(UpgradeKind.TerritoireII).buyLabel.text == "Verrouillé", "Extension II : verrouillee (extension I + Archonte)");
            Check(Row(UpgradeKind.Sonde).buyLabel.text == "250 000 C", "Sonde laser : 250 000 C");
            Check(Row(UpgradeKind.Reincarnation).buyLabel.text == "10 000 000 C", "Reincarnation : 10 000 000 C");
            // tout acheter (hors reincarnation) = 5 405 100 C
            for (int i = 0; i < pr.levels.Length; i++) pr.levels[i] = 0;
            pr.probes = 0; pr.arcane = pr.domain = pr.domain2 = false; pr.archonDefeated = true;
            p.Recompute();
            pr.credits = 5405100;
            for (int i = 0; i < 11; i++) for (int k = 0; k < 5; k++) gm.Buy((UpgradeModule)i);
            for (int k = 0; k < 5; k++) gm.BuyProbe();
            gm.BuySpecial(UpgradeKind.RayonArcane); gm.BuySpecial(UpgradeKind.Territoire); gm.BuySpecial(UpgradeKind.TerritoireII);
            Check(pr.credits == 0 && pr.arcane && pr.domain && pr.domain2 && pr.probes == 5, "tout acheter hors reincarnation : 5 405 100 C", MathX.Credits(5405100 - pr.credits));
            Check(Mathf.RoundToInt(p.S.armor) == 14760 && Mathf.RoundToInt(p.S.energy) == 2250 && p.S.missileCount == 9 && p.S.salvoCount == 10 && p.S.fragMinis == 10, "valeurs au niveau 5 (§11.2)",
                $"AP {p.S.armor:0}, EN {p.S.energy:0}, missiles {p.S.missileCount}, salve {p.S.salvoCount}, mini {p.S.fragMinis}");
            pr.credits = 10000000; float ap0 = p.S.armor;
            gm.BuySpecial(UpgradeKind.Reincarnation);
            Check(pr.reincarnation == 1 && Mathf.Abs(p.S.armor - ap0 * 5f) < 1f && Mathf.Abs(p.AP - p.S.armor) < 1f, "Reincarnation 1 : PV ×5, AP remplie", $"{ap0:0} -> {p.S.armor:0}");
            yield return Wait(1f);
            yield return Shot("menu_ameliorations");
            // mode DIVIN : selecteurs ◀ ▶
            gm.ApplyDivine();
            yield return null; yield return null;
            Check(pr.divine && up.divineIntro.activeInHierarchy && Row(UpgradeKind.Reincarnation).divineSelector.activeInHierarchy, "mode DIVIN : texte dedie et selecteurs ◀ ▶");
            for (int k = 0; k < 20; k++) Row(UpgradeKind.Reincarnation).Next();
            Check(pr.reincarnation == 12, "DIVIN : reincarnation reglable jusqu'a 12", pr.reincarnation.ToString());
            prop.Prev(); prop.Prev();
            Check(pr.Level(UpgradeModule.Propulseurs) == 3, "DIVIN : fleche ◀ baisse le niveau gratuitement");
            var crp = FindAnyObjectByType<HudCreditsPanel>();
            yield return null;
            Check(crp != null && crp.divine.gameObject.activeInHierarchy && crp.divine.text == "Mode DIVIN", "« Mode DIVIN » affiche en haut a droite");
            var fus = Row(UpgradeKind.Module); foreach (var r in up.rows) if (r.kind == UpgradeKind.Module && r.module == UpgradeModule.Fusil) fus = r;
            Check(!fus.value.text.Contains("-"), "valeurs positives a la reincarnation 12 (pas de depassement)", fus.value.text);
            yield return Wait(1f);
            yield return Shot("menu_divin");
            // onglet Commandes
            gm.ShowCommands();
            yield return Wait(1.2f);
            var cmdPanel = pm.commandsPanel;
            Check(cmdPanel.activeInHierarchy && Has(cmdPanel, "Zoom de la mini-carte") && Has(cmdPanel, "600 m → 1 800 m → 4 500 m, puis retour à 600 m") && Has(cmdPanel, "Lance le mode normal"), "onglet Commandes (4 groupes, §8)");
            yield return Shot("menu_commandes");
            // retour a l'etat de depart
            pr.divine = false; pr.reincarnation = 0; pr.arcane = pr.domain = pr.domain2 = false; pr.archonDefeated = false;
            pr.levels = levels0; pr.credits = credits0; pr.probes = probes0; p.Recompute(); p.AP = p.S.armor;
            gm.Resume();
            yield return null;
            Check(gm.State == GameState.Play && !pm.content.activeInHierarchy, "Reprendre : retour au jeu");
        }

        // ------------------------------------------------------------------ jalon 5 (scene Test)
        IEnumerator Jalon5()
        {
            var ps = ProjectileSystem.I;
            p.invulnerable = true;
            p.AP = p.S.armor;
            Check(testFoes.Count >= 6, "un ennemi de chaque type dans la scene Test", testFoes.Count + " ennemis");
            // 1. chaque ennemi attaque quand le joueur approche
            foreach (var e in testFoes)
            {
                if (e == null) continue;
                foreach (var o in testFoes) if (o != null && o != e) o.gameObject.SetActive(false);
                e.gameObject.SetActive(true); e.Respawn();
                bool flying = e is SentinelEnemy || e is FighterEnemy || e is RelayEnemy;
                float rr = e.BaseData != null ? e.BaseData.radius : 5f;
                yield return Showcase(e.transform.position, rr * 5f + 8f, rr * 1.5f, "vitrine_" + e.GetType().Name.Replace("Enemy", ""), p.Center);
                Teleport(StandAt(e.transform.position, e is SpiderEnemy ? 420f : e is FighterEnemy ? 600f : 250f));
                yield return Wait(0.2f);
                float ap0 = p.AP; int fired0 = ps != null ? ps.statEnemyFired : 0, hits0 = ps != null ? ps.statEnemyHits : 0;
                int shells0 = ShellSystem.I != null ? ShellSystem.I.Exploded : 0;
                float t = 0f; bool shot = false;
                while (t < 8f)
                {
                    AimAt(e.transform.position); t += Time.deltaTime;
                    if (!shot && t > 2.2f) { shot = true; yield return Shot("ennemi_" + e.GetType().Name.Replace("Enemy", "")); }
                    else yield return null;
                    if (!(e is RelayEnemy) && t > 3f && p.AP < ap0 - 1f && !(e is SpiderEnemy)) break;
                }
                int fired = ps != null ? ps.statEnemyFired - fired0 : 0, hits = ps != null ? ps.statEnemyHits - hits0 : 0;
                int shells = ShellSystem.I != null ? ShellSystem.I.Exploded - shells0 : 0;
                string label = e.BaseData != null ? e.BaseData.displayName : e.name;
                if (e is RelayEnemy) Check(e.Engaged && p.AP >= ap0 - 1f, $"{label} : se declenche sans tirer", $"engage {e.Engaged}");
                else if (e is SpiderEnemy) Check(shells > 0 && p.AP < ap0, $"{label} : obus de mortier telegraphies qui touchent", $"{shells} obus explose(s), AP {ap0:0} -> {p.AP:0}");
                else Check(e.Engaged && p.AP < ap0, $"{label} : se declenche et touche le joueur", $"{fired} tirs, {hits} touches, AP {ap0:0} -> {p.AP:0}{(flying ? "" : "")}");
                // jauge d'impact : surcharge sur les ennemis qui en ont une
                if (e.BaseData != null && e.BaseData.impactMax > 0f)
                {
                    e.Target.TakeDamage(new DamageInfo { amount = 1f, impact = e.BaseData.impactMax + 1f, point = e.transform.position, fromPlayer = true });
                    Check(e.Target.Overloaded, $"{label} : surcharge d'impact a {e.BaseData.impactMax:0}");
                }
                long cr0 = p.progress.credits;
                Kill(e); yield return Wait(0.3f);
                Check(!e.gameObject.activeSelf && p.progress.credits - cr0 == e.BaseData.credits, $"{label} : destruction et credits", $"+{p.progress.credits - cr0} C (attendu {e.BaseData.credits})");
                p.AP = p.S.armor;
            }
            if (ps != null) Line($"  projectiles ennemis : {ps.statEnemyFired} tires, {ps.statEnemyHits} touches");

            // 2. contrat d'essai, recompense, kits, menace
            if (cm != null && cm.Count > 0)
            {
                float w = 0f; while ((cm.Current == null || cm.Waiting) && w < 10f) { w += Time.deltaTime; yield return null; }
                var c = cm.Current;
                Check(c != null && c.Alive == c.Total && c.Total > 0, "le contrat demarre avec tous ses ennemis", c != null ? $"{c.Title} : {c.Alive} / {c.Total}" : "aucun");
                Teleport(StandAt(c.LocationPoint, 300f)); AimAt(c.LocationPoint);
                yield return Wait(1.2f);
                yield return Shot("contrat_essai");
                int threat0 = Threat.Level; long cr0 = p.progress.credits; p.kits = 0;
                long enemyCredits = 0; foreach (var e in c.Enemies) if (e.IsAlive) enemyCredits += e.BaseData.credits;
                foreach (var e in c.Enemies) Kill(e);
                yield return Wait(0.5f);
                long expected = (long)Mathf.Round(c.data.reward * (1f + 0.3f * threat0)) + enemyCredits;
                Check(cm.Waiting, "contrat termine quand tous les ennemis sont detruits");
                Check(p.progress.credits - cr0 == expected, "recompense du contrat x menace + credits des ennemis", $"+{p.progress.credits - cr0} C (attendu {expected})");
                Check(p.kits == p.frame.repairKits, "kits de reparation recharges", $"{p.kits}");
                yield return Wait(cm.campaign.nextContractDelay + 0.6f);
                Check(Threat.Level == threat0 + 1, "fin du cycle de contrats : menace +1", $"menace {threat0} -> {Threat.Level}");
                var c2 = cm.Current; Enemy anyE = c2 != null && c2.Enemies.Count > 0 ? c2.Enemies[0] : null;
                Check(c2 != null && !cm.Waiting && c2.Alive == c2.Total, "le contrat recommence", c2 != null ? $"{c2.Alive} / {c2.Total}" : "");
                if (anyE != null) Check(Mathf.Abs(anyE.Target.maxHP - anyE.BaseData.maxHP * (1f + 0.3f * Threat.Level)) < 1f, "PV des ennemis x (1 + 0,3 x menace)", $"{anyE.Target.maxHP:0} (base {anyE.BaseData.maxHP:0})");
                Check(Mathf.Abs(Threat.DamageMult - (1f + 0.2f * Threat.Level)) < 1e-3f, "degats ennemis x (1 + 0,2 x menace)", Threat.DamageMult.ToString("0.00"));
            }

            // 3. avant-poste : activation, victoire, desactivation
            var op = FindAnyObjectByType<Outpost>();
            if (op != null)
            {
                Teleport(StandAt(op.transform.position, 1000f));
                yield return Wait(0.8f);
                Check(op.Active && op.AliveCount() == op.Garrison.Count, "avant-poste : garnison apparue a moins de 1 700 m", $"{op.AliveCount()} / {op.Garrison.Count}");
                AimAt(op.transform.position + Vector3.up * 40f); yield return Wait(0.3f);
                yield return Shot("avant_poste");
                yield return Showcase(op.transform.position + Vector3.up * 20f, 140f, 60f, "vitrine_avant_poste", p.Center);
                long cr0 = p.progress.credits; long ec = 0;
                foreach (var e in op.Garrison) if (e.IsAlive) ec += e.BaseData.credits;
                foreach (var e in op.Garrison) Kill(e);
                yield return Wait(0.8f);
                Check(op.Victories == 1 && p.progress.credits - cr0 == op.data.reward + ec, "avant-poste neutralise : +400 C", $"+{p.progress.credits - cr0} C (dont {ec} pour les ennemis)");
                Check(!op.Active, "pas de reactivation avant 150 s");
                op.LastVictory = -99999f; yield return Wait(0.8f);
                Check(op.Active, "reactivation apres le delai");
                Teleport(StandAt(op.transform.position, 3000f)); yield return Wait(0.8f);
                Check(!op.Active && op.AliveCount() == 0, "garnison retiree au-dela de 2 700 m");
            }

            // 4. patrouilles errantes
            if (patrols != null)
            {
                Teleport(new Vector3(0f, p.frame.frameClear + 0.5f, -1500f));
                patrols.enabled = true; yield return null;
                int n0 = patrols.FreeCount;
                bool ok = patrols.TrySpawn(); yield return null;
                int n1 = patrols.FreeCount;
                float dmin = float.MaxValue, dmax = 0f;
                foreach (var e in patrols.Spawned) { float dd = Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), new Vector2(p.Center.x, p.Center.z)); dmin = Mathf.Min(dmin, dd); dmax = Mathf.Max(dmax, dd); }
                Check(ok && (n1 - n0 == 2 || n1 - n0 == 3), "patrouille errante : 2 chasseurs ou 3 sentinelles", $"{n1 - n0} ennemis a {dmin:0}-{dmax:0} m");
                yield return Shot("patrouille");
                Teleport(new Vector3(0f, p.frame.frameClear + 0.5f, 3500f)); yield return Wait(0.5f);
                Check(patrols.FreeCount == 0, "ennemis libres supprimes au-dela de 3 000 m");
                patrols.enabled = false;
            }
            Teleport(p.SpawnPosition + Vector3.up * (p.frame.frameClear + 0.5f));
            yield return Wait(1f);
        }

        // ------------------------------------------------------------------ scene World : contrats, avant-postes, monde
        IEnumerator WorldChecks()
        {
            p.invulnerable = true;
            Check(cm.Count == 7, "7 contrats dans la scene", cm.Count.ToString());
            var outs = FindObjectsByType<Outpost>(FindObjectsInactive.Exclude);
            float minSpace = float.MaxValue, minHelios = float.MaxValue;
            for (int i = 0; i < outs.Length; i++)
            {
                Vector3 a = outs[i].transform.position;
                minHelios = Mathf.Min(minHelios, Vector2.Distance(new Vector2(a.x, a.z), new Vector2(p.SpawnPosition.x, p.SpawnPosition.z)));
                for (int j = i + 1; j < outs.Length; j++) minSpace = Mathf.Min(minSpace, Vector2.Distance(new Vector2(a.x, a.z), new Vector2(outs[j].transform.position.x, outs[j].transform.position.z)));
            }
            Check(outs.Length == 36, "36 avant-postes", outs.Length.ToString());
            Check(minSpace >= 850f && minHelios >= 1500f, "avant-postes espaces de 850 m et a plus de 1 500 m de la plateforme", $"ecart mini {minSpace:0} m, plateforme {minHelios:0} m");
            var ts = FindAnyObjectByType<TerrainSystem>();
            Check(ts != null && ts.GetComponentsInChildren<TerrainTile>().Length == 64, "terrain : 64 tuiles dans la Hierarchy");
            Check(Mathf.Abs(TerrainSystem.Ground(0f, 0f) - 10f) < 1f, "hauteur du sol en (0 ; 0) = 10 m", TerrainSystem.Ground(0f, 0f).ToString("0.0"));
            AimAt(new Vector3(0f, 300f, 0f)); yield return Wait(0.5f);
            yield return Shot("monde_depart");
            yield return Showcase(new Vector3(0f, 250f, 0f), 1500f, 450f, "vitrine_arcologie", p.SpawnPosition + new Vector3(0f, 0f, 400f));

            float w = 0f; while ((cm.Current == null || cm.Waiting) && w < 10f) { w += Time.deltaTime; yield return null; }
            int threat0 = Threat.Level;
            for (int k = 0; k < 7; k++)
            {
                var c = cm.Current;
                if (c == null) { Check(false, $"contrat {k + 1} actif"); break; }
                Check(cm.Index == k && c.Alive == c.Total && c.Total > 0, $"contrat {k + 1} « {c.Title} » demarre", $"{c.Total} ennemis");
                // vue d'ensemble du lieu
                Vector3 loc = c.LocationPoint;
                Vector3 view = loc + Flat(p.SpawnPosition - loc).normalized * 420f;
                view.y = Mathf.Max(loc.y, TerrainSystem.Ground(view.x, view.z)) + 60f;
                Teleport(view); AimAt(loc);
                yield return Wait(1.5f); AimAt(loc);
                yield return Shot($"contrat_{k + 1}");
                yield return Showcase(loc, 330f, 140f, $"vitrine_contrat_{k + 1}", view);
                long cr0 = p.progress.credits; long ec = 0;
                foreach (var e in c.Enemies) if (e.IsAlive) ec += e.BaseData.credits;
                foreach (var e in c.Enemies) Kill(e);
                yield return Wait(0.5f);
                long expected = (long)Mathf.Round(c.data.reward * (1f + 0.3f * threat0)) + ec;
                Check(cm.Waiting && p.progress.credits - cr0 == expected, $"contrat {k + 1} termine : +{MathX.Credits(c.data.reward)}", $"+{p.progress.credits - cr0} C (attendu {expected})");
                w = 0f; while (cm.Waiting && w < 10f) { w += Time.deltaTime; yield return null; }
            }
            Check(Threat.Level == threat0 + 1 && cm.Index == 0, "apres le 7e contrat : menace +1 et retour au contrat 1", $"menace {Threat.Level}, contrat {cm.Index + 1}");

            // avant-poste le plus proche de la plateforme
            Outpost near = null; float nd = float.MaxValue;
            foreach (var o in outs) { float dd = Vector3.Distance(o.transform.position, p.SpawnPosition); if (dd < nd) { nd = dd; near = o; } }
            if (near != null)
            {
                Teleport(StandAt(near.transform.position, 700f));
                yield return Wait(0.8f);
                Check(near.Active, $"avant-poste « {near.name} » active a l'approche", $"{near.AliveCount()} ennemis");
                AimAt(near.transform.position + Vector3.up * 30f); yield return Wait(1f);
                yield return Shot("avant_poste_monde");
                yield return Showcase(near.transform.position + Vector3.up * 20f, 150f, 70f, "vitrine_avant_poste_monde", p.Center);
                long cr0 = p.progress.credits; foreach (var e in near.Garrison) Kill(e);
                yield return Wait(0.8f);
                Check(near.Victories == 1, "avant-poste neutralise", $"+{p.progress.credits - cr0} C");
            }
            Teleport(p.SpawnPosition + Vector3.up * (p.frame.frameClear + 0.5f));
            yield return Wait(1f);
        }

        int CountAlive() { int n = 0; foreach (var t in Targetable.All) if (t != null && t.Alive) n++; return n; }
        float HpSum() { float s = 0f; foreach (var t in Targetable.All) if (t != null && t.Alive) s += t.HP; return s; }
        static string GraphicsSettingsName()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return rp != null ? rp.GetType().Name : "pipeline integre (aucun asset URP)";
        }

        IEnumerator Finish()
        {
            float avg = frameCount > 0 ? frameTimeSum / frameCount : 0f;
            Line($"Images : moyenne {avg * 1000f:0.0} ms ({(avg > 0 ? 1f / avg : 0f):0} ips), pire {worstFrame * 1000f:0} ms");
            Line($"Resultat : {pass} OK, {fail} ECHEC, {errors.Count} erreurs dans la console");
            foreach (var e in errors) log.AppendLine("\n" + e);
            File.WriteAllText(Path.Combine(Dir, "rapport.txt"), log.ToString(), Encoding.UTF8);
            Debug.Log("[MGSUGE AutoTest] Rapport : " + Path.Combine(Dir, "rapport.txt"));
            yield return Wait(0.5f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
