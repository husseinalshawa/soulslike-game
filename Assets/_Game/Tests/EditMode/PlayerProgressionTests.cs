using NUnit.Framework;
using SoulsGame.Data;
using SoulsGame.Progression;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class PlayerProgressionTests
    {
        private PlayerProgression NewProgression(out SoulsWallet wallet)
        {
            var data = ScriptableObject.CreateInstance<ProgressionData>();
            var baseStats = ScriptableObject.CreateInstance<CharacterStats>(); // hp 100, st 100
            wallet = new SoulsWallet();
            return new PlayerProgression(data, baseStats);
        }

        [Test]
        public void Costs_FollowCurve()
        {
            var p = NewProgression(out var wallet);
            wallet.Add(10000);

            Assert.AreEqual(100, p.NextLevelCost, "المستوى 1→2 يكلف 100");
            p.TryLevelUp(StatId.Vigor, wallet);
            Assert.AreEqual(125, p.NextLevelCost, "ثم 125");
            p.TryLevelUp(StatId.Endurance, wallet);
            Assert.AreEqual(150, p.NextLevelCost, "ثم 150");
        }

        [Test]
        public void LevelUp_SpendsAndRaisesDerived()
        {
            var p = NewProgression(out var wallet);
            wallet.Add(100);

            Assert.IsTrue(p.TryLevelUp(StatId.Vigor, wallet));

            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(11, p.GetStat(StatId.Vigor));
            Assert.AreEqual(112f, p.MaxHealth, "صلابة +1 = +12 صحة");
            Assert.AreEqual(0, wallet.Current);
        }

        [Test]
        public void LevelUp_InsufficientSouls_RejectedAndUnchanged()
        {
            var p = NewProgression(out var wallet);
            wallet.Add(99);

            Assert.IsFalse(p.TryLevelUp(StatId.Strength, wallet));
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(10, p.GetStat(StatId.Strength));
            Assert.AreEqual(99, wallet.Current);
        }

        [Test]
        public void BonusDamage_FromStrength()
        {
            var p = NewProgression(out var wallet);
            wallet.Add(1000);
            p.TryLevelUp(StatId.Strength, wallet);
            p.TryLevelUp(StatId.Strength, wallet);

            Assert.AreEqual(4f, p.BonusDamage, "قوة +2 نقاط = +4 ضرر");
            Assert.AreEqual(100f, p.MaxHealth, "القوة لا تمس الصحة");
        }

        [Test]
        public void Endurance_RaisesMaxStamina()
        {
            var p = NewProgression(out var wallet);
            wallet.Add(100);
            p.TryLevelUp(StatId.Endurance, wallet);

            Assert.AreEqual(106f, p.MaxStamina, "جَلَد +1 = +6 ستامينا");
        }
    }
}
