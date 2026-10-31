using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Obus de mortier et attaques de zone telegraphiees (§10.4, araignee de siege) : trajectoire en cloche, telegraphe au sol
    /// qui se remplit pendant le vol, explosion avec degats decroissants de 100 % au centre a 40 % au bord (§10.1).
    /// Les obus et telegraphes sont des prefabs copies au lancement.
    /// </summary>
    public class ShellSystem : MonoBehaviour
    {
        public static ShellSystem I { get; private set; }
        [Tooltip("prefab d'un obus")] public GameObject shellPrefab;
        [Tooltip("prefab du telegraphe au sol")] public GameObject telegraphPrefab;
        [Tooltip("nombre d'obus simultanes maximum")] [Min(1)] public int poolSize = 32;
        [Tooltip("hauteur de la cloche = distance x ce facteur")] public float arcFactor = 0.3f;
        [Tooltip("hauteur minimale de la cloche (m)")] public float minArc = 40f;
        [Tooltip("parent des copies (par defaut cet objet)")] public Transform poolParent;

        class S { public bool active; public Vector3 from, to; public float t, T, damage, radius, edge, arc; public Transform shell; public Telegraph tel; }
        readonly List<S> pool = new List<S>();
        public int ActiveCount { get; private set; }
        public int Exploded { get; private set; }

        void Awake()
        {
            I = this;
            var parent = poolParent != null ? poolParent : transform;
            for (int i = 0; i < poolSize; i++)
            {
                var s = new S();
                if (shellPrefab != null) { var go = Instantiate(shellPrefab, parent); go.name = shellPrefab.name; go.SetActive(false); s.shell = go.transform; }
                if (telegraphPrefab != null) { var go = Instantiate(telegraphPrefab, parent); go.name = telegraphPrefab.name; s.tel = go.GetComponent<Telegraph>(); go.SetActive(false); }
                pool.Add(s);
            }
        }

        public bool Launch(Vector3 from, Vector3 to, float flightTime, float damage, float radius, float edgeFactor)
        {
            S s = null;
            foreach (var x in pool) if (!x.active) { s = x; break; }
            if (s == null) return false;
            s.active = true; s.from = from; s.to = to; s.t = 0f; s.T = Mathf.Max(0.1f, flightTime);
            s.damage = damage; s.radius = radius; s.edge = edgeFactor;
            s.arc = Mathf.Max(minArc, Vector3.Distance(from, to) * arcFactor);
            if (s.shell != null) { s.shell.position = from; s.shell.gameObject.SetActive(true); }
            if (s.tel != null) s.tel.Show(to, radius);
            return true;
        }

        void Update()
        {
            int n = 0;
            float dt = GameClock.WorldDt;
            foreach (var s in pool)
            {
                if (!s.active) continue;
                n++;
                s.t += dt;
                float k = Mathf.Clamp01(s.t / s.T);
                Vector3 p = Vector3.Lerp(s.from, s.to, k) + Vector3.up * s.arc * 4f * k * (1f - k);
                if (s.shell != null) s.shell.position = p;
                if (s.tel != null) s.tel.SetProgress(k, s.radius);
                if (k >= 1f) Explode(s);
            }
            ActiveCount = n;
        }

        void Explode(S s)
        {
            s.active = false; Exploded++;
            if (s.shell != null) s.shell.gameObject.SetActive(false);
            if (s.tel != null) s.tel.Hide();
            Fx.Explosion(s.to, s.radius * 1.2f, false); Sfx.Play("boom", s.to);
            var pl = PlayerController.I;
            if (pl == null || pl.dead) return;
            // distance au segment pieds -> bassin du chassis (un obus aux pieds touche a 100 %)
            float d = MathX.SegmentDistance(pl.Center - Vector3.up * pl.Clear, pl.Center, s.to);
            if (d < s.radius)
                pl.TakeDamage(new DamageInfo { amount = s.damage * Mathf.Lerp(1f, s.edge, d / s.radius), point = s.to, direction = (pl.Center - s.to).normalized, fromPlayer = false, source = this });
        }
    }
}
