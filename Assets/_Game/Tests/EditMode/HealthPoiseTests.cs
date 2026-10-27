using NUnit.Framework;
using SoulsGame.Combat;

namespace SoulsGame.Tests
{
    public class HealthPoiseTests
    {
        [Test]
        public void TakeDamage_ClampsAtZero_AndDiedFiresOnce()
        {
            var health = new HealthPool(max: 100f);
            int diedCount = 0;
            health.Died += () => diedCount++;

            health.TakeDamage(200f);

            Assert.AreEqual(0f, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, diedCount);

            health.TakeDamage(50f); // ضرر بعد الموت يُتجاهل
            Assert.AreEqual(0f, health.Current);
            Assert.AreEqual(1, diedCount, "حدث الموت لا يُطلق مرتين");
        }

        [Test]
        public void SetMax_DoesNotReviveDead()
        {
            var health = new HealthPool(max: 100f);
            health.TakeDamage(100f);
            Assert.IsTrue(health.IsDead);

            health.SetMax(112f); // رفع مستوى لا يُحيي ميتاً — الإحياء عبر ResetToFull فقط

            Assert.IsTrue(health.IsDead, "SetMax ليس طريق إحياء جانبياً");
        }

        [Test]
        public void Poise_BreaksAtThreshold_AndResets()
        {
            var poise = new PoiseMeter(max: 40f, regenPerSecond: 10f);

            Assert.IsFalse(poise.TakePoiseDamage(15f));
            Assert.IsFalse(poise.TakePoiseDamage(15f)); // تراكم 30 < 40
            Assert.IsTrue(poise.TakePoiseDamage(15f), "التراكم 45 ≥ 40 يكسر التوازن");

            // بعد الكسر يُعاد الضبط: ضربة جديدة لا تكسر فوراً
            Assert.IsFalse(poise.TakePoiseDamage(15f));
        }

        [Test]
        public void Poise_ThresholdMultiplier_RaisesBreakPoint()
        {
            var poise = new PoiseMeter(max: 40f, regenPerSecond: 0f);
            poise.SetThresholdMultiplier(1.5f); // العتبة الفعلية 60

            Assert.IsFalse(poise.TakePoiseDamage(45f), "45 < 60 لا تكسر مع المضاعف");
            Assert.IsTrue(poise.TakePoiseDamage(20f), "التراكم 65 ≥ 60 يكسر");

            poise.SetThresholdMultiplier(1f); // عودة للسلوك الأصلي
            Assert.IsTrue(poise.TakePoiseDamage(45f), "45 ≥ 40 تكسر بدون مضاعف");
        }

        [Test]
        public void Poise_RegensOverTime()
        {
            var poise = new PoiseMeter(max: 40f, regenPerSecond: 10f);
            poise.TakePoiseDamage(30f);

            poise.Tick(3.0f); // يتعافى 30 → التراكم صفر

            Assert.IsFalse(poise.TakePoiseDamage(35f), "بعد التعافي الكامل، 35 < 40 لا تكسر");
        }
    }
}
