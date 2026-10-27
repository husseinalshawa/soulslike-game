using NUnit.Framework;
using SoulsGame.Story;

namespace SoulsGame.Tests
{
    public class StoryStateTests
    {
        [Test]
        public void Flags_StartUnset_SetRaisesEventOnce()
        {
            var state = new StoryState();
            Assert.IsFalse(state.IsSet(StoryFlag.BossDefeated));

            int raised = 0;
            StoryFlag lastFlag = StoryFlag.CaveDiscovered;
            state.FlagChanged += f => { raised++; lastFlag = f; };

            state.Set(StoryFlag.BossDefeated);
            Assert.IsTrue(state.IsSet(StoryFlag.BossDefeated));
            Assert.AreEqual(1, raised, "الرفع يطلق الحدث");
            Assert.AreEqual(StoryFlag.BossDefeated, lastFlag);

            state.Set(StoryFlag.BossDefeated);
            Assert.AreEqual(1, raised, "علم مرفوع لا يطلق الحدث ثانية");
        }

        [Test]
        public void KeyItems_AddHasRemove_NoDuplicates()
        {
            var items = new KeyItemInventory();
            Assert.IsFalse(items.Has("Rusted Bell"));
            Assert.AreEqual(0, items.Count);

            items.Add("Rusted Bell");
            Assert.IsTrue(items.Has("Rusted Bell"));
            Assert.AreEqual(1, items.Count);

            items.Add("Rusted Bell"); // مكرر لا يضيف
            Assert.AreEqual(1, items.Count);

            Assert.IsTrue(items.Remove("Rusted Bell"));
            Assert.IsFalse(items.Has("Rusted Bell"));
            Assert.IsFalse(items.Remove("Rusted Bell"), "إزالة غائب تعيد false");
        }
    }
}
