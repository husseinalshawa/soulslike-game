using NUnit.Framework;
using SoulsGame.Progression;

namespace SoulsGame.Tests
{
    public class GearInventoryTests
    {
        [Test]
        public void Add_First_BecomesCurrent()
        {
            var inv = new GearInventory<string>();
            Assert.IsTrue(inv.Add("sword"));
            Assert.AreEqual("sword", inv.Current);
            Assert.AreEqual(1, inv.Count);
        }

        [Test]
        public void Add_Duplicate_Rejected()
        {
            var inv = new GearInventory<string>();
            inv.Add("sword");
            Assert.IsFalse(inv.Add("sword"));
            Assert.AreEqual(1, inv.Count);
        }

        [Test]
        public void Cycle_WrapsAround()
        {
            var inv = new GearInventory<string>();
            inv.Add("a"); inv.Add("b"); inv.Add("c");

            Assert.AreEqual("b", inv.Cycle());
            Assert.AreEqual("c", inv.Cycle());
            Assert.AreEqual("a", inv.Cycle(), "يلف للبداية");
        }

        [Test]
        public void Cycle_SingleItem_Stays()
        {
            var inv = new GearInventory<string>();
            inv.Add("only");
            Assert.AreEqual("only", inv.Cycle());
        }
    }
}
