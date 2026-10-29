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
        static IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }
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

        IEnumerator Start()
        {
            Line("Test automatique M.G-S.U.G.E — " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Line($"Unity {Application.unityVersion} · {SystemInfo.graphicsDeviceName} · {Screen.width}x{Screen.height}");
            yield return Wait(2.5f);
            p = PlayerController.I != null ? PlayerController.I : FindAnyObjectByType<PlayerController>();
            if (p == null) { Check(false, "joueur present dans la scene"); yield return Finish(); yield break; }
            lockOn = FindAnyObjectByType<LockOn>(); weapons = FindAnyObjectByType<WeaponController>(); probes = FindAnyObjectByType<ProbeController>();
            Check(GameManager.I != null, "GameManager present");
            Check(lockOn != null && weapons != null && probes != null, "systemes d'armes presents (verrouillage, armes, sondes)");
            si = new ScriptedInput();
            p.input.OverrideBackend(si);
            var rend = GraphicsSettingsName();
            Check(rend.Contains("Universal"), "pipeline de rendu URP actif", rend);

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
                    yield return AimFor(t, 0.3f); hpSum0 = HpSum();
                    si.Tap(Cmd.Salvo); yield return AimFor(t, 3f);
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
                float hpSum0 = HpSum();
                yield return AimFor(t, 1.5f);
                yield return Shot("sondes");
                yield return AimFor(t, 3f);
                Check(probes.Fighting || HpSum() < hpSum0, "sondes laser : decollent et tirent sur la cible verrouillee", $"PV totaux {hpSum0:0} -> {HpSum():0}");
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

            // ---------------------------------------------------------------- mort et redeploiement
            p.TakeDamage(new DamageInfo { amount = p.AP + 99999f, point = p.Center, direction = Vector3.forward, fromPlayer = false });
            yield return Wait(2.5f);
            Check(p.dead, "destruction du chassis a 0 AP");
            yield return Shot("mort");
            GameManager.I.Redeploy();
            yield return Wait(2f);
            Check(!p.dead && p.AP > 0f, "redeploiement", $"AP {p.AP:0}");
            yield return Shot("redeploiement");

            yield return Finish();
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
