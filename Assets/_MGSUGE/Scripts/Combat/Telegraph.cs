using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Telegraphe d'une attaque de zone (§10.4) : cercle rouge-orange au sol qui se remplit jusqu'a l'instant de l'impact.
    /// Prefab MGSUGE_Telegraphe : un anneau (bord) et un disque (remplissage), tous deux de rayon 1.
    /// </summary>
    public class Telegraph : MonoBehaviour
    {
        [Tooltip("anneau du bord (rayon 1, mis a l'echelle du rayon d'explosion)")] public Transform ring;
        [Tooltip("disque de remplissage (rayon 1)")] public Transform fill;
        [Tooltip("hauteur au-dessus du sol (m)")] public float lift = 0.6f;

        public void Show(Vector3 groundPoint, float radius)
        {
            transform.position = groundPoint + Vector3.up * lift;
            if (ring != null) ring.localScale = new Vector3(radius, 1f, radius);
            SetProgress(0f, radius);
            gameObject.SetActive(true);
        }
        public void SetProgress(float k, float radius)
        {
            if (fill != null) fill.localScale = new Vector3(radius * Mathf.Clamp01(k), 1f, radius * Mathf.Clamp01(k));
        }
        public void Hide() { gameObject.SetActive(false); }
    }
}
