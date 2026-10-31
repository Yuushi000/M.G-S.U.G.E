using UnityEngine;

namespace MGSUGE
{
    /// <summary>Garde un objet centre sur la camera (astres du ciel : planete, lune, etoiles, toujours « a l'infini »).</summary>
    [DefaultExecutionOrder(200)]
    public class FollowCamera : MonoBehaviour
    {
        [Tooltip("camera suivie (par defaut la camera principale)")] public Camera target;
        void LateUpdate()
        {
            if (target == null) target = Camera.main;
            if (target != null) transform.position = target.transform.position;
        }
    }
}
