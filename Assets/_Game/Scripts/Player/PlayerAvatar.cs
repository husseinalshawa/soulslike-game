using System.Collections.Generic;
using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Progression;
using SoulsGame.World;
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
        public HorseAvatar horse;
        public List<ForbiddenZone> mountForbiddenZones = new List<ForbiddenZone>();
        public MarshZone marsh; // الماء الضحل حول الكنيسة — يسنده البناء

        // توقيف إدخال القتال والحركة أثناء القوائم (قائمة رفع المستوى)
        public bool InputLocked { get; set; }

        // حدث زر التفاعل (E) — يستهلكه GameLoop للجلوس عند مواقع الراحة
        public event System.Action InteractPressed;

        // تغيّر العتاد المجهز — يستهلكه سطر العتاد في الواجهة
        public event System.Action GearChanged;

        public PlayerBrain Brain { get; private set; }
        public StaminaPool Stamina { get; private set; }
        public SoulsWallet Wallet { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public MountSystem Mount { get; private set; }
        public bool IsRiding => Mount != null && Mount.IsMounted;

        public GearInventory<WeaponData> Weapons { get; } = new GearInventory<WeaponData>();
        public GearInventory<ArmorData> Armors { get; } = new GearInventory<ArmorData>();
        public GearInventory<SpellData> Spells { get; } = new GearInventory<SpellData>();
        public FocusPool Focus { get; private set; }
        public WeaponBuff Buff { get; } = new WeaponBuff();

        public ArmorData defaultArmor;
        public SpellData spellBolt;
        public SpellData spellBuff;
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
            Wallet = new SoulsWallet();
            if (progressionData == null)
                progressionData = ScriptableObject.CreateInstance<ProgressionData>();
            Progression = new PlayerProgression(progressionData, stats);
            Mount = new MountSystem();
            Focus = new FocusPool(Progression.MaxFocus);

            Weapons.Add(weapon);
            if (defaultArmor == null) defaultArmor = GearLibrary.Cloth();
            Armors.Add(defaultArmor);
            if (spellBolt == null) spellBolt = GearLibrary.GlintBolt();
            if (spellBuff == null) spellBuff = GearLibrary.WeaponBuffSpell();
            Spells.Add(spellBolt);
            Spells.Add(spellBuff);

            Brain = new PlayerBrain(Stamina, weapon, stats, Focus);

            Controls = new PlayerControls();
            Controls.Enable();

            WireBrainEvents();
            WireDamageEvents();
            WireInput();
            EquipCurrentWeapon();
            EquipCurrentArmor();
        }

        // الالتقاط يضيف ويجهز فوراً
        public void AcquireWeapon(WeaponData newWeapon)
        {
            Weapons.Add(newWeapon);
            Weapons.Select(newWeapon);
            EquipCurrentWeapon();
        }

        public void AcquireArmor(ArmorData newArmor)
        {
            Armors.Add(newArmor);
            Armors.Select(newArmor);
            EquipCurrentArmor();
        }

        // ضرر ضربة السلاح الحالية: السلاح + القوة + التعزيز.
        public float CurrentAttackDamage(AttackType type)
        {
            var w = Brain.CurrentWeapon;
            float damage = type == AttackType.Heavy ? w.heavyDamage : w.lightDamage;
            return damage + Progression.BonusDamage + Buff.CurrentBonus;
        }

        // ضرر ضربة المهارة: المهارة + التعزيز («ينطبق على السلاح المجهز وقت الضربة»).
        public float CurrentSkillDamage() => Brain.CurrentSkill.damage + Buff.CurrentBonus;

        private void OnSpellReleased(SpellData spell)
        {
            if (spell.isBuff)
            {
                Buff.Activate(spell.buffBonusDamage, spell.buffDuration);
                return;
            }

            // قذيفة: نحو هدف القفل أو للأمام
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 direction = transform.forward;
            if (lockOn != null && lockOn.CurrentTarget != null)
                direction = (lockOn.CurrentTarget.Position - origin).normalized;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GlintBolt";
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var boltCollider = go.GetComponent<Collider>();
            if (boltCollider != null) boltCollider.enabled = false;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.5f, 0.7f, 1f);
            var projectile = go.AddComponent<ProjectileAvatar>();
            projectile.Launch(origin, direction, spell.projectileSpeed, spell.maxRange,
                new DamagePacket(spell.damage, spell.poiseDamage, transform.position));
        }

        private void OnSkillWindowOpen()
        {
            var skill = Brain.CurrentSkill;
            var packet = new DamagePacket(CurrentSkillDamage(), skill.poiseDamage, transform.position);

            if (skill.radius > 0f)
            {
                // ضربة دائرية حول اللاعب (نفس مسار ضربة البوس الأرضية)
                var targets = LockOnSystem.Targets;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (!targets[i].IsAlive)
                        continue;
                    if (Vector3.Distance(transform.position, targets[i].Position) > skill.radius)
                        continue;
                    var component = targets[i] as Component;
                    var damageable = component != null ? component.GetComponent<Damageable>() : null;
                    if (damageable != null && damageable != Damageable)
                        damageable.ApplyDamage(packet);
                }
                return;
            }

            if (weaponHitbox != null)
                weaponHitbox.Activate(packet);
        }

        public void EquipCurrentWeapon()
        {
            var w = Weapons.Current;
            if (w == null) return;
            // ذرية التجهيز: رفض العقل (أثناء التزام/موت) يبقي الـ hitbox على
            // السلاح القديم — المزامنة تعاد تلقائياً عند العودة للمحايد.
            if (!Brain.SetWeapon(w)) return;
            if (weaponHitbox != null)
            {
                // شكل السلاح والـ hitbox يتبعان بيانات السلاح
                weaponHitbox.transform.localScale = new Vector3(w.hitboxWidth, w.hitboxWidth, w.reach);
                weaponHitbox.transform.localPosition = new Vector3(0f, 0.2f, 0.2f + w.reach * 0.5f);
            }
        }

        public void EquipCurrentArmor()
        {
            var a = Armors.Current;
            if (a == null) return;
            Damageable.DamageTransform = packet => new DamagePacket(
                packet.HealthDamage * (1f - a.damageReduction), packet.PoiseDamage, packet.SourcePosition);
            Brain.SetDodgeProfile(a.dodgeDurationMultiplier);
            motor.DodgeDistanceMultiplier = a.dodgeDistanceMultiplier;
            motor.DodgeDurationMultiplier = a.dodgeDurationMultiplier;
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = a.tint;
        }

        private void WireBrainEvents()
        {
            Brain.HitboxOpen += type =>
            {
                if (weaponHitbox == null) return;
                var w = Brain.CurrentWeapon;
                float poise = type == AttackType.Heavy ? w.heavyPoiseDamage : w.lightPoiseDamage;
                weaponHitbox.Activate(new DamagePacket(CurrentAttackDamage(type), poise, transform.position));
            };
            Brain.SpellReleased += OnSpellReleased;
            Brain.SkillWindowOpen += OnSkillWindowOpen;
            Brain.SkillWindowClose += () =>
            {
                if (weaponHitbox != null) weaponHitbox.Deactivate();
            };
            Brain.SkillMotion += distance => motor.BeginSkillDash(distance);
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
                // تبديل/التقاط رُفض أثناء التزام: المخزون تقدم والعقل لا —
                // العودة للمحايد تلحق العقل والـ hitbox بالمخزون
                if ((state == PlayerStateId.Locomotion || state == PlayerStateId.Block) &&
                    !ReferenceEquals(Brain.CurrentWeapon, Weapons.Current))
                    EquipCurrentWeapon();
            };
        }

        private void WireDamageEvents()
        {
            // الصد يعترض الضربة قبل أن تمس الصحة؛ فشله = كسر حرس (يعالجه الـ Brain)
            // — ولا اعتراض إطلاقاً على السرج (لا صد ولا i-frames راكباً)
            Damageable.DamageInterceptor = packet => !IsRiding && Brain.NotifyBlockedHit(packet);
            Damageable.PoiseBroken += () => Brain.NotifyDamaged(poiseBroken: true);
            Damageable.Damaged += _ =>
            {
                // ضربة على السرج = ترجّل إجباري (الضرر طُبّق كاملاً قبل هذا الحدث)
                if (Mount.NotifyRiderHit())
                {
                    motor.Frozen = false;
                    if (horse != null)
                        horse.DismountRider();
                }
            };
            Damageable.Died += () =>
            {
                ForceDismount();
                Brain.NotifyDied();
            };
        }

        // زر الاستدعاء: راكباً = ترجّل؛ وإلا محاولة استدعاء وركوب فوري.
        public bool TrySummonHorse()
        {
            if (IsRiding)
            {
                ForceDismount();
                return true;
            }

            if (!Mount.TrySummon(transform.position, mountForbiddenZones, Brain.CurrentState, InputLocked))
                return false;

            Brain.SetBlockHeld(false); // لا صد على السرج — الركوب يسقط حالة الصد
            if (lockOn != null)
                lockOn.Release(); // لا قفل على السرج
            motor.Frozen = true;
            if (horse != null)
                horse.SummonAndMount(this);
            return true;
        }

        public void ForceDismount()
        {
            if (!Mount.IsMounted && (horse == null || !horse.HasRider))
                return;
            Mount.Dismount();
            motor.Frozen = false;
            if (horse != null)
                horse.DismountRider();
        }

        // يطبّق القيم المشتقة من التقدم على المجمّعات (يُستدعى بعد كل رفع مستوى).
        public void ApplyProgression()
        {
            Damageable.Health.SetMax(Progression.MaxHealth);
            Stamina.SetMax(Progression.MaxStamina);
            Focus.SetMax(Progression.MaxFocus);
        }

        private void WireInput()
        {
            Controls.Dodge.performed += _ =>
            {
                if (InputLocked) return;
                if (IsRiding)
                {
                    if (horse != null) horse.Jump(); // التفادي راكباً = قفزة الحصان
                    return;
                }
                Brain.RequestDodge();
            };
            Controls.Mount.performed += _ =>
            {
                if (!InputLocked) TrySummonHorse();
            };
            Controls.SwitchWeapon.performed += _ =>
            {
                if (InputLocked || IsRiding) return;
                var before = Weapons.Current;
                var next = Weapons.Cycle();
                if (!ReferenceEquals(before, next))
                {
                    EquipCurrentWeapon();
                    GearChanged?.Invoke();
                }
            };
            Controls.SwitchArmor.performed += _ =>
            {
                if (InputLocked || IsRiding) return;
                if (Brain.CurrentState != PlayerStateId.Locomotion && Brain.CurrentState != PlayerStateId.Block)
                    return;
                var before = Armors.Current;
                var next = Armors.Cycle();
                if (!ReferenceEquals(before, next))
                {
                    EquipCurrentArmor();
                    GearChanged?.Invoke();
                }
            };
            Controls.SwitchSpell.performed += _ =>
            {
                if (InputLocked || IsRiding) return;
                Spells.Cycle();
                GearChanged?.Invoke();
            };
            Controls.CastSpell.performed += _ =>
            {
                if (!InputLocked && !IsRiding) Brain.RequestCast(Spells.Current);
            };
            Controls.WeaponSkill.performed += _ =>
            {
                if (!InputLocked && !IsRiding) Brain.RequestSkill();
            };
            // يعمل حتى أثناء قفل الإدخال — إغلاق القائمة يمر من هنا
            Controls.Interact.performed += _ => InteractPressed?.Invoke();
            Controls.LightAttack.performed += _ =>
            {
                if (InputLocked || IsRiding) return;
                // Shift+زر أيسر = ثقيلة؛ الربط البسيط للزر الأيسر يُطلق أيضاً —
                // نتجاهله والـ Shift مضغوط حتى لا تخرج ضربتان من ضغطة واحدة
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                    return;
                Brain.RequestLightAttack();
            };
            Controls.HeavyAttack.performed += _ =>
            {
                if (!InputLocked && !IsRiding) Brain.RequestHeavyAttack();
            };
            Controls.Block.performed += _ =>
            {
                if (!InputLocked && !IsRiding) Brain.SetBlockHeld(true);
            };
            Controls.Block.canceled += _ => Brain.SetBlockHeld(false);
            Controls.LockOn.performed += _ =>
            {
                if (!InputLocked && !IsRiding && lockOn != null) lockOn.Toggle();
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 move = InputLocked ? Vector2.zero : Controls.Move.ReadValue<Vector2>();

            bool wantsSprint = !IsRiding && Controls.Sprint.IsPressed() && move.sqrMagnitude > 0.01f;
            isSprinting = wantsSprint && Stamina.Current > 0f &&
                          Brain.CurrentState == PlayerStateId.Locomotion;

            Stamina.Tick(dt, Brain.IsBlocking, isSprinting);
            Focus.Tick(dt);
            Buff.Tick(dt);
            Brain.Tick(dt);

            // السلاح يتوهج أزرق أثناء التعزيز
            if (weaponHitbox != null)
            {
                var weaponRenderer = weaponHitbox.GetComponent<Renderer>();
                if (weaponRenderer != null)
                    weaponRenderer.material.color = Buff.IsActive
                        ? new Color(0.45f, 0.65f, 1f)
                        : new Color(0.6f, 0.6f, 0.65f);
            }

            if (IsRiding && horse != null)
            {
                horse.MoveInput = move; // راكباً: الإدخال يقود الحصان
            }
            else
            {
                motor.MoveInput = move;
                motor.SprintActive = isSprinting;
                motor.State = Brain.CurrentState;
                motor.TerrainSpeedMultiplier =
                    marsh != null && marsh.Contains(transform.position)
                        ? MarshZone.SpeedMultiplier : 1f;
            }

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
