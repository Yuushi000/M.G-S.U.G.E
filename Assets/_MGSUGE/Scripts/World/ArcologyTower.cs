using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Tour de l'arcologie (prefab MGSUGE_Tour) : regler Largeur et Hauteur dans l'Inspector replace le corps, les liseres,
    /// les bandeaux de fenetres, l'antenne, le feu rouge et la collision.
    /// </summary>
    [ExecuteAlways]
    public class ArcologyTower : MonoBehaviour
    {
        [Header("Dimensions (§6.2)")]
        [Tooltip("cote de la tour (m)")] [Min(4f)] public float width = 32f;
        [Tooltip("hauteur de la tour (m)")] [Min(10f)] public float height = 200f;
        [Tooltip("ecart vertical entre deux bandeaux de fenetres (m)")] [Min(5f)] public float windowSpacing = 40f;
        [Tooltip("materiau lumineux des liseres et fenetres (orange ou rose)")] public Material accent;

        [Header("Pieces du prefab")]
        public Transform body;
        public Transform[] edges = new Transform[4];
        public Transform[] windowBands = new Transform[0];
        public Transform antenna;
        public BoxCollider box;

        void OnEnable() { Layout(); }
        void OnValidate() { Layout(); }

        [ContextMenu("Recalculer la tour")]
        public void Layout()
        {
            if (body != null) body.localScale = new Vector3(width, height, width);
            for (int k = 0; k < edges.Length; k++)
            {
                var e = edges[k]; if (e == null) continue;
                float a = k * 90f * Mathf.Deg2Rad;
                e.localPosition = new Vector3(Mathf.Cos(a) * width * 0.5f, height * 0.04f, Mathf.Sin(a) * width * 0.5f);
                e.localScale = new Vector3(1.2f, height * 0.92f, 1.2f);
                SetAccent(e);
            }
            for (int f = 0; f < windowBands.Length; f++)
            {
                var w = windowBands[f]; if (w == null) continue;
                float y = (f + 1) * windowSpacing;
                w.localPosition = new Vector3(0f, y, 0f);
                w.localScale = new Vector3(width + 0.6f, 2.2f, width + 0.6f);
                var r = w.GetComponent<Renderer>(); if (r != null) r.enabled = y < height - 6f;
                SetAccent(w);
            }
            if (antenna != null) antenna.localPosition = new Vector3(0f, height, 0f);
            if (box != null) { box.center = new Vector3(0f, height * 0.5f, 0f); box.size = new Vector3(width, height, width); }
        }
        void SetAccent(Transform t)
        {
            if (accent == null) return;
            var r = t.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != accent) r.sharedMaterial = accent;
        }
    }
}
