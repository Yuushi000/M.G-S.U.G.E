using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Sons provisoires synthetises au demarrage (le prototype synthetise ses sons en code, §17.1).
    /// A remplacer par de vrais clips : il suffit de remplir la liste "clips" avec des AudioClip du meme nom.
    /// Volume maitre 0,3, cadence maximale par type, son spatialise pour les explosions.
    /// </summary>
    public class SfxSystem : MonoBehaviour
    {
        public static SfxSystem I { get; private set; }
        [System.Serializable] public class NamedClip { public string name; public AudioClip clip; }
        public List<NamedClip> clips = new List<NamedClip>();
        [Range(0, 1)] public float masterVolume = 0.3f;
        public bool muted;

        readonly Dictionary<string, AudioClip> map = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        static readonly Dictionary<string, float> MinGap = new Dictionary<string, float>
        { { "shot", 0.055f }, { "hit", 0.045f }, { "probe", 0.04f }, { "small", 0.06f }, { "lock", 0.12f }, { "eshot", 0.09f }, { "missile", 0.07f }, { "beam", 0.4f } };
        readonly List<AudioSource> sources = new List<AudioSource>();

        void Awake()
        {
            I = this;
            foreach (var c in clips) if (c.clip != null) map[c.name] = c.clip;
            Synth("shot", 0.09f, (t, d) => Sq(t, Mathf.Lerp(1800f, 700f, t / d)) * Env(t, d, 0.002f) * 0.5f);
            Synth("hit", 0.04f, (t, d) => (Sq(t, 2600f) * 0.5f + Noise() * 0.5f) * Env(t, d, 0.001f) * 0.6f);
            Synth("hurt", 0.25f, (t, d) => (Mathf.Sin(t * 2 * Mathf.PI * 80f) + Noise() * 0.6f) * Env(t, d, 0.004f) * 0.8f);
            Synth("boom", 1.4f, (t, d) => (Noise() * 0.7f + Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(70f, 30f, t / d)) * 0.8f) * Mathf.Exp(-t * 3f));
            Synth("small", 0.35f, (t, d) => Noise() * Mathf.Exp(-t * 12f) * 0.8f);
            Synth("missile", 0.5f, (t, d) => (Noise() * 0.4f + Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(400f, 1400f, t / d)) * 0.3f) * Env(t, d, 0.02f));
            Synth("qb", 0.35f, (t, d) => Noise() * Env(t, d, 0.01f) * Mathf.Lerp(1f, 0.3f, t / d) * 0.7f);
            Synth("transform", 0.75f, (t, d) => (Sq(t, Mathf.Lerp(110f, 330f, t / d)) * 0.25f + Noise() * 0.15f) * Env(t, d, 0.03f));
            Synth("blade", 0.4f, (t, d) => (Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(2200f, 300f, t / d)) * 0.5f + Noise() * 0.3f) * Env(t, d, 0.005f));
            Synth("lock", 0.07f, (t, d) => Mathf.Sin(t * 2 * Mathf.PI * 1760f) * Env(t, d, 0.002f) * 0.4f);
            Synth("buy", 0.35f, (t, d) => Mathf.Sin(t * 2 * Mathf.PI * (t < 0.12f ? 880f : 1320f)) * Env(t, d, 0.005f) * 0.4f);
            Synth("alarm", 0.6f, (t, d) => Sq(t, (int)(t * 8f) % 2 == 0 ? 880f : 660f) * 0.25f * Env(t, d, 0.01f));
            Synth("probe", 0.06f, (t, d) => Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(3000f, 2200f, t / d)) * Env(t, d, 0.002f) * 0.25f);
            for (int i = 0; i < 24; i++) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; sources.Add(s); }
        }
        public void ToggleMute() { muted = !muted; AudioListener.volume = muted ? 0f : 1f; Msg.Post(muted ? "Son coupé" : "Son rétabli"); }

        delegate float Wave(float t, float dur);
        static float Sq(float t, float f) => Mathf.Repeat(t * f, 1f) < 0.5f ? 1f : -1f;
        static float Noise() => Random.value * 2f - 1f;
        static float Env(float t, float d, float a) => Mathf.Min(1f, t / Mathf.Max(a, 1e-4f)) * Mathf.Clamp01(1f - t / d);
        void Synth(string name, float dur, Wave w)
        {
            if (map.ContainsKey(name)) return;
            int rate = 22050, n = Mathf.CeilToInt(dur * rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(w(i / (float)rate, dur), -1f, 1f);
            var c = AudioClip.Create("sfx_" + name, n, 1, rate, false); c.SetData(data, 0); map[name] = c;
        }

        public void Play(string name, Vector3? pos)
        {
            if (muted || !map.TryGetValue(name, out var clip)) return;
            float now = Time.unscaledTime;
            if (MinGap.TryGetValue(name, out float gap) && lastPlay.TryGetValue(name, out float l) && now - l < gap) return;
            lastPlay[name] = now;
            AudioSource src = null;
            foreach (var s in sources) if (!s.isPlaying) { src = s; break; }
            if (src == null) return;
            float vol = masterVolume;
            if (pos.HasValue && Camera.main != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, pos.Value);
                vol *= Mathf.Clamp01(1f - dist / 2500f);   // attenuation simple (jalon 10 : AudioSource 3D)
                if (vol <= 0.01f) return;
            }
            src.PlayOneShot(clip, vol);
        }
    }

    public static class Sfx
    {
        public static void Play(string name) { SfxSystem.I?.Play(name, null); }
        public static void Play(string name, Vector3 pos) { SfxSystem.I?.Play(name, pos); }
    }
}
