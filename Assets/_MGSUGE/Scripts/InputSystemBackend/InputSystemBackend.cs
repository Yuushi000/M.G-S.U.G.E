using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MGSUGE
{
    /// <summary>
    /// Entrees avec le package Input System (cahier §5.1 et §8). Cet assembly n'est compile que si le package est installe
    /// (contrainte MGSUGE_HAS_INPUT_SYSTEM dans MGSUGE.InputSystem.asmdef) ; il s'enregistre alors tout seul.
    /// Deplacements par position physique (&lt;Keyboard&gt;/w = Z sur AZERTY), autres touches par lettre imprimee (#(x)).
    /// </summary>
    public class InputSystemBackend : IInputBackend
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() { PlayerInputs.NewInputSystemFactory = () => new InputSystemBackend(); }

        public string Name => "Input System";
        readonly InputAction move, look, arrows;
        readonly Dictionary<Cmd, InputAction> cmds = new Dictionary<Cmd, InputAction>();

        public InputSystemBackend()
        {
            move = new InputAction("Deplacement", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = new InputAction("Viser", InputActionType.Value, "<Mouse>/delta");
            arrows = new InputAction("Fleches", InputActionType.Value);
            arrows.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            B(Cmd.Jump, "<Keyboard>/space"); B(Cmd.Down, "<Keyboard>/ctrl"); B(Cmd.Boost, "<Keyboard>/shift");
            B(Cmd.QuickBoost, "<Keyboard>/#(e)"); B(Cmd.AssaultBoost, "<Keyboard>/#(r)"); B(Cmd.Transform, "<Keyboard>/#(t)");
            B(Cmd.Fire, "<Mouse>/leftButton"); B(Cmd.Missiles, "<Mouse>/rightButton");
            B(Cmd.Salvo, "<Keyboard>/#(c)"); B(Cmd.Frag, "<Keyboard>/#(v)"); B(Cmd.Blade, "<Keyboard>/#(f)"); B(Cmd.NextTarget, "<Keyboard>/#(x)");
            B(Cmd.Repair, "<Keyboard>/#(h)"); B(Cmd.Arcane, "<Keyboard>/#(g)"); B(Cmd.Domain, "<Keyboard>/#(b)");
            B(Cmd.Pause, "<Keyboard>/escape"); cmds[Cmd.Pause].AddBinding("<Keyboard>/#(p)");
            B(Cmd.Upgrades, "<Keyboard>/#(u)"); B(Cmd.Commands, "<Keyboard>/#(k)"); B(Cmd.MapZoom, "<Keyboard>/#(m)"); B(Cmd.Mute, "<Keyboard>/#(n)");
        }
        void B(Cmd c, string binding) { cmds[c] = new InputAction(c.ToString(), InputActionType.Button, binding); }

        public void Enable() { move.Enable(); look.Enable(); arrows.Enable(); foreach (var a in cmds.Values) a.Enable(); }
        public void Disable() { move.Disable(); look.Disable(); arrows.Disable(); foreach (var a in cmds.Values) a.Disable(); }
        public Vector2 Move() => move.ReadValue<Vector2>();
        public Vector2 LookDelta() => look.ReadValue<Vector2>();
        public Vector2 Arrows() => arrows.ReadValue<Vector2>();
        public bool Held(Cmd c) => cmds[c].IsPressed();
        public bool Pressed(Cmd c) => cmds[c].WasPressedThisFrame();
        public bool TestKey(int n)
        {
            var kb = Keyboard.current; if (kb == null) return false;
            switch (n) { case 1: return kb.f1Key.wasPressedThisFrame; case 2: return kb.f2Key.wasPressedThisFrame; case 3: return kb.f3Key.wasPressedThisFrame; default: return kb.f4Key.wasPressedThisFrame; }
        }
        public bool Clicked() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}
