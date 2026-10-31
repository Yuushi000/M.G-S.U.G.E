using UnityEngine;

namespace MGSUGE
{
    public enum GameState { Title, Play, Pause, Dead }
    /// <summary>Lignes du menu Ameliorations : un des 11 modules, ou un achat unique (§11.3).</summary>
    public enum UpgradeKind { Module, Sonde, RayonArcane, Territoire, TerritoireII, Reincarnation }

    /// <summary>
    /// Machine a etats du jeu (§4.4) : title (ecran titre, le monde est affiche), play, pause (temps du monde arrete, souris
    /// liberee), dead. Modes de jeu Normal / DIVIN (§4.3) et achats du menu Ameliorations (§11.2, §11.3).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        [Tooltip("joueur de la scene")] public PlayerController player;
        [Tooltip("patrouilles errantes (nettoyees autour de la plateforme au redeploiement) ; facultatif")] public PatrolSpawner patrols;
        [Tooltip("touches de test F1-F4 (credits, sonde, ameliorations, plein). A desactiver pour une version publique.")]
        public bool testKeys = true;
        [Header("Ecran titre (§4.4, §16.3)")]
        [Tooltip("la scene demarre sur l'ecran titre (Entree ou « Mode normal » pour jouer) ; decoche = partie en mode normal directement")]
        public bool startOnTitle = true;
        [Header("Message d'accueil (au debut de la partie)")]
        public string startTitle = "M.G-S.U.G.E";
        public string startSubtitle = "K : commandes · Échap : menu";
        [Tooltip("duree du message d'accueil (s)")] [Range(2.5f, 5f)] public float startMessageDuration = 5f;
        [Header("Mode DIVIN (§4.3)")]
        [Tooltip("message affiche au lancement du mode DIVIN")] public string divineMessage = "Mode DIVIN : tout est débloqué";
        [Header("4.4 Redeploiement")]
        [Tooltip("les ennemis non-boss hors contrat a moins de cette distance de la plateforme sont supprimes (m)")] public float redeployClearRadius = 600f;
        [Header("Messages des competences speciales (activation au jalon 8)")]
        public string arcaneSoon = "Rayon Arcane : activation au jalon 8";
        public string domainSoon = "Extension du territoire : activation au jalon 8";

        public GameState State { get; private set; } = GameState.Play;
        public enum PauseTab { Upgrades, Commands }
        public PauseTab tab = PauseTab.Upgrades;
        /// <summary>Change a chaque ouverture du menu ou changement d'onglet (les ecrans relancent leurs animations).</summary>
        public int MenuVersion { get; private set; }

