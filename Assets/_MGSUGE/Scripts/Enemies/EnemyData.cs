using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Reglages communs a tous les ennemis standard (§13.1). Chaque type a son propre asset (Data/Ennemis), avec ses valeurs
    /// de base AVANT le multiplicateur de menace (§10.3) : PV x (1 + 0,3 x menace), degats x (1 + 0,2 x menace).
    /// </summary>
    public abstract class EnemyData : ScriptableObject
    {
        [Header("Identite (§13.1, §11.1)")]
        [Tooltip("nom affiche (cible verrouillee, messages)")] public string displayName = "Ennemi";
        [Tooltip("credits gagnes a la destruction")] [Min(0)] public long credits = 100;

        [Header("Resistance")]
        [Tooltip("points de vie de base (avant menace)")] [Min(1f)] public float maxHP = 650f;
        [Tooltip("rayon de la sphere de touche (m)")] [Min(0.5f)] public float radius = 3.8f;
        [Tooltip("jauge d'impact maximale ; 0 = pas de jauge (§10.2)")] [Min(0f)] public float impactMax = 0f;
        [Tooltip("losange orange : visable par les missiles multi-cibles")] public bool missileTarget = true;

        [Header("Declenchement (aggro)")]
        [Tooltip("distance de declenchement (m)")] public float aggroRange = 750f;
        [Tooltip("distance de declenchement s'il fait partie d'un contrat (m)")] public float contractAggroRange = 950f;
        [Tooltip("se desengage au-dela de cette distance x ce facteur")] public float disengageFactor = 1.6f;
        [Tooltip("masque au-dela de cette distance s'il n'est pas engage (m)")] public float hideDistance = 3200f;

        [Header("Patrouille hors combat")]
        [Tooltip("rayon de patrouille autour du point d'origine (m)")] public float patrolRadius = 60f;
        [Tooltip("vitesse de patrouille (m/s)")] public float patrolSpeed = 12f;

        [Header("Apparence des tirs")]
        [Tooltip("couleur des projectiles de cet ennemi")] public Color shotColor = new Color32(0xFF, 0x44, 0x58, 0xFF);
        [Tooltip("longueur du fuseau lumineux (m)")] public float shotLength = 6f;
        [Tooltip("rayon de touche supplementaire des projectiles (gros projectiles)")] public float shotExtraRadius = 0f;
    }
}
