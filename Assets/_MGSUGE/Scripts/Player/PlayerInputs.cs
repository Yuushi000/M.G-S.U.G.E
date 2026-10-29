using System;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>Commandes du jeu (cahier §8).</summary>
    public enum Cmd
    {
        Jump, Down, Boost, QuickBoost, AssaultBoost, Transform, Fire, Missiles, Salvo, Frag, Blade, NextTarget, Repair,
        Arcane, Domain, Pause, Upgrades, Commands, MapZoom, Mute
    }

    /// <summary>Source des entrees : Input System (si le package est installe) ou ancien Input Manager (repli).</summary>
    public interface IInputBackend
    {
        string Name { get; }
        void Enable(); void Disable();
        Vector2 Move(); Vector2 LookDelta(); Vector2 Arrows();
        bool Held(Cmd c); bool Pressed(Cmd c);
        bool TestKey(int n);      // F1..F4
        bool Clicked();           // clic gauche (recapture de la souris)
    }

    /// <summary>
    /// Commandes du joueur. Avec le package Input System (recommande par le cahier, §5.1) : deplacements selon la POSITION
    /// physique des touches (ZQSD sur AZERTY = WASD sur QWERTY) et autres touches selon la lettre imprimee.
    /// Sans le package : repli sur l'ancien Input Manager (ZQSD et WASD acceptes).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class PlayerInputs : MonoBehaviour
    {
        /// <summary>Renseigne automatiquement par MGSUGE.InputSystem quand le package est present.</summary>
        public static Func<IInputBackend> NewInputSystemFactory;
        public IInputBackend Backend { get; private set; }
        [Tooltip("desactive toutes les commandes de jeu (menus, mort, Rayon Arcane)")] public bool locked;

        // noms historiques utilises par le reste du code
        public Cmd Jump => Cmd.Jump; public Cmd Down => Cmd.Down; public Cmd Boost => Cmd.Boost; public Cmd QuickBoost => Cmd.QuickBoost;
        public Cmd AssaultBoost => Cmd.AssaultBoost; public Cmd TransformKey => Cmd.Transform; public Cmd Fire => Cmd.Fire; public Cmd Missiles => Cmd.Missiles;
        public Cmd Salvo => Cmd.Salvo; public Cmd Frag => Cmd.Frag; public Cmd Blade => Cmd.Blade; public Cmd NextTarget => Cmd.NextTarget;
        public Cmd Repair => Cmd.Repair; public Cmd Arcane => Cmd.Arcane; public Cmd Domain => Cmd.Domain; public Cmd Pause => Cmd.Pause;
        public Cmd Upgrades => Cmd.Upgrades; public Cmd Commands => Cmd.Commands; public Cmd MapZoom => Cmd.MapZoom; public Cmd Mute => Cmd.Mute;

        void Awake()
        {
            Backend = NewInputSystemFactory != null ? NewInputSystemFactory() : new LegacyInputBackend();
            Debug.Log("[MGSUGE] Entrees : " + Backend.Name);
        }
        /// <summary>Remplace la source des entrees (test automatique, rejeu).</summary>
        public void OverrideBackend(IInputBackend b)
        {
            Backend?.Disable(); Backend = b;
            if (isActiveAndEnabled) Backend.Enable();
            Debug.Log("[MGSUGE] Entrees : " + Backend.Name);
        }
        void OnEnable() { Backend?.Enable(); }
        void OnDisable() { Backend?.Disable(); }

        public Vector2 MoveV => locked ? Vector2.zero : Vector2.ClampMagnitude(Backend.Move(), 1f);
        public Vector2 LookDelta => locked ? Vector2.zero : Backend.LookDelta();
        public Vector2 Arrows => locked ? Vector2.zero : Backend.Arrows();
        public bool Held(Cmd c) => !locked && Backend.Held(c);
        public bool Pressed(Cmd c) => !locked && Backend.Pressed(c);
        /// <summary>Ignore le verrouillage (menus).</summary>
        public bool PressedRaw(Cmd c) => Backend.Pressed(c);
        public bool TestKey(int n) => Backend.TestKey(n);
        public bool Clicked() => Backend.Clicked();
    }

    /// <summary>Repli sur l'ancien Input Manager (Edit > Project Settings > Player > Active Input Handling = Both ou Input Manager).</summary>
    public class LegacyInputBackend : IInputBackend
    {
        public string Name => "ancien Input Manager (installez le package Input System pour le jeu final)";
        bool failed;
        static KeyCode[] K(Cmd c)
        {
            switch (c)
            {
                case Cmd.Jump: return new[] { KeyCode.Space };
                case Cmd.Down: return new[] { KeyCode.LeftControl, KeyCode.RightControl };
                case Cmd.Boost: return new[] { KeyCode.LeftShift, KeyCode.RightShift };
                case Cmd.QuickBoost: return new[] { KeyCode.E };
                case Cmd.AssaultBoost: return new[] { KeyCode.R };
                case Cmd.Transform: return new[] { KeyCode.T };
                case Cmd.Fire: return new[] { KeyCode.Mouse0 };
                case Cmd.Missiles: return new[] { KeyCode.Mouse1 };
                case Cmd.Salvo: return new[] { KeyCode.C };
                case Cmd.Frag: return new[] { KeyCode.V };
                case Cmd.Blade: return new[] { KeyCode.F };
                case Cmd.NextTarget: return new[] { KeyCode.X };
                case Cmd.Repair: return new[] { KeyCode.H };
                case Cmd.Arcane: return new[] { KeyCode.G };
                case Cmd.Domain: return new[] { KeyCode.B };
                case Cmd.Pause: return new[] { KeyCode.Escape, KeyCode.P };
                case Cmd.Upgrades: return new[] { KeyCode.U };
                case Cmd.Commands: return new[] { KeyCode.K };
                case Cmd.MapZoom: return new[] { KeyCode.M };
                default: return new[] { KeyCode.N };
            }
        }
        T Safe<T>(Func<T> f, T fallback)
        {
            if (failed) return fallback;
            try { return f(); }
            catch (InvalidOperationException)
            {
                failed = true;
                Debug.LogError("[MGSUGE] Aucune entree disponible : installez le package Input System (Window > Package Manager > Unity Registry > Input System).");
                return fallback;
            }
        }
        static bool Any(KeyCode[] ks, Func<KeyCode, bool> f) { foreach (var k in ks) if (f(k)) return true; return false; }
        public void Enable() { }
        public void Disable() { }
        public Vector2 Move() => Safe(() =>
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
        }, Vector2.zero);
        // l'axe "Mouse X/Y" de l'Input Manager vaut environ 0,1 x le deplacement en pixels
        public Vector2 LookDelta() => Safe(() => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f, Vector2.zero);
        public Vector2 Arrows() => Safe(() => new Vector2((Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
                                                          (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f)), Vector2.zero);
        public bool Held(Cmd c) => Safe(() => Any(K(c), Input.GetKey), false);
        public bool Pressed(Cmd c) => Safe(() => Any(K(c), Input.GetKeyDown), false);
        public bool TestKey(int n) => Safe(() => Input.GetKeyDown(KeyCode.F1 + (n - 1)), false);
        public bool Clicked() => Safe(() => Input.GetMouseButtonDown(0), false);
    }
}
