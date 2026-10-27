using NUnit.Framework;
using SoulsGame.Combat;

namespace SoulsGame.Tests
{
    public class StaminaPoolTests
    {
        private static StaminaPool NewPool() =>
            new StaminaPool(max: 100f, regenPerSecond: 25f, regenDelay: 1.0f, blockingRegenMultiplier: 0.33f);

        [Test]
        public void TryConsume_ExactAmount_Succeeds()
        {
            var pool = NewPool();

            Assert.IsTrue(pool.TryConsume(100f));
            Assert.AreEqual(0f, pool.Current);
        }

        [Test]
        public void TryConsume_Insufficient_FailsAndKeeps()
        {
            var pool = NewPool();
            pool.TryConsume(90f);

            Assert.IsFalse(pool.TryConsume(20f));
            Assert.AreEqual(10f, pool.Current);
        }

        [Test]
        public void Regen_WaitsForDelay()
        {
            var pool = NewPool();
            pool.TryConsume(50f);

            pool.Tick(0.5f, isBlocking: false, isSprinting: false);
            Assert.AreEqual(50f, pool.Current, 0.001f, "لا تجدد قبل انقضاء المهلة");

            pool.Tick(0.5f, isBlocking: false, isSprinting: false); // اكتملت المهلة (1.0 ث)
            pool.Tick(1.0f, isBlocking: false, isSprinting: false); // ثانية تجدد كاملة
            Assert.AreEqual(75f, pool.Current, 0.001f);
        }

        [Test]
        public void Regen_SlowerWhileBlocking()
        {
            var pool = NewPool();
            pool.TryConsume(50f);

            pool.Tick(1.0f, isBlocking: true, isSprinting: false); // المهلة تمضي
            pool.Tick(1.0f, isBlocking: true, isSprinting: false); // تجدد مع الصد
            Assert.AreEqual(50f + 25f * 0.33f, pool.Current, 0.001f);
        }

        [Test]
        public void Regen_ClampsAtMax()
        {
            var pool = NewPool();
            pool.TryConsume(10f);

            pool.Tick(1.0f, isBlocking: false, isSprinting: false);
            pool.Tick(10f, isBlocking: false, isSprinting: false);
            Assert.AreEqual(100f, pool.Current);
        }

        [Test]
        public void Sprint_DrainsAndResetsDelay()
        {
            var pool = NewPool();
            pool.SetSprintCost(10f);

            pool.Tick(1.0f, isBlocking: false, isSprinting: true);
            Assert.AreEqual(90f, pool.Current, 0.001f, "العدو يستهلك 10/ث");

            // توقف عن العدو: المهلة بدأت من جديد، فلا تجدد خلال أول نصف ثانية
            pool.Tick(0.5f, isBlocking: false, isSprinting: false);
            Assert.AreEqual(90f, pool.Current, 0.001f);
        }
    }
}
