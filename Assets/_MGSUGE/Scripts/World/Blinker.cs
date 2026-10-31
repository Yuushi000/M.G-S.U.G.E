using UnityEngine;

namespace MGSUGE
{
    /// <summary>Feu clignotant (antennes, tours des avant-postes) : allume puis eteint le rendu.</summary>
    public class Blinker : MonoBehaviour
    {
        [Tooltip("rendu a faire clignoter (par defaut celui de cet objet)")] public Renderer target;
        [Tooltip("periode du clignotement (s)")] [Min(0.05f)] public float period = 1.6f;
        [Tooltip("duree allumee par periode (s)")] [Min(0f)] public float onTime = 0.5f;
        [Tooltip("decalage de phase (s)")] public float phase;
        void Reset() { target = GetComponent<Renderer>(); }
        void Update()
        {
            if (target == null) return;
            target.enabled = Mathf.Repeat(Time.time + phase, period) < onTime;
        }
    }
}
