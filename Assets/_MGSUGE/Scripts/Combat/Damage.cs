using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    public struct DamageInfo
    {
        public float amount;
        public float impact;
        public Vector3 point;
        public Vector3 direction;   // direction d'ou vient le coup (vers la victime)
        public bool fromPlayer;
        public Object source;
    }

    public interface IDamageable { void TakeDamage(DamageInfo d); }

    /// <summary>Chiffres de degats flottants (§16.2).</summary>
    public static class DamageNumbers
    {
        public struct Entry { public Vector3 pos; public float value; public float time; public bool overload; }
        public static readonly List<Entry> List = new List<Entry>();
        public const float Life = 0.9f;
        public static void Add(Vector3 p, float v, bool overload)
        {
            List.Add(new Entry { pos = p, value = v, time = Time.time, overload = overload });
            if (List.Count > 60) List.RemoveAt(0);
        }
    }
}
