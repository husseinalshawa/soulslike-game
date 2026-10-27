using System;
using SoulsGame.Data;

namespace SoulsGame.Enemies
{
    public enum EnemyStateId
    {
        Patrol,
        Chase,
        AttackWindup,
        AttackActive,
        AttackRecovery,
        Stagger,
        Returning, // تجاوز الـ leash: يعود لمعسكره متجاهلاً اللاعب
        Dead
    }

    // آلة حالات العدو — نمط السولز: هجوم مُعلَن مسبقاً (windup مقروء)،
    // ثم نافذة ضرر قصيرة، ثم استراحة طويلة هي فرصة اللاعب للعقاب.
    // منطق خالص يقاد بإدراك مُمرَّر (مسافة وزاوية اللاعب) ليكون قابلاً للاختبار.
    public class EnemyBrain
    {
        private readonly WeaponData weapon;
        private readonly float detectRange;
        private readonly float fovDegrees;
        private readonly float attackRange;
        private readonly float staggerDuration;

        // أبعد مسافة مطاردة عن نقطة المعسكر قبل التراجع
        public const float LeashRange = 25f;
        private const float HomeArrivalRadius = 2f;

        private float stateTime;

        public EnemyStateId CurrentState { get; private set; } = EnemyStateId.Patrol;

        public event Action<EnemyStateId> StateChanged;
        public event Action HitboxOpen;
        public event Action HitboxClose;

        public EnemyBrain(WeaponData weapon, float detectRange = 10f, float fovDegrees = 90f,
            float attackRange = 2f, float staggerDuration = 0.8f)
        {
            this.weapon = weapon;
            this.detectRange = detectRange;
            this.fovDegrees = fovDegrees;
            this.attackRange = attackRange;
            this.staggerDuration = staggerDuration;
        }

        public void Tick(float deltaTime, float distanceToPlayer, float angleToPlayerDegrees,
            float distanceFromHome = 0f)
        {
            stateTime += deltaTime;

            switch (CurrentState)
            {
                case EnemyStateId.Patrol:
                    if (distanceToPlayer <= detectRange && angleToPlayerDegrees <= fovDegrees * 0.5f)
                        Enter(EnemyStateId.Chase);
                    break;

                case EnemyStateId.Chase:
                    if (distanceFromHome > LeashRange)
                    {
                        Enter(EnemyStateId.Returning);
                        break;
                    }
                    if (distanceToPlayer <= attackRange)
                        Enter(EnemyStateId.AttackWindup);
                    break;

                case EnemyStateId.Returning:
                    // hysteresis: يتجاهل اللاعب تماماً حتى يصل بيته
                    if (distanceFromHome <= HomeArrivalRadius)
                        Enter(EnemyStateId.Patrol);
                    break;

                case EnemyStateId.AttackWindup:
                    if (stateTime >= weapon.light.windup)
                    {
                        Enter(EnemyStateId.AttackActive);
                        HitboxOpen?.Invoke();
                    }
                    break;

                case EnemyStateId.AttackActive:
                    if (stateTime >= weapon.light.active)
                    {
                        HitboxClose?.Invoke();
                        Enter(EnemyStateId.AttackRecovery);
                    }
                    break;

                case EnemyStateId.AttackRecovery:
                    if (stateTime >= weapon.light.recovery)
                        Enter(EnemyStateId.Chase);
                    break;

                case EnemyStateId.Stagger:
                    if (stateTime >= staggerDuration)
                        Enter(EnemyStateId.Chase);
                    break;
            }
        }

        public void NotifyPoiseBroken()
        {
            if (CurrentState == EnemyStateId.Dead)
                return;

            if (CurrentState == EnemyStateId.AttackActive)
                HitboxClose?.Invoke();

            Enter(EnemyStateId.Stagger);
        }

        public void NotifyDied()
        {
            if (CurrentState == EnemyStateId.AttackActive)
                HitboxClose?.Invoke();

            Enter(EnemyStateId.Dead);
        }

        public void ResetToPatrol()
        {
            Enter(EnemyStateId.Patrol);
        }

        private void Enter(EnemyStateId next)
        {
            CurrentState = next;
            stateTime = 0f;
            StateChanged?.Invoke(next);
        }
    }
}
