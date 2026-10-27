using SoulsGame.Combat;
using SoulsGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulsGame.Player
{
    // الغلاف الجامع للاعب: يبني StaminaPool وPlayerBrain من الأصول، يوصل
    // الإدخال بأوامر الـ Brain، ويترجم أحداثه إلى المشهد (Hitbox، مناعة، حركة).
    [RequireComponent(typeof(PlayerMotor), typeof(Damageable))]
    public class PlayerAvatar : MonoBehaviour
    {
        public CharacterStats stats;
        public WeaponData weapon;
        public Hitbox weaponHitbox;
        public LockOnSystem lockOn;
        public ThirdPersonCamera cameraRig;

        public PlayerBrain Brain { get; private set; }
        public StaminaPool Stamina { get; private set; }
        public Damageable Damageable { get; private set; }
        public PlayerControls Controls { get; private set; }

        private PlayerMotor motor;
        private bool isSprinting;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            Damageable = GetComponent<Damageable>();

            Stamina = new StaminaPool(stats.maxStamina, stats.staminaRegenPerSecond,
                stats.staminaRegenDelay, stats.blockingRegenMultiplier);
            Stamina.SetSprintCost(stats.sprintStaminaPerSecond);
            Brain = new PlayerBrain(Stamina, weapon, stats);

            Controls = new PlayerControls();
            Controls.Enable();

            WireBrainEvents();
            WireDamageEvents();
            WireInput();
        }

        private void WireBrainEvents()
        {
            Brain.HitboxOpen += type =>
            {
                if (weaponHitbox == null) return;
                float damage = type == AttackType.Heavy ? weapon.heavyDamage : weapon.lightDamage;
                float poise = type == AttackType.Heavy ? weapon.heavyPoiseDamage : weapon.lightPoiseDamage;
                weaponHitbox.Activate(new DamagePacket(damage, poise, transform.position));
            };
            Brain.HitboxClose += () =>
            {
                if (weaponHitbox != null) weaponHitbox.Deactivate();
            };
            Brain.InvulnerabilityChanged += on => Damageable.IsInvulnerable = on;
            Brain.StateChanged += state =>
            {
                motor.State = state;
                if (state == PlayerStateId.Dodge)
                    motor.BeginDodge();
            };
        }

        private void WireDamageEvents()
        {
            // الصد يعترض الضربة قبل أن تمس الصحة؛ فشله = كسر حرس (يعالجه الـ Brain)
            Damageable.DamageInterceptor = packet => Brain.NotifyBlockedHit(packet);
            Damageable.PoiseBroken += () => Brain.NotifyDamaged(poiseBroken: true);
            Damageable.Died += () => Brain.NotifyDied();
        }

        private void WireInput()
        {
            Controls.Dodge.performed += _ => Brain.RequestDodge();
            Controls.LightAttack.performed += _ => Brain.RequestLightAttack();
            Controls.HeavyAttack.performed += _ => Brain.RequestHeavyAttack();
            Controls.Block.performed += _ => Brain.SetBlockHeld(true);
            Controls.Block.canceled += _ => Brain.SetBlockHeld(false);
            Controls.LockOn.performed += _ =>
            {
                if (lockOn != null) lockOn.Toggle();
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 move = Controls.Move.ReadValue<Vector2>();

            bool wantsSprint = Controls.Sprint.IsPressed() && move.sqrMagnitude > 0.01f;
            isSprinting = wantsSprint && Stamina.Current > 0f &&
                          Brain.CurrentState == PlayerStateId.Locomotion;

            Stamina.Tick(dt, Brain.IsBlocking, isSprinting);
            Brain.Tick(dt);

            motor.MoveInput = move;
            motor.SprintActive = isSprinting;
            motor.State = Brain.CurrentState;

            Vector2 look = Controls.Look.ReadValue<Vector2>();
            if (cameraRig != null)
                cameraRig.LookInput = look;
            if (lockOn != null)
                lockOn.ReportLookInput(look);
        }

        private void OnDestroy()
        {
            Controls?.Disable();
        }
    }
}
