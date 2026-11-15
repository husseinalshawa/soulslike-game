using NUnit.Framework;
using SoulsGame.Data;
using SoulsGame.Enemies;
using UnityEngine;

namespace SoulsGame.Tests
{
    // مستويات الوحوش: المستوى 1 لا يغير شيئاً، وكل مستوى أعلى أقسى وأكرم.
    public class EnemyLevelsTests
    {
        private static CharacterStats Soldier()
        {
            var s = ScriptableObject.CreateInstance<CharacterStats>();
            s.maxHealth = 150f; s.maxPoise = 30f; s.soulsReward = 100; s.runSpeed = 5f;
            return s;
        }

        private static WeaponData Sword()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.lightDamage = 20f; w.lightPoiseDamage = 20f;
            w.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            return w;
        }

        [Test]
        public void Tier1_ReturnsSameAsset()
        {
            var s = Soldier();
            var w = Sword();
            Assert.AreSame(s, EnemyLevels.Scale(s, 1));
            Assert.AreSame(w, EnemyLevels.Scale(w, 1));
        }

        [Test]
        public void HigherTier_StrongerEverywhere_SpeedUnchanged()
        {
            var s = Soldier();
            var l2 = EnemyLevels.Scale(s, 2);
            var l4 = EnemyLevels.Scale(s, 4);
            Assert.AreEqual(240f, l2.maxHealth, 0.01f, "×1.6");
            Assert.AreEqual(150f * 1.6f * 1.6f * 1.6f, l4.maxHealth, 0.01f);
            Assert.Greater(l4.maxPoise, l2.maxPoise);
            Assert.AreEqual(5f, l4.runSpeed, "السرعة ثابتة — الصعوبة من القوة لا من المطاردة");
            Assert.AreEqual(150f, s.maxHealth, "الأصل لا يُمس");
        }

        [Test]
        public void Weapon_HitsHarder_WithShorterWindup()
        {
            var w = Sword();
            var l3 = EnemyLevels.Scale(w, 3);
            Assert.AreEqual(20f * 1.3f * 1.3f, l3.lightDamage, 0.01f);
            Assert.Less(l3.light.windup, w.light.windup, "إعلان أقصر");
            Assert.GreaterOrEqual(l3.light.windup, w.light.windup * EnemyLevels.MinWindupFactor - 0.0001f);
            Assert.GreaterOrEqual(l3.light.cancelWindowStart, l3.light.windup + l3.light.active,
                "نافذة الإلغاء لا تسبق الضربة");
        }

        [Test]
        public void Souls_RoundedToTens_AndGrow()
        {
            Assert.AreEqual(100, EnemyLevels.ScaleSouls(100, 1));
            Assert.AreEqual(180, EnemyLevels.ScaleSouls(100, 2));
            Assert.AreEqual(320, EnemyLevels.ScaleSouls(100, 3));
            Assert.AreEqual(580, EnemyLevels.ScaleSouls(100, 4));
        }

        [Test]
        public void Scale_IsCachedPerTier_AndClamped()
        {
            var s = Soldier();
            Assert.AreSame(EnemyLevels.Scale(s, 3), EnemyLevels.Scale(s, 3), "نسخة واحدة لكل مستوى");
            Assert.AreSame(EnemyLevels.Scale(s, EnemyLevels.MaxTier), EnemyLevels.Scale(s, 99));
            Assert.AreSame(s, EnemyLevels.Scale(s, 0));
        }

        // الموازنة: قوة المستوى 4 تقتل لاعباً عارياً (100 صحة) بثلاث ضربات —
        // «قوي جداً» لكن ليس ضربة واحدة: التفادي يبقى ممكناً.
        [Test]
        public void Tier4_ThreeHitsKillBasePlayer_NotOne()
        {
            var l4 = EnemyLevels.Scale(Sword(), 4);
            float playerHealth = ScriptableObject.CreateInstance<CharacterStats>().maxHealth;
            Assert.Less(l4.lightDamage, playerHealth / 2f, "لا ضربتان");
            Assert.GreaterOrEqual(l4.lightDamage * 3f, playerHealth, "ثلاث ضربات تقتل");
        }
    }
}
