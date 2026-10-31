using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Une tuile du terrain (64 x 64 cellules). L'objet existe dans la scene ; son maillage d'affichage et de collision est
    /// recalcule depuis la heightmap a l'ouverture (edition et jeu) au lieu d'etre stocke dans le projet (64 maillages = ~50 Mo).
    /// Deplacer la tuile deplace son affichage, pas la hauteur du sol utilisee par le gameplay (qui vient de la heightmap).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainTile : MonoBehaviour
    {
        [Tooltip("terrain parent (heightmap et materiau)")] public TerrainSystem terrain;
        [Tooltip("indice de la tuile en X (0 = ouest)")] public int tileX;
        [Tooltip("indice de la tuile en Z (0 = sud)")] public int tileZ;
        Mesh render, collision;

        void OnEnable() { Rebuild(); }
        void OnDisable() { Free(); }

        [ContextMenu("Recalculer la tuile")]
        public void Rebuild()
        {
            if (terrain == null) return;
            terrain.EnsureLoaded();
            if (terrain.Field == null) return;
            Free();
            terrain.Field.BuildTile(tileX, tileZ, terrain.chunkCells, out render, out collision);
            render.hideFlags = HideFlags.DontSave; collision.hideFlags = HideFlags.DontSave;
            GetComponent<MeshFilter>().sharedMesh = render;
            var mr = GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == null) mr.sharedMaterial = terrain.material;
            GetComponent<MeshCollider>().sharedMesh = collision;
        }

        void Free()
        {
            if (render != null) DestroyImmediate(render);
            if (collision != null) DestroyImmediate(collision);
            render = collision = null;
        }
    }
}
