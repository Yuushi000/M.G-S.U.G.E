using UnityEngine;

namespace MGSUGE
{
    /// <summary>Fait tourner l'objet en continu (gyroscopes, halo, anneaux des relais...).</summary>
    public class Rotator : MonoBehaviour
    {
        [Tooltip("axe de rotation en coordonnees locales")] public Vector3 axis = Vector3.up;
        [Tooltip("vitesse en degres par seconde (negatif = sens inverse)")] public float degreesPerSecond = 10f;
        [Tooltip("suit l'horloge du monde (ralentie pendant le Rayon Arcane)")] public bool worldClock = true;
        void Update()
        {
            float dt = worldClock ? GameClock.WorldDt : Time.deltaTime;
            transform.Rotate(axis, degreesPerSecond * dt, Space.Self);
        }
    }
}
