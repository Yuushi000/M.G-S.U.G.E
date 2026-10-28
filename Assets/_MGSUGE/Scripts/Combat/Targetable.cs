using System;
using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Tout ce que le joueur peut viser et detruire : points de vie, sphere de collision des tirs, jauge d'impact (§10.2),
    /// credits, marquage "losange orange" pour les missiles. Les sous-parties des boss utiliseront plusieurs spheres (jalon 7).
    /// </summary>
    public class Targetable : MonoBehaviour, IDamageable
    {
        public static readonly List<Targetable> All = new List<Targetable>();

        public string displayName = "Cible";
        public float maxHP = 650f;
        public float HP = 650f;
        public float radius = 3.8f;
        public bool isBoss, isAce;
        [Tooltip("losange orange : les missiles peuvent la viser")] public bool missileTarget = true;
        [Tooltip("0 = pas de jauge d'impact")] public float impactMax;
        public float impact;
        public long credits = 120;
        [Tooltip("faux pour les as non provoques")] public bool lockable = true;
        public WeaponData weaponData;

        public float OverloadTimer { get; private set; }
        public bool Overloaded => OverloadTimer > 0f;
        public bool Alive => HP > 0f && isActiveAndEnabled;
        public Vector3 Velocity { get; private set; }
        public Vector3 Position => transform.position;
        public event Action<Targetable, DamageInfo> Died;
        public float LastHitTime { get; private set; } = -99f;

        Vector3 lastPos; bool hasLast;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); hasLast = false; }
        void OnDisable() { All.Remove(this); }

        public void ResetHealth() { HP = maxHP; impact = 0f; OverloadTimer = 0f; }

        public void TakeDamage(DamageInfo d)
        {
            if (!Alive) return;
            bool ov = Overloaded;
            float dmg = d.amount * (ov ? (weaponData != null ? weaponData.overloadDamageMult : 1.6f) : 1f);
            HP -= dmg;
            LastHitTime = Time.time;
            DamageNumbers.Add(transform.position + Vector3.up * radius, dmg, ov);
            Sfx.Play("hit", transform.position);
            if (impactMax > 0f && !ov)
            {
                impact += d.impact;
                if (impact >= impactMax)
                {
                    impact = impactMax;
                    float t = weaponData != null ? (isBoss ? weaponData.overloadTimeBoss : isAce ? weaponData.overloadTimeAce : weaponData.overloadTime) : 3f;
                    OverloadTimer = t;
                    Msg.Post($"{displayName} : surcharge d'impact, dégâts ×1,6", MsgKind.Warning);
                    Fx.Flash(transform.position, radius * 2.5f, new Color(1f, 0.55f, 0.1f));
                }
            }
            if (HP <= 0f) Kill(d);
        }

        void Kill(DamageInfo d)
        {
            HP = 0f;
            if (d.fromPlayer && PlayerController.I != null && credits > 0)
            {
                PlayerController.I.progress.credits += credits;
                HudCredits.Gain(credits);
            }
            Fx.Explosion(transform.position, radius * 3f, isBoss);
            Sfx.Play(radius > 6f ? "boom" : "small", transform.position);
            Died?.Invoke(this, d);
        }

        void Update()
        {
            float dt = GameClock.WorldDt;
            if (dt > 0f)
            {
                if (hasLast) Velocity = (transform.position - lastPos) / dt;
                lastPos = transform.position; hasLast = true;
            }
            if (impactMax <= 0f || weaponData == null) return;
            if (OverloadTimer > 0f)
            {
                OverloadTimer -= dt;
                if (OverloadTimer <= 0f) { OverloadTimer = 0f; impact = 0f; }
                else if (UnityEngine.Random.value < 0.3f) Fx.Sparks(transform.position + UnityEngine.Random.insideUnitSphere * radius, 0.3f);
            }
            else
            {
                float delay = isBoss ? weaponData.impactDecayDelayBoss : weaponData.impactDecayDelay;
                float rate = isBoss ? weaponData.impactDecayBoss : weaponData.impactDecay;
                if (Time.time - LastHitTime > delay) impact = Mathf.Max(0f, impact - impactMax * rate * dt);
            }
        }
    }
}
