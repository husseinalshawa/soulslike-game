using System;
using SoulsGame.Combat;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Player
{
    // آلة حالات اللاعب — منطق خالص يقاد بالزمن عبر Tick. جوهر إحساس السولز:
    // كل فعل التزام، والخروج منه فقط عبر النوافذ المعرّفة في AttackTimeline.
    // الغلاف (PlayerAvatar) يوصل الإدخال والأحداث بالمشهد والأنيميشن.
    public class PlayerBrain
    {
        private enum BufferedAction { None, Dodge, LightAttack, HeavyAttack, Cast, Skill }

        public const float DodgeDuration = 0.6f;
        public const float DodgeIFrameStart = 0.1f;
        public const float DodgeIFrameEnd = 0.4f;
        public const float StaggerDuration = 0.8f;
        public const float InputBufferSeconds = 0.2f;

        // timeline الإلقاء الموحد: تحضير/فعل/استراحة
        private static readonly AttackTimeline CastTimeline = new AttackTimeline(0.35f, 0.15f, 0.4f, 0.9f);

        private readonly StaminaPool stamina;
        private readonly CharacterStats stats;
        private readonly FocusPool focus;
        private WeaponData weapon;
        private float dodgeDurationMultiplier = 1f;

        private SpellData castSpell;
        private SpellData pendingCastSpell; // المطلوبة وقت الضغط — تُستهلك عند التنفيذ
        private bool spellReleased;
        private int skillWindowIndex;
        private bool skillWindowOpen;

        private float stateTime;
        private int comboIndex;
        private bool blockHeld;
        private bool invulnerable;
        private bool hitboxOpened;
        private bool hitboxClosed;
        private BufferedAction buffered;
        private float bufferRemaining;

        public PlayerStateId CurrentState { get; private set; } = PlayerStateId.Locomotion;
        public bool IsBlocking => CurrentState == PlayerStateId.Block;
        public WeaponData CurrentWeapon => weapon;
        public SkillData CurrentSkill => weapon.skill;

        public event Action<PlayerStateId> StateChanged;
        public event Action<AttackType> HitboxOpen;
        public event Action HitboxClose;
        public event Action<bool> InvulnerabilityChanged;
        public event Action StaminaRejected;
        public event Action GuardBroken; // صد بلا ستامينا كافية — اهتزاز الكاميرا يستهلكه
        public event Action FocusRejected;
        public event Action<SpellData> SpellReleased;
        public event Action SkillWindowOpen;
        public event Action SkillWindowClose;
        public event Action<float> SkillMotion; // مسافة اندفاع المهارة (الغلاف يحرك)

        public PlayerBrain(StaminaPool stamina, WeaponData weapon, CharacterStats stats, FocusPool focus = null)
        {
            this.stamina = stamina;
            this.weapon = weapon;
            this.stats = stats;
            this.focus = focus;
        }

        // التبديل مسموح من الوضع المحايد فقط (لا أثناء التزام).
        // يعيد false عند الرفض كي لا يتقدم الغلاف (hitbox/واجهة) على العقل.
        public bool SetWeapon(WeaponData newWeapon)
        {
            if (newWeapon == null)
                return false;
            if (ReferenceEquals(weapon, newWeapon))
                return true;
            if (CurrentState != PlayerStateId.Locomotion && CurrentState != PlayerStateId.Block)
                return false;
            weapon = newWeapon;
            comboIndex = 0;
            return true;
        }

        // ملف تفادي الدرع: يمدد مدة الالتزام، ونافذة الحصانة تبقى مطلقة.
        public void SetDodgeProfile(float durationMultiplier) =>
            dodgeDurationMultiplier = Mathf.Max(0.5f, durationMultiplier);

        // الضغط أثناء التزام يُخزَّن (كالهجمات)، وخصم الـFP لحظة التنفيذ الفعلي.
        public bool RequestCast(SpellData spell)
        {
            if (spell == null)
                return false;
            pendingCastSpell = spell;
            return Request(BufferedAction.Cast);
        }

        public bool RequestSkill() => Request(BufferedAction.Skill);

        private AttackTimeline CurrentTimeline =>
            CurrentState == PlayerStateId.AttackHeavy ? weapon.heavy : weapon.light;

        // ---- أوامر الإدخال ----

        public bool RequestDodge() => Request(BufferedAction.Dodge);

        public bool RequestLightAttack() => Request(BufferedAction.LightAttack);

        public bool RequestHeavyAttack() => Request(BufferedAction.HeavyAttack);

        public void SetBlockHeld(bool held)
        {
            blockHeld = held;
            if (CurrentState == PlayerStateId.Locomotion && held)
                Enter(PlayerStateId.Block);
            else if (CurrentState == PlayerStateId.Block && !held)
                Enter(PlayerStateId.Locomotion);
        }

        private bool Request(BufferedAction action)
        {
            if (CurrentState == PlayerStateId.Death)
                return false;

            if (CanActNow(action))
                return Execute(action);

            if (CanEverAct(action))
            {
                buffered = action;
                bufferRemaining = InputBufferSeconds;
            }
            return false;
        }

        // هل الحالة الحالية تسمح بتنفيذ الفعل في هذه اللحظة؟
        private bool CanActNow(BufferedAction action)
        {
            switch (CurrentState)
            {
                case PlayerStateId.Locomotion:
                case PlayerStateId.Block:
                    return true;
                case PlayerStateId.AttackLight:
                case PlayerStateId.AttackHeavy:
                    if (stateTime < CurrentTimeline.cancelWindowStart)
                        return false;
                    // التسلسل: ضربة خفيفة إضافية فقط ضمن طول الكومبو
                    if (action == BufferedAction.LightAttack)
                        return CurrentState == PlayerStateId.AttackLight && comboIndex + 1 < weapon.comboLength;
                    return action == BufferedAction.Dodge;
                case PlayerStateId.Dodge:
                    // إلغاء متأخر من التفادي إلى هجوم — نقطة الإلغاء تتمدد مع
                    // مضاعف الدرع (الصفائح تدفع ثمن ثقلها فعلاً)، والحصانة مطلقة
                    return stateTime >= DodgeIFrameEnd * dodgeDurationMultiplier &&
                           (action == BufferedAction.LightAttack || action == BufferedAction.HeavyAttack);
                default:
                    return false; // Stagger / Death / Cast / Skill (التزام كامل)
            }
        }

        // هل يستحق الفعل التخزين المؤقت؟ (الرفض بسبب الحالة لا بسبب الستامينا)
        private bool CanEverAct(BufferedAction action)
        {
            switch (CurrentState)
            {
                case PlayerStateId.AttackLight:
                case PlayerStateId.AttackHeavy:
                case PlayerStateId.Dodge:
                case PlayerStateId.Stagger:
                case PlayerStateId.Cast:  // الضغط أثناء الإلقاء/المهارة يُخزَّن
                case PlayerStateId.Skill: // لا يُهمل — الالتزام يبقى كاملاً
                    return true;
                default:
                    return false;
            }
        }

        private bool Execute(BufferedAction action)
        {
            switch (action)
            {
                case BufferedAction.Dodge:
                    if (!stamina.TryConsume(stats.dodgeStaminaCost))
                    {
                        StaminaRejected?.Invoke();
                        return false;
                    }
                    Enter(PlayerStateId.Dodge);
                    return true;

                case BufferedAction.LightAttack:
                {
                    if (!stamina.TryConsume(weapon.lightStaminaCost))
                    {
                        StaminaRejected?.Invoke();
                        return false;
                    }
                    int nextCombo = CurrentState == PlayerStateId.AttackLight ? comboIndex + 1 : 0;
                    Enter(PlayerStateId.AttackLight);
                    comboIndex = nextCombo;
                    return true;
                }

                case BufferedAction.HeavyAttack:
                    if (!stamina.TryConsume(weapon.heavyStaminaCost))
                    {
                        StaminaRejected?.Invoke();
                        return false;
                    }
                    Enter(PlayerStateId.AttackHeavy);
                    return true;

                case BufferedAction.Cast:
                {
                    var spell = pendingCastSpell;
                    if (spell == null)
                        return false;
                    if (focus == null || !focus.TrySpend(spell.fpCost))
                    {
                        FocusRejected?.Invoke();
                        return false;
                    }
                    castSpell = spell;
                    Enter(PlayerStateId.Cast);
                    return true;
                }

                case BufferedAction.Skill:
                    if (focus == null || !focus.TrySpend(weapon.skill.fpCost))
                    {
                        FocusRejected?.Invoke();
                        return false;
                    }
                    Enter(PlayerStateId.Skill);
                    if (weapon.skill.dashDistance > 0f)
                        SkillMotion?.Invoke(weapon.skill.dashDistance);
                    return true;

                default:
                    return false;
            }
        }

        // ---- إشعارات خارجية (من Damageable عبر الغلاف) ----

        public bool NotifyBlockedHit(DamagePacket hit)
        {
            if (CurrentState != PlayerStateId.Block)
                return false;

            if (stamina.TryConsume(hit.HealthDamage))
                return true;

            Enter(PlayerStateId.Stagger); // كسر الحرس
            GuardBroken?.Invoke();
            return false;
        }

        public void NotifyDamaged(bool poiseBroken)
        {
            if (CurrentState == PlayerStateId.Death)
                return;
            if (poiseBroken)
                Enter(PlayerStateId.Stagger);
        }

        public void NotifyDied()
        {
            Enter(PlayerStateId.Death);
        }

        // الإحياء عند إعادة الظهور — المخرج الوحيد من حالة الموت.
        public void Revive()
        {
            if (CurrentState == PlayerStateId.Death)
            {
                blockHeld = false;
                Enter(PlayerStateId.Locomotion);
            }
        }

        // ---- الزمن ----

        public void Tick(float deltaTime)
        {
            if (bufferRemaining > 0f)
                bufferRemaining -= deltaTime;

            stateTime += deltaTime;

            switch (CurrentState)
            {
                case PlayerStateId.Dodge:
                    TickDodge();
                    break;
                case PlayerStateId.AttackLight:
                case PlayerStateId.AttackHeavy:
                    TickAttack();
                    break;
                case PlayerStateId.Cast:
                    TickCast();
                    break;
                case PlayerStateId.Skill:
                    TickSkill();
                    break;
                case PlayerStateId.Stagger:
                    if (stateTime >= StaggerDuration)
                        Enter(blockHeld ? PlayerStateId.Block : PlayerStateId.Locomotion);
                    break;
            }

            TryConsumeBuffer();
        }

        private void TickDodge()
        {
            if (!invulnerable && stateTime >= DodgeIFrameStart && stateTime < DodgeIFrameEnd)
                SetInvulnerable(true);
            else if (invulnerable && stateTime >= DodgeIFrameEnd)
                SetInvulnerable(false);

            // الدرع الثقيل يطيل ذيل الالتزام — الحصانة تبقى [0.1, 0.4] مطلقة
            if (stateTime >= DodgeDuration * dodgeDurationMultiplier)
                Enter(blockHeld ? PlayerStateId.Block : PlayerStateId.Locomotion);
        }

        private void TickCast()
        {
            if (!spellReleased && stateTime >= CastTimeline.windup)
            {
                spellReleased = true;
                SpellReleased?.Invoke(castSpell);
            }
            if (stateTime >= CastTimeline.Total)
                Enter(blockHeld ? PlayerStateId.Block : PlayerStateId.Locomotion);
        }

        private void TickSkill()
        {
            var skill = weapon.skill;
            int count = Math.Max(1, skill.hitCount);
            float slice = skill.timeline.active / count;

            if (!skillWindowOpen && skillWindowIndex < count &&
                stateTime >= skill.timeline.windup + skillWindowIndex * slice)
            {
                skillWindowOpen = true;
                SkillWindowOpen?.Invoke();
            }
            if (skillWindowOpen &&
                stateTime >= skill.timeline.windup + skillWindowIndex * slice + slice * 0.5f)
            {
                skillWindowOpen = false;
                skillWindowIndex++;
                SkillWindowClose?.Invoke();
            }
            if (stateTime >= skill.timeline.Total)
                Enter(blockHeld ? PlayerStateId.Block : PlayerStateId.Locomotion);
        }

        private void TickAttack()
        {
            var timeline = CurrentTimeline;
            var type = CurrentState == PlayerStateId.AttackHeavy ? AttackType.Heavy : AttackType.Light;

            if (!hitboxOpened && stateTime >= timeline.windup)
            {
                hitboxOpened = true;
                HitboxOpen?.Invoke(type);
            }
            if (hitboxOpened && !hitboxClosed && stateTime >= timeline.windup + timeline.active)
            {
                hitboxClosed = true;
                HitboxClose?.Invoke();
            }
            if (stateTime >= timeline.Total)
                Enter(blockHeld ? PlayerStateId.Block : PlayerStateId.Locomotion);
        }

        private void TryConsumeBuffer()
        {
            if (buffered == BufferedAction.None || bufferRemaining <= 0f)
            {
                buffered = BufferedAction.None;
                return;
            }
            if (!CanActNow(buffered))
                return;

            var action = buffered;
            buffered = BufferedAction.None;
            Execute(action);
        }

        private void Enter(PlayerStateId next)
        {
            // إنهاء آثار الحالة السابقة قبل الانتقال
            if (hitboxOpened && !hitboxClosed)
            {
                hitboxClosed = true;
                HitboxClose?.Invoke();
            }
            if (skillWindowOpen)
            {
                skillWindowOpen = false;
                SkillWindowClose?.Invoke();
            }
            if (invulnerable)
                SetInvulnerable(false);

            if ((CurrentState == PlayerStateId.AttackLight && next != PlayerStateId.AttackLight) ||
                CurrentState == PlayerStateId.AttackHeavy)
                comboIndex = 0;

            CurrentState = next;
            stateTime = 0f;
            hitboxOpened = false;
            hitboxClosed = false;
            spellReleased = false;
            skillWindowIndex = 0;

            if (next == PlayerStateId.Death)
            {
                buffered = BufferedAction.None;
                bufferRemaining = 0f;
            }

            StateChanged?.Invoke(next);
        }

        private void SetInvulnerable(bool value)
        {
            invulnerable = value;
            InvulnerabilityChanged?.Invoke(value);
        }
    }
}
