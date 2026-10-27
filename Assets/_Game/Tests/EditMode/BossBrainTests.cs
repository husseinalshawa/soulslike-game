using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.Enemies;

namespace SoulsGame.Tests
{
    public class BossBrainTests
    {
        private BossBrain brain;
        private List<BossAttack> chosen;
        private int opens, closes;

        [SetUp]
        public void SetUp()
        {
            brain = new BossBrain();
            chosen = new List<BossAttack>();
            opens = 0; closes = 0;
            brain.AttackChosen += a => chosen.Add(a);
            brain.HitboxOpen += _ => opens++;
            brain.HitboxClose += () => closes++;
        }

        private void TickBy(float total, float distance, float step = 0.05f)
        {
            for (float t = 0f; t < total - 1e-4f; t += step)
                brain.Tick(step, distance);
        }

        [Test]
        public void Dormant_IgnoresTicks_UntilActivate()
        {
            TickBy(2f, 1f);
            Assert.AreEqual(BossStateId.Dormant, brain.CurrentState);

            brain.Activate();
            Assert.AreEqual(BossStateId.Approach, brain.CurrentState);
        }

        [Test]
        public void Approach_PicksSmashInCloseRange_AndFollowsTimeline()
        {
            brain.Activate();
            brain.Tick(0.05f, 2.5f);

            Assert.AreEqual(BossStateId.Windup, brain.CurrentState);
            Assert.AreEqual(BossAttack.Smash, brain.CurrentAttack);
            Assert.AreEqual(0, opens, "لا ضرر أثناء الإعلان (0.9 ث)");

            TickBy(0.95f, 2.5f); // اكتمال الإعلان
            Assert.AreEqual(BossStateId.Active, brain.CurrentState);
            Assert.AreEqual(1, opens);

            TickBy(0.30f, 2.5f); // نافذة الساحقة 0.25
            Assert.AreEqual(BossStateId.Recovery, brain.CurrentState);
            Assert.AreEqual(1, closes, "نافذة العقاب الكبرى (1.6 ث)");

            TickBy(1.65f, 10f); // استراحة كاملة واللاعب ابتعد
            Assert.AreEqual(BossStateId.Approach, brain.CurrentState);
        }

        [Test]
        public void Sweep_FiresTwoHitWindows()
        {
            brain.Activate();
            brain.Tick(0.05f, 3.5f); // مدى الكنس (>3، ≤4)
            Assert.AreEqual(BossAttack.Sweep, brain.CurrentAttack);

            TickBy(0.75f, 3.5f); // إعلان 0.7
            TickBy(0.95f, 3.5f); // طور Active بطول 0.9 فيه نافذتان

            Assert.AreEqual(2, opens, "الكنس ضربتان");
            Assert.AreEqual(2, closes);
            Assert.AreEqual(BossStateId.Recovery, brain.CurrentState);
        }

        [Test]
        public void Slam_ForcedAfterThreeNormalAttacks_OnlyWhenClose()
        {
            brain.Activate();
            for (int i = 0; i < 3; i++)
            {
                brain.Tick(0.05f, 2.9f); // Smash
                TickBy(3.0f, 10f);       // أكمل الهجوم والاستراحة (اللاعب بعيد كي لا يبدأ تلقائياً)
                Assert.AreEqual(BossStateId.Approach, brain.CurrentState, "عاد للتقدم بعد الهجوم " + i);
            }
            Assert.AreEqual(3, chosen.Count);
            Assert.IsTrue(chosen.TrueForAll(a => a == BossAttack.Smash));

            brain.Tick(0.05f, 3.5f); // 3 هجمات عادية لكن اللاعب ليس لاصقاً
            Assert.AreEqual(BossAttack.Sweep, brain.CurrentAttack, "لا Slam إلا بمسافة ≤2.5");

            TickBy(4.0f, 10f); // أكمل الكنس
            brain.Tick(0.05f, 2.0f); // الآن لاصق + العداد ≥3
            Assert.AreEqual(BossAttack.Slam, brain.CurrentAttack);
        }

        [Test]
        public void PoiseBreak_DuringActive_ClosesWindowAndStaggers()
        {
            bool staggerEnded = false;
            brain.StaggerEnded += () => staggerEnded = true;

            brain.Activate();
            brain.Tick(0.05f, 2.5f);
            TickBy(0.95f, 2.5f); // Active، النافذة مفتوحة

            brain.NotifyPoiseBroken();

            Assert.AreEqual(BossStateId.Stagger, brain.CurrentState);
            Assert.AreEqual(1, closes, "كسر التوازن يغلق النافذة فوراً");

            TickBy(0.85f, 10f);
            Assert.AreEqual(BossStateId.Approach, brain.CurrentState);
            Assert.IsTrue(staggerEnded, "حدث نهاية الترنح لبدء نافذة المضاعف");
        }

        [Test]
        public void ResetFight_ReturnsToDormant()
        {
            brain.Activate();
            brain.Tick(0.05f, 2.5f);

            brain.ResetFight();

            Assert.AreEqual(BossStateId.Dormant, brain.CurrentState);
            TickBy(1f, 1f);
            Assert.AreEqual(BossStateId.Dormant, brain.CurrentState, "خامل حتى Activate جديد");
        }

        [Test]
        public void Dead_IsTerminal()
        {
            brain.Activate();
            brain.NotifyDied();

            TickBy(3f, 1f);
            Assert.AreEqual(BossStateId.Dead, brain.CurrentState);
            Assert.AreEqual(0, opens);
        }
    }
}
