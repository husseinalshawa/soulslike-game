using NUnit.Framework;
using SoulsGame.Data;

namespace SoulsGame.Tests
{
    // يثبّت أرقام جداول المواصفة §2/§5/§6 — مصدر الأرقام الواحد للبناء والمحرر.
    public class GearLibraryTests
    {
        [Test]
        public void Greatsword_Numbers()
        {
            var w = GearLibrary.Greatsword();

            Assert.AreEqual("Greatsword", w.weaponName);
            Assert.AreEqual(45f, w.lightDamage);
            Assert.AreEqual(35f, w.lightPoiseDamage);
            Assert.AreEqual(22f, w.lightStaminaCost);
            Assert.AreEqual(0.45f, w.light.windup);
            Assert.AreEqual(0.30f, w.light.active);
            Assert.AreEqual(0.70f, w.light.recovery);
            Assert.AreEqual(0.85f, w.light.cancelWindowStart);
            Assert.AreEqual(85f, w.heavyDamage);
            Assert.AreEqual(2, w.comboLength);
            Assert.AreEqual(2.0f, w.reach);
            Assert.AreEqual("Quake", w.skill.skillName);
            Assert.AreEqual(25f, w.skill.fpCost);
            Assert.AreEqual(3f, w.skill.radius);
        }

        [Test]
        public void TwinDaggers_FiveHitCombo()
        {
            var w = GearLibrary.TwinDaggers();

            Assert.AreEqual(12f, w.lightDamage);
            Assert.AreEqual(5, w.comboLength);
            Assert.AreEqual(1.0f, w.reach);
            Assert.AreEqual(3, w.skill.hitCount, "الوابل ثلاث إصابات");
            Assert.AreEqual(18f, w.skill.fpCost);
        }

        [Test]
        public void Plate_Armor_Numbers()
        {
            var a = GearLibrary.Plate();

            Assert.AreEqual(0.30f, a.damageReduction);
            Assert.AreEqual(1.30f, a.dodgeDurationMultiplier);
            Assert.AreEqual(0.85f, a.dodgeDistanceMultiplier);
        }

        [Test]
        public void GlintBolt_Numbers()
        {
            var s = GearLibrary.GlintBolt();

            Assert.IsFalse(s.isBuff);
            Assert.AreEqual(12f, s.fpCost);
            Assert.AreEqual(35f, s.damage);
            Assert.AreEqual(20f, s.projectileSpeed);
            Assert.AreEqual(30f, s.maxRange);

            var b = GearLibrary.WeaponBuffSpell();
            Assert.IsTrue(b.isBuff);
            Assert.AreEqual(20f, b.fpCost);
            Assert.AreEqual(10f, b.buffBonusDamage);
            Assert.AreEqual(30f, b.buffDuration);
        }

        [Test]
        public void Longsword_MatchesPhase1Defaults()
        {
            var w = GearLibrary.Longsword();
            Assert.AreEqual(25f, w.lightDamage);
            Assert.AreEqual(3, w.comboLength);
            Assert.AreEqual(1.6f, w.reach);
            Assert.AreEqual(15f, w.skill.fpCost, "اندفاعة قاطعة");
            Assert.AreEqual(4f, w.skill.dashDistance);
        }
    }
}
