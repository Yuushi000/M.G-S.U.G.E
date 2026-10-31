using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Lave (§6.4) : plan de 2 450 x 6 200 m a -16 m (objet « Lave » de la scene, avec sa lumiere orange en enfant).
    /// En jeu, fait glisser la croute sombre (texture de bruit du materiau MGSUGE_Lave).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class LavaSurface : MonoBehaviour
    {
        [Tooltip("vitesse de defilement de la croute (UV/s)")] public Vector2 flow = new Vector2(0f, 0.012f);
        [Tooltip("ondulation laterale (amplitude UV, frequence rad/s)")] public Vector2 wobble = new Vector2(0.2f, 0.03f);
        Material inst; Vector2 offset0;

        void Start()
        {
            inst = GetComponent<Renderer>().material;
            if (inst.HasProperty("_BaseMap")) offset0 = inst.GetTextureOffset("_BaseMap");
        }
        void Update()
        {
            if (inst == null || !inst.HasProperty("_BaseMap")) return;
            float t = Time.time;
            inst.SetTextureOffset("_BaseMap", offset0 + new Vector2(Mathf.Sin(t * wobble.y) * wobble.x + flow.x * t, flow.y * t));
        }
    }
}
