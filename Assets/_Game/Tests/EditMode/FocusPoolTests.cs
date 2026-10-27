using NUnit.Framework;
using SoulsGame.Combat;

namespace SoulsGame.Tests
{
    public class FocusPoolTests
    {
        [Test]
        public void TrySpend_Exact_Succeeds()
        {
            var pool = new FocusPool(60f);
            Assert.IsTrue(pool.TrySpend(60f));
            Assert.AreEqual(0f, pool.Current);
            Assert.IsFalse(pool.TrySpend(1f));
        }

        [Test]
        public void Regen_WaitsTwoSeconds_Then3PerSec()
        {
            var pool = new FocusPool(60f);
            pool.TrySpend(30f);

            pool.Tick(1.9f);
            Assert.AreEqual(30f, pool.Current, 0.001f, "لا تجدد قبل مهلة الثانيتين");

            pool.Tick(0.1f); // اكتملت المهلة
            pool.Tick(2.0f); // ثانيتان تجدد
            Assert.AreEqual(36f, pool.Current, 0.001f, "3 لكل ثانية");
        }

        [Test]
        public void SetMax_Refills()
        {
            var pool = new FocusPool(60f);
            pool.TrySpend(50f);

            pool.SetMax(65f);

            Assert.AreEqual(65f, pool.Max);
            Assert.AreEqual(65f, pool.Current);
        }
    }
}
