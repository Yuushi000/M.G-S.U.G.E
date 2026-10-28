using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Cible d'entrainement du jalon 4 (ne tire pas) : tourne autour de son point d'origine pour tester l'anticipation,
    /// reapparait apres sa destruction. S'immobilise en surcharge d'impact. Les vrais ennemis arrivent au jalon 5.
    /// </summary>
    [RequireComponent(typeof(Targetable))]
    public class TargetDummy : MonoBehaviour
    {
        public Vector3 anchor;
        public float orbitRadius = 60f, orbitSpeed = 0.25f, bobAmplitude = 6f;
        public float respawnDelay = 6f;
        public bool followGround = false;
        public float groundClear = 7f;
        Targetable tg; float angle; float deadTimer = -1f;
        Renderer[] renderers;

        void Awake()
        {
            tg = GetComponent<Targetable>();
            tg.Died += OnDied;
            renderers = GetComponentsInChildren<Renderer>();
            angle = Random.value * Mathf.PI * 2f;
        }
        void OnDied(Targetable t, DamageInfo d) { deadTimer = respawnDelay; Show(false); }
        void Show(bool on) { foreach (var r in renderers) if (r != null) r.enabled = on; }

        void Update()
        {
            float dt = GameClock.WorldDt;
            if (deadTimer >= 0f)
            {
                deadTimer -= dt;
                if (deadTimer < 0f) { tg.ResetHealth(); Show(true); }
                return;
            }
            if (tg.Overloaded) { transform.position += Random.insideUnitSphere * 0.08f; return; }
            angle += orbitSpeed * dt;
            Vector3 p = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
            if (followGround) p.y = TerrainSystem.Ground(p.x, p.z) + groundClear;
            else p.y = anchor.y + Mathf.Sin(angle * 2.3f) * bobAmplitude;
            Vector3 v = p - transform.position;
            transform.position = p;
            if (v.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(v.x, 0f, v.z).normalized + Vector3.forward * 1e-4f), MathX.Damp(4f, dt));
        }
    }
}
