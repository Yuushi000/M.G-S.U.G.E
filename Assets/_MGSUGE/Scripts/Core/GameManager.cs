using UnityEngine;

namespace MGSUGE
{
    public enum GameState { Title, Play, Pause, Dead }

    /// <summary>Machine a etats du jeu (§4.4) : title, play, pause (temps du monde arrete, souris liberee), dead.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        public PlayerController player;
        [Tooltip("touches de test F1-F4 (credits, sonde, ameliorations, plein). A desactiver pour une version publique.")]
        public bool testKeys = true;
        public GameState State { get; private set; } = GameState.Play;
        public enum PauseTab { Upgrades, Commands }
        public PauseTab tab = PauseTab.Upgrades;

        void Awake() { I = this; Msg.Clear(); }
        void Start()
        {
            SetState(GameState.Play);
            Msg.Big("M.G-S.U.G.E", "Terrain d'essai du jalon 4 · K : commandes · Échap : menu", 5f);
        }

        public void SetState(GameState s)
        {
            State = s;
            Time.timeScale = s == GameState.Pause ? 0f : 1f;
            bool play = s == GameState.Play;
            Cursor.lockState = play ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !play;
            if (player != null && player.input != null) player.input.locked = !play;
        }
        public void ShowDead() { SetState(GameState.Dead); }
        public void Redeploy() { player.Redeploy(); SetState(GameState.Play); }
        public void OpenMenu(PauseTab t) { tab = t; SetState(GameState.Pause); }

        void Update()
        {
            var inp = player != null ? player.input : null;
            if (inp == null) return;
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

        /// <summary>Achat d'un niveau d'amelioration (§11.2).</summary>
        public bool Buy(UpgradeModule m)
        {
            var pr = player.progress; int lvl = pr.Level(m);
            if (lvl >= player.progression.maxLevel) return false;
            long cost = player.progression.levelCosts[lvl];
            if (pr.credits < cost) return false;
            pr.credits -= cost; pr.levels[(int)m] = lvl + 1;
            player.Recompute(); Sfx.Play("buy");
            return true;
        }
        public bool BuyProbe()
        {
            var pr = player.progress; var w = player.weaponData;
            if (pr.probes >= w.probeMax || pr.credits < w.probeCost) return false;
            pr.credits -= w.probeCost; pr.probes++; Sfx.Play("buy");
            Msg.Post($"Sonde laser déployée — {pr.probes} / {w.probeMax} sondes · tir automatique sur la cible verrouillée", MsgKind.Good);
            return true;
        }
    }
}
