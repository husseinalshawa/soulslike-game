using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Progression;
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
        public ProgressionData progressionData;
        public Hitbox weaponHitbox;
        public LockOnSystem lockOn;
        public ThirdPersonCamera cameraRig;

        // توقيف إدخال القتال والحركة أثناء القوائم (قائمة رفع المستوى)
        public bool InputLocked { get; set; }

        // حدث زر التفاعل (E) — يستهلكه GameLoop للجلوس عند مواقع الراحة
        public event System.Action InteractPressed;

        public PlayerBrain Brain { get; private set; }
        public StaminaPool Stamina { get; private set; }
        public SoulsWallet Wallet { get; private set; }
        public PlayerProgression Progression { get; private set; }
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
            Wallet = new SoulsWallet();
            if (progressionData == null)
                progressionData = ScriptableObject.CreateInstance<ProgressionData>();
            Progression = new PlayerProgression(progressionData, stats);

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
                damage += Progression.BonusDamage; // إحصائية القوة
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

        // يطبّق القيم المشتقة من التقدم على المجمّعات (يُستدعى بعد كل رفع مستوى).
        public void ApplyProgression()
        {
            Damageable.Health.SetMax(Progression.MaxHealth);
            Stamina.SetMax(Progression.MaxStamina);
        }

        private void WireInput()
        {
            Controls.Dodge.performed += _ =>
            {
                if (!InputLocked) Brain.RequestDodge();
            };
            // يعمل حتى أثناء قفل الإدخال — إغلاق القائمة يمر من هنا
            Controls.Interact.performed += _ => InteractPressed?.Invoke();
            Controls.LightAttack.performed += _ =>
            {
                if (InputLocked) return;
                // Shift+زر أيسر = ثقيلة؛ الربط البسيط للزر الأيسر يُطلق أيضاً —
                // نتجاهله والـ Shift مضغوط حتى لا تخرج ضربتان من ضغطة واحدة
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                    return;
                Brain.RequestLightAttack();
            };
            Controls.HeavyAttack.performed += _ =>
            {
                if (!InputLocked) Brain.RequestHeavyAttack();
            };
            Controls.Block.performed += _ =>
            {
                if (!InputLocked) Brain.SetBlockHeld(true);
            };
            Controls.Block.canceled += _ => Brain.SetBlockHeld(false);
            Controls.LockOn.performed += _ =>
            {
                if (!InputLocked && lockOn != null) lockOn.Toggle();
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 move = InputLocked ? Vector2.zero : Controls.Move.ReadValue<Vector2>();

            bool wantsSprint = Controls.Sprint.IsPressed() && move.sqrMagnitude > 0.01f;
            isSprinting = wantsSprint && Stamina.Current > 0f &&
                          Brain.CurrentState == PlayerStateId.Locomotion;

            Stamina.Tick(dt, Brain.IsBlocking, isSprinting);
            Brain.Tick(dt);

            motor.MoveInput = move;
            motor.SprintActive = isSprinting;
            motor.State = Brain.CurrentState;

            Vector2 mouseLook = InputLocked ? Vector2.zero : Controls.LookMouse.ReadValue<Vector2>();
            Vector2 stickLook = InputLocked ? Vector2.zero : Controls.LookStick.ReadValue<Vector2>();
            if (cameraRig != null)
            {
                cameraRig.MouseLook = mouseLook;
                cameraRig.StickLook = stickLook;
            }
            if (lockOn != null)
                lockOn.ReportLookInput(stickLook, mouseLook);
        }

        private void OnDestroy()
        {
            Controls?.Disable();
        }
    }
}
