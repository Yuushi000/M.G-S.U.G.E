using UnityEngine;

namespace MGSUGE
{
    /// <summary>Armes du joueur, missiles, verrouillage et jauge d'impact (cahier §9 et §10.2). Valeurs niveau 0.</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Armes", fileName = "MGSUGE_Weapons")]
    public class WeaponData : ScriptableObject
    {
        [Header("Fusil a impulsions (clic gauche)")]
        public float rifleDamage = 115f;
        public float rifleInterval = 0.105f;
        public float rifleSpeed = 680f;
        public float rifleLife = 2.4f;
        public float rifleLength = 7f;
        public float rifleSpread = 0.006f;
        [Tooltip("anticipation : temps de vol x ce facteur")] public float rifleLead = 0.9f;
        public float rifleImpact = 45f;
        public int projectilePool = 700;

        [Header("Missiles multi-cibles (clic droit)")]
        public float missileDamage = 420f;
        public float missileCooldown = 3.2f;
        public int missileCount = 4;
        public int missileMaxTargets = 5;
        public float missileCone = 0.55f;
        public float missileRange = 1100f;
        public float missileImpact = 260f;
        public int missilePool = 160;

        [Header("Salve (C)")]
        public int salvoCount = 5;
        public float salvoInterval = 0.33f;
        public float salvoCooldown = 6f;
        [Tooltip("bonus de degats par niveau du module Salve")] public float salvoDamagePerLevel = 0.06f;

        [Header("Fragmentation (V)")]
        public float fragCooldown = 7f;
        [Range(0, 1)] public float fragCarrierDamage = 0.5f;
        [Range(0, 1)] public float fragMiniDamage = 0.75f;
        public float fragMiniDamagePerLevel = 0.12f;
        public int fragMiniCount = 5;
        public float fragCarrierSpeedFactor = 1.15f;
        public float fragMiniSpeedFactor = 1.5f;
        public float fragBurstDistance = 45f;
        [Range(0, 1)] public float fragBurstPath = 0.92f;

        [Header("Lame a plasma (F)")]
        public float bladeDamage = 1900f;
        public float bladeCooldown = 2.1f;
        public float bladeRange = 85f;
        public float bladeChargeSpeed = 270f;
        public float bladeChargeTime = 0.38f;
        public float bladeStrikeDistance = 10f;
        public float bladeNoTargetDelay = 0.14f;
        public float bladeHitRadius = 14f;
        public float bladeHitForward = 7f;
        public float bladeImpact = 1100f;
        public float bladeAnimTime = 0.55f;

        [Header("Missiles (detail §9.2)")]
        public float missileSpeed0 = 110f;
        public float missileAccel = 260f;
        public float missileMaxSpeed = 340f;
        [Tooltip("rad/s")] public float missileTurn = 3.4f;
        public float missileGuidanceDelay = 0.25f;
        public float miniGuidanceDelay = 0.12f;
        public float missileLife = 6f;
        public float miniLife = 5f;
        [Tooltip("vise position + vitesse x temps de vol x ce facteur")] public float missileLead = 0.6f;
        public float missileContact = 2.5f;
        public float missileSplash = 8f;

        [Header("Verrouillage (§9.3)")]
        public float lockCone = 0.2f;
        public float lockConeBoss = 0.3f;
        public float lockRangeMech = 700f;
        public float lockRangeShip = 900f;
        public float lockRangeBossBonus = 700f;
        public float lockLoseAngle = 0.55f;
        [Tooltip("perte du verrou au-dela de ce facteur x portee")] public float lockLoseRange = 1.15f;
        public float switchCone = 0.8f;
        public float headTrackLimit = 0.8f;

        [Header("Jauge d'impact (§10.2)")]
        public float overloadTime = 3f, overloadTimeBoss = 4f, overloadTimeAce = 2f;
        public float overloadDamageMult = 1.6f;
        public float impactDecay = 0.15f, impactDecayBoss = 0.10f;
        public float impactDecayDelay = 1.5f, impactDecayDelayBoss = 2f;

        [Header("Sondes laser (§12.3)")]
        public int probeMax = 5;
        public long probeCost = 250000;
        public float probeOrbitRadius = 28f;
        public float probeOrbitSpeed = 1.3f;
        public float probeSpeed = 260f;

        [Header("Kit de reparation (H)")]
        public float repairCooldown = 0.5f;
    }
}
