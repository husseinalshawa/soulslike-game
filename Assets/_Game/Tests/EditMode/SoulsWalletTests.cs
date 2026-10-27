using NUnit.Framework;
using SoulsGame.Progression;

namespace SoulsGame.Tests
{
    public class SoulsWalletTests
    {
        [Test]
        public void Add_Accumulates()
        {
            var wallet = new SoulsWallet();
            wallet.Add(100);
            wallet.Add(250);
            Assert.AreEqual(350, wallet.Current);
        }

        [Test]
        public void TrySpend_ExactAmount_Succeeds()
        {
            var wallet = new SoulsWallet();
            wallet.Add(100);
            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(0, wallet.Current);
        }

        [Test]
        public void TrySpend_Insufficient_FailsAndKeeps()
        {
            var wallet = new SoulsWallet();
            wallet.Add(50);
            Assert.IsFalse(wallet.TrySpend(100));
            Assert.AreEqual(50, wallet.Current);
        }

        [Test]
        public void TakeAll_ReturnsBalanceAndZeroes()
        {
            var wallet = new SoulsWallet();
            wallet.Add(777);
            Assert.AreEqual(777, wallet.TakeAll());
            Assert.AreEqual(0, wallet.Current);
        }
    }
}
