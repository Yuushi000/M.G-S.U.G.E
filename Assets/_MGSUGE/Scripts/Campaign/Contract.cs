using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Un contrat dans la scene (§13.4). Ses ennemis sont ses objets enfants (prefabs d'ennemis, desactives tant que le contrat
    /// n'est pas en cours) : on peut les deplacer, les dupliquer ou en supprimer dans la Hierarchy. Le gestionnaire de
    /// contrats (ContractManager) les active, les reinitialise (PV x menace) et compte les destructions.
    /// </summary>
    public class Contract : MonoBehaviour
    {
        [Tooltip("textes et recompense (Data/Contrats)")] public ContractData data;
        [Tooltip("point vise par le marqueur d'objectif quand plus aucun ennemi n'est visible ; vide = cet objet")] public Transform location;
        [Tooltip("ennemis du contrat ; vide = tous les ennemis enfants de cet objet (recommande)")] public List<Enemy> enemies = new List<Enemy>();

        readonly List<Enemy> list = new List<Enemy>();
        public IReadOnlyList<Enemy> Enemies { get { Collect(); return list; } }
        public string Title => data != null ? data.title : name;
        public Vector3 LocationPoint => location != null ? location.position : transform.position;

        void Collect()
        {
            if (list.Count > 0) return;
            if (enemies != null && enemies.Count > 0) { foreach (var e in enemies) if (e != null) list.Add(e); }
            else list.AddRange(GetComponentsInChildren<Enemy>(true));
        }

        /// <summary>Masque tous les ennemis (contrat pas encore commence).</summary>
        public void Hide()
        {
            Collect();
            foreach (var e in list) if (e != null) e.gameObject.SetActive(false);
        }

        /// <summary>Demarre le contrat : chaque ennemi reapparait a sa place, PV x menace, declenchement a 950 m.</summary>
        public void Begin()
        {
            Collect();
            foreach (var e in list)
            {
                if (e == null) continue;
                e.contractEnemy = true;
                e.gameObject.SetActive(true);
                e.Respawn();
            }
        }

        public int Total { get { Collect(); return list.Count; } }
        public int Alive { get { Collect(); int n = 0; foreach (var e in list) if (e != null && e.IsAlive) n++; return n; } }
        public int Killed => Total - Alive;

        public Enemy NearestAlive(Vector3 from)
        {
            Collect();
            Enemy best = null; float bd = float.MaxValue;
            foreach (var e in list)
            {
                if (e == null || !e.IsAlive) continue;
                float d = (e.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }
    }
}
