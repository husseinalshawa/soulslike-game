using System;

namespace SoulsGame.Enemies
{
    public enum BossAttack { Smash, Sweep, Slam }

    public enum BossStateId { Dormant, Approach, Windup, Active, Recovery, Stagger, Dead }

    // عقل «حارس الساحة»: جبار بطيء بثلاث هجمات معلنة من جدول مثبت بالمواصفة.
    // الضربة الأرضية تُفرض دورياً حتى لا يكون الالتصاق بالبوس آمناً دائماً.
    public class BossBrain
    {
        private struct AttackDef
        {
            public float Windup;
            public float ActivePhase;
            public (float open, float close)[] Windows;
            public float Recovery;
            public float MaxRange;
        }

        private static readonly AttackDef Smash = new AttackDef
        { Windup = 0.9f, ActivePhase = 0.25f, Windows = new[] { (0f, 0.25f) }, Recovery = 1.6f, MaxRange = 3f };
        private static readonly AttackDef Sweep = new AttackDef
        { Windup = 0.7f, ActivePhase = 0.9f, Windows = new[] { (0f, 0.2f), (0.5f, 0.7f) }, Recovery = 1.2f, MaxRange = 4f };
        private static readonly AttackDef Slam = new AttackDef
        { Windup = 1.1f, ActivePhase = 0.2f, Windows = new[] { (0f, 0.2f) }, Recovery = 1.4f, MaxRange = 2.5f };

        private const float StaggerDuration = 0.8f;
        private const int SlamEvery = 3;

        private float stateTime;
        private int normalAttacksSinceSlam;
        private int windowIndex;
        private bool windowOpen;

        public BossStateId CurrentState { get; private set; } = BossStateId.Dormant;
        public BossAttack CurrentAttack { get; private set; }

        public event Action<BossStateId> StateChanged;
        public event Action<BossAttack> AttackChosen;
        public event Action<BossAttack> HitboxOpen;
        public event Action HitboxClose;
        public event Action StaggerEnded;

        private AttackDef Current => CurrentAttack switch
        {
            BossAttack.Smash => Smash,
            BossAttack.Sweep => Sweep,
            _ => Slam
        };

        public void Activate()
        {
            if (CurrentState == BossStateId.Dormant)
                Enter(BossStateId.Approach);
        }

        public void ResetFight()
        {
            CloseWindowIfOpen();
            normalAttacksSinceSlam = 0;
            Enter(BossStateId.Dormant);
        }

        public void NotifyPoiseBroken()
        {
            if (CurrentState == BossStateId.Dead || CurrentState == BossStateId.Dormant)
                return;
            CloseWindowIfOpen();
            Enter(BossStateId.Stagger);
        }

        public void NotifyDied()
        {
            CloseWindowIfOpen();
            Enter(BossStateId.Dead);
        }

        public void Tick(float deltaTime, float distanceToPlayer)
        {
            stateTime += deltaTime;

            switch (CurrentState)
            {
                case BossStateId.Approach:
                    ChooseAttack(distanceToPlayer);
                    break;

                case BossStateId.Windup:
                    if (stateTime >= Current.Windup)
                        Enter(BossStateId.Active);
                    break;

                case BossStateId.Active:
                    TickActive();
                    break;

                case BossStateId.Recovery:
                    if (stateTime >= Current.Recovery)
                        Enter(BossStateId.Approach);
                    break;

                case BossStateId.Stagger:
                    if (stateTime >= StaggerDuration)
                    {
                        Enter(BossStateId.Approach);
                        StaggerEnded?.Invoke();
                    }
                    break;
            }
        }

        private void ChooseAttack(float distance)
        {
            if (normalAttacksSinceSlam >= SlamEvery && distance <= Slam.MaxRange)
            {
                normalAttacksSinceSlam = 0;
                Begin(BossAttack.Slam);
            }
            else if (distance <= Smash.MaxRange)
            {
                normalAttacksSinceSlam++;
                Begin(BossAttack.Smash);
            }
            else if (distance <= Sweep.MaxRange)
            {
                normalAttacksSinceSlam++;
                Begin(BossAttack.Sweep);
            }
            // أبعد من 4م: يواصل التقدم (الغلاف يمشي به نحو اللاعب)
        }

        private void Begin(BossAttack attack)
        {
            CurrentAttack = attack;
            AttackChosen?.Invoke(attack);
            Enter(BossStateId.Windup);
        }

        private void TickActive()
        {
            var def = Current;

            if (!windowOpen && windowIndex < def.Windows.Length && stateTime >= def.Windows[windowIndex].open)
            {
                windowOpen = true;
                HitboxOpen?.Invoke(CurrentAttack);
            }
            if (windowOpen && stateTime >= def.Windows[windowIndex].close)
            {
                windowOpen = false;
                windowIndex++;
                HitboxClose?.Invoke();
            }
            if (stateTime >= def.ActivePhase && !windowOpen && windowIndex >= def.Windows.Length)
                Enter(BossStateId.Recovery);
        }

        private void CloseWindowIfOpen()
        {
            if (windowOpen)
            {
                windowOpen = false;
                HitboxClose?.Invoke();
            }
        }

        private void Enter(BossStateId next)
        {
            CurrentState = next;
            stateTime = 0f;
            windowIndex = 0;
            windowOpen = false;
            StateChanged?.Invoke(next);
        }
    }
}
