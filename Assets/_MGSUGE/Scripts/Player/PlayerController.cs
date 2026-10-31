using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    public enum FrameForm { Mech, Ship }

    /// <summary>
    /// Chassis du joueur "Vesper" (cahier §7) : armure, energie, deplacements des deux formes, transformation,
    /// atterrissage, lave, limites du monde. Les armes sont dans WeaponController, la camera dans CameraRig.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(0)]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public static PlayerController I { get; private set; }

        [Header("Donnees")]
        public FrameStatsData frame;
        public WeaponData weaponData;
        public ProgressionData progression;
        public WorldData world;
        [Header("References")]
        public FrameRig rig;
        public CameraRig cam;
        public PlayerInputs input;
        [Tooltip("racine visuelle (modele) : tourne selon la forme ; la capsule de collision reste droite")] public Transform visual;
        [Tooltip("point de redeploiement (plateforme Helios) ; vide = position Helios des donnees du monde")] public Transform spawnPoint;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : world.heliosPosition;

        [Header("Etat (lecture)")]
        public PlayerProgress progress = new PlayerProgress();
        public FrameForm form = FrameForm.Mech;
        public bool transforming;
        [Range(0, 1)] public float shipBlend;
        public float AP, EN, apTrail;
        public int kits;
        public Vector3 velocity;
        public bool grounded;
        public bool boosting, assault, dead, invulnerable;
        public float heightAboveGround;
        public PlayerStats S { get; private set; }
        public string EnergyState { get; private set; } = "";
        public float DamageFactor => (form == FrameForm.Ship && !transforming) ? frame.shipDamageFactor : 1f;
        public bool IsShip => form == FrameForm.Ship && !transforming;
        public float Clear => Mathf.Lerp(frame.frameClear, frame.shipClear, shipBlend);
        public bool Overheated => overheatTimer > 0f;
        public float QuickBoostCooldown01 => Mathf.Clamp01(qbCd / frame.quickBoostCooldown);
        public Vector3 Center => transform.position;
        public Vector3 AimPoint => cam != null ? cam.AimPoint : transform.position + Forward * 100f;
        public Vector3 Forward => IsShip ? shipRot * Vector3.forward : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Quaternion ShipRotation => shipRot;
        public float YawDeg => yaw;
        /// <summary>Direction et intensite de poussee (pour les flammes des tuyeres), en espace monde.</summary>
        public Vector3 ThrustVector { get; private set; }
        public float FovKick { get; private set; }
        public float SpeedKmh => velocity.magnitude * 3.6f;
        public float Altitude => transform.position.y - world.seaLevel;
        public readonly List<KeyValuePair<float, Vector3>> recentHits = new List<KeyValuePair<float, Vector3>>(); // (temps, direction)

        CharacterController cc;
        float yaw; Quaternion shipRot = Quaternion.identity;
        FrameForm targetForm; float transformTimer;
        float lastSpend = -10f, overheatTimer; bool recovering;
        float qbTimer, qbCd, jumpTimer, jumpCd, diveTimer; Vector3 qbDir;
        float landCrouch, walkPhase, lastVy;
        float lastMouse; float rollTimer; int rollDir = 1;
        float deathTimer; bool deathShown;
        float repairCd;

        void Awake()
        {
            I = this;
            cc = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputs>();
            progress.credits = world != null ? world.startCredits : 2000;
            Recompute();
            AP = apTrail = S.armor; EN = S.energy; kits = frame.repairKits;
            SetCollider(FrameForm.Mech);
            yaw = transform.eulerAngles.y;
        }

        /// <summary>Recalcule les statistiques (apres un achat d'amelioration).</summary>
        public void Recompute()
        {
            float apRatio = S != null && S.armor > 0 ? AP / S.armor : 1f;
            float enRatio = S != null && S.energy > 0 ? EN / S.energy : 1f;
            S = PlayerStats.Compute(frame, weaponData, progression, progress);
            if (AP > 0) { AP = S.armor * apRatio; apTrail = AP; }
            EN = S.energy * enRatio;
        }

        void SetCollider(FrameForm f)
        {
            if (f == FrameForm.Mech)
            {
                // sphere haute r 3,2 au centre + sphere basse r 2,3 a -3,5 => capsule couvrant de -5,7 a +3,2
                float top = frame.frameRadiusTop, bottom = frame.frameClear;
                cc.radius = Mathf.Lerp(frame.frameRadiusBottom, frame.frameRadiusTop, 0.5f);
                cc.height = top + bottom;
                cc.center = new Vector3(0f, (top - bottom) * 0.5f, 0f);
            }
            else
            {
                cc.radius = frame.shipClear; cc.height = frame.shipClear * 2f; cc.center = Vector3.zero;
            }
            cc.stepOffset = 1.5f; cc.slopeLimit = 55f; cc.skinWidth = 0.3f; cc.minMoveDistance = 0f;
        }

        void Update()
        {
            if (GameManager.I != null && GameManager.I.State != GameState.Play) return;
            float dt = GameClock.PlayerDt;
            if (dt <= 0f) return;
            if (dead) { DeathTick(dt); return; }

            if (input.Pressed(input.TransformKey) && !transforming) StartTransform();
            EnergyTick(dt);
            ThrustVector = Vector3.zero;
            FovKick = 0f;
            qbCd -= dt; jumpCd -= dt; repairCd -= dt;

            if (bladeDashTimer > 0f && !transforming)
            {
                bladeDashTimer -= dt; velocity = bladeDashVel; ThrustVector = bladeDashVel.normalized; EnergyState = "";
                if (form == FrameForm.Mech) { Vector3 f = bladeDashVel; f.y = 0f; if (f.sqrMagnitude > 1f) yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg; }
            }
            else if (transforming) TransformTick(dt);
            else if (form == FrameForm.Mech) MechMotion(dt);
            else ShipMotion(dt);

            DoMove(dt);
            VisualTick(dt);
            Hazards(dt);
            ArmorTick(dt);
        }

        // ------------------------------------------------------------------ energie (§7.2)
        public bool TrySpend(float cost)
        {
            if (Overheated || EN < cost) return false;
            EN -= cost; lastSpend = Time.time;
            if (EN <= 0.5f) StartOverheat();
            return true;
        }
        bool Drain(float rate, float dt)
        {
            if (Overheated || EN <= 0f) return false;
            EN -= rate * dt; lastSpend = Time.time;
            if (EN <= 0f) { EN = 0f; StartOverheat(); return false; }
            return true;
        }
        void StartOverheat()
        {
            EN = 0f; overheatTimer = S.overheat; recovering = true;
            boosting = false; assault = false;
            Msg.Post("EN épuisée : surchauffe du générateur", MsgKind.Danger);
            Sfx.Play("alarm");
        }
        void EnergyTick(float dt)
        {
            if (overheatTimer > 0f) { overheatTimer -= dt; EnergyState = "SURCHAUFFE"; return; }
            if (Time.time - lastSpend >= frame.regenDelay && EN < S.energy)
            {
                EN += S.regen * (recovering ? frame.overheatRecoveryMult : 1f) * dt;
                if (EN >= S.energy) { EN = S.energy; if (recovering) { recovering = false; Msg.Post("Générateur rétabli", MsgKind.Good); } }
            }
        }

        // ------------------------------------------------------------------ forme chassis (§7.3)
        void MechMotion(float dt)
        {
            Vector2 mv = input.MoveV;
            Quaternion camYaw = Quaternion.Euler(0f, cam != null ? cam.YawDeg : yaw, 0f);
            Vector3 wish = camYaw * new Vector3(mv.x, 0f, mv.y);
            bool hasDir = wish.sqrMagnitude > 0.01f;
            Vector3 hv = new Vector3(velocity.x, 0f, velocity.z);
            float vy = velocity.y;
            EnergyState = "";

            // quick boost (E)
            if (input.Pressed(input.QuickBoost) && qbCd <= 0f && TrySpend(S.quickCost))
            {
                qbDir = hasDir ? wish.normalized : Forward; qbTimer = frame.quickBoostDuration; qbCd = frame.quickBoostCooldown;
                assault = false; boosting = false;
                cam?.Shake(cam.data.shakeQuickBoost); Sfx.Play("qb");
            }
            // assault boost (R) : vers le point vise ; S ou R l'arrete
            if (input.Pressed(input.AssaultBoost))
            {
                if (assault) assault = false;
                else if (EN >= S.energy * frame.assaultBoostMinEnergy && TrySpend(frame.assaultBoostStartCost))
                { assault = true; qbTimer = 0f; cam?.Shake(cam.data.shakeAssault); Sfx.Play("qb"); }
            }
            if (assault && (mv.y < -0.5f || !Drain(frame.assaultBoostCost, dt))) assault = false;

            if (qbTimer > 0f)
            {
                qbTimer -= dt;
                hv = qbDir * S.quickSpeed; vy = Mathf.Lerp(vy, 0f, MathX.Damp(12f, dt));
                ThrustVector = qbDir * 1f; FovKick = cam != null ? cam.data.quickBoostFov : 6f; EnergyState = "QUICK BOOST";
            }
            else if (assault)
            {
                Vector3 dir = (AimPoint - transform.position); if (dir.sqrMagnitude < 1f) dir = Forward; dir.Normalize();
                Vector3 v = dir * S.assault; hv = new Vector3(v.x, 0f, v.z); vy = v.y;
                ThrustVector = dir; FovKick = cam != null ? cam.data.assaultFov : 10f; EnergyState = "ASSAULT BOOST";
            }
            else
            {
                boosting = input.Held(input.Boost) && hasDir && !Overheated && EN > 0f;
                if (boosting) boosting = Drain(grounded ? frame.groundBoostCost : frame.airBoostCost, dt);
                float speed, rate;
                if (boosting) { speed = grounded ? S.groundBoost : S.airBoost; rate = grounded ? frame.boostAccelGround : frame.boostAccelAir; EnergyState = "BOOST"; ThrustVector = wish.normalized; }
                else { speed = S.walk * (grounded ? 1f : frame.airWalkFactor); rate = grounded ? frame.walkAccelGround : frame.walkAccelAir; }
                hv = Vector3.Lerp(hv, wish * speed, MathX.Damp(rate, dt));

                // saut boost (Espace) : impulsion a chaque pression
                if (input.Pressed(input.Jump) && jumpCd <= 0f)
                {
                    if (TrySpend(S.jumpCost)) { jumpTimer = frame.jumpDuration; jumpCd = frame.jumpCooldown; vy = S.jumpSpeed; FovKick = cam != null ? cam.data.jumpFov : 4f; Sfx.Play("qb"); }
                    else if (grounded) vy = frame.noEnergyJumpSpeed;
                }
                // plongee (Ctrl) en l'air : impulsion vers le bas
                if (input.Pressed(input.Down) && !grounded && jumpCd <= 0f && TrySpend(S.jumpCost))
                { diveTimer = frame.jumpDuration; jumpCd = frame.jumpCooldown; vy = -S.jumpSpeed; Sfx.Play("qb"); }

                if (jumpTimer > 0f) { jumpTimer -= dt; vy = Mathf.Max(vy, S.jumpSpeed); ThrustVector += Vector3.up; EnergyState = "SAUT BOOST"; }
                else if (diveTimer > 0f) { diveTimer -= dt; vy = Mathf.Min(vy, -S.jumpSpeed); EnergyState = "PLONGÉE BOOST"; }
                else if (!grounded && input.Held(input.Jump) && Drain(frame.ascentCost, dt))
                { vy = Mathf.Lerp(vy, S.ascent, MathX.Damp(6f, dt)); ThrustVector += Vector3.up; EnergyState = "MONTÉE"; }
                else if (!grounded && input.Held(input.Down) && Drain(frame.descentCost, dt))
                { vy = Mathf.Lerp(vy, -S.descent, MathX.Damp(6f, dt)); EnergyState = "DESCENTE"; }
                else
                {
                    bool airBoost = boosting && !grounded;
                    vy -= frame.gravity * (airBoost ? frame.airBoostGravityFactor : 1f) * dt;
                    if (airBoost) vy = Mathf.Max(vy, -frame.airBoostMaxFall);
                    if (grounded && vy < 0f) vy = -2f;
                }
            }
            velocity = new Vector3(hv.x, vy, hv.z);
            if (Overheated) EnergyState = "SURCHAUFFE";

            // le chassis se tourne vers la direction de la camera (lisse)
            float targetYaw = cam != null ? cam.YawDeg : yaw;
            yaw = Mathf.LerpAngle(yaw, targetYaw, MathX.Damp(frame.turnRate, dt));
        }

        // ------------------------------------------------------------------ forme vaisseau (§7.4)
        void ShipMotion(float dt)
        {
            Vector2 look = input.LookDelta;
            float sens = (cam != null ? cam.data.mouseSensitivity : 0.0023f) * Mathf.Rad2Deg;
            if (look.sqrMagnitude > 0.01f) lastMouse = Time.time;
            Vector2 arrows = input.Arrows;
            float yawD = look.x * sens + arrows.x * frame.shipArrowYaw * Mathf.Rad2Deg * dt;
            float pitchD = -look.y * sens - arrows.y * frame.shipArrowPitch * Mathf.Rad2Deg * dt;
            shipRot = shipRot * Quaternion.Euler(pitchD, yawD, 0f);
            // remise a niveau de l'assiette apres 0,9 s sans souris (sauf vaisseau vertical)
            Vector3 fwd = shipRot * Vector3.forward;
            if (Time.time - lastMouse > frame.shipAutoLevelDelay && Mathf.Abs(fwd.y) < 0.95f)
            {
                Vector3 up = shipRot * Vector3.up;
                Vector3 wantUp = Vector3.ProjectOnPlane(Vector3.up, fwd).normalized;
                float ang = Vector3.SignedAngle(up, wantUp, fwd);
                float step = Mathf.Clamp(ang, -frame.shipAutoLevelRate * Mathf.Rad2Deg * dt, frame.shipAutoLevelRate * Mathf.Rad2Deg * dt);
                shipRot = Quaternion.AngleAxis(step, fwd) * shipRot;
            }
            shipRot = Quaternion.Normalize(shipRot);

            Vector2 mv = input.MoveV;
            bool after = input.Held(input.Boost) && mv.y > 0.1f && Drain(frame.afterburnerCost, dt);
            float fz = mv.y > 0.1f ? mv.y * (after ? S.shipAfterburner : S.shipMax) : mv.y < -0.1f ? mv.y * S.shipReverse : 0f;
            float fx = mv.x * S.shipStrafe;
            float fy = (input.Held(input.Jump) ? 1f : 0f) - (input.Held(input.Down) ? 1f : 0f);
            Vector3 target = shipRot * new Vector3(fx, fy * frame.shipStrafeSpeed, fz);
            velocity = Vector3.Lerp(velocity, target, MathX.Damp(frame.shipAccel, dt));
            ThrustVector = target.sqrMagnitude > 1f ? target.normalized * (after ? 1.6f : 1f) : Vector3.zero;
            EnergyState = after ? "POSTCOMBUSTION" : Overheated ? "SURCHAUFFE" : "";
            boosting = after; assault = false;

            // tonneau d'esquive (E) : impulsion laterale 150 x 1,15 m/s, rotation complete en 0,45 s
            if (input.Pressed(input.QuickBoost) && qbCd <= 0f && rollTimer <= 0f && TrySpend(S.quickCost))
            {
                rollDir = mv.x < -0.1f ? -1 : 1;
                velocity += shipRot * Vector3.right * rollDir * frame.barrelRollImpulse;
                rollTimer = frame.barrelRollTime; qbCd = frame.quickBoostCooldown;
                cam?.Shake(cam.data.shakeQuickBoost); Sfx.Play("qb");
            }
            if (rollTimer > 0f) rollTimer -= dt;
            float speed = velocity.magnitude;
            FovKick = cam != null ? Mathf.InverseLerp(cam.data.shipFovSpeedStart, cam.data.shipFovSpeedFull, speed) * cam.data.shipSpeedFov : 0f;
            yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------ transformation (§7.4, 0,75 s)
        void StartTransform()
        {
            transforming = true; transformTimer = 0f;
            targetForm = form == FrameForm.Mech ? FrameForm.Ship : FrameForm.Mech;
            assault = false; boosting = false; qbTimer = 0f;
            if (targetForm == FrameForm.Ship)
            {
                shipRot = cam != null ? cam.LookRotation : Quaternion.Euler(0f, yaw, 0f);
                SetCollider(FrameForm.Ship);
                Msg.Post("Forme vaisseau : vol libre, vitesse accrue, dégâts −10 %", MsgKind.Good);
            }
            else Msg.Post("Forme châssis : dégâts nominaux", MsgKind.Normal);
            cam?.OnTransform(targetForm);
            Sfx.Play("transform");
        }
        void TransformTick(float dt)
        {
            transformTimer += dt;
            float k = MathX.SmoothStep01(transformTimer / frame.transformTime);
            shipBlend = targetForm == FrameForm.Ship ? k : 1f - k;
            velocity = Vector3.Lerp(velocity, Vector3.zero, MathX.Damp(0.8f, dt));
            if (targetForm == FrameForm.Mech) velocity.y -= frame.gravity * 0.5f * k * dt;
            EnergyState = "";
            if (transformTimer >= frame.transformTime)
            {
                transforming = false; form = targetForm; shipBlend = form == FrameForm.Ship ? 1f : 0f;
                if (form == FrameForm.Mech)
                {
                    Vector3 f = shipRot * Vector3.forward; f.y = 0f;
                    if (f.sqrMagnitude > 0.01f) yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                    SetCollider(FrameForm.Mech);
                    cam?.Relevel();
                }
            }
        }
        public string FormLabel
        {
            get
            {
                if (transforming) return "Transformation…";
                if (form == FrameForm.Mech) return "Châssis";
                return velocity.sqrMagnitude < 4f ? "Vaisseau · stationnaire" : "Vaisseau · dégâts −10 %";
            }
        }

        // ------------------------------------------------------------------ deplacement, sol, limites
        void DoMove(float dt)
        {
            bool was = grounded;
            lastVy = velocity.y;
            var flags = cc.Move(velocity * dt);
            grounded = (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

            Vector3 p = transform.position; bool fix = false;
            float lim = world.playableLimit;
            if (Mathf.Abs(p.x) > lim) { p.x = Mathf.Sign(p.x) * lim; velocity.x = 0f; fix = true; }
            if (Mathf.Abs(p.z) > lim) { p.z = Mathf.Sign(p.z) * lim; velocity.z = 0f; fix = true; }
            if (p.y > world.ceiling) { p.y = world.ceiling; if (velocity.y > 0f) velocity.y = 0f; fix = true; }
            // plancher du prototype : max(terrain, -43,4) et -16 sur la lave (on marche sur l'eau peu profonde / la lave)
            float gh = TerrainSystem.Ground(p.x, p.z);
            float clear = Clear;
            if (p.y < gh + clear)
            {
                p.y = gh + clear; if (velocity.y < 0f) velocity.y = form == FrameForm.Mech && !transforming ? -2f : 0f;
                grounded = form == FrameForm.Mech || transforming; fix = true;
            }
            if (fix) { cc.enabled = false; transform.position = p; cc.enabled = true; }
            heightAboveGround = p.y - clear - gh;

            if (grounded && !was && form == FrameForm.Mech && lastVy < -frame.landingSpeed)
            {
                float k = Mathf.Clamp01((-lastVy - frame.landingSpeed) / 50f);
                landCrouch = Mathf.Max(landCrouch, Mathf.Lerp(0.35f, 1f, k));
                cam?.Shake(Mathf.Lerp(0.15f, cam.data.shakeLandingMax, k));
                Fx.Dust(p - Vector3.up * clear, 18f + 20f * k);
            }
            if (IsShip && velocity.magnitude > frame.shipSparkSpeed && heightAboveGround < 8f) Fx.Sparks(p - Vector3.up * clear, 0.5f);
        }

        void VisualTick(float dt)
        {
            if (visual == null) return;
            Quaternion mech = Quaternion.Euler(0f, yaw, 0f);
            Quaternion target = shipBlend <= 0f ? mech : shipBlend >= 1f ? shipRot : Quaternion.Slerp(mech, shipRot, shipBlend);
            visual.rotation = target;
            if (rig == null) return;
            rig.ship = shipBlend;
            landCrouch = Mathf.MoveTowards(landCrouch, 0f, dt / 0.45f);
            rig.crouch = landCrouch;
            Vector3 hv = new Vector3(velocity.x, 0f, velocity.z);
            float hs = hv.magnitude;
            bool walking = form == FrameForm.Mech && !transforming && grounded && !boosting && !assault && qbTimer <= 0f;
            rig.walkAmount = Mathf.MoveTowards(rig.walkAmount, walking ? Mathf.Clamp01(hs / Mathf.Max(1f, S.walk)) : 0f, dt * 4f);
            if (walking)
            {
                float before = Mathf.Sin(walkPhase);
                walkPhase += hs * dt * Mathf.PI * 2f / 15f;
                if (Mathf.Sign(before) != Mathf.Sign(Mathf.Sin(walkPhase)) && hs > 3f) Fx.Dust(transform.position - Vector3.up * Clear, 6f);
            }
            rig.walkPhase = walkPhase;
            Vector3 right = mech * Vector3.right, fwd = mech * Vector3.forward;
            float lean = Mathf.Clamp01(Vector3.Dot(hv, fwd) / Mathf.Max(1f, S.groundBoost)) * frame.walkLeanMax;
            float side = Mathf.Clamp(Vector3.Dot(hv, right) / Mathf.Max(1f, S.groundBoost), -1f, 1f) * frame.walkSideLeanMax;
            rig.leanForward = Mathf.Lerp(rig.leanForward, lean, MathX.Damp(6f, dt));
            rig.leanSide = Mathf.Lerp(rig.leanSide, side, MathX.Damp(6f, dt));
            rig.barrelRoll = rollTimer > 0f ? -rollDir * (1f - rollTimer / frame.barrelRollTime) * Mathf.PI * 2f : 0f;
        }

        // ------------------------------------------------------------------ armure (§7.6, §10)
        public void TakeDamage(DamageInfo d)
        {
            if (dead || d.fromPlayer) return;
            float amount = d.amount;
            if (invulnerable) { AP = Mathf.Max(1f, AP - amount); return; }
            AP -= amount;
            cam?.Shake(cam.data.shakeHit);
            recentHits.Add(new KeyValuePair<float, Vector3>(Time.time, d.direction));
            if (recentHits.Count > 12) recentHits.RemoveAt(0);
            Sfx.Play("hurt");
            if (AP <= 0f) Die();
        }
        void ArmorTick(float dt)
        {
            apTrail = Mathf.MoveTowards(apTrail, AP, S.armor * frame.armorTrailRate * dt);
            if (apTrail < AP) apTrail = AP;
            if (AP < S.armor * frame.criticalArmor && AP > 0f)
                Msg.Post("AP critique : kit de réparation avec H", MsgKind.Danger, 6f);
            if (input.Pressed(input.Repair) && repairCd <= 0f)
            {
                repairCd = weaponData.repairCooldown;
                if (kits <= 0) Msg.Post("Plus de kit de réparation", MsgKind.Warning);
                else if (AP >= S.armor - 0.5f) Msg.Post("Armure déjà intacte", MsgKind.Normal);
                else { AP = Mathf.Min(S.armor, AP + S.armor * frame.repairAmount); kits--; Msg.Post($"Kit de réparation : +{Mathf.RoundToInt(frame.repairAmount * 100)} % d'AP ({kits} restant{(kits > 1 ? "s" : "")})", MsgKind.Good); Sfx.Play("buy"); }
            }
        }
        void Hazards(float dt)
        {
            Vector3 p = transform.position;
            // au-dessus de la lave : rectangle de lave et pieds sous -7 m
            if (world.InLavaRect(p.x, p.z) && p.y - Clear < frame.lavaDepth)
            {
                AP -= frame.lavaDamage * dt;
                Msg.Post("Lave : l'armure fond, quittez la coulée", MsgKind.Danger, 3f);
                Fx.Sparks(p - Vector3.up * Clear, 0.4f);
                if (AP <= 0f) Die();
            }
        }
        public void RefillKits() { kits = frame.repairKits; }

        // ------------------------------------------------------------------ charge de la lame (§9.1 : 270 m/s pendant 0,38 s)
        float bladeDashTimer; Vector3 bladeDashVel;
        public void BladeDash(Vector3 vel, float time) { bladeDashVel = vel; bladeDashTimer = time; assault = false; qbTimer = 0f; boosting = false; }
        public void StopBladeDash()
        {
            if (bladeDashTimer > 0f) velocity = bladeDashVel.normalized * Mathf.Min(bladeDashVel.magnitude, S.walk);
            bladeDashTimer = 0f;
        }
        public bool BladeDashing => bladeDashTimer > 0f;

        // ------------------------------------------------------------------ mort et redeploiement (§10.6)
        void Die()
        {
            if (dead) return;
            dead = true; AP = 0f; deathTimer = 0f; deathShown = false;
            velocity = Vector3.zero; boosting = assault = false;
            Fx.Explosion(transform.position, 22f, true);
            cam?.Shake(1.2f);
            Sfx.Play("boom");
        }
        void DeathTick(float dt)
        {
            deathTimer += dt;
            if (visual != null && deathTimer > 0.6f) visual.gameObject.SetActive(false);
            if (deathTimer > 0.3f && deathTimer < frame.deathDelay && Random.value < 0.25f)
                Fx.Explosion(transform.position + Random.insideUnitSphere * 5f, 8f, false);
            if (!deathShown && deathTimer >= frame.deathDelay) { deathShown = true; GameManager.I?.ShowDead(); }
        }
        public void Redeploy()
        {
            dead = false;
            if (visual != null) visual.gameObject.SetActive(true);
            form = FrameForm.Mech; transforming = false; shipBlend = 0f; SetCollider(FrameForm.Mech);
            AP = apTrail = S.armor; EN = S.energy; overheatTimer = 0f; recovering = false; kits = frame.repairKits;
            velocity = Vector3.zero; landCrouch = 0f;
            cc.enabled = false; transform.position = SpawnPosition + Vector3.up * (frame.frameClear + 0.5f); cc.enabled = true;
            yaw = spawnPoint != null ? spawnPoint.eulerAngles.y : 0f; cam?.Snap(yaw, 0f);
        }
    }
}
