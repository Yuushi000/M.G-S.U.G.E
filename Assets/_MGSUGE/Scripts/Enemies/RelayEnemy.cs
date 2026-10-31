using UnityEngine;

namespace MGSUGE
{
    /// <summary>Relais de brouillage (§13.1) : ne tire pas ; noyau octaedrique flottant et deux anneaux qui tournent.</summary>
    public class RelayEnemy : Enemy
    {
        [Tooltip("donnees du relais (Data/Ennemis)")] public RelayData data;
        [Header("Pieces du prefab")]
        [Tooltip("noyau qui flotte")] public Transform core;
        [Tooltip("premier anneau")] public Transform ringA;
        [Tooltip("second anneau")] public Transform ringB;
        public override EnemyData BaseData => data;
        float phase; Vector3 core0;

        protected override void Awake() { if (core != null) core0 = core.localPosition; base.Awake(); }
        protected override void OnRespawn() { phase = Random.value * 10f; }

        protected override void Tick(float dt, bool engaged)
        {
            phase += dt;
            if (core != null) core.localPosition = core0 + Vector3.up * Mathf.Sin(phase * data.bobSpeed) * data.bobAmplitude;
            if (ringA != null) ringA.Rotate(0f, data.ringSpeeds.x * dt, 0f, Space.Self);
            if (ringB != null) ringB.Rotate(0f, data.ringSpeeds.y * dt, 0f, Space.Self);
        }
    }
}
