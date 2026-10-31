using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Base des six ennemis standard (§13.1). Gere le declenchement (aggro a 750 m, 950 m pour un contrat), le desengagement
    /// (x 1,6), le masquage au-dela de 3 200 m hors combat, la surcharge d'impact (plus d'attaque ni de deplacement),
    /// la menace (PV et degats) et la reapparition. Les valeurs viennent de l'asset de donnees du type (Data/Ennemis).
    /// Un ennemi detruit est desactive (contrats, avant-postes) ou supprime (patrouilles errantes).
    /// </summary>
    [RequireComponent(typeof(Targetable))]
    [SelectionBase]
    public abstract class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        [Header("Role")]
        [Tooltip("ennemi de contrat : declenchement a 950 m (regle automatiquement par le contrat parent)")] public bool contractEnemy;
        [Tooltip("cree par les patrouilles errantes : supprime quand il est detruit ou trop loin")] public bool roaming;

        [Header("References")]
        [Tooltip("parametres de surcharge d'impact (asset MGSUGE_Weapons)")] public WeaponData weaponData;

        public abstract EnemyData BaseData { get; }
        public bool Engaged { get; private set; }
        public bool Hidden { get; private set; }
        public bool IsAlive => this != null && gameObject.activeSelf && tg != null && tg.HP > 0f;
        public Targetable Target => tg;
        public Vector3 Home => home;

        protected Targetable tg;
        protected float distToPlayer;
        Vector3 home; Quaternion homeRot; bool homeSet;
        Renderer[] rends;

        protected static PlayerController Player => PlayerController.I;
        protected float AggroRange => contractEnemy ? Mathf.Max(BaseData.aggroRange, BaseData.contractAggroRange) : BaseData.aggroRange;

        protected virtual void Awake()
        {
            tg = GetComponent<Targetable>();
            rends = GetComponentsInChildren<Renderer>(true);
            CaptureHome();
            ApplyStats();
            OnRespawn();
        }
        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        void CaptureHome()
        {
            if (homeSet) return;
            home = transform.position; homeRot = transform.rotation; homeSet = true;
        }

        /// <summary>Recopie les valeurs de l'asset de donnees dans la cible (PV x menace).</summary>
        public void ApplyStats()
        {
            var d = BaseData; if (d == null || tg == null) return;
            tg.displayName = d.displayName; tg.credits = d.credits; tg.radius = d.radius; tg.impactMax = d.impactMax;
            tg.missileTarget = d.missileTarget; tg.weaponData = weaponData;
            tg.maxHP = d.maxHP * Threat.HpMult;
            tg.ResetHealth();
        }

        /// <summary>Remet l'ennemi a son point d'origine, PV pleins (menace actuelle), non engage. A appeler apres SetActive(true).</summary>
        public void Respawn()
        {
            if (tg == null) tg = GetComponent<Targetable>();
            CaptureHome();
            transform.SetPositionAndRotation(home, homeRot);
            Engaged = false;
            ApplyStats();
            SetHidden(false);
            OnRespawn();
        }

        void SetHidden(bool h)
        {
            if (Hidden == h) return;
            Hidden = h;
            if (rends == null) return;
            foreach (var r in rends) if (r != null) r.enabled = !h;
        }

        void Update()
        {
            if (tg == null || tg.HP <= 0f) return;
            float dt = GameClock.WorldDt; if (dt <= 0f) return;
            var p = Player;
            bool pAlive = p != null && !p.dead;
            distToPlayer = p != null ? Vector3.Distance(transform.position, p.Center) : float.MaxValue;
            if (!Engaged && pAlive && distToPlayer < AggroRange) Engaged = true;
            else if (Engaged && (!pAlive || distToPlayer > AggroRange * BaseData.disengageFactor)) Engaged = false;
            SetHidden(!Engaged && distToPlayer > BaseData.hideDistance);
            if (Hidden) return;
            if (tg.Overloaded) { OverloadedTick(dt); return; }
            Tick(dt, Engaged && pAlive);
        }

        void LateUpdate()
        {
            if (tg == null || tg.HP > 0f) return;
            OnKilled();
            if (roaming) Destroy(gameObject); else gameObject.SetActive(false);
        }

        /// <summary>Comportement a chaque image (hors surcharge). engaged = le joueur est repere et vivant.</summary>
        protected abstract void Tick(float dt, bool engaged);
        /// <summary>En surcharge : ne bouge plus, n'attaque plus, tremble (§10.2).</summary>
        protected virtual void OverloadedTick(float dt) { transform.position += Random.insideUnitSphere * 0.08f; }
        protected virtual void OnRespawn() { }
        protected virtual void OnKilled() { }

        // ------------------------------------------------------------------ outils de tir
        /// <summary>Position future du joueur pour un projectile de vitesse donnee.</summary>
        protected static Vector3 Predict(Vector3 from, float speed, float lead)
        {
            var p = Player; if (p == null) return from;
            float tof = Vector3.Distance(from, p.Center) / Mathf.Max(1f, speed);
            return p.Center + p.velocity * tof * lead;
        }

        /// <summary>Tire un projectile ennemi (degats x menace) avec dispersion, eclair de bouche et son.</summary>
        protected void FireShot(Vector3 origin, Vector3 aim, float speed, float damage, float spread, float range, Color color, float length, float extraRadius)
        {
            var ps = ProjectileSystem.I; if (ps == null) return;
            Vector3 dir = (aim - origin).normalized;
            dir = (dir + Random.insideUnitSphere * spread).normalized;
            ps.Fire(origin, dir * speed, damage * Threat.DamageMult, 0f, range / Mathf.Max(1f, speed) * 1.15f, length, color, false, extraRadius);
            Fx.Flash(origin, 1.6f, color);
            Sfx.Play("shot", origin);
        }

        protected static float Ground(Vector3 p) => TerrainSystem.Ground(p.x, p.z);
        protected static float Rand(Vector2 range) => Random.Range(range.x, range.y);
    }
}
