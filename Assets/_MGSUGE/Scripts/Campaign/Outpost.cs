using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    public enum OutpostType { Patrouille, Aerien, Blinde, Siege, Mixte }

    /// <summary>
    /// Avant-poste hostile (§13.3). Batiment visible en permanence ; sa garnison (ennemis enfants, desactives au repos)
    /// apparait quand le joueur arrive a moins de 1 700 m (1 300 m dans l'espace) et que plus de 150 s se sont ecoulees
    /// depuis la derniere victoire ; elle disparait si le joueur s'eloigne a plus de 2 700 m. Tous detruits : +400 C.
    /// </summary>
    public class Outpost : MonoBehaviour
    {
        [Tooltip("distances, delais et recompense (asset MGSUGE_AvantPostes)")] public OutpostData data;
        [Tooltip("type de garnison (information ; la garnison est faite des enfants)")] public OutpostType type;
        [Tooltip("ennemis de la garnison ; vide = tous les ennemis enfants (recommande)")] public List<Enemy> garrison = new List<Enemy>();

        [Header("Etat (lecture seule en jeu)")]
        [SerializeField] bool active;
        [SerializeField] float lastVictory = -99999f;
        public bool Active => active;
        public float LastVictory { get => lastVictory; set => lastVictory = value; }
        public int Victories { get; private set; }

        readonly List<Enemy> list = new List<Enemy>();
        float checkT;
        /// <summary>Avant-postes actifs dans la scene (mini-carte).</summary>
        public static readonly List<Outpost> All = new List<Outpost>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Collect()
        {
            if (list.Count > 0) return;
            if (garrison != null && garrison.Count > 0) { foreach (var e in garrison) if (e != null) list.Add(e); }
            else list.AddRange(GetComponentsInChildren<Enemy>(true));
        }
        public IReadOnlyList<Enemy> Garrison { get { Collect(); return list; } }

        void Start()
        {
            Collect();
            foreach (var e in list) if (e != null) e.gameObject.SetActive(false);
            active = false;
            checkT = Random.value * (data != null ? data.checkInterval : 0.25f);
        }

        void Update()
        {
            if (data == null) return;
            checkT -= Time.deltaTime;
            if (checkT > 0f) return;
            checkT = data.checkInterval;
            var p = PlayerController.I; if (p == null) return;
            float d = Vector3.Distance(transform.position, p.Center);
            if (!active)
            {
                bool space = p.Center.y > data.spaceAltitude;
                if (!p.dead && d < (space ? data.activationDistanceSpace : data.activationDistance) && Time.time - lastVictory > data.refreshDelay) Activate();
            }
            else if (d > data.deactivationDistance) Deactivate();
            else if (AliveCount() == 0) Victory();
        }

        public int AliveCount() { Collect(); int n = 0; foreach (var e in list) if (e != null && e.IsAlive) n++; return n; }

        public void Activate()
        {
            Collect(); active = true;
            foreach (var e in list) { if (e == null) continue; e.contractEnemy = false; e.gameObject.SetActive(true); e.Respawn(); }
        }
        public void Deactivate()
        {
            Collect(); active = false;
            foreach (var e in list) if (e != null) e.gameObject.SetActive(false);
        }
        void Victory()
        {
            active = false; lastVictory = Time.time; Victories++;
            var p = PlayerController.I;
            if (p != null) { p.progress.credits += data.reward; HudCredits.Gain(data.reward); }
            Msg.Post(string.Format(data.victoryMessage, MathX.Credits(data.reward)), MsgKind.Good);
            Sfx.Play("buy");
        }
    }
}
