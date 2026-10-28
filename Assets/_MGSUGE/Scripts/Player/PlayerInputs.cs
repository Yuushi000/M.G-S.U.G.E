using UnityEngine;
using UnityEngine.InputSystem;

namespace MGSUGE
{
    /// <summary>
    /// Commandes du cahier §8 avec le nouvel Input System. Deplacements selon la POSITION physique des touches
    /// (ZQSD sur AZERTY = WASD sur QWERTY), autres touches selon la LETTRE imprimee (#(x)).
    /// Les actions sont creees par code et peuvent etre reaffectees (rebinding) plus tard.
    /// </summary>
    public class PlayerInputs : MonoBehaviour
    {
        public InputAction Move, Look, ArrowLook, Jump, Down, Boost, QuickBoost, AssaultBoost, TransformKey,
            Fire, Missiles, Salvo, Frag, Blade, NextTarget, Repair, Arcane, Domain, Pause, Upgrades, Commands, MapZoom, Mute;
        [Tooltip("desactive toutes les commandes de jeu (menus, mort, Rayon Arcane)")] public bool locked;

        void Awake()
        {
            Move = new InputAction("Deplacement", InputActionType.Value);
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Look = new InputAction("Viser", InputActionType.Value, "<Mouse>/delta");
            ArrowLook = new InputAction("Fleches", InputActionType.Value);
            ArrowLook.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Jump = Btn("Saut boost / monter", "<Keyboard>/space");
            Down = Btn("Saut boost vers le bas / descendre", "<Keyboard>/ctrl");
            Boost = Btn("Boost / postcombustion", "<Keyboard>/shift");
            QuickBoost = Btn("Quick boost / tonneau", "<Keyboard>/#(e)");
            AssaultBoost = Btn("Assault boost", "<Keyboard>/#(r)");
            TransformKey = Btn("Changer de forme", "<Keyboard>/#(t)");
            Fire = Btn("Fusil a impulsions", "<Mouse>/leftButton");
            Missiles = Btn("Missiles multi-cibles", "<Mouse>/rightButton");
            Salvo = Btn("Salve de missiles", "<Keyboard>/#(c)");
            Frag = Btn("Missile a fragmentation", "<Keyboard>/#(v)");
            Blade = Btn("Lame a plasma", "<Keyboard>/#(f)");
            NextTarget = Btn("Changer de cible", "<Keyboard>/#(x)");
            Repair = Btn("Kit de reparation", "<Keyboard>/#(h)");
            Arcane = Btn("Rayon Arcane", "<Keyboard>/#(g)");
            Domain = Btn("Extension du territoire", "<Keyboard>/#(b)");
            Pause = Btn("Menu et pause", "<Keyboard>/escape"); Pause.AddBinding("<Keyboard>/#(p)");
            Upgrades = Btn("Ameliorations", "<Keyboard>/#(u)");
            Commands = Btn("Commandes", "<Keyboard>/#(k)");
            MapZoom = Btn("Zoom de la mini-carte", "<Keyboard>/#(m)");
            Mute = Btn("Couper le son", "<Keyboard>/#(n)");
        }
        static InputAction Btn(string name, string binding) => new InputAction(name, InputActionType.Button, binding);
        InputAction[] All => new[] { Move, Look, ArrowLook, Jump, Down, Boost, QuickBoost, AssaultBoost, TransformKey, Fire, Missiles, Salvo, Frag, Blade, NextTarget, Repair, Arcane, Domain, Pause, Upgrades, Commands, MapZoom, Mute };
        void OnEnable() { foreach (var a in All) a?.Enable(); }
        void OnDisable() { foreach (var a in All) a?.Disable(); }
        void OnDestroy() { foreach (var a in All) a?.Dispose(); }

        // aides de lecture (renvoient neutre si les commandes sont bloquees)
        public Vector2 MoveV => locked ? Vector2.zero : Vector2.ClampMagnitude(Move.ReadValue<Vector2>(), 1f);
        public Vector2 LookDelta => locked ? Vector2.zero : Look.ReadValue<Vector2>();
        public Vector2 Arrows => locked ? Vector2.zero : ArrowLook.ReadValue<Vector2>();
        public bool Held(InputAction a) => !locked && a.IsPressed();
        public bool Pressed(InputAction a) => !locked && a.WasPressedThisFrame();
    }
}
