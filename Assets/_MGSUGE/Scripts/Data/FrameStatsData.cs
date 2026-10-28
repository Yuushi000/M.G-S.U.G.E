using UnityEngine;

namespace MGSUGE
{
    /// <summary>Statistiques de base du chassis Vesper (cahier §7, niveau 0 sans reincarnation).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Chassis (statistiques)", fileName = "MGSUGE_FrameStats")]
    public class FrameStatsData : ScriptableObject
    {
        [Header("7.1 Armure et energie")]
        public float armor = 8200f;
        public float energy = 1000f;
        [Tooltip("EN/s")] public float energyRegen = 420f;
        [Tooltip("s apres la derniere depense")] public float regenDelay = 0.45f;
        [Tooltip("s, generateur coupe a 0 EN")] public float overheatTime = 1.6f;
        [Tooltip("facteur de recharge apres surchauffe, jusqu'au plein")] public float overheatRecoveryMult = 1.5f;

        [Header("7.1 Vitesses forme chassis (m/s)")]
        public float walkSpeed = 34f;
        [Tooltip("facteur de marche en l'air")] public float airWalkFactor = 0.7f;
        public float groundBoostSpeed = 72f;
        public float airBoostSpeed = 70f;
        public float ascentSpeed = 46f;
        public float descentSpeed = 64.4f;
        public float assaultBoostSpeed = 190f;
        public float quickBoostSpeed = 150f;
        public float quickBoostDuration = 0.24f;
        public float quickBoostCooldown = 0.32f;
        public float jumpSpeed = 84f;
        public float jumpDuration = 0.22f;
        public float jumpCooldown = 0.16f;
        [Tooltip("petit saut au sol sans EN")] public float noEnergyJumpSpeed = 30f;

        [Header("7.3 Accelerations (taux de rapprochement, 1/s)")]
        public float walkAccelGround = 9f;
        public float walkAccelAir = 1.4f;
        public float boostAccelGround = 6.5f;
        public float boostAccelAir = 4f;

        [Header("7.1 Gravite")]
        public float gravity = 64f;
        [Tooltip("gravite pendant le boost aerien")] public float airBoostGravityFactor = 0.16f;
        [Tooltip("chute maximale pendant le boost aerien (m/s)")] public float airBoostMaxFall = 9f;

        [Header("7.2 Couts en energie")]
        public float groundBoostCost = 55f;      // EN/s
        public float airBoostCost = 140f;        // EN/s
        public float ascentCost = 290f;          // EN/s
        public float descentCost = 174f;         // EN/s
        public float quickBoostCost = 220f;      // par impulsion
        public float jumpCost = 170f;            // par impulsion
        public float assaultBoostStartCost = 120f;
        public float assaultBoostCost = 230f;    // EN/s
        [Range(0, 1)] public float assaultBoostMinEnergy = 0.15f;
        public float afterburnerCost = 170f;     // EN/s

        [Header("7.4 Forme vaisseau (m/s)")]
        public float shipMaxSpeed = 300f;
        public float shipAfterburnerSpeed = 440f;
        public float shipReverseSpeed = 70f;
        public float shipStrafeSpeed = 90f;
        [Tooltip("taux de rapprochement de la vitesse cible (1/s) - valeur non chiffree dans le cahier")] public float shipAccel = 1.6f;
        public float transformTime = 0.75f;
        public float barrelRollImpulse = 150f * 1.15f;
        public float barrelRollTime = 0.45f;
        [Tooltip("s sans mouvement de souris avant remise a niveau")] public float shipAutoLevelDelay = 0.9f;
        [Tooltip("rad/s")] public float shipAutoLevelRate = 1.4f;
        [Tooltip("rad/s")] public float shipArrowYaw = 1.8f;
        [Tooltip("rad/s")] public float shipArrowPitch = 1.5f;
        public float shipDamageFactor = 0.9f;
        [Tooltip("etincelles au ras du sol au-dela de cette vitesse")] public float shipSparkSpeed = 120f;

        [Header("7.1 Collisions et hauteurs")]
        [Tooltip("centre du bassin -> sol. 7 m valide par l'auteur (chassis de 12 m) ; prototype : 5,7 m")] public float frameClear = 7f;
        public float shipClear = 3.2f;
        public float frameRadiusTop = 3.2f;
        public float frameRadiusBottom = 2.3f;
        [Tooltip("sphere basse decalee pour toucher le sol a -frameClear (prototype : -3,5)")] public float frameBottomOffset = -4.7f;
        public float shipRadius = 3.4f;

        [Header("7.3 Atterrissage et marche")]
        public float landingSpeed = 22f;
        public float landingMaxCrouch = 1.1f;
        public float walkLeanMax = 0.4f;
        public float walkSideLeanMax = 0.3f;
        [Tooltip("rad/s : lissage de la rotation du chassis vers la camera")] public float turnRate = 8f;

        [Header("7.6 Reparation")]
        public int repairKits = 3;
        [Range(0, 1)] public float repairAmount = 0.4f;
        [Range(0, 1)] public float criticalArmor = 0.25f;
        [Tooltip("vitesse de la trainee blanche de la barre d'AP (fraction de l'AP max par s)")] public float armorTrailRate = 0.22f;
        public float deathDelay = 1.8f;

        [Header("10.5 Lave")]
        public float lavaDamage = 380f;
        public float lavaDepth = -7f;
    }
}
