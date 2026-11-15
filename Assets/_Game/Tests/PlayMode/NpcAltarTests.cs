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

        // ---- تكامل GameLoop (المهمة 7) ----

        private static NpcAvatar PlaceNpcAtPlayer(GameBootstrap bootstrap, NpcQuestline questline)
        {
            var npcGo = new GameObject($"Npc_{questline.NpcName}");
            npcGo.transform.position = bootstrap.Player.transform.position;
            var npc = npcGo.AddComponent<NpcAvatar>();
            npc.Questline = questline;
            bootstrap.Loop.npcs = new[] { npc };
            bootstrap.Loop.BeginGame(); // تجاوز شاشة العنوان
            return npc;
        }

        [Test]
        public void Interact_DialoguePriority_TalkDoesNotRest()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;
            PlaceNpcAtPlayer(bootstrap, StoryLibrary.Mara());

            // اللاعب واقف على موقع راحة البداية وشخصية ضمن المدى معاً
            bootstrap.Player.Damageable.Health.TakeDamage(10f);
            float hpBefore = bootstrap.Player.Damageable.Health.Current;

            loop.HandleInteract();

            Assert.IsTrue(loop.Dialogue.IsActive, "الحديث له الأولوية على الجلوس");
            Assert.IsFalse(loop.levelUpMenu.IsOpen, "قائمة الرفع لم تفتح");
            Assert.AreEqual(hpBefore, bootstrap.Player.Damageable.Health.Current, 0.001f,
                "لا ملء صحة — لم تقع راحة في نفس الضغطة");
            Assert.IsTrue(bootstrap.Player.InputLocked, "الحوار يقفل الإدخال كالقائمة");
        }

        [Test]
        public void Dialogue_LocksInput_AdvanceToEnd_Unlocks()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;
            PlaceNpcAtPlayer(bootstrap, StoryLibrary.Mara()); // مرحلة 0: سطر واحد

            loop.HandleInteract(); // بدء
            Assert.IsTrue(loop.Dialogue.IsActive);
            StringAssert.Contains("bells", loop.Dialogue.CurrentLine);

            loop.HandleInteract(); // آخر سطر → نهاية
            Assert.IsFalse(loop.Dialogue.IsActive);
            Assert.IsFalse(bootstrap.Player.InputLocked, "نهاية الحوار تفك القفل");
            Assert.IsFalse(Enemies.EnemyAvatar.AggroSuppressed);
        }

        [Test]
        public void Death_MidDialogue_AbortsWithoutCompletion()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;
            var player = bootstrap.Player;

            // مرحلة تسليم الجرس (لها فعل إكمال: يأخذ الجرس ويدفع 500)
            loop.Story.Set(StoryFlag.CaveDiscovered);
            loop.Story.Set(StoryFlag.BellFound);
            player.KeyItems.Add(StoryLibrary.RustedBell);
            PlaceNpcAtPlayer(bootstrap, StoryLibrary.Mara());

            loop.HandleInteract();
            Assert.IsTrue(loop.Dialogue.IsActive);

            player.Damageable.Health.TakeDamage(100000f); // موت منتصف الحوار

            Assert.IsFalse(loop.Dialogue.IsActive, "الموت يجهض الجلسة");
            Assert.IsTrue(player.KeyItems.Has(StoryLibrary.RustedBell),
                "فعل الإكمال لم يقع — الجرس ما زال معك");
            Assert.IsFalse(loop.Story.IsSet(StoryFlag.BellReturned));

            loop.Respawn();
            Assert.IsFalse(player.InputLocked, "بعد الإحياء الإدخال حر");
        }
    }
}
