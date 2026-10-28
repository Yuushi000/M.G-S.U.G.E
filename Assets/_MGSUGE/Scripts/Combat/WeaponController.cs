using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Armes du joueur (cahier §9) : fusil a impulsions, missiles multi-cibles (nacelle du bouclier), salve, fragmentation, lame a plasma.
    /// Les tirs partent des points du modele : Muzzle (fusil), MuzzleL (bouclier, en alternance en vaisseau), PodOut (nacelle).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public WeaponData data;
        public PlayerController player;
        public LockOn lockOn;
        public FrameRig rig;

        public float rifleCd, missileCd, salvoCd, fragCd, bladeCd;
        public int SalvoLeft { get; private set; }
        public float BladeArcTime { get; private set; } = -9f;
        public bool BladeAvailable => bladeCd <= 0f;

        Transform muzzle, muzzleL, podOut, pod, blade, bladeGrip, bladeEnergy, bladeHome;
        Vector3 bladeHomePos; Quaternion bladeHomeRot; Vector3 bladeHomeScale;
        bool altMuzzle;
        float salvoTimer; int salvoIdx; List<Targetable> salvoTargets = new List<Targetable>();
        readonly List<(float time, Targetable target)> pending = new List<(float, Targetable)>();
        float lidTimer;
        enum BladeState { Idle, Charging, Striking }
        BladeState bs; float bladeT, strikeTime; Targetable bladeTarget;
        static readonly CultureInfo FR = CultureInfo.GetCultureInfo("fr-FR");
        static readonly Color Pink = new Color32(0xFF, 0x4F, 0xD8, 0xFF);

        void Start()
        {
            if (rig == null) return;
            muzzle = rig.Find("Muzzle"); muzzleL = rig.Find("MuzzleL"); podOut = rig.Find("PodOut"); pod = rig.Find("MissilePod");
            blade = rig.Find("Blade"); bladeGrip = rig.Find("BladeGrip");
            if (blade != null)
            {
                bladeHome = blade.parent; bladeHomePos = blade.localPosition; bladeHomeRot = blade.localRotation; bladeHomeScale = blade.localScale;
                bladeEnergy = MathX.FindDeep(blade, "Blade_Energy");
                if (bladeEnergy != null) bladeEnergy.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (player == null || player.dead) return;
            if (GameManager.I != null && GameManager.I.State != GameState.Play) return;
            float dt = GameClock.PlayerDt;
            rifleCd -= dt; missileCd -= dt; salvoCd -= dt; fragCd -= dt; bladeCd -= dt; lidTimer -= dt;
            var input = player.input;
            bool blocked = player.transforming;   // la salve reste possible pendant la transformation

            if (!blocked && input.Held(input.Fire) && rifleCd <= 0f && bs == BladeState.Idle) FireRifle();
            if (input.Pressed(input.Missiles) && !blocked) TryMissiles();
            if (input.Pressed(input.Salvo)) TrySalvo();
            if (input.Pressed(input.Frag) && !blocked) TryFrag();
            if (input.Pressed(input.Blade) && !blocked) TryBlade();

            // lancements differes (missiles multi-cibles) et salve
            for (int i = pending.Count - 1; i >= 0; i--)
                if (Time.time >= pending[i].time)
                {
                    var t = pending[i].target; pending.RemoveAt(i);
                    LaunchFromPod(MissileKind.Normal, t, null, player.S.missileDamage * player.DamageFactor);
                }
            if (SalvoLeft > 0)
            {
                salvoTimer -= dt;
                if (salvoTimer <= 0f)
                {
                    salvoTargets.RemoveAll(x => x == null || !x.Alive);
                    Targetable t = salvoTargets.Count > 0 ? salvoTargets[salvoIdx++ % salvoTargets.Count] : null;
                    LaunchFromPod(MissileKind.Salvo, t, t == null ? (Vector3?)player.AimPoint : null, player.S.salvoDamage * player.DamageFactor);
                    SalvoLeft--; salvoTimer = data.salvoInterval;
                    if (SalvoLeft == 0) salvoCd = data.salvoCooldown;
                }
            }
            BladeTick(dt);
            if (rig != null)
            {
                rig.lidOpen = Mathf.MoveTowards(rig.lidOpen, lidTimer > 0f ? 1f : 0f, dt * 6f);
                HeadLook(dt);
            }
        }

        // ------------------------------------------------------------------ fusil
        void FireRifle()
        {
            Transform m = player.IsShip && muzzleL != null && altMuzzle ? muzzleL : muzzle;
            altMuzzle = !altMuzzle;
            Vector3 origin = m != null ? m.position : player.Center + player.Forward * 4f;
            Vector3 aim;
            var tgt = lockOn != null ? lockOn.Target : null;
            if (tgt != null)
            {
                float tof = Vector3.Distance(origin, tgt.Position) / data.rifleSpeed;
                aim = tgt.Position + tgt.Velocity * tof * data.rifleLead;
            }
            else aim = player.IsShip ? origin + player.Forward * 1200f : player.AimPoint;
            Vector3 dir = (aim - origin).normalized;
            dir = (dir + Random.insideUnitSphere * data.rifleSpread).normalized;
            Vector3 vel = dir * data.rifleSpeed + (player.IsShip ? player.velocity : Vector3.zero);
            ProjectileSystem.I?.Fire(origin, vel, player.S.rifleDamage * player.DamageFactor, data.rifleImpact, data.rifleLife, data.rifleLength, Pink, true);
            rifleCd = player.S.rifleInterval;
            player.cam?.Shake(player.cam.data.shakeShot);
            Fx.Flash(origin, 1.6f, Pink);
            Sfx.Play("shot");
        }

        // ------------------------------------------------------------------ missiles
        void LaunchFromPod(MissileKind kind, Targetable t, Vector3? point, float dmg)
        {
            if (MissileSystem.I == null) return;
            Vector3 pos = podOut != null ? podOut.position : player.Center + Vector3.up * 3f;
            Vector3 dir = pod != null && podOut != null ? (podOut.position - pod.position).normalized : Vector3.up;
            dir = (dir + Random.insideUnitSphere * 0.35f).normalized;
            MissileSystem.I.Launch(kind, pos, dir, t, point, dmg, data.missileImpact, true);
            lidTimer = 0.6f;
        }
        void TryMissiles()
        {
            if (missileCd > 0f) return;
            var targets = lockOn != null ? lockOn.MissileTargets(data.missileMaxTargets, data.missileCone, data.missileRange) : new List<Targetable>();
            if (targets.Count == 0) { Msg.Post("Aucune cible dans le viseur pour les missiles", MsgKind.Warning, 1.5f); return; }
            missileCd = data.missileCooldown;
            for (int i = 0; i < player.S.missileCount; i++) pending.Add((Time.time + i * 0.06f, targets[i % targets.Count]));
            lidTimer = 0.6f;
        }
        void TrySalvo()
        {
            if (SalvoLeft > 0) return;
            if (salvoCd > 0f) { Msg.Post("Salve en recharge : " + salvoCd.ToString("0.0", FR) + " s", MsgKind.Warning, 0.5f); return; }
            SalvoLeft = player.S.salvoCount; salvoTimer = 0f; salvoIdx = 0;
            salvoTargets = lockOn != null ? lockOn.MissileTargets(data.missileMaxTargets, data.missileCone, data.missileRange) : new List<Targetable>();
        }
        void TryFrag()
        {
            if (fragCd > 0f || MissileSystem.I == null) return;
            fragCd = data.fragCooldown;
            var t = lockOn != null ? lockOn.Target : null;
            Vector3 pos = podOut != null ? podOut.position : player.Center + Vector3.up * 3f;
            Vector3 goal = t != null ? t.Position : player.AimPoint;
            var m = MissileSystem.I.Launch(MissileKind.FragCarrier, pos, (goal - pos).normalized, t, t == null ? (Vector3?)goal : null,
                player.S.fragCarrierDamage * player.DamageFactor, data.missileImpact, true);
            if (m != null) MissileSystem.I.SetFrag(m, player.S.fragMinis, player.S.fragMiniDamage * player.DamageFactor);
            lidTimer = 0.6f;
        }

        // ------------------------------------------------------------------ lame a plasma
        void TryBlade()
        {
            if (bladeCd > 0f || bs != BladeState.Idle) return;
            bladeCd = data.bladeCooldown;
            bs = BladeState.Charging; bladeT = 0f; strikeTime = -1f;
            bladeTarget = null;
            var t = lockOn != null ? lockOn.Target : null;
            if (t != null && Vector3.Distance(player.Center, t.Position) <= player.S.bladeRange)
            {
                bladeTarget = t;
                player.BladeDash((t.Position - player.Center).normalized * data.bladeChargeSpeed, data.bladeChargeTime);
            }
            if (blade != null && bladeGrip != null)
            {
                blade.SetParent(bladeGrip, false); blade.localPosition = Vector3.zero; blade.localRotation = Quaternion.identity; blade.localScale = Vector3.one;
            }
            if (bladeEnergy != null) bladeEnergy.gameObject.SetActive(true);
            Sfx.Play("blade");
        }
        void BladeTick(float dt)
        {
            if (bs == BladeState.Idle) { if (rig != null) { rig.bladeRaise = Mathf.MoveTowards(rig.bladeRaise, 0f, dt * 7f); rig.bladeSweep = Mathf.MoveTowards(rig.bladeSweep, 0f, dt * 7f); } return; }
            bladeT += dt;
            if (bs == BladeState.Charging)
            {
                if (rig != null) rig.bladeRaise = Mathf.MoveTowards(rig.bladeRaise, 1f, dt * 8f);
                bool strike;
                if (bladeTarget != null)
                    strike = !bladeTarget.Alive || Vector3.Distance(player.Center, bladeTarget.Position) <= data.bladeStrikeDistance + bladeTarget.radius || bladeT >= data.bladeChargeTime;
                else strike = bladeT >= data.bladeNoTargetDelay;
                if (strike) Strike();
            }
            else
            {
                if (rig != null) { rig.bladeSweep = Mathf.MoveTowards(rig.bladeSweep, 1f, dt * 10f); rig.bladeRaise = Mathf.MoveTowards(rig.bladeRaise, 0f, dt * 10f); }
                if (bladeT >= Mathf.Max(data.bladeAnimTime, strikeTime + 0.2f)) EndBlade();
            }
        }
        void Strike()
        {
            bs = BladeState.Striking; strikeTime = bladeT;
            player.StopBladeDash();
            Vector3 fwd = bladeTarget != null ? (bladeTarget.Position - player.Center).normalized : player.Forward;
            Vector3 p = player.Center + fwd * data.bladeHitForward;
            foreach (var t in Targetable.All.ToArray())
                if (t.Alive && Vector3.Distance(t.Position, p) < data.bladeHitRadius + t.radius)
                    t.TakeDamage(new DamageInfo { amount = player.S.bladeDamage * player.DamageFactor, impact = data.bladeImpact, point = t.Position, direction = fwd, fromPlayer = true, source = this });
            Fx.Flash(p, 9f, Pink);
            for (int i = 0; i < 24; i++) FxSystem.I?.Emit(p + Random.insideUnitSphere * 4f, Random.onUnitSphere * 30f, 1f, 0.35f, Pink);
            BladeArcTime = Time.time;
            player.cam?.Shake(0.3f);
        }
        void EndBlade()
        {
            bs = BladeState.Idle;
            if (bladeEnergy != null) bladeEnergy.gameObject.SetActive(false);
            if (blade != null && bladeHome != null)
            {
                blade.SetParent(bladeHome, false); blade.localPosition = bladeHomePos; blade.localRotation = bladeHomeRot; blade.localScale = bladeHomeScale;
            }
        }

        // ------------------------------------------------------------------ la tete suit la cible verrouillee (±0,8 rad)
        void HeadLook(float dt)
        {
            Vector2 want = Vector2.zero;
            var t = lockOn != null ? lockOn.Target : null;
            if (t != null && player.visual != null && !player.IsShip)
            {
                Vector3 local = Quaternion.Inverse(player.visual.rotation) * (t.Position - (player.Center + Vector3.up * 3.5f));
                float yaw = Mathf.Atan2(local.x, local.z);
                float pitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude);
                want = new Vector2(Mathf.Clamp(yaw, -data.headTrackLimit, data.headTrackLimit), Mathf.Clamp(pitch, -data.headTrackLimit, data.headTrackLimit));
            }
            rig.headLook = Vector2.Lerp(rig.headLook, want, MathX.Damp(8f, dt));
        }

        public float Cooldown01(string w)
        {
            switch (w)
            {
                case "rifle": return Mathf.Clamp01(rifleCd / Mathf.Max(0.01f, player.S.rifleInterval));
                case "missile": return Mathf.Clamp01(missileCd / data.missileCooldown);
                case "salvo": return SalvoLeft > 0 ? 1f : Mathf.Clamp01(salvoCd / data.salvoCooldown);
                case "frag": return Mathf.Clamp01(fragCd / data.fragCooldown);
                case "blade": return Mathf.Clamp01(bladeCd / data.bladeCooldown);
            }
            return 0f;
        }
    }
}
