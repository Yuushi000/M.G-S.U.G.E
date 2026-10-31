using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Effets visuels de base (§17.2) : un nuage de particules additif unique (etincelles, poussiere, explosions),
    /// eclairs lumineux, rayons (sondes laser). Le systeme de particules est un objet enfant de la scene (FX/Particules),
    /// les eclairs et rayons sont des prefabs copies au lancement. Remplacable par VFX Graph au jalon 10.
    /// </summary>
    public class FxSystem : MonoBehaviour
    {
        public static FxSystem I { get; private set; }
        [Tooltip("systeme de particules partage (objet FX/Particules de la scene)")] public ParticleSystem particles;
        [Tooltip("prefab d'eclair (sphere additive)")] public GameObject flashPrefab;
        [Tooltip("prefab de rayon (LineRenderer additif)")] public GameObject beamPrefab;
        [Tooltip("nombre maximal d'eclairs simultanes")] [Min(1)] public int maxFlashes = 48;
        [Tooltip("nombre maximal de rayons simultanes")] [Min(1)] public int maxBeams = 64;
        [Tooltip("parent des copies (par defaut cet objet)")] public Transform poolParent;

        class Flash { public Transform t; public Renderer r; public float time, life, size; public Color color; }
        class Beam { public LineRenderer lr; public float time, life; public Color color; public float width; }
        readonly List<Flash> flashes = new List<Flash>();
        readonly List<Beam> beams = new List<Beam>();
        MaterialPropertyBlock mpb;

        void Awake()
        {
            I = this;
            mpb = new MaterialPropertyBlock();
            if (particles == null) Debug.LogError("[MGSUGE] FxSystem : systeme de particules non assigne.");
            else if (!particles.isPlaying) particles.Play();
        }
        Transform Parent => poolParent != null ? poolParent : transform;

        public void Emit(Vector3 pos, Vector3 vel, float size, float life, Color color)
        {
            if (particles == null) return;
            var ep = new ParticleSystem.EmitParams { position = pos, velocity = vel, startSize = size, startLifetime = life, startColor = color, applyShapeToPosition = false };
            particles.Emit(ep, 1);
        }

        public void SpawnFlash(Vector3 p, float size, Color c, float life)
        {
            Flash f = null;
            foreach (var x in flashes) if (!x.t.gameObject.activeSelf) { f = x; break; }
            if (f == null)
            {
                if (flashes.Count >= maxFlashes || flashPrefab == null) return;
                var go = Instantiate(flashPrefab, Parent); go.name = flashPrefab.name;
                f = new Flash { t = go.transform, r = go.GetComponentInChildren<Renderer>() }; flashes.Add(f);
            }
            f.t.gameObject.SetActive(true); f.t.position = p; f.time = Time.time; f.life = life; f.size = size; f.color = c;
        }

        public void SpawnBeam(Vector3 a, Vector3 b, Color c, float width, float life)
        {
            Beam bm = null;
            foreach (var x in beams) if (!x.lr.gameObject.activeSelf) { bm = x; break; }
            if (bm == null)
            {
                if (beams.Count >= maxBeams || beamPrefab == null) return;
                var go = Instantiate(beamPrefab, Parent); go.name = beamPrefab.name;
                var lr = go.GetComponent<LineRenderer>(); lr.positionCount = 2; lr.useWorldSpace = true;
                bm = new Beam { lr = lr }; beams.Add(bm);
            }
            bm.lr.gameObject.SetActive(true); bm.lr.SetPosition(0, a); bm.lr.SetPosition(1, b);
            bm.time = Time.time; bm.life = life; bm.color = c; bm.width = width;
        }

        void Update()
        {
            float now = Time.time;
            foreach (var f in flashes)
            {
                if (!f.t.gameObject.activeSelf) continue;
                float k = (now - f.time) / f.life;
                if (k >= 1f) { f.t.gameObject.SetActive(false); continue; }
                f.t.localScale = Vector3.one * f.size * Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(k));
                f.r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", f.color * (1f - k) * 3f);
                f.r.SetPropertyBlock(mpb);
            }
            foreach (var b in beams)
            {
                if (!b.lr.gameObject.activeSelf) continue;
                float k = (now - b.time) / b.life;
                if (k >= 1f) { b.lr.gameObject.SetActive(false); continue; }
                Color c = b.color * 3f; c.a = 1f - k;
                b.lr.startColor = c; b.lr.endColor = c;
                b.lr.startWidth = b.lr.endWidth = b.width * (1f - k * 0.5f);
            }
        }
    }

    /// <summary>Acces simplifie aux effets.</summary>
    public static class Fx
    {
        static readonly Color Orange = new Color(1f, 0.55f, 0.18f), Rose = new Color(1f, 0.31f, 0.85f), DustC = new Color(0.55f, 0.42f, 0.38f);
        public static void Explosion(Vector3 p, float size, bool big)
        {
            var I = FxSystem.I; if (I == null) return;
            I.SpawnFlash(p, size, Orange, big ? 0.7f : 0.4f);
            int n = Mathf.Clamp((int)(size * (big ? 6f : 3f)), 10, 260);
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Random.onUnitSphere;
                I.Emit(p + d * size * 0.1f, d * size * Random.Range(1.2f, 3.5f), Random.Range(0.6f, 1.6f) * Mathf.Sqrt(size), Random.Range(0.4f, 1.1f),
                       Color.Lerp(Orange, Color.white, Random.value * 0.4f));
            }
            for (int i = 0; i < n / 3; i++) // debris sombres et anneau de souffle
            {
                Vector3 d = Random.onUnitSphere; d.y *= 0.25f;
                I.Emit(p, d.normalized * size * 4f, Random.Range(0.4f, 1f) * Mathf.Sqrt(size), 0.6f, new Color(0.9f, 0.4f, 0.2f, 0.6f));
            }
        }
        public static void Flash(Vector3 p, float size, Color c) { FxSystem.I?.SpawnFlash(p, size, c, 0.35f); }
        public static void Sparks(Vector3 p, float amount)
        {
            var I = FxSystem.I; if (I == null) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(amount * 8f));
            for (int i = 0; i < n; i++)
                I.Emit(p, Random.onUnitSphere * Random.Range(10f, 40f) + Vector3.up * 8f, Random.Range(0.2f, 0.5f), Random.Range(0.2f, 0.5f), Orange);
        }
        public static void Dust(Vector3 p, float radius)
        {
            var I = FxSystem.I; if (I == null) return;
            int n = Mathf.Clamp((int)(radius * 2f), 8, 60);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 d = new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a));
                I.Emit(p + d * 1.5f, d * radius * Random.Range(0.8f, 1.4f), Random.Range(2f, 4f), Random.Range(0.6f, 1.1f), new Color(DustC.r, DustC.g, DustC.b, 0.35f));
            }
        }
        public static void Hit(Vector3 p, Color c)
        {
            var I = FxSystem.I; if (I == null) return;
            I.SpawnFlash(p, 2.5f, c, 0.12f);
            for (int i = 0; i < 6; i++) I.Emit(p, Random.onUnitSphere * 25f, 0.5f, 0.25f, c);
        }
        public static void Beam(Vector3 a, Vector3 b, Color c, float width = 0.35f, float life = 0.08f) { FxSystem.I?.SpawnBeam(a, b, c, width, life); }
        public static void Spiral(Vector3 center, float t)
        {
            var I = FxSystem.I; if (I == null) return;
            for (int i = 0; i < 4; i++)
            {
                float a = t * 14f + i * Mathf.PI * 0.5f;
                Vector3 off = new Vector3(Mathf.Cos(a), (t - 0.5f) * 2f, Mathf.Sin(a)) * 7f;
                I.Emit(center + off, Vector3.up * 3f, 0.8f, 0.5f, new Color(0.55f, 0.85f, 1f, 0.8f));
            }
        }
        public static Color PlayerPink => Rose;
    }
}
