using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>Horloge de jeu. Le prototype plafonne le pas de temps a 1/30 s ; le Rayon Arcane ralentit le monde (pas le joueur).</summary>
    public static class GameClock
    {
        public const float MaxDelta = 1f / 30f;
        /// <summary>Facteur applique aux ennemis et projectiles ennemis (0,15 pendant le Rayon Arcane).</summary>
        public static float WorldScale = 1f;
        public static float PlayerDt => Mathf.Min(Time.deltaTime, MaxDelta);
        public static float WorldDt => Mathf.Min(Time.deltaTime, MaxDelta) * WorldScale;
    }

    public enum MsgKind { Normal, Warning, Danger, Good }

    /// <summary>Fil de messages (haut gauche) et grand message central.</summary>
    public static class Msg
    {
        public struct Entry { public string text; public MsgKind kind; public float time; }
        /// <summary>Duree d'un message (s) et nombre maximal de lignes : recopies depuis l'asset Interface par le HUD.</summary>
        public static float Duration = 3.8f;
        public static int MaxLines = 5;
        /// <summary>Bornes de duree du grand message (s), recopiees depuis l'asset Interface.</summary>
        public static float BigMin = 2.5f, BigMax = 5f;
        public static readonly List<Entry> Feed = new List<Entry>();
        public static string BigTitle, BigSub;
        public static float BigTime = -99f, BigDuration = 3f;
        static readonly Dictionary<string, float> last = new Dictionary<string, float>();

        /// <summary>Ajoute un message. throttle : intervalle minimal (s) entre deux messages identiques.</summary>
        public static void Post(string text, MsgKind kind = MsgKind.Normal, float throttle = 0f)
        {
            float now = Time.unscaledTime;
            if (throttle > 0f && last.TryGetValue(text, out float t) && now - t < throttle) return;
            last[text] = now;
            Feed.Add(new Entry { text = text, kind = kind, time = now });
            while (Feed.Count > MaxLines) Feed.RemoveAt(0);
        }
        public static void Big(string title, string sub = "", float duration = 3f)
        {
            BigTitle = title; BigSub = sub; BigTime = Time.unscaledTime; BigDuration = Mathf.Clamp(duration, BigMin, BigMax);
        }
        public static void Clear() { Feed.Clear(); last.Clear(); BigTime = -99f; }
    }

    public static class MathX
    {
        /// <summary>Facteur de rapprochement exponentiel independant du framerate (taux en 1/s).</summary>
        public static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
        public static float SmoothStep01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        /// <summary>Distance d'un point au segment [a,b].</summary>
        public static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a; float l2 = ab.sqrMagnitude;
            if (l2 < 1e-8f) return Vector3.Distance(a, p);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
            return Vector3.Distance(a + ab * t, p);
        }
        public static float SegmentT(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a; float l2 = ab.sqrMagnitude;
            return l2 < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        }
        /// <summary>Format des credits : separateur de milliers francais + " C".</summary>
        public static string Credits(long c)
        {
            string s = c.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
            return s.Replace(' ', ' ').Replace(' ', ' ') + " C";
        }
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }

    /// <summary>Couches physiques creees par le constructeur de projet (menu MGSUGE).</summary>
    public static class Layers
    {
        public const int Player = 8, World = 9, Enemy = 10;
        /// <summary>Tout ce qui arrete un tir ou un rayon : decor et sol.</summary>
        public static int WorldMask => (1 << 0) | (1 << World);
    }
}
