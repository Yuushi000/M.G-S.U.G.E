using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Camera maison (cahier §7.5) : epaule en chassis (lacet + tangage limite), poursuite libre en vaisseau (quaternion),
    /// champ de vision dynamique, secousses, point vise par rayon, jamais sous le sol (+2,5 m).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraRig : MonoBehaviour
    {
        public CameraData data;
        public PlayerController player;
        public Camera cam;

        float yaw, pitch;               // radians ; pitch > 0 = vers le haut
        float dist; float shake; float fov;
        float relevelT = 1f; Quaternion relevelFrom;
        Vector3 shipCamPos; bool shipInit;
        public Vector3 AimPoint { get; private set; }
        public bool AimHit { get; private set; }
        public float YawDeg => yaw * Mathf.Rad2Deg;
        public float Pitch => pitch;
        public Quaternion LookRotation => Quaternion.Euler(-pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0f);

        void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            dist = data.mechDistance; fov = data.fov;
            cam.fieldOfView = data.fov; cam.nearClipPlane = data.near; cam.farClipPlane = data.far;
        }
        public void Shake(float a) { shake = Mathf.Max(shake, a); }
        public void Snap(float yawDeg, float pitchRad) { yaw = yawDeg * Mathf.Deg2Rad; pitch = pitchRad; shipInit = false; }
        public void OnTransform(FrameForm to) { if (to == FrameForm.Ship) { shipInit = false; } }
        public void Relevel()
        {
            Vector3 f = player.ShipRotation * Vector3.forward;
            yaw = Mathf.Atan2(f.x, f.z);
            pitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)), data.pitchMin, data.pitchMax);
            relevelFrom = transform.rotation; relevelT = 0f;
        }

        void LateUpdate()
        {
            if (player == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, GameClock.MaxDelta);
            bool playing = GameManager.I == null || GameManager.I.State == GameState.Play;
            if (Time.timeScale <= 0f) dt = 0f;
            var input = player.input;
            if (playing && !player.IsShip && !player.dead)
            {
                Vector2 d = input.LookDelta; Vector2 ar = input.Arrows;
                yaw += d.x * data.mouseSensitivity + ar.x * data.arrowYaw * dt;
                pitch += d.y * data.mouseSensitivity + ar.y * data.arrowPitch * dt;
                pitch = Mathf.Clamp(pitch, data.pitchMin, data.pitchMax);
            }

            Vector3 center = player.transform.position;
            // --- camera chassis
            float speedExtra = Mathf.Clamp01(player.velocity.magnitude / Mathf.Max(1f, player.S.assault)) * data.mechSpeedExtra;
            float wantDist = data.mechDistance + (player.assault ? data.mechAssaultExtra : 0f) + speedExtra;
            dist = Mathf.Lerp(dist, wantDist, MathX.Damp(4f, dt));
            Quaternion R = LookRotation;
            Vector3 pivot = center + Vector3.up * data.mechLookHeight;
            Vector3 shoulder = pivot + R * new Vector3(data.mechShoulder, 0f, 0f);
            Vector3 mechPos = shoulder + R * new Vector3(0f, data.mechElevation - data.mechLookHeight, -dist);
            if (Physics.SphereCast(shoulder, 1.2f, (mechPos - shoulder).normalized, out RaycastHit h, Vector3.Distance(shoulder, mechPos), Layers.WorldMask, QueryTriggerInteraction.Ignore))
                mechPos = h.point + h.normal * 1.5f;
            Quaternion mechRot = R;
            if (relevelT < 1f) { relevelT += dt / data.relevelTime; mechRot = Quaternion.Slerp(relevelFrom, R, MathX.SmoothStep01(relevelT)); }

            // --- camera vaisseau
            Quaternion sr = player.ShipRotation;
            Vector3 shipTarget = center + sr * new Vector3(0f, data.shipElevation, -data.shipDistance);
            if (!shipInit) { shipCamPos = Vector3.Lerp(transform.position, shipTarget, 0.5f); shipInit = true; }
            shipCamPos = Vector3.Lerp(shipCamPos, shipTarget, MathX.Damp(12f, dt));
            shipCamPos += player.velocity * dt * (1f - MathX.Damp(12f, dt)) * 0.9f; // compense la vitesse (pas de trainee excessive a 440 m/s)
            Vector3 look = center + sr * new Vector3(0f, data.shipLookHeight, 0f);
            Quaternion shipRot = Quaternion.LookRotation((look - shipCamPos).normalized, sr * Vector3.up);

            float b = player.shipBlend;
            Vector3 pos = Vector3.Lerp(mechPos, shipCamPos, b);
            Quaternion rot = Quaternion.Slerp(mechRot, shipRot, b);
            if (b < 0.01f) shipInit = false;

            // jamais sous le sol
            float gh = TerrainSystem.Ground(pos.x, pos.z) + data.groundClearance;
            if (pos.y < gh) pos.y = gh;
            // secousses
            if (shake > 0f)
            {
                pos += Random.insideUnitSphere * shake * 0.8f;
                rot = rot * Quaternion.Euler(Random.Range(-1f, 1f) * shake * 0.8f, Random.Range(-1f, 1f) * shake * 0.8f, 0f);
                shake = Mathf.Max(0f, shake - data.shakeDecay * dt);
            }
            transform.SetPositionAndRotation(pos, rot);
            fov = Mathf.Lerp(fov, data.fov + player.FovKick, MathX.Damp(6f, dt));
            cam.fieldOfView = fov;

            // point vise : rayon depuis la camera (30 -> 1200 m)
            Vector3 fwd = transform.forward;
            Vector3 start = pos + fwd * data.aimMin;
            if (Physics.Raycast(start, fwd, out RaycastHit ah, data.aimMax - data.aimMin, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            { AimPoint = ah.point; AimHit = true; }
            else { AimPoint = pos + fwd * data.aimMax; AimHit = false; }
        }
    }
}
