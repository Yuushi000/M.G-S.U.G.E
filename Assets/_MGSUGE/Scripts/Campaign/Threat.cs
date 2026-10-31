namespace MGSUGE
{
    /// <summary>
    /// Niveau de menace (§10.3) : 0 au depart, +1 a chaque fin de cycle de contrats (gere par ContractManager).
    /// Les coefficients viennent de l'asset CampagneData de la scene.
    /// </summary>
    public static class Threat
    {
        public static int Level;
        public static CampaignData Data;
        /// <summary>PV des ennemis standard : x (1 + 0,3 x menace).</summary>
        public static float HpMult => 1f + (Data != null ? Data.hpPerThreat : 0.3f) * Level;
        /// <summary>Degats des tirs, obus et missiles ennemis standard : x (1 + 0,2 x menace).</summary>
        public static float DamageMult => 1f + (Data != null ? Data.damagePerThreat : 0.2f) * Level;
        /// <summary>Recompense d'un contrat : x (1 + 0,3 x menace).</summary>
        public static float RewardMult => 1f + (Data != null ? Data.rewardPerThreat : 0.3f) * Level;
    }
}