        void Awake() { I = this; Msg.Clear(); }
        void Start()
        {
            if (startOnTitle) SetState(GameState.Title);
            else StartGame(false);
        }

        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = s == GameState.Pause ? 0f : 1f;
            bool play = s == GameState.Play;
            Cursor.lockState = play ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !play;
            if (player != null && player.input != null) player.input.locked = !play;
            MenuVersion++;
        }

        /// <summary>Lance la partie depuis l'ecran titre : mode normal (2 000 C, tout au niveau 0) ou DIVIN (tout debloque).</summary>
        public void StartGame(bool divine)
        {
            if (player != null)
            {
                player.progress.divine = divine;
                if (divine) ApplyDivine();
            }
            SetState(GameState.Play);
            Msg.Big(startTitle, divine ? divineMessage : startSubtitle, startMessageDuration);
        }
        public void StartNormal() => StartGame(false);
        public void StartDivine() => StartGame(true);

        /// <summary>Mode DIVIN : 11 ameliorations au niveau 5, Rayon Arcane, Extension du territoire I et II, 5 sondes.</summary>
        public void ApplyDivine()
        {
            var pr = player.progress; var pd = player.progression;
            pr.divine = true;
            for (int i = 0; i < pr.levels.Length; i++) pr.levels[i] = pd.maxLevel;
            pr.arcane = pr.domain = pr.domain2 = true;
            pr.probes = player.weaponData != null ? player.weaponData.probeMax : 5;
            player.Recompute(); player.AP = player.S.armor; player.EN = player.S.energy;
        }

        public void ShowDead() { SetState(GameState.Dead); }
        public void Redeploy()
        {
            player.Redeploy();
            Vector3 c = player.SpawnPosition;
            if (patrols != null) patrols.ClearNear(c, redeployClearRadius);
            // garnisons d'avant-postes trop proches : retirees (§4.4) ; les ennemis de contrat restent
            foreach (var e in new System.Collections.Generic.List<Enemy>(Enemy.All))
                if (e != null && !e.contractEnemy && Vector3.Distance(e.transform.position, c) < redeployClearRadius)
                {
                    if (e.roaming) { Destroy(e.gameObject); continue; }
                    var op = e.GetComponentInParent<Outpost>();
                    if (op != null) op.Deactivate(); else e.gameObject.SetActive(false);
                }
            SetState(GameState.Play);
        }
        public void OpenMenu(PauseTab t) { tab = t; SetState(GameState.Pause); }
        public void Resume() { if (State == GameState.Pause) SetState(GameState.Play); }
        public void ShowUpgrades() { tab = PauseTab.Upgrades; MenuVersion++; }
        public void ShowCommands() { tab = PauseTab.Commands; MenuVersion++; }

        void Update()
        {
            var inp = player != null ? player.input : null;
            if (inp == null) return;
            if (State == GameState.Title)
            {
                if (TitleScreen.I == null && inp.PressedRaw(Cmd.Start)) StartGame(false); // sinon l'ecran titre gere Entree
                return;
            }
            if (inp.PressedRaw(inp.Pause))
            {
                if (State == GameState.Play) OpenMenu(tab);
                else if (State == GameState.Pause) SetState(GameState.Play);
            }
            if (State == GameState.Play && inp.PressedRaw(inp.Upgrades)) OpenMenu(PauseTab.Upgrades);
            if (State == GameState.Play && inp.PressedRaw(inp.Commands)) OpenMenu(PauseTab.Commands);
            if (inp.PressedRaw(inp.Mute)) SfxSystem.I?.ToggleMute();
            if (State == GameState.Play && Cursor.lockState != CursorLockMode.Locked && inp.Clicked())
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (State == GameState.Play)
            {
                if (inp.Pressed(Cmd.Arcane) && player.progress.arcane) Msg.Post(arcaneSoon, MsgKind.Warning, 2f);
                if (inp.Pressed(Cmd.Domain) && player.progress.domain) Msg.Post(domainSoon, MsgKind.Warning, 2f);
            }

            if (!testKeys) return;
            if (inp.TestKey(1)) { player.progress.credits += 1000000; HudCredits.Gain(1000000); Msg.Post("Test : +1 000 000 C"); }
            if (inp.TestKey(2) && player.progress.probes < player.weaponData.probeMax)
            { player.progress.probes++; Msg.Post($"Sonde laser déployée — {player.progress.probes} / {player.weaponData.probeMax} sondes · tir automatique sur la cible verrouillée", MsgKind.Good); }
            if (inp.TestKey(3))
            {
                for (int i = 0; i < player.progress.levels.Length; i++) player.progress.levels[i] = Mathf.Min(player.progression.maxLevel, player.progress.levels[i] + 1);
                player.Recompute(); Msg.Post("Test : toutes les améliorations +1 niveau", MsgKind.Good);
            }
            if (inp.TestKey(4)) { player.AP = player.S.armor; player.EN = player.S.energy; player.RefillKits(); Msg.Post("Test : AP, EN et kits au maximum"); }
        }

        // ------------------------------------------------------------------ achats (§11.2, §11.3)
        bool Pay(long cost)
        {
            var pr = player.progress;
            if (pr.credits < cost) return false;
            pr.credits -= cost; Sfx.Play("buy");
            return true;
        }
        /// <summary>Achat d'un niveau d'amelioration (§11.2).</summary>
        public bool Buy(UpgradeModule m)
        {
            var pr = player.progress; int lvl = pr.Level(m);
            if (lvl >= player.progression.maxLevel) return false;
            if (!Pay(player.progression.levelCosts[lvl])) return false;
            pr.levels[(int)m] = lvl + 1;
            player.Recompute();
            return true;
        }
        public bool BuyProbe()
        {
            var pr = player.progress; var w = player.weaponData;
            if (pr.probes >= w.probeMax || !Pay(w.probeCost)) return false;
            pr.probes++;
            Msg.Post($"Sonde laser déployée — {pr.probes} / {w.probeMax} sondes · tir automatique sur la cible verrouillée", MsgKind.Good);
            return true;
        }
        /// <summary>Extension II : Extension I achetee et Archonte « Zenith » vaincu (§11.3).</summary>
        public bool Domain2Unlocked => player.progress.domain && (player.progress.archonDefeated || player.progress.divine);
        public bool BuySpecial(UpgradeKind k)
        {
            var pr = player.progress; var pd = player.progression;
            switch (k)
            {
                case UpgradeKind.Sonde: return BuyProbe();
                case UpgradeKind.RayonArcane:
                    if (pr.arcane || !Pay(pd.arcaneCost)) return false;
                    pr.arcane = true; Msg.Post("Rayon Arcane acquis — touche G", MsgKind.Good); return true;
                case UpgradeKind.Territoire:
                    if (pr.domain || !Pay(pd.domainCost)) return false;
                    pr.domain = true; Msg.Post("Extension du territoire acquise — touche B", MsgKind.Good); return true;
                case UpgradeKind.TerritoireII:
                    if (pr.domain2 || !Domain2Unlocked || !Pay(pd.domain2Cost)) return false;
                    pr.domain2 = true; Msg.Post("Extension du territoire II : recharge 2 min 30 s", MsgKind.Good); return true;
                case UpgradeKind.Reincarnation:
                    if (!Pay(pd.reincarnationCost)) return false;
                    SetReincarnation(pr.reincarnation + 1); return true;
            }
            return false;
        }
        void SetReincarnation(int n)
        {
            var pr = player.progress; var pd = player.progression;
            int before = pr.reincarnation;
            pr.reincarnation = Mathf.Max(0, n);
            player.Recompute();
            if (pr.reincarnation > before)
            {
                player.AP = player.S.armor;
                float x = Mathf.Pow(pd.reincarnationFactor, pr.reincarnation);
                Msg.Post($"Réincarnation {pr.reincarnation} — PV et dégâts ×{UiFx.N(x)}", MsgKind.Good);
            }
        }

        /// <summary>Mode DIVIN : fleches ◀ ▶ qui reglent gratuitement le niveau d'une ligne (§11.4).</summary>
        public void DivineStep(UpgradeKind k, UpgradeModule m, int delta)
        {
            var pr = player.progress; var pd = player.progression;
            if (!pr.divine) return;
            switch (k)
            {
                case UpgradeKind.Module: pr.levels[(int)m] = Mathf.Clamp(pr.levels[(int)m] + delta, 0, pd.maxLevel); player.Recompute(); break;
                case UpgradeKind.Sonde: pr.probes = Mathf.Clamp(pr.probes + delta, 0, player.weaponData.probeMax); break;
                case UpgradeKind.RayonArcane: pr.arcane = delta > 0; break;
                case UpgradeKind.Territoire: pr.domain = delta > 0; if (!pr.domain) pr.domain2 = false; break;
                case UpgradeKind.TerritoireII: pr.domain2 = delta > 0; if (pr.domain2) pr.domain = true; break;
                case UpgradeKind.Reincarnation: SetReincarnation(Mathf.Clamp(pr.reincarnation + delta, 0, pd.divineMaxReincarnation)); break;
            }
            Sfx.Play("buy");
        }
    }
}
