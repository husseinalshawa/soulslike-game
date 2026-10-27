using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Core;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests
{
    // ثانويات المرحلة 6 (1-7): إشعار التبديل، تخزين Cast/Skill،
    // التزام درع الصفائح، مدى الرمح، حارس FP، إلخ.
    public class PolishMinorsTests
    {
        private static GameBootstrap Build()
        {
            var go = new GameObject("Bootstrap");
            return go.AddComponent<GameBootstrap>();
        }

        private static PlayerBrain FreshBrain(out StaminaPool stamina, out FocusPool focus,
            WeaponData weapon = null)
        {
            var stats = ScriptableObject.CreateInstance<CharacterStats>();
            stamina = new StaminaPool(stats.maxStamina, stats.staminaRegenPerSecond,
                stats.staminaRegenDelay, stats.blockingRegenMultiplier);
            focus = new FocusPool(60f);
            return new PlayerBrain(stamina,
                weapon != null ? weapon : GearLibrary.Longsword(), stats, focus);
        }

        [Test]
        public void GearSwitch_RaisesMessage()
        {
            var bootstrap = Build();
            var player = bootstrap.Player;
            player.AcquireWeapon(GearLibrary.Greatsword());

            string lastMessage = null;
            player.GearChanged += m => lastMessage = m;

            player.TrySwitchWeapon(); // يرجع للسيف الطويل
            StringAssert.Contains("Longsword", lastMessage, "رسالة التبديل تسمي السلاح");

            player.TrySwitchSpell();
            StringAssert.Contains("Edge", lastMessage, "التعويذة الثانية (Glintblade Edge)");
        }

        [Test]
        public void CastBufferedDuringAttack_FiresAfterRecovery()
        {
            var brain = FreshBrain(out var stamina, out var focus);
            var bolt = GearLibrary.GlintBolt();

            Assert.IsTrue(brain.RequestLightAttack());
            // نافذة التخزين 0.2ث كالهجمات: الضغط قرب نهاية الالتزام يُلتقط
            for (int i = 0; i < 45; i++) brain.Tick(1f / 60f); // 0.75 من أصل 0.90
            Assert.IsFalse(brain.RequestCast(bolt), "أثناء الالتزام يُخزَّن لا يُنفذ");
            Assert.AreEqual(PlayerStateId.AttackLight, brain.CurrentState);
            Assert.AreEqual(60f, focus.Current, "لا خصم FP قبل التنفيذ الفعلي");

            for (int i = 0; i < 20 && brain.CurrentState == PlayerStateId.AttackLight; i++)
                brain.Tick(1f / 60f);

            Assert.AreEqual(PlayerStateId.Cast, brain.CurrentState,
                "المخزنة تنفذ عند أول نافذة");
            Assert.AreEqual(60f - bolt.fpCost, focus.Current, 0.001f, "الخصم لحظة التنفيذ");
        }

        [Test]
        public void BufferedCast_WithoutFp_RejectsWithoutStuckState()
        {
            var brain = FreshBrain(out _, out var focus);
            var bolt = GearLibrary.GlintBolt();
            focus.TrySpend(55f); // يبقى 5 < 12

            int rejected = 0;
            brain.FocusRejected += () => rejected++;

            brain.RequestLightAttack();
            for (int i = 0; i < 45; i++) brain.Tick(1f / 60f);
            brain.RequestCast(bolt); // تُخزَّن قرب النهاية
            for (int i = 0; i < 20 && brain.CurrentState == PlayerStateId.AttackLight; i++)
                brain.Tick(1f / 60f);

            Assert.AreEqual(PlayerStateId.Locomotion, brain.CurrentState, "لا حالة عالقة");
            Assert.AreEqual(1, rejected, "رُفضت عند لحظة التنفيذ بوميض البار");
        }

        [Test]
        public void SkillBufferedDuringCast_Fires()
        {
            var brain = FreshBrain(out _, out var focus);

            Assert.IsTrue(brain.RequestCast(GearLibrary.GlintBolt()));
            for (int i = 0; i < 45; i++) brain.Tick(1f / 60f); // 0.75 من إلقاء 0.90
            Assert.IsFalse(brain.RequestSkill(), "أثناء الإلقاء تُخزَّن");

            for (int i = 0; i < 20 && brain.CurrentState == PlayerStateId.Cast; i++)
                brain.Tick(1f / 60f);

            Assert.AreEqual(PlayerStateId.Skill, brain.CurrentState,
                "مهارة مخزنة تنطلق بعد الإلقاء");
        }

        [Test]
        public void PlateDodge_CancelPointScalesWithArmor()
        {
            // درع ثقيل (مضاعف 1.3): الإلغاء الهجومي عند 0.52 لا 0.4
            var heavy = FreshBrain(out _, out _);
            heavy.SetDodgeProfile(1.3f);
            Assert.IsTrue(heavy.RequestDodge());
            for (int i = 0; i < 30; i++) heavy.Tick(1f / 60f); // 0.50ث
            Assert.IsFalse(heavy.RequestLightAttack(), "قبل 0.52: ما زال ملتزماً");
            Assert.AreEqual(PlayerStateId.Dodge, heavy.CurrentState);
            for (int i = 0; i < 3; i++) heavy.Tick(1f / 60f); // 0.55ث — الفعل المخزن ينطلق
            Assert.AreEqual(PlayerStateId.AttackLight, heavy.CurrentState,
                "بعد نقطة الإلغاء الممددة ينفذ المخزن");

            // درع خفيف (1.0): الإلغاء عند 0.4 كما كان
            var light = FreshBrain(out _, out _);
            Assert.IsTrue(light.RequestDodge());
            for (int i = 0; i < 25; i++) light.Tick(1f / 60f); // 0.416ث
            Assert.IsTrue(light.RequestLightAttack(), "خفيف: الإلغاء المبكر القديم");
        }

        [Test]
        public void Spear_SkillDash2_4()
        {
            Assert.AreEqual(2.4f, GearLibrary.Spear().skill.dashDistance,
                "شحنة الرمح 2.4 + مدى 2.4 = 4.8م الموعودة");
        }

        [Test]
        public void FocusPool_TrySpendZero_KeepsRegenTimer()
        {
            var pool = new FocusPool(60f);
            Assert.IsTrue(pool.TrySpend(10f));
            pool.Tick(1.9f); // مهلة التركيز 2.0 لم تكتمل بعد

            Assert.IsTrue(pool.TrySpend(0f), "صفر يُقبل دائماً");
            pool.Tick(0.2f); // المجموع 2.1 > المهلة — التجدد بدأ

            Assert.Greater(pool.Current, 50f, "صرف الصفر لم يصفّر مؤقت التجدد");
        }
    }
}
