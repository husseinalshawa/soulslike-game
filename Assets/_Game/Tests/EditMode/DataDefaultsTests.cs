using NUnit.Framework;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Tests
{
    // Pins the initial balance values from the spec. Changing balance on
    // purpose means updating these numbers in one place alongside the data.
    public class DataDefaultsTests
    {
        [Test]
        public void WeaponData_Defaults_MatchInitialSwordBalance()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();

            Assert.AreEqual(25f, w.lightDamage);
            Assert.AreEqual(15f, w.lightPoiseDamage);
            Assert.AreEqual(15f, w.lightStaminaCost);
            Assert.AreEqual(50f, w.heavyDamage);
            Assert.AreEqual(35f, w.heavyPoiseDamage);
            Assert.AreEqual(30f, w.heavyStaminaCost);
            Assert.AreEqual(3, w.comboLength);

            Assert.AreEqual(0.25f, w.light.windup);
            Assert.AreEqual(0.20f, w.light.active);
            Assert.AreEqual(0.45f, w.light.recovery);
            Assert.AreEqual(0.55f, w.light.cancelWindowStart);

            Assert.AreEqual(0.60f, w.heavy.windup);
            Assert.AreEqual(0.25f, w.heavy.active);
            Assert.AreEqual(0.80f, w.heavy.recovery);
            Assert.AreEqual(1.00f, w.heavy.cancelWindowStart);
        }

        [Test]
        public void CharacterStats_Defaults_MatchInitialPlayerBalance()
        {
            var s = ScriptableObject.CreateInstance<CharacterStats>();

            Assert.AreEqual(100f, s.maxHealth);
            Assert.AreEqual(100f, s.maxStamina);
            Assert.AreEqual(40f, s.maxPoise);
            Assert.AreEqual(10f, s.poiseRegenPerSecond);
            Assert.AreEqual(25f, s.staminaRegenPerSecond);
            Assert.AreEqual(1.0f, s.staminaRegenDelay);
            Assert.AreEqual(0.33f, s.blockingRegenMultiplier);
            Assert.AreEqual(2f, s.walkSpeed);
            Assert.AreEqual(5f, s.runSpeed);
            Assert.AreEqual(8f, s.sprintSpeed);
            Assert.AreEqual(10f, s.sprintStaminaPerSecond);
            Assert.AreEqual(20f, s.dodgeStaminaCost);
        }
    }
}
