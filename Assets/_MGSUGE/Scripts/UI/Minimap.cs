using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGSUGE
{
    /// <summary>
    /// Mini-carte (bas gauche, §16.1) : disque de 210 px (140 px sur petit ecran) oriente dans le sens de la camera, fond issu
    /// de la carte du monde pre-calculee (Art/Generated/MGSUGE_Carte_Monde.png), 2 cercles de repere, legendes « Portee N m »
    /// et « M · zoom ». Zoom 600 / 1 800 / 4 500 m (touche M). Affiche les ennemis, boss, objectifs et avant-postes.
    /// Les points sont des copies du prefab MGSUGE_UI_PointCarte creees au lancement (reserve).
    /// </summary>
    [AddComponentMenu("MGSUGE/UI/Mini-carte")]
    public class Minimap : MonoBehaviour
    {
        [Tooltip("racine du HUD")] public HudController hud;
        [Header("Elements")]
        [Tooltip("RectTransform du disque (taille = diametre)")] public RectTransform disc;
        [Tooltip("carte du monde (tourne avec la camera)")] public RawImage map;
        [Tooltip("parent des points (ennemis, objectifs...)")] public RectTransform blipParent;
        [Tooltip("prefab d'un point de la carte")] public Image blipPrefab;
        [Tooltip("fleche du joueur au centre")] public RectTransform playerArrow;
        [Tooltip("legende « Portee N m »")] public Text rangeText;
        [Tooltip("legende « M · zoom »")] public Text zoomText;
        [Header("Reglages")]
        [Tooltip("nombre de points en reserve")] [Range(16, 400)] public int poolSize = 120;
        [Tooltip("taille d'un ennemi (px)")] [Range(2f, 20f)] public float enemySize = 5f;
        [Tooltip("taille d'un boss (px)")] [Range(2f, 30f)] public float bossSize = 10f;
        [Tooltip("taille d'un avant-poste (px)")] [Range(2f, 30f)] public float outpostSize = 8f;
        [Tooltip("taille du losange d'objectif (px)")] [Range(2f, 30f)] public float objectiveSize = 11f;
        [Tooltip("sprite carre (ennemis, avant-postes)")] public Sprite squareSprite;
        [Tooltip("sprite losange (objectifs)")] public Sprite diamondSprite;
        [Tooltip("sprite rond (boss)")] public Sprite dotSprite;
        [Tooltip("legende de zoom")] public string zoomLegend = "M · zoom";
        [Tooltip("format de la portee ; {0} = metres")] public string rangeFormat = "Portée {0} m";

        int zoomIndex; readonly List<Image> pool = new List<Image>(); int used;
        float Range => hud != null && hud.data != null && hud.data.minimapZooms.Length > 0 ? hud.data.minimapZooms[Mathf.Clamp(zoomIndex, 0, hud.data.minimapZooms.Length - 1)] : 600f;
        public float CurrentRange => Range;

        void Start()
        {
            if (blipPrefab == null || blipParent == null) return;
            for (int i = 0; i < poolSize; i++)
            {
                var b = Instantiate(blipPrefab, blipParent); b.name = blipPrefab.name;
                b.gameObject.SetActive(false); pool.Add(b);
            }
        }

        public void CycleZoom()
        {
            int n = hud != null && hud.data != null ? hud.data.minimapZooms.Length : 1;
            zoomIndex = (zoomIndex + 1) % Mathf.Max(1, n);
        }

        void Update()
        {
            if (hud == null || hud.player == null || hud.data == null || hud.cam == null || !hud.Visible) return;
            var d = hud.data; var p = hud.player;
            var gm = GameManager.I;
            if ((gm == null || gm.State == GameState.Play) && p.input != null && p.input.PressedRaw(Cmd.MapZoom)) CycleZoom();

            float D = Screen.height < d.smallScreenHeight ? d.minimapSmallDiameter : d.minimapDiameter;
            var rt = (RectTransform)transform;
            if (rt.sizeDelta.x != D) rt.sizeDelta = new Vector2(D, D);
            float R = D * 0.5f, range = Range;
            UiFx.SetText(rangeText, string.Format(rangeFormat, UiFx.N(range)));
            UiFx.SetText(zoomText, zoomLegend);

            // orientation : le haut du disque = la direction de la camera
            Vector3 cf = hud.cam.transform.forward; cf.y = 0f;
            if (cf.sqrMagnitude < 1e-4f) cf = hud.cam.transform.up; cf.y = 0f; cf.Normalize();
            Vector3 cr = new Vector3(cf.z, 0f, -cf.x);
            float yaw = Mathf.Atan2(cf.x, cf.z) * Mathf.Rad2Deg;
            Vector3 c = p.Center;
            if (map != null)
            {
                float size = hud.world != null ? hud.world.worldSize : 15000f;
                float span = map.rectTransform.rect.width / Mathf.Max(1f, R) * range; // metres couverts par la largeur de l'image
                map.uvRect = new Rect((c.x + size * 0.5f - span * 0.5f) / size, (c.z + size * 0.5f - span * 0.5f) / size, span / size, span / size);
                map.rectTransform.localRotation = Quaternion.Euler(0f, 0f, yaw);
            }
            if (playerArrow != null)
            {
                Vector3 pf = p.IsShip ? p.ShipRotation * Vector3.forward : p.transform.forward; pf.y = 0f;
                float rel = pf.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(cf, pf, Vector3.up) : 0f;
                playerArrow.localRotation = Quaternion.Euler(0f, 0f, -rel);
            }

            used = 0;
            float k = R / range;
            // avant-postes
            foreach (var op in Outpost.All)
            {
                if (op == null) continue;
                bool hostile = op.Active && op.AliveCount() > 0;
                Place(op.transform.position, c, cf, cr, k, R, squareSprite, outpostSize, hostile ? d.threat : UiFx.A(d.amber, 0.55f), 45f, false);
            }
            // ennemis et boss
            foreach (var t in Targetable.All)
            {
                if (!t.Alive || !t.lockable) continue;
                if (t.isBoss) Place(t.Position, c, cf, cr, k, R, dotSprite, bossSize, d.violet, 0f, true);
                else Place(t.Position, c, cf, cr, k, R, squareSprite, enemySize, d.threat, 0f, false);
            }
            // objectif du contrat (ramene au bord du disque)
            var cm = ContractManager.I;
            if (cm != null && cm.Current != null && !cm.Waiting)
            {
                var e = cm.Current.NearestAlive(c);
                Place(e != null ? e.transform.position : cm.Current.LocationPoint, c, cf, cr, k, R, diamondSprite, objectiveSize, d.amber, 0f, true);
            }
            for (int i = used; i < pool.Count; i++) if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
        }

        void Place(Vector3 world, Vector3 c, Vector3 cf, Vector3 cr, float k, float R, Sprite sprite, float size, Color col, float rot, bool clampToEdge)
        {
            if (used >= pool.Count) return;
            Vector3 dlt = world - c;
            Vector2 pos = new Vector2(Vector3.Dot(dlt, cr), Vector3.Dot(dlt, cf)) * k;
            float lim = R - size * 0.5f - 2f;
            if (pos.magnitude > lim)
            {
                if (!clampToEdge) return;
                pos = pos.normalized * lim;
            }
            var b = pool[used++];
            if (!b.gameObject.activeSelf) b.gameObject.SetActive(true);
            if (b.sprite != sprite) b.sprite = sprite;
            b.color = col;
            var rt = b.rectTransform;
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(size, size); rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}
