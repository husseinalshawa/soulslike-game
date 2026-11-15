using NUnit.Framework;
using SoulsGame.Core;
using SoulsGame.Enemies;
using UnityEngine;

namespace SoulsGame.Tests
{
    // العالم يرتب الوحوش بمستويات: الحصن 1، المعسكر الشرقي والكهف 2،
    // المعسكر الجنوبي والكنيسة 3، الغابة المظلمة 4.
    public class EnemyTierWorldTests
    {
        [Test]
        public void Zones_HaveIncreasingTiers()
        {
            var bootstrap = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            int[] expected = { 1, 1, 2, 2, 3, 3, 2, 2, 3, 3, 3, 4, 4, 4 };
            Assert.AreEqual(expected.Length, bootstrap.AllEnemies.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], bootstrap.AllEnemies[i].tier, $"الوحش {i}");
        }

        [Test]
        public void ForestElites_AreTheStrongestNonBoss()
        {
            var bootstrap = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            var elite = bootstrap.AllEnemies[11];
            foreach (var enemy in bootstrap.AllEnemies)
            {
                if (enemy.tier == EnemyLevels.MaxTier) continue;
                Assert.Greater(elite.Damageable.Health.Max, enemy.Damageable.Health.Max);
                // الضرر يُقارن بنفس السلاح الأساسي (الجنود)؛ مطرقة المرتّل سلاح أثقل بطبيعته
                if (enemy.name.StartsWith("EnemyMelee"))
                    Assert.Greater(elite.weapon.lightDamage, enemy.weapon.lightDamage);
            }
            Assert.Less(elite.Damageable.Health.Max, bootstrap.Boss.Damageable.Health.Max,
                "البوس يبقى قمة الهرم");
        }

        [Test]
        public void KillingElite_PaysScaledSouls()
        {
            var bootstrap = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            int before = bootstrap.Player.Wallet.Current;
            bootstrap.AllEnemies[11].Damageable.Health.TakeDamage(100000f);
            Assert.AreEqual(before + EnemyLevels.ScaleSouls(100, 4), bootstrap.Player.Wallet.Current);
        }
    }
}
