using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class PlayerBrainTests
    {
        private StaminaPool stamina;
        private WeaponData weapon;
        private CharacterStats stats;
        private PlayerBrain brain;
        private List<(bool on, string note)> invulnEvents;
        private int hitboxOpens;

        [SetUp]
        public void SetUp()
        {
            stats = ScriptableObject.CreateInstance<CharacterStats>();
            weapon = ScriptableObject.CreateInstance<WeaponData>();
            stamina = new StaminaPool(stats.maxStamina, stats.staminaRegenPerSecond,
                stats.staminaRegenDelay, stats.blockingRegenMultiplier);
            brain = new PlayerBrain(stamina, weapon, stats);
            invulnEvents = new List<(bool, string)>();
            hitboxOpens = 0;
            brain.InvulnerabilityChanged += on => invulnEvents.Add((on, null));
            brain.HitboxOpen += _ => hitboxOpens++;
        }

        private void TickBy(float total, float step = 0.05f)
        {
            for (float t = 0f; t < total - 1e-4f; t += step)
                brain.Tick(step);
        }

        [Test]
        public void Dodge_ConsumesStamina_AndGrantsIFramesWindow()
        {
            Assert.IsTrue(brain.RequestDodge());
            Assert.AreEqual(PlayerStateId.Dodge, brain.CurrentState);
            Assert.AreEqual(80f, stamina.Current, "التفادي يستهلك 20 ستامينا");

            brain.Tick(0.05f); // t=0.05: قبل نافذة الحصانة
            Assert.IsEmpty(invulnEvents);

            brain.Tick(0.10f); // t=0.15: داخل النافذة (0.1–0.4)
            Assert.AreEqual(1, invulnEvents.Count);
            Assert.IsTrue(invulnEvents[0].on);

            brain.Tick(0.30f); // t=0.45: بعد النافذة
            Assert.AreEqual(2, invulnEvents.Count);
            Assert.IsFalse(invulnEvents[1].on);

            brain.Tick(0.20f); // t=0.65 > 0.6: انتهى التفادي
            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState);
        }

        [Test]
        public void Dodge_InsufficientStamina_Rejected()
        {
            stamina.TryConsume(90f); // بقي 10 < 20
            bool rejectedFired = false;
            brain.StaminaRejected += () => rejectedFired = true;

            Assert.IsFalse(brain.RequestDodge());
            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState);
            Assert.IsTrue(rejectedFired);
        }

        [Test]
        public void Attack_LocksUntilCancelWindow()
        {
            Assert.IsTrue(brain.RequestLightAttack());
            Assert.AreEqual(PlayerStateId.AttackLight, brain.CurrentState);

            brain.Tick(0.10f); // t=0.10: داخل الـ windup
            Assert.IsFalse(brain.RequestDodge(), "التفادي مرفوض فوراً أثناء الالتزام بالهجوم");
            Assert.AreEqual(PlayerStateId.AttackLight, brain.CurrentState);
        }

        [Test]
        public void BufferedDodge_FiresAtCancelWindow()
        {
            brain.RequestLightAttack();
            TickBy(0.45f); // t=0.45: قبل نافذة الإلغاء (0.55)

            Assert.IsFalse(brain.RequestDodge()); // يُخزَّن
            TickBy(0.15f); // t=0.60: تجاوزنا نافذة الإلغاء

            Assert.AreEqual(PlayerStateId.Dodge, brain.CurrentState, "التفادي المخزن ينفَّذ عند النافذة");
        }

        [Test]
        public void BufferedInput_ExpiresAfter200ms()
        {
            brain.RequestLightAttack();
            brain.Tick(0.10f); // t=0.10
            Assert.IsFalse(brain.RequestDodge()); // يُخزَّن، تنتهي صلاحيته عند t=0.30 < 0.55

            TickBy(0.80f); // حتى t=0.90: الهجوم اكتمل

            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState, "المخزّن منتهي الصلاحية لا ينفَّذ");
        }

        [Test]
        public void LightCombo_AdvancesToThreeThenEnds()
        {
            brain.RequestLightAttack();           // الضربة 1
            TickBy(0.56f);                        // بعد نافذة الإلغاء
            Assert.IsTrue(brain.RequestLightAttack(), "الضربة 2 تتسلسل عند النافذة");
            TickBy(0.56f);
            Assert.IsTrue(brain.RequestLightAttack(), "الضربة 3");
            TickBy(0.56f);
            Assert.IsFalse(brain.RequestLightAttack(), "الكومبو 3 ضربات فقط");

            TickBy(1.0f); // اكتمال الضربة الثالثة
            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState);
            Assert.AreEqual(3, hitboxOpens, "ثلاث نوافذ ضرر لثلاث ضربات");
        }

        [Test]
        public void BlockedHit_InsufficientStamina_GuardBreaksToStagger()
        {
            brain.SetBlockHeld(true);
            Assert.AreEqual(PlayerStateId.Block, brain.CurrentState);

            stamina.TryConsume(90f); // بقي 10
            var hit = new DamagePacket(20f, 20f, Vector3.zero);

            Assert.IsFalse(brain.NotifyBlockedHit(hit), "ستامينا غير كافية = كسر حرس");
            Assert.AreEqual(PlayerStateId.Stagger, brain.CurrentState);
        }

        [Test]
        public void BlockedHit_WithStamina_AbsorbsAndCosts()
        {
            brain.SetBlockHeld(true);
            var hit = new DamagePacket(20f, 20f, Vector3.zero);

            Assert.IsTrue(brain.NotifyBlockedHit(hit));
            Assert.AreEqual(80f, stamina.Current, "الصد الناجح يستهلك ستامينا بقدر ضرر الضربة");
            Assert.AreEqual(PlayerStateId.Block, brain.CurrentState);
        }

        [Test]
        public void Death_IsTerminal()
        {
            brain.NotifyDied();
            Assert.AreEqual(PlayerStateId.Death, brain.CurrentState);

            Assert.IsFalse(brain.RequestDodge());
            Assert.IsFalse(brain.RequestLightAttack());
            brain.SetBlockHeld(true);
            TickBy(5f);
            Assert.AreEqual(PlayerStateId.Death, brain.CurrentState);
        }

        [Test]
        public void PoiseBreak_InterruptsAttack_AndClosesHitbox()
        {
            bool hitboxClosed = false;
            brain.HitboxClose += () => hitboxClosed = true;

            brain.RequestLightAttack();
            TickBy(0.30f); // t=0.30: الـ hitbox مفتوح (0.25–0.45)

            brain.NotifyDamaged(poiseBroken: true);

            Assert.AreEqual(PlayerStateId.Stagger, brain.CurrentState);
            Assert.IsTrue(hitboxClosed, "كسر التوازن أثناء الضربة يغلق نافذة الضرر");

            TickBy(0.85f); // مدة الترنح 0.8
            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState);
        }
    }
}
