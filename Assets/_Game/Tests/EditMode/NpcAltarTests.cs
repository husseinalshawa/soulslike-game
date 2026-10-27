using NUnit.Framework;
using SoulsGame.Core;
using SoulsGame.Story;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class NpcAltarTests
    {
        [Test]
        public void Altar_SilentWithoutAshes_PlacedOnce()
        {
            var state = new StoryState();
            var items = new KeyItemInventory();

            Assert.AreEqual(AltarLogic.Result.Silent, AltarLogic.TryPlace(state, items),
                "بلا رماد المذبح صامت");

            items.Add(StoryLibrary.WardensAshes);
            Assert.AreEqual(AltarLogic.Result.Placed, AltarLogic.TryPlace(state, items));
            Assert.IsTrue(state.IsSet(StoryFlag.AshPlaced));
            Assert.IsFalse(items.Has(StoryLibrary.WardensAshes), "الرماد وُضع على المذبح");

            Assert.AreEqual(AltarLogic.Result.AlreadyPlaced, AltarLogic.TryPlace(state, items),
                "الخاتمة لا تتكرر");
        }

        [Test]
        public void KeyItemPickup_CollectsOnce_SetsFlag_DeadGuard()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var player = bootstrap.Player;

            var story = new StoryState();
            var pickupGo = new GameObject("BellPickup");
            pickupGo.transform.position = player.transform.position;
            var pickup = pickupGo.AddComponent<KeyItemPickup>();
            pickup.itemName = StoryLibrary.RustedBell;
            pickup.description = StoryLibrary.RustedBellDescription;
            pickup.flagOnCollect = StoryFlag.BellFound;
            pickup.player = player;
            pickup.story = story;

            // ميت: لا التقاط
            player.Damageable.Health.TakeDamage(100000f);
            Assert.AreEqual(0, pickup.TryCollect(player.transform.position), "الميت لا يلتقط");

            bootstrap.Loop.Respawn();
            Assert.AreEqual(1, pickup.TryCollect(player.transform.position));
            Assert.IsTrue(player.KeyItems.Has(StoryLibrary.RustedBell));
            Assert.IsTrue(story.IsSet(StoryFlag.BellFound));
            Assert.AreEqual(0, pickup.TryCollect(player.transform.position), "مرة واحدة فقط");
        }

        [Test]
        public void Npc_InRangeByDistance()
        {
            var go = new GameObject("Npc");
            go.transform.position = new Vector3(10f, 0f, 10f);
            var npc = go.AddComponent<NpcAvatar>();
            npc.Questline = StoryLibrary.Mara();

            Assert.IsTrue(npc.InRange(new Vector3(10f + 2.4f, 0f, 10f)));
            Assert.IsFalse(npc.InRange(new Vector3(10f + 2.6f, 0f, 10f)));
        }
    }
}
