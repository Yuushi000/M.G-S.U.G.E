using System;
using UnityEngine;

namespace MGSUGE
{
    public enum UpgradeModule
    {
        Propulseurs, ReservoirEN, Generateur, QuickBoostSaut, Blindage, Fusil, LanceMissiles, Salve, Fragmentation, Lame, FormeVaisseau
    }

    /// <summary>Couts et effets des 11 ameliorations (cahier §11.2) et achats uniques (§11.3).</summary>
    [CreateAssetMenu(menuName = "MGSUGE/Donnees/Progression", fileName = "MGSUGE_Progression")]
    public class ProgressionData : ScriptableObject
    {
        public long[] levelCosts = { 900, 1600, 2600, 3800, 5200 };
        public int maxLevel = 5;
        [Header("Effets par niveau")]
        public float thrusterSpeed = 0.12f, thrusterAscent = 0.08f, thrusterShipSpeed = 0.04f;
        public float tankEnergy = 0.25f;
        public float generatorRegen = 0.22f, generatorOverheat = -0.12f;
        public float quickBoostSpeed = 0.12f, jumpSpeed = 0.08f, quickJumpCost = -0.09f;
        public float armor = 0.16f;
        public float rifleDamage = 0.16f, rifleInterval = -0.05f;
        public float missileDamage = 0.12f;
        public float salvoDamage = 0.06f;
        public float fragDamage = 0.12f;
        public float bladeDamage = 0.18f, bladeRange = 0.08f;
        public float shipSpeed = 0.10f, shipStrafe = 0.05f;
        [Header("Achats uniques")]
        public long arcaneCost = 1000000, domainCost = 500000, domain2Cost = 2500000, reincarnationCost = 10000000;
        public float reincarnationFactor = 5f;
    }

    /// <summary>Progression du joueur (credits, niveaux, achats). Sera sauvegardee en JSON (jalon 10).</summary>
    [Serializable]
    public class PlayerProgress
    {
        public long credits = 2000;
        public int[] levels = new int[11];
        public int probes;
        public bool arcane, domain, domain2;
        public int reincarnation;
        public bool divine;
        public int contractIndex, threat;
        public int Level(UpgradeModule m) => levels[(int)m];
    }

    /// <summary>Statistiques effectives du chassis = base x ameliorations x reincarnation (equivalent de computeStats() du prototype).</summary>
    public class PlayerStats
    {
        public float armor, energy, regen, overheat;
        public float walk, groundBoost, airBoost, ascent, descent, assault;
        public float quickSpeed, quickCost, jumpSpeed, jumpCost;
        public float shipMax, shipAfterburner, shipStrafe, shipReverse;
        public float rifleDamage, rifleInterval;
        public int missileCount; public float missileDamage;
        public int salvoCount; public float salvoDamage;
        public int fragMinis; public float fragCarrierDamage, fragMiniDamage;
        public float bladeDamage, bladeRange;
        public float power; // facteur de reincarnation

        public static PlayerStats Compute(FrameStatsData f, WeaponData w, ProgressionData p, PlayerProgress pr)
        {
            int L(UpgradeModule m) => pr.Level(m);
            var s = new PlayerStats();
            s.power = Mathf.Pow(p.reincarnationFactor, pr.reincarnation);
            float thr = 1f + p.thrusterSpeed * L(UpgradeModule.Propulseurs);
            s.walk = f.walkSpeed * thr;
            s.groundBoost = f.groundBoostSpeed * thr;
            s.airBoost = f.airBoostSpeed * thr;
            s.assault = f.assaultBoostSpeed * thr;
            s.ascent = f.ascentSpeed * (1f + p.thrusterAscent * L(UpgradeModule.Propulseurs));
            s.descent = f.descentSpeed;
            s.energy = f.energy * (1f + p.tankEnergy * L(UpgradeModule.ReservoirEN));
            s.regen = f.energyRegen * (1f + p.generatorRegen * L(UpgradeModule.Generateur));
            s.overheat = f.overheatTime * (1f + p.generatorOverheat * L(UpgradeModule.Generateur));
            int q = L(UpgradeModule.QuickBoostSaut);
            s.quickSpeed = f.quickBoostSpeed * (1f + p.quickBoostSpeed * q);
            s.jumpSpeed = f.jumpSpeed * (1f + p.jumpSpeed * q);
            s.quickCost = f.quickBoostCost * (1f + p.quickJumpCost * q);
            s.jumpCost = f.jumpCost * (1f + p.quickJumpCost * q);
            s.armor = f.armor * (1f + p.armor * L(UpgradeModule.Blindage)) * s.power;
            float shipMul = (1f + p.shipSpeed * L(UpgradeModule.FormeVaisseau)) * (1f + p.thrusterShipSpeed * L(UpgradeModule.Propulseurs));
            s.shipMax = f.shipMaxSpeed * shipMul;
            s.shipAfterburner = f.shipAfterburnerSpeed * shipMul;
            s.shipStrafe = f.shipStrafeSpeed * (1f + p.shipStrafe * L(UpgradeModule.FormeVaisseau));
            s.shipReverse = f.shipReverseSpeed;
            int rf = L(UpgradeModule.Fusil);
            s.rifleDamage = w.rifleDamage * (1f + p.rifleDamage * rf) * s.power;
            s.rifleInterval = w.rifleInterval * (1f + p.rifleInterval * rf);
            int lm = L(UpgradeModule.LanceMissiles);
            s.missileCount = w.missileCount + lm;
            s.missileDamage = w.missileDamage * (1f + p.missileDamage * lm) * s.power;
            int ls = L(UpgradeModule.Salve);
            s.salvoCount = w.salvoCount + ls;
            s.salvoDamage = s.missileDamage * (1f + p.salvoDamage * ls);
            int lf = L(UpgradeModule.Fragmentation);
            s.fragMinis = w.fragMiniCount + lf;
            s.fragCarrierDamage = s.missileDamage * w.fragCarrierDamage;
            s.fragMiniDamage = s.missileDamage * w.fragMiniDamage * (1f + p.fragDamage * lf);
            int lb = L(UpgradeModule.Lame);
            s.bladeDamage = w.bladeDamage * (1f + p.bladeDamage * lb) * s.power;
            s.bladeRange = w.bladeRange * (1f + p.bladeRange * lb);
            return s;
        }
    }
}
