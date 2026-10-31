using UnityEngine;

namespace MGSUGE
{
    /// <summary>Emission pulsee (balise de la fleche, liseres) via un MaterialPropertyBlock.</summary>
    public class EmissionPulse : MonoBehaviour
    {
        [Tooltip("rendu concerne (par defaut celui de cet objet)")] public Renderer target;
        [ColorUsage(false, true)] public Color color = new Color(1f, 0.55f, 0.15f);
        [Tooltip("intensite mini et maxi de l'emission")] public Vector2 intensity = new Vector2(2f, 9f);
        [Tooltip("vitesse du battement (rad/s)")] public float speed = 2.4f;
        MaterialPropertyBlock mpb;
        void Reset() { target = GetComponent<Renderer>(); }
        void Update()
        {
            if (target == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            target.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", color * Mathf.Lerp(intensity.x, intensity.y, k));
            target.SetPropertyBlock(mpb);
        }
    }
}
