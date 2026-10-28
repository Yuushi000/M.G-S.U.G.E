using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Terrain d'essai du jalon 4 : cibles d'entrainement autour de la plateforme Helios
    /// (sentinelles volantes, relais immobiles, chassis "Corbeau" en palette hostile qui marchent).
    /// A supprimer quand les vrais ennemis et contrats seront en place (jalon 5).
    /// </summary>
    public class TestRange : MonoBehaviour
    {
        public WeaponData weaponData;
        public WorldData world;
        public Material sentinelMaterial, relayMaterial, relayRingMaterial;
        [Tooltip("prefab du modele de chassis (meme FBX que le joueur)")] public GameObject frameModel;
        public PaletteData hostilePalette;
        public PoseLibrary poses;
        [Tooltip("centre du bassin -> sol")] public float frameClear = 7f;
        public int sentinels = 8, relays = 2, frames = 2;

        void Start()
        {
            Vector3 c = world.heliosPosition;
            var oct = MeshKit.Octahedron(3.8f, 1.3f);
            for (int i = 0; i < sentinels; i++)
            {
                float a = i * Mathf.PI * 2f / sentinels;
                Vector3 anchor = new Vector3(c.x + Mathf.Cos(a) * 300f, 170f + (i % 3) * 50f, c.z + 560f + Mathf.Sin(a) * 160f);   // au-dela des tours de l'arcologie
                var go = MeshKit.Spawn($"Sentinelle_Essai_{i + 1}", oct, sentinelMaterial, transform, anchor, Quaternion.identity, false, Layers.Enemy);
                MeshKit.Spawn("Anneau", MeshKit.Torus(4.6f, 0.35f, 24, 4), relayRingMaterial, go.transform, Vector3.zero, Quaternion.identity);
                var t = go.AddComponent<Targetable>();
                Setup(t, "Sentinelle d'essai", 650f, 3.8f, 0f, 120);
                var d = go.AddComponent<TargetDummy>(); d.anchor = anchor; d.orbitRadius = 40f + i * 9f; d.orbitSpeed = (i % 2 == 0 ? 1f : -1f) * (0.25f + i * 0.05f);
            }
            for (int i = 0; i < relays; i++)
            {
                Vector3 anchor = new Vector3(c.x + (i == 0 ? -220f : 220f), 70f, c.z + 560f);
                var go = MeshKit.Spawn($"Relais_Essai_{i + 1}", MeshKit.Octahedron(7f, 1.4f), relayMaterial, transform, anchor, Quaternion.identity, false, Layers.Enemy);
                MeshKit.Spawn("Anneau_1", MeshKit.Torus(11f, 0.6f, 32, 4), relayRingMaterial, go.transform, Vector3.zero, Quaternion.Euler(20f, 0f, 0f));
                MeshKit.Spawn("Anneau_2", MeshKit.Torus(13f, 0.6f, 32, 4), relayRingMaterial, go.transform, Vector3.zero, Quaternion.Euler(-35f, 40f, 0f));
                var t = go.AddComponent<Targetable>();
                Setup(t, "Relais d'essai", 5200f, 7f, 3800f, 400);
                var d = go.AddComponent<TargetDummy>(); d.anchor = anchor; d.orbitRadius = 0.01f; d.orbitSpeed = 0.5f; d.bobAmplitude = 3f;
            }
            if (frameModel != null)
                for (int i = 0; i < frames; i++)
                {
                    Vector3 anchor = new Vector3(c.x + (i == 0 ? -150f : 170f), 0f, c.z + 560f + i * 110f);
                    var go = new GameObject($"Corbeau_Essai_{i + 1}"); go.layer = Layers.Enemy;
                    go.transform.SetParent(transform, false); go.transform.position = anchor;
                    var model = Instantiate(frameModel, go.transform);
                    model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
                    foreach (var tr in model.GetComponentsInChildren<Transform>(true)) if (tr.name.StartsWith("Flame_") || tr.name == "Blade_Energy") tr.gameObject.SetActive(false);
                    var nose = MathX.FindDeep(model.transform, "Nose"); if (nose != null) nose.localScale = Vector3.zero;
                    var pa = go.AddComponent<PaletteApplier>(); pa.palette = hostilePalette;
                    if (poses != null) { var rig = go.AddComponent<FrameRig>(); rig.poses = poses; rig.model = model.transform; rig.Init(); }
                    var t = go.AddComponent<Targetable>();
                    Setup(t, "Châssis hostile « Corbeau » (essai)", 4200f, 5f, 2400f, 900);
                    var d = go.AddComponent<TargetDummy>(); d.anchor = anchor; d.orbitRadius = 70f; d.orbitSpeed = 0.35f * (i == 0 ? 1f : -1f);
                    d.followGround = true; d.groundClear = frameClear;
                }
        }
        void Setup(Targetable t, string name, float hp, float r, float impact, long credits)
        {
            t.displayName = name; t.maxHP = hp; t.HP = hp; t.radius = r; t.impactMax = impact; t.credits = credits; t.weaponData = weaponData;
        }
    }
}
