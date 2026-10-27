using NUnit.Framework;
using SoulsGame.Data;
using SoulsGame.Enemies;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class EnemyBrainTests
    {
        private WeaponData weapon;
        private EnemyBrain brain;
        private int hitboxOpens;
        private int hitboxCloses;

        [SetUp]
        public void SetUp()
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f); // سلاح العدو
            brain = new EnemyBrain(weapon, detectRange: 10f, fovDegrees: 90f,
                attackRange: 2f, staggerDuration: 0.8f);
            hitboxOpens = 0;
            hitboxCloses = 0;
            brain.HitboxOpen += () => hitboxOpens++;
            brain.HitboxClose += () => hitboxCloses++;
        }

        private void TickBy(float total, float distance, float angle, float step = 0.05f)
        {
            for (float t = 0f; t < total - 1e-4f; t += step)
                brain.Tick(step, distance, angle);
        }

        [Test]
        public void Patrol_DetectsPlayerInRangeAndFov()
        {
            Assert.AreEqual(EnemyStateId.Patrol, brain.CurrentState);

            brain.Tick(0.05f, distanceToPlayer: 8f, angleToPlayerDegrees: 30f);

            Assert.AreEqual(EnemyStateId.Chase, brain.CurrentState);
        }

        [Test]
        public void OutOfFov_StaysPatrolling()
        {
            brain.Tick(0.05f, distanceToPlayer: 8f, angleToPlayerDegrees: 120f); // خلفه
            Assert.AreEqual(EnemyStateId.Patrol, brain.CurrentState);

            brain.Tick(0.05f, distanceToPlayer: 14f, angleToPlayerDegrees: 10f); // بعيد
            Assert.AreEqual(EnemyStateId.Patrol, brain.CurrentState);
        }

        [Test]
        public void Chase_AttacksInRange_FollowsTimeline()
        {
            brain.Tick(0.05f, 8f, 0f); // → Chase
            brain.Tick(0.05f, 1.5f, 0f); // داخل مدى الهجوم → Windup

            Assert.AreEqual(EnemyStateId.AttackWindup, brain.CurrentState);
            Assert.AreEqual(0, hitboxOpens, "لا ضرر أثناء الإعلان المسبق");

            TickBy(0.60f, 1.5f, 0f); // اكتمال الـ windup
            Assert.AreEqual(EnemyStateId.AttackActive, brain.CurrentState);
            Assert.AreEqual(1, hitboxOpens);

            TickBy(0.30f, 1.5f, 0f); // اكتمال الضربة
            Assert.AreEqual(EnemyStateId.AttackRecovery, brain.CurrentState);
            Assert.AreEqual(1, hitboxCloses, "نافذة العقاب: الـ hitbox مغلق");

            TickBy(1.30f, 1.5f, 0f); // اكتمال الاستراحة (1.2) ثم tick إضافي
            Assert.AreEqual(EnemyStateId.AttackWindup, brain.CurrentState,
                "اللاعب ما زال في المدى: هجوم جديد بعد الاستراحة");
        }

        [Test]
        public void PoiseBreak_DuringActive_ClosesHitboxAndStaggers()
        {
            brain.Tick(0.05f, 8f, 0f);
            brain.Tick(0.05f, 1.5f, 0f);
            TickBy(0.60f, 1.5f, 0f); // AttackActive، الـ hitbox مفتوح

            brain.NotifyPoiseBroken();

            Assert.AreEqual(EnemyStateId.Stagger, brain.CurrentState);
            Assert.AreEqual(1, hitboxCloses, "كسر التوازن يلغي نافذة الضرر فوراً");

            TickBy(0.85f, 8f, 0f); // اللاعب ابتعد أثناء الترنح
            Assert.AreEqual(EnemyStateId.Chase, brain.CurrentState, "بعد الترنح يعود للمطاردة");
        }

        [Test]
        public void Chase_BeyondLeash_Returns()
        {
            brain.Tick(0.05f, 8f, 0f); // → Chase

            brain.Tick(0.05f, 5f, 0f, distanceFromHome: 26f); // تجاوز 25م من المعسكر

            Assert.AreEqual(EnemyStateId.Returning, brain.CurrentState);
        }

        [Test]
        public void Returning_IgnoresPlayer_UntilHome()
        {
            brain.Tick(0.05f, 8f, 0f);
            brain.Tick(0.05f, 5f, 0f, distanceFromHome: 26f); // → Returning

            brain.Tick(0.05f, 1f, 0f, distanceFromHome: 20f); // اللاعب لاصق — يتجاهله
            Assert.AreEqual(EnemyStateId.Returning, brain.CurrentState, "لا re-aggro أثناء العودة");

            brain.Tick(0.05f, 30f, 0f, distanceFromHome: 1f); // وصل بيته
            Assert.AreEqual(EnemyStateId.Patrol, brain.CurrentState);

            brain.Tick(0.05f, 8f, 0f, distanceFromHome: 1f); // اكتشاف جديد مشروع
            Assert.AreEqual(EnemyStateId.Chase, brain.CurrentState);
        }

        [Test]
        public void Dead_IsTerminal()
        {
            brain.NotifyDied();
            Assert.AreEqual(EnemyStateId.Dead, brain.CurrentState);

            TickBy(5f, 1f, 0f);
            Assert.AreEqual(EnemyStateId.Dead, brain.CurrentState);
            Assert.AreEqual(0, hitboxOpens);
        }
    }
}
