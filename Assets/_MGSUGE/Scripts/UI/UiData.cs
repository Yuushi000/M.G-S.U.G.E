using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Reglages partages de l'interface (cahier §4.5 et §16) : couleurs, polices, durees des messages, mini-carte, animations.
    /// Un seul asset : Data/MGSUGE_Interface (clic droit > Create > MGSUGE > Donnees > Interface).
    /// </summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Interface", fileName = "MGSUGE_Interface")]
    public class UiData : ScriptableObject
    {
        [Header("Couleurs (§4.5)")]
        [Tooltip("fond / vide #0B0610")] public Color background = new Color32(0x0B, 0x06, 0x10, 0xFF);
        [Tooltip("texte principal « bone » #F1E8DA")] public Color bone = new Color32(0xF1, 0xE8, 0xDA, 0xFF);
        [Tooltip("opacite de la version attenuee du texte")] [Range(0f, 1f)] public float boneDim = 0.62f;
        [Tooltip("opacite de la version tres faible du texte")] [Range(0f, 1f)] public float boneFaint = 0.20f;
        [Tooltip("accent « amber » #FFA733 : objectifs, energie, avertissements")] public Color amber = new Color32(0xFF, 0xA7, 0x33, 0xFF);
        [Tooltip("menace « threat » #FF4458 : danger, degats, boss")] public Color threat = new Color32(0xFF, 0x44, 0x58, 0xFF);
        [Tooltip("glace « ice » #86D8FF : bonne nouvelle, vaisseau, bouclier")] public Color ice = new Color32(0x86, 0xD8, 0xFF, 0xFF);
        [Tooltip("rose laser #FF4FD8 : armes du joueur, lame, Rayon Arcane")] public Color rose = new Color32(0xFF, 0x4F, 0xD8, 0xFF);
        [Tooltip("violet #C07BFF : nom des boss")] public Color violet = new Color32(0xC0, 0x7B, 0xFF, 0xFF);
        [Tooltip("or divin / reincarnation #FFD27A")] public Color gold = new Color32(0xFF, 0xD2, 0x7A, 0xFF);
        [Tooltip("violet divin #B44DFF")] public Color divineViolet = new Color32(0xB4, 0x4D, 0xFF, 0xFF);
        [Tooltip("sondes #FFD84A")] public Color probe = new Color32(0xFF, 0xD8, 0x4A, 0xFF);

        [Header("Polices (§4.5)")]
        [Tooltip("titres : Saira Extra Condensed 400")] public Font titleRegular;
        [Tooltip("titres : Saira Extra Condensed 600")] public Font titleSemiBold;
        [Tooltip("titres : Saira Extra Condensed 800")] public Font titleExtraBold;
        [Tooltip("texte : Saira")] public Font text;
        [Tooltip("texte appuye : Saira 600")] public Font textSemiBold;
        [Tooltip("donnees chiffrees : IBM Plex Mono")] public Font mono;

        [Header("Fil de messages (§16.1)")]
        [Tooltip("nombre maximal de messages affiches")] [Range(1, 10)] public int maxMessages = 5;
        [Tooltip("duree d'affichage d'un message (s)")] [Range(1f, 10f)] public float messageDuration = 3.8f;
        [Tooltip("duree de l'apparition par glissement (s)")] [Range(0f, 1f)] public float messageSlideTime = 0.18f;
        [Tooltip("distance du glissement (px a 1080p)")] [Range(0f, 120f)] public float messageSlideDistance = 30f;
        [Tooltip("duree du fondu de disparition (s)")] [Range(0f, 2f)] public float messageFadeTime = 0.5f;

        [Header("Grand message (§16.1)")]
        [Tooltip("duree minimale (s)")] [Range(1f, 10f)] public float bigMinDuration = 2.5f;
        [Tooltip("duree maximale (s)")] [Range(1f, 10f)] public float bigMaxDuration = 5f;
        [Tooltip("espacement des lettres au debut de l'apparition (px)")] [Range(0f, 80f)] public float bigSpacingFrom = 34f;
        [Tooltip("espacement des lettres final (px)")] [Range(0f, 40f)] public float bigSpacingTo = 10f;
        [Tooltip("duree de l'apparition par espacement de lettres (s)")] [Range(0.05f, 2f)] public float bigAppearTime = 0.7f;
        [Tooltip("duree du fondu de sortie (s)")] [Range(0.05f, 2f)] public float bigFadeTime = 0.6f;

        [Header("Credits (§11.1)")]
        [Tooltip("duree du « +N C » sous le compteur (s)")] [Range(0.2f, 5f)] public float creditGainDuration = 1.25f;
        [Tooltip("vitesse du glissement du compteur (1/s)")] [Range(1f, 30f)] public float creditCounterRate = 8f;

        [Header("Mini-carte (§16.1)")]
        [Tooltip("diametre du disque (px a 1080p)")] [Range(80f, 400f)] public float minimapDiameter = 210f;
        [Tooltip("diametre sur petit ecran (px a 1080p)")] [Range(60f, 400f)] public float minimapSmallDiameter = 140f;
        [Tooltip("en dessous de cette hauteur d'ecran (px reels), le disque passe au petit diametre")] [Min(0)] public int smallScreenHeight = 720;
        [Tooltip("portees successives (touche M), en m")] public float[] minimapZooms = { 600f, 1800f, 4500f };

        [Header("Animations")]
        [Tooltip("« reduire les animations » : coupe glissements, espacements animes et apparitions echelonnees")] public bool reduceMotion;
        [Tooltip("decalage entre deux lignes lors des apparitions echelonnees des menus (s)")] [Range(0f, 0.2f)] public float staggerStep = 0.035f;
        [Tooltip("duree de l'apparition d'une ligne de menu (s)")] [Range(0.05f, 1f)] public float staggerTime = 0.22f;
        [Tooltip("duree de l'eclat blanc-rose quand une arme est de nouveau prete (s)")] [Range(0.05f, 2f)] public float readyFlashTime = 0.45f;

        public Color Dim(Color c) => new Color(c.r, c.g, c.b, c.a * boneDim);
        public Color Faint(Color c) => new Color(c.r, c.g, c.b, c.a * boneFaint);
        public Color BoneDim => Dim(bone);
        public Color BoneFaint => Faint(bone);
    }
}
