using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Patrouilles errantes (§13.2) : toutes les 16 s (premiere apres 8 s), s'il y a moins de 8 ennemis libres, un groupe
    /// apparait a 800-1 200 m du joueur (jamais a moins de 500 m de la plateforme Helios) : 40 % de chance de 2 chasseurs
    /// (160-260 m au-dessus du sol), sinon 3 sentinelles (50-120 m). Au-dessus de 3 000 m : toujours 2 chasseurs.
    /// Les ennemis libres a plus de 3 000 m du joueur sont supprimes. Les ennemis crees sont ranges sous « Parent ».
    /// </summary>
    public class PatrolSpawner : MonoBehaviour
    {
        [Tooltip("frequences, distances et compositions (asset MGSUGE_Patrouilles)")] public PatrolData data;
        [Tooltip("joueur autour duquel les patrouilles apparaissent")] public PlayerController player;
        [Tooltip("prefab de sentinelle")] public GameObject sentinelPrefab;
        [Tooltip("prefab de chasseur")] public GameObject fighterPrefab;
        [Tooltip("objet de la Hierarchy qui recoit les ennemis crees (Enemies/Patrouilles)")] public Transform spawnParent;
        [Tooltip("plateforme Helios (zone protegee) ; vide = position Helios des donnees du monde")] public Transform helios;
        [Tooltip("donnees du monde (limite jouable, Helios)")] public WorldData world;

        readonly List<Enemy> spawned = new List<Enemy>();
        float timer = -1f;
        public int FreeCount { get { Clean(); int n = 0; foreach (var e in spawned) if (e.IsAlive) n++; return n; } }
        public IReadOnlyList<Enemy> Spawned { get { Clean(); return spawned; } }
        Vector3 HeliosPos => helios != null ? helios.position : (world != null ? world.heliosPosition : Vector3.zero);

        void Start() { timer = data != null ? data.firstDelay : 8f; }

        void Update()
        {
            if (data == null || player == null) return;
            if (GameManager.I != null && GameManager.I.State == GameState.Title) return; // pas de patrouille sur l'ecran titre
            float dt = GameClock.WorldDt;
            timer -= dt;
            if (timer <= 0f) { timer = data.interval; TrySpawn(); }
            // suppression des ennemis libres trop eloignes
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                var e = spawned[i];
                if (e == null) { spawned.RemoveAt(i); continue; }
                if (Vector3.Distance(e.transform.position, player.Center) > data.despawnDistance) { Destroy(e.gameObject); spawned.RemoveAt(i); }
            }
        }

        void Clean() { spawned.RemoveAll(e => e == null); }

        /// <summary>Tente une apparition (meme regles que le minuteur).</summary>
        public bool TrySpawn()
        {
            if (player == null || player.dead || data == null) return false;
            if (FreeCount >= data.maxFree) return false;
            Vector3 c = player.Center;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(data.spawnDistance.x, data.spawnDistance.y);
                Vector3 p = new Vector3(c.x + Mathf.Cos(a) * r, 0f, c.z + Mathf.Sin(a) * r);
                if (world != null) { float lim = world.playableLimit - 100f; if (Mathf.Abs(p.x) > lim || Mathf.Abs(p.z) > lim) continue; }
                Vector3 h = HeliosPos;
                if (new Vector2(p.x - h.x, p.z - h.z).magnitude < data.heliosClearance) continue;
                SpawnGroupAt(p);
                return true;
            }
            return false;
        }

        /// <summary>Fait apparaitre un groupe a la position horizontale donnee (la hauteur est calculee selon le type).</summary>
        public void SpawnGroupAt(Vector3 p)
        {
            bool high = player.Center.y > data.highAltitude;
            bool fighters = high || Random.value < data.fighterChance;
            var prefab = fighters ? fighterPrefab : sentinelPrefab;
            if (prefab == null) return;
            int n = fighters ? data.fighterGroup : data.sentinelGroup;
            float ground = TerrainSystem.Ground(p.x, p.z);
            Vector3 face = player.Center - p; face.y = 0f;
            Quaternion rot = face.sqrMagnitude > 1f ? Quaternion.LookRotation(face) : Quaternion.identity;
            for (int i = 0; i < n; i++)
            {
                Vector3 q = p + rot * new Vector3((i - (n - 1) * 0.5f) * data.groupSpacing, 0f, -Mathf.Abs(i - (n - 1) * 0.5f) * data.groupSpacing * 0.5f);
                float gy = TerrainSystem.Ground(q.x, q.z);
                q.y = high ? player.Center.y + Random.Range(-data.highAltitudeSpread, data.highAltitudeSpread)
                           : Mathf.Max(ground, gy) + Random.Range(fighters ? data.fighterHeight.x : data.sentinelHeight.x, fighters ? data.fighterHeight.y : data.sentinelHeight.y);
                var go = Instantiate(prefab, q, rot, spawnParent);
                var e = go.GetComponent<Enemy>();
                if (e != null) { e.roaming = true; e.contractEnemy = false; spawned.Add(e); }
            }
        }

        /// <summary>Supprime les ennemis libres a moins de r metres du point c (redeploiement, §4.4).</summary>
        public int ClearNear(Vector3 c, float r)
        {
            int n = 0;
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                var e = spawned[i];
                if (e == null) { spawned.RemoveAt(i); continue; }
                if (Vector3.Distance(e.transform.position, c) < r) { Destroy(e.gameObject); spawned.RemoveAt(i); n++; }
            }
            return n;
        }
    }
}
