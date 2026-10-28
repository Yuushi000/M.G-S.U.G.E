using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Verrouillage de cible (§9.3) : cible la plus proche du centre de l'ecran dans un cone de 0,2 rad (0,3 boss) et a portee
    /// (700 m chassis, 900 m vaisseau, +700 m boss) ; score = angle + distance/portee x 0,15.
    /// Perte : cible morte ou cachee, angle > 0,55 rad, distance > 115 % ; X : cible suivante dans un cone de 0,8 rad.
    /// </summary>
    public class LockOn : MonoBehaviour
    {
        public WeaponData data;
        public PlayerController player;
        public Targetable Target { get; private set; }
        float losTimer; bool losOk = true;

        public float Range(Targetable t) => (player.IsShip ? data.lockRangeShip : data.lockRangeMech) + (t != null && t.isBoss ? data.lockRangeBossBonus : 0f);

        void Update()
        {
            if (player == null || player.cam == null || player.dead) { Target = null; return; }
            if (GameManager.I != null && GameManager.I.State != GameState.Play) return;
            Transform ct = player.cam.transform;
            Vector3 cp = ct.position, cf = ct.forward;

            if (Target != null)
            {
                bool lose = !Target.Alive || !Target.lockable;
                if (!lose)
                {
                    Vector3 d = Target.Position - cp;
                    float ang = Vector3.Angle(cf, d) * Mathf.Deg2Rad;
                    float dist = Vector3.Distance(player.Center, Target.Position);
                    if (ang > data.lockLoseAngle || dist > Range(Target) * data.lockLoseRange) lose = true;
                    losTimer -= Time.deltaTime;
                    if (losTimer <= 0f) { losTimer = 0.2f; losOk = !Physics.Linecast(cp, Target.Position, Layers.WorldMask, QueryTriggerInteraction.Ignore); }
                    if (!losOk) lose = true;
                }
                if (lose) Target = null;
            }
            if (Target == null) Acquire(cp, cf);
            if (player.input.Pressed(player.input.NextTarget)) Cycle(cp, cf);
        }

        void Acquire(Vector3 cp, Vector3 cf)
        {
            Targetable best = null; float bestScore = float.MaxValue;
            foreach (var t in Targetable.All)
            {
                if (!t.Alive || !t.lockable) continue;
                float range = Range(t);
                float dist = Vector3.Distance(player.Center, t.Position);
                if (dist > range) continue;
                float ang = Vector3.Angle(cf, t.Position - cp) * Mathf.Deg2Rad;
                if (ang > (t.isBoss ? data.lockConeBoss : data.lockCone)) continue;
                float score = ang + dist / range * 0.15f;
                if (score < bestScore && !Physics.Linecast(cp, t.Position, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { bestScore = score; best = t; }
            }
            if (best != null) { Target = best; losOk = true; losTimer = 0.2f; Sfx.Play("lock"); }
        }

        /// <summary>X : passe a la cible suivante (seule facon de cibler un as des flottes, meme non hostile).</summary>
        void Cycle(Vector3 cp, Vector3 cf)
        {
            var list = new List<Targetable>();
            foreach (var t in Targetable.All)
            {
                if (!t.Alive) continue;
                if (Vector3.Distance(player.Center, t.Position) > Range(t) * data.lockLoseRange) continue;
                if (Vector3.Angle(cf, t.Position - cp) * Mathf.Deg2Rad > data.switchCone) continue;
                list.Add(t);
            }
            if (list.Count == 0) return;
            list.Sort((a, b) => Vector3.Angle(cf, a.Position - cp).CompareTo(Vector3.Angle(cf, b.Position - cp)));
            int i = Target != null ? list.IndexOf(Target) : -1;
            Target = list[(i + 1) % list.Count]; losOk = true; Sfx.Play("lock");
        }

        /// <summary>Cibles visables par les missiles : la cible verrouillee d'abord, puis jusqu'a N cibles dans le cone.</summary>
        public List<Targetable> MissileTargets(int max, float cone, float range)
        {
            var res = new List<Targetable>();
            if (Target != null && Target.Alive) res.Add(Target);
            Transform ct = player.cam.transform;
            var cands = new List<Targetable>();
            foreach (var t in Targetable.All)
            {
                if (!t.Alive || !t.missileTarget || !t.lockable || res.Contains(t)) continue;
                if (Vector3.Distance(player.Center, t.Position) > range) continue;
                if (Vector3.Angle(ct.forward, t.Position - ct.position) * Mathf.Deg2Rad > cone) continue;
                cands.Add(t);
            }
            cands.Sort((a, b) => Vector3.Angle(ct.forward, a.Position - ct.position).CompareTo(Vector3.Angle(ct.forward, b.Position - ct.position)));
            foreach (var c in cands) { if (res.Count >= max) break; res.Add(c); }
            return res;
        }
    }
}
