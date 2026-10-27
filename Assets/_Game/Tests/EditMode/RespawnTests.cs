using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class RespawnTests
    {
        [Test]
        public void PlayerBrain_Revive_ReturnsFromDeathToLocomotion()
        {
            var stats = ScriptableObject.CreateInstance<CharacterStats>();
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            var stamina = new StaminaPool(100f, 25f, 1f, 0.33f);
            var brain = new PlayerBrain(stamina, weapon, stats);

            brain.NotifyDied();
            Assert.AreEqual(PlayerStateId.Death, brain.CurrentState);

            brain.Revive();
            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState);
            Assert.IsTrue(brain.RequestDodge(), "بعد الإحياء تعود الأوامر للعمل");
        }

        [Test]
        public void StaminaPool_ResetToFull_RefillsAndRegensNormally()
        {
            var pool = new StaminaPool(100f, 25f, 1f, 0.33f);
            pool.TryConsume(70f);

            pool.ResetToFull();

            Assert.AreEqual(100f, pool.Current);
        }
    }
}
