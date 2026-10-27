using System;
using SoulsGame.Combat;
using SoulsGame.Data;

namespace SoulsGame.Player
{
    // آلة حالات اللاعب — منطق خالص يقاد بالزمن عبر Tick. جوهر إحساس السولز:
    // كل فعل التزام، والخروج منه فقط عبر النوافذ المعرّفة في AttackTimeline.
    // الغلاف (PlayerAvatar) يوصل الإدخال والأحداث بالمشهد والأنيميشن.
    public class PlayerBrain
    {
        private enum BufferedAction { None, Dodge, LightAttack, HeavyAttack }

        public const float DodgeDuration = 0.6f;
        public const float DodgeIFrameStart = 0.1f;
        public const float DodgeIFrameEnd = 0.4f;
        public const float StaggerDuration = 0.8f;
        public const float InputBufferSeconds = 0.2f;

        private readonly StaminaPool stamina;
        private readonly WeaponData weapon;
        private readonly CharacterStats stats;

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

        public event Action<PlayerStateId> StateChanged;
        public event Action<AttackType> HitboxOpen;
        public event Action HitboxClose;
        public event Action<bool> InvulnerabilityChanged;
        public event Action StaminaRejected;

        public PlayerBrain(StaminaPool stamina, WeaponData weapon, CharacterStats stats)
        {
            this.stamina = stamina;
            this.weapon = weapon;
            this.stats = stats;
        }

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
                    // إلغاء متأخر من التفادي إلى هجوم بعد انتهاء الحصانة
                    return stateTime >= DodgeIFrameEnd &&
                           (action == BufferedAction.LightAttack || action == BufferedAction.HeavyAttack);
                default:
                    return false; // Stagger / Death
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

            if (stateTime >= DodgeDuration)
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
            if (invulnerable)
                SetInvulnerable(false);

            if ((CurrentState == PlayerStateId.AttackLight && next != PlayerStateId.AttackLight) ||
                CurrentState == PlayerStateId.AttackHeavy)
                comboIndex = 0;

            CurrentState = next;
            stateTime = 0f;
            hitboxOpened = false;
            hitboxClosed = false;

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
