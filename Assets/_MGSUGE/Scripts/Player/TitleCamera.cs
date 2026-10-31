using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Camera de presentation de l'ecran titre (§4.4) : tourne lentement autour du chassis pose sur la plateforme
    /// (distance 30 m, FOV 50), decalee pour laisser le texte a gauche. S'execute apres la CameraRig et la remplace
    /// tant que le jeu est dans l'etat Title.
    /// </summary>
    [DefaultExecutionOrder(150)]
    [AddComponentMenu("MGSUGE/Camera de l'ecran titre")]
    public class TitleCamera : MonoBehaviour
    {
        [Tooltip("chassis presente")] public PlayerController player;
        [Tooltip("camera pilotee")] public Camera cam;
        [Tooltip("camera de jeu, resynchronisee a la sortie de l'ecran titre")] public CameraRig rig;
        [Header("Cadrage (§4.4)")]
        [Tooltip("distance au chassis (m)")] [Range(5f, 120f)] public float distance = 30f;
        [Tooltip("champ de vision (degres)")] [Range(20f, 100f)] public float fov = 50f;
        [Tooltip("hauteur du point vise au-dessus du centre du chassis (m)")] [Range(-10f, 20f)] public float lookHeight = 1.5f;
        [Tooltip("hauteur de la camera au-dessus du point vise (m)")] [Range(-20f, 40f)] public float height = 4f;
        [Tooltip("decalage lateral du point vise (m) : le chassis apparait a droite, le texte reste lisible a gauche")] [Range(-30f, 30f)] public float sideOffset = -9f;
        [Tooltip("vitesse de rotation autour du chassis (degres/s)")] [Range(-60f, 60f)] public float orbitSpeed = 6f;
        [Tooltip("angle de depart (degres, 0 = face au chassis)")] public float startAngle = 200f;

        float angle; bool wasTitle;

        void Start() { angle = startAngle; }

        void LateUpdate()
        {
            if (player == null || cam == null) return;
            bool title = GameManager.I != null && GameManager.I.State == GameState.Title;
            if (!title)
            {
                if (wasTitle && rig != null) rig.Snap(player.transform.eulerAngles.y, 0f);
                wasTitle = false; return;
            }
            wasTitle = true;
            angle += orbitSpeed * Time.unscaledDeltaTime;
            Vector3 c = player.transform.position + Vector3.up * lookHeight;
            Quaternion q = Quaternion.Euler(0f, player.transform.eulerAngles.y + angle, 0f);
            Vector3 back = q * Vector3.back, right = q * Vector3.right;
            Vector3 aim = c + right * sideOffset;
            Vector3 pos = c + back * distance + Vector3.up * height + right * sideOffset;
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(aim - pos, Vector3.up));
            cam.fieldOfView = fov;
        }
    }
}
