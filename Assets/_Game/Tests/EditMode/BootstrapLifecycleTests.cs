using NUnit.Framework;
using SoulsGame.Core;
using UnityEngine;

namespace SoulsGame.Tests
{
    // يحاكي سلوك Unity الحقيقي: AddComponent على كائن نشط يستدعي Awake فوراً
    // قبل أن يضبط المستدعي الحقول. البناء البرمجي للساحة يجب أن ينجو من ذلك
    // (نمط الكائن المعطّل: أضف المكونات، اربط الحقول، ثم فعّل).
    public class BootstrapLifecycleTests
    {
        [Test]
        public void GameBootstrap_BuildsEverything_WithUnityAwakeOrdering()
        {
            var go = new GameObject("Bootstrap");

            Assert.DoesNotThrow(() => go.AddComponent<GameBootstrap>());

            var bootstrap = go.GetComponent<GameBootstrap>();
            Assert.IsNotNull(bootstrap.Player, "اللاعب بُني");
            Assert.IsNotNull(bootstrap.Player.Brain, "عقل اللاعب تهيأ بعد ضبط الحقول");
            Assert.IsNotNull(bootstrap.Player.Damageable.Health, "مكونات الضرر تهيأت");
            Assert.IsNotNull(bootstrap.Enemy, "العدو بُني");
            Assert.IsNotNull(bootstrap.Enemy.Brain, "عقل العدو تهيأ");
        }

        [Test]
        public void SoulsEconomy_WiredIntoScene()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.IsNotNull(bootstrap.Player.Wallet, "المحفظة تهيأت");
            Assert.AreEqual(0, bootstrap.Player.Wallet.Current);

            bootstrap.Enemy.Damageable.Health.TakeDamage(1000f); // قتل الجندي

            Assert.AreEqual(100, bootstrap.Player.Wallet.Current, "قتل الجندي يمنح 100 روح");
        }

        [Test]
        public void GracesAndProgression_WiredIntoScene()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.IsNotNull(bootstrap.Loop, "GameLoop مكشوف من البناء");
            Assert.AreEqual(3, bootstrap.Loop.Graces.SiteCount, "ثلاثة مواقع راحة (مرحلة 3)");

            var player = bootstrap.Player;
            Assert.IsNotNull(player.Progression, "تقدم اللاعب تهيأ");

            player.Wallet.Add(100);
            Assert.IsTrue(player.Progression.TryLevelUp(Progression.StatId.Vigor, player.Wallet));
            player.ApplyProgression();

            Assert.AreEqual(112f, player.Damageable.Health.Max, "صلابة +1 ترفع الحد الأقصى إلى 112");
            Assert.AreEqual(112f, player.Damageable.Health.Current, "ممتلئ بعد الرفع (يحدث عند الراحة)");
        }

        [Test]
        public void Boss_WiredIntoScene_VictoryPaysSouls()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.IsNotNull(bootstrap.Boss, "البوس بُني");
            Assert.AreEqual(Enemies.BossStateId.Dormant, bootstrap.Boss.Brain.CurrentState, "خامل قبل عبور الضباب");
            Assert.AreEqual(1000f, bootstrap.Boss.Damageable.Health.Max, "صحة البوس 1000");

            bootstrap.Boss.BeginFight();
            Assert.AreEqual(Enemies.BossStateId.Approach, bootstrap.Boss.Brain.CurrentState);

            bootstrap.Boss.Damageable.Health.TakeDamage(2000f);

            Assert.AreEqual(2000, bootstrap.Player.Wallet.Current, "النصر يمنح 2000 روح");
            Assert.IsTrue(bootstrap.Boss.DefeatedForever, "البوس لا يعود أبداً");
        }

        [Test]
        public void World_BuiltWithTerrain_AndEntitiesGrounded()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.AreEqual(64, bootstrap.TerrainChunkCount, "شبكة 8×8 قطع تضاريس (400م ÷ 50)");

            Vector3 p = bootstrap.Player.transform.position;
            Assert.GreaterOrEqual(p.y, World.WorldHeightmap.GetHeight(p.x, p.z) - 0.01f,
                "اللاعب فوق التضاريس لا داخلها");

            Vector3 e = bootstrap.Enemy.transform.position;
            Assert.GreaterOrEqual(e.y, World.WorldHeightmap.GetHeight(e.x, e.z) - 0.01f,
                "العدو فوق التضاريس");
        }

        [Test]
        public void MeadowContent_GracesEnemiesAndCache()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.AreEqual(3, bootstrap.Loop.Graces.SiteCount,
                "ثلاثة مواقع: بوابة الحصن، منتصف المرج، فم الكهف");
            Assert.AreEqual(8, bootstrap.AllEnemies.Length,
                "8 جنود: 2 حصن + 2×2 معسكران + 2 كهف");

            Vector3 cachePos = bootstrap.Cache.transform.position;
            Assert.AreEqual(800, bootstrap.Cache.TryCollect(cachePos), "الكنز يمنح 800");
            Assert.AreEqual(0, bootstrap.Cache.TryCollect(cachePos), "مرة واحدة فقط");
        }

        [Test]
        public void Interact_TogglesMenu_AndIsDeadSafe()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;

            loop.RestAtGrace(0);
            Assert.IsTrue(loop.levelUpMenu.IsOpen, "الراحة تفتح القائمة");
            Assert.IsTrue(bootstrap.Player.InputLocked);

            loop.HandleInteract(); // زر التفاعل وهي مفتوحة = إغلاق (مخرج كيبورد مضمون)
            Assert.IsFalse(loop.levelUpMenu.IsOpen);
            Assert.IsFalse(bootstrap.Player.InputLocked);

            bootstrap.Player.Damageable.Health.TakeDamage(1000f); // اللاعب مات
            Assert.IsFalse(loop.levelUpMenu.IsOpen, "موت اللاعب يغلق القائمة لو كانت مفتوحة");

            loop.HandleInteract(); // محاولة جلوس وهو ميت
            Assert.IsFalse(loop.levelUpMenu.IsOpen, "لا راحة ولا إحياء أثناء شاشة الموت");
            Assert.IsTrue(bootstrap.Player.Damageable.Health.IsDead, "ما زال ميتاً حتى الإحياء الرسمي");
        }
    }
}
