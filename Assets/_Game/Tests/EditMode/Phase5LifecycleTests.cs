using NUnit.Framework;
using SoulsGame.Core;
using SoulsGame.Story;
using UnityEngine;

namespace SoulsGame.Tests
{
    // المرحلة 5 في المشهد المبني: الشخصيات والكنيسة والمذبح والجرس،
    // والسيناريو القصصي الكامل حتى الخاتمة — عبر GameBootstrap الحقيقي.
    public class Phase5LifecycleTests
    {
        [Test]
        public void Phase5_WorldBuilt()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;

            Assert.AreEqual(4, loop.Graces.SiteCount, "موقع الكنيسة هو الرابع");
            Assert.AreEqual(11, bootstrap.AllEnemies.Length, "8 + 3 مرتّلين غارقين");
            Assert.AreEqual(3, loop.npcs.Length, "مارا وفاليون وسيلا");
            foreach (var npc in loop.npcs)
            {
                Assert.IsNull(npc.GetComponent<Combat.Damageable>(),
                    $"{npc.Questline.NpcName} لا يُقتل — لا Damageable");
                Assert.IsNotNull(npc.Questline);
            }
            Assert.IsTrue(loop.altarPosition.HasValue, "المذبح موصول بالحلقة");
            Assert.IsNotNull(bootstrap.BellPickup, "الجرس الصدئ في الكهف");
            Assert.IsNotNull(bootstrap.Player.marsh, "منطقة الماء الضحل موصولة للاعب");

            // المرتّلون: إحصاءات الغارقين لا الجنود
            var chanter = bootstrap.AllEnemies[8];
            Assert.AreEqual(200f, chanter.Damageable.Health.Max, "صحة المرتّل 200");
        }

        [Test]
        public void Discovery_RaisesStoryFlags()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;

            // نقل اللاعب لموقع راحة الكهف (site 2) ثم معالجة الاكتشاف
            loop.player.transform.position = loop.Graces.PositionOf(2);
            loop.ProcessDiscovery();
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.CaveDiscovered));

            loop.player.transform.position = loop.Graces.PositionOf(3);
            loop.ProcessDiscovery();
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.ChapelDiscovered));
        }

        [Test]
        public void Phase5_FullStoryScenario()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;
            var player = bootstrap.Player;

            // 1) اكتشاف الكهف والتقاط الجرس
            player.transform.position = loop.Graces.PositionOf(2);
            loop.ProcessDiscovery();
            Assert.AreEqual(1, bootstrap.BellPickup.TryCollect(bootstrap.BellPickup.transform.position));
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.BellFound));

            // 2) تسليم الجرس لمارا (مرحلة من 3 أسطر)
            var mara = loop.npcs[0];
            player.transform.position = mara.transform.position;
            loop.HandleInteract(); // بدء
            loop.HandleInteract();
            loop.HandleInteract();
            loop.HandleInteract(); // آخر سطر → الإكمال
            Assert.IsFalse(loop.Dialogue.IsActive);
            Assert.AreEqual(StoryLibrary.BellReward, player.Wallet.Current, "تسليم الجرس = 500");
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.BellReturned));

            // 3) قتل البوس يرفع علم القصة
            bootstrap.Boss.BeginFight();
            bootstrap.Boss.Damageable.Health.TakeDamage(5000f);
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.BossDefeated));

            // 4) فاليون يناول الرماد (مرحلة من 3 أسطر)
            var valion = loop.npcs[1];
            player.transform.position = valion.transform.position;
            loop.HandleInteract();
            loop.HandleInteract();
            loop.HandleInteract();
            loop.HandleInteract();
            Assert.IsTrue(player.KeyItems.Has(StoryLibrary.WardensAshes));
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.AshCarried));

            // 5) المذبح: الخاتمة — أرواح + علم + بدء تحول اللون
            int before = player.Wallet.Current;
            player.transform.position = loop.altarPosition.Value;
            loop.HandleInteract();
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.AshPlaced));
            Assert.AreEqual(before + GameLoop.AltarReward, player.Wallet.Current);
            Assert.IsFalse(player.KeyItems.Has(StoryLibrary.WardensAshes));
            Assert.IsNotNull(bootstrap.ActiveThemeShift, "تحول اللون بدأ");

            // 6) موت وراحة بعد الخاتمة: القصة لا ترجع (Review Focus 5)
            player.Damageable.Health.TakeDamage(100000f);
            loop.Respawn();
            loop.RestAtGrace(0);
            Assert.IsTrue(loop.Story.IsSet(StoryFlag.AshPlaced), "الأعلام لا تُنزل");
            Assert.IsFalse(player.KeyItems.Has(StoryLibrary.RustedBell), "المسلَّم لا يعود");
            var maraStage = loop.npcs[0].Questline.ActiveStage(loop.Story, player.KeyItems);
            Assert.AreSame(loop.npcs[0].Questline.Stages[4], maraStage,
                "مارا على خاتمتها بعد الموت والراحة");
        }
    }
}
