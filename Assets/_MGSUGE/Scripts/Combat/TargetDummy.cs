using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Cible d'entrainement de la scene Test (ne tire pas) : tourne autour de sa position de depart pour tester l'anticipation,
    /// reapparait apres sa destruction. S'immobilise en surcharge d'impact.
    /// </summary>
    [RequireComponent(typeof(Targetable))]
    public class TargetDummy : MonoBehaviour
    {
        [Tooltip("centre de l'orbite ; si « Ancre au depart » est coche, c'est la position de l'objet au lancement")] public Vector3 anchor;
        [Tooltip("utilise la position de l'objet dans la scene comme centre de l'orbite")] public bool anchorAtStart = true;
        [Tooltip("rayon de l'orbite (m) ; ~0 = immobile")] public float orbitRadius = 60f;
        [Tooltip("vitesse angulaire (rad/s), negative = sens inverse")] public float orbitSpeed = 0.25f;
        [Tooltip("amplitude du balancement vertical (m)")] public float bobAmplitude = 6f;
        [Tooltip("delai de reapparition apres destruction (s)")] public float respawnDelay = 6f;
        [Tooltip("suit le sol (chassis qui marche)")] public bool followGround = false;
        [Tooltip("hauteur du centre au-dessus du sol si « suit le sol »")] public float groundClear = 7f;
        Targetable tg; float angle; float deadTimer = -1f;
        Renderer[] renderers;

        void Awake()
        {
            tg = GetComponent<Targetable>();
            tg.Died += OnDied;
            renderers = GetComponentsInChildren<Renderer>();
            angle = Random.value * Mathf.PI * 2f;
            if (anchorAtStart) anchor = transform.position;
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
