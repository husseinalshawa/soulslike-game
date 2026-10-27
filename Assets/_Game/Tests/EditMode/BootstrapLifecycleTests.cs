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
            Assert.AreEqual(2, bootstrap.Loop.Graces.SiteCount, "موقعا راحة مبنيان");

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
    }
}
