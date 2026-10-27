using NUnit.Framework;
using SoulsGame.Combat;

namespace SoulsGame.Tests
{
    public class HitRegistryTests
    {
        [Test]
        public void SameTarget_OncePerSwing()
        {
            var registry = new HitRegistry();
            var target = new object();

            registry.BeginSwing();
            Assert.IsTrue(registry.TryRegister(target));
            Assert.IsFalse(registry.TryRegister(target), "نفس الهدف لا يُصاب مرتين في نفس الضربة");

            registry.BeginSwing();
            Assert.IsTrue(registry.TryRegister(target), "ضربة جديدة تسمح بإصابة الهدف من جديد");
        }

        [Test]
        public void DifferentTargets_BothRegister()
        {
            var registry = new HitRegistry();

            registry.BeginSwing();
            Assert.IsTrue(registry.TryRegister(new object()));
            Assert.IsTrue(registry.TryRegister(new object()));
        }
    }
}
