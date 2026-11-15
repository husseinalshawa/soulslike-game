using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Core;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests
{
    // اختبارات المراجعة النهائية للمرحلة 4 — كل اختبار يثبت إصلاحاً من تقرير
    // المراجع: ذرية تجهيز السلاح، تجاهل البوس النائم، تصفير اندفاعة المهارة،
    // أصول العتاد في البناء، التعزيز على المهارات، ملء التركيز بالإحياء،
    // ومنع الالتقاط أثناء الموت.
    public class FinalReviewFixTests
    {
        private static GameBootstrap BuildBootstrap()
        {
            var go = new GameObject("Bootstrap");
            return go.AddComponent<GameBootstrap>();
        }

        // الحرجة 1: التقاط سلاح أثناء التزام هجومي لا يفصل المخزون عن العقل —
        // الـ hitbox يبقى على شكل السلاح القديم حتى يقبل العقل التبديل في
        // الوضع المحايد، وعندها يتزامن كل شيء تلقائياً.
        [Test]
        public void WeaponPickup_MidAttack_DefersEquip_ThenSyncsOnNeutral()
        {
            var bootstrap = BuildBootstrap();
            var player = bootstrap.Player;
            float longswordReach = player.Brain.CurrentWeapon.reach;

            Assert.IsTrue(player.Brain.RequestLightAttack());
            Assert.AreEqual(PlayerStateId.AttackLight, player.Brain.CurrentState);

            var greatswordPickup = bootstrap.Pickups[0];
            Assert.AreEqual(1, greatswordPickup.TryCollect(greatswordPickup.transform.position),
                "الالتقاط نفسه ينجح");

            // منتصف الالتزام: المخزون تقدم لكن العقل والـ hitbox على القديم
            Assert.AreEqual("Greatsword", player.Weapons.Current.weaponName);
            Assert.AreNotEqual("Greatsword", player.Brain.CurrentWeapon.weaponName,
                "العقل يرفض التبديل أثناء الالتزام");
            Assert.AreEqual(longswordReach, player.weaponHitbox.transform.localScale.z, 0.001f,
                "الـ hitbox لا يتشوه لسلاح لم يتجهز بعد");

            // إنهاء الهجوم: العودة للمحايد تزامن العقل والـ hitbox تلقائياً
            for (int i = 0; i < 60 && player.Brain.CurrentState != PlayerStateId.Locomotion; i++)
                player.Brain.Tick(0.1f);

            Assert.AreEqual(PlayerStateId.Locomotion, player.Brain.CurrentState);
            Assert.AreEqual("Greatsword", player.Brain.CurrentWeapon.weaponName,
                "العقل التحق بالمخزون عند المحايد");
            Assert.AreEqual(2.0f, player.weaponHitbox.transform.localScale.z, 0.001f,
                "الـ hitbox أخذ طول السيف العظيم");
        }

        // الحرجة 2: البوس النائم ليس هدفاً للقذائف — لا قنص للمعركة قبل بدئها.
        [Test]
        public void Projectile_IgnoresDormantBoss_UntilFightBegins()
        {
            var bootstrap = BuildBootstrap();
            var bossDamageable = bootstrap.Boss.Damageable;
            // الشيم لا يستدعي OnEnable — نسجل البوس كما يفعل Unity عند التفعيل
            LockOnSystem.Register(bootstrap.Boss);
            try
            {
                var dormantCandidates = ProjectileAvatar.CollectCandidates();
                foreach (var (target, _, _) in dormantCandidates)
                    Assert.AreNotSame(bossDamageable, target, "البوس النائم خارج قائمة أهداف القذائف");

                bootstrap.Boss.BeginFight();
                bool found = false;
                foreach (var (target, _, _) in ProjectileAvatar.CollectCandidates())
                    if (ReferenceEquals(bossDamageable, target))
                        found = true;
                Assert.IsTrue(found, "بعد بدء المعركة يصبح هدفاً مشروعاً");
            }
            finally
            {
                LockOnSystem.Unregister(bootstrap.Boss);
            }
        }

        // المهمة 3: مغادرة حالة المهارة (كسر توازن مثلاً) تصفّر بقية الاندفاعة —
        // لا تتسرب لمهارة لاحقة بلا اندفاع.
        [Test]
        public void SkillDash_ClearedWhenStateLeavesSkill()
        {
            var bootstrap = BuildBootstrap();
            var motor = bootstrap.Player.GetComponent<PlayerMotor>();

            motor.State = PlayerStateId.Skill;
            motor.BeginSkillDash(4f);
            Assert.Greater(motor.SkillDashRemaining, 0f, "الاندفاعة بدأت");

            motor.State = PlayerStateId.Stagger; // كسر توازن منتصف الاندفاعة
            Assert.AreEqual(0f, motor.SkillDashRemaining,
                "مغادرة حالة المهارة تصفّر الاندفاعة المتبقية");
        }

        // المهمة 4: أصول العتاد المسندة من المشهد (Inspector) هي ما يصل للعالم —
        // لا إعادة بناء صامتة من GearLibrary عندما يوفر المشهد أصلاً.
        [Test]
        public void Bootstrap_UsesAssignedGearAssets_OverLibraryFallback()
        {
            var go = new GameObject("Bootstrap");
            go.SetActive(false);
            var bootstrap = go.AddComponent<GameBootstrap>();

            var customGreatsword = ScriptableObject.CreateInstance<WeaponData>();
            customGreatsword.weaponName = "Tuned Greatsword";
            var customCloth = GearLibrary.Cloth();
            customCloth.armorName = "Tuned Cloth";
            var customBolt = GearLibrary.GlintBolt();
            bootstrap.greatswordData = customGreatsword;
            bootstrap.clothArmorData = customCloth;
            bootstrap.boltSpellData = customBolt;

            go.SetActive(true); // Awake يبني الآن

            Assert.AreSame(customGreatsword, bootstrap.Pickups[0].weapon,
                "أصل المشهد يصل للعالم كما هو — تعديله من الـ Inspector يؤثر فعلاً");
            Assert.AreSame(customCloth, bootstrap.Player.Armors.Current,
                "درع البداية من أصل المشهد");
            Assert.AreSame(customBolt, bootstrap.Player.Spells.Current,
                "تعويذة القذيفة من أصل المشهد");
        }

        // المهمة 5: تعزيز السلاح (+10) ينطبق على ضربات المهارة لا الضربات العادية فقط.
        [Test]
        public void WeaponBuff_AppliesToSkillDamage()
        {
            var bootstrap = BuildBootstrap();
            var player = bootstrap.Player;
            float baseSkill = player.CurrentSkillDamage();

            player.Buff.Activate(10f, 30f);

            Assert.AreEqual(baseSkill + 10f, player.CurrentSkillDamage(), 0.001f,
                "التعزيز يرفع ضرر المهارة أيضاً");
        }

        // المهمة 6: الإحياء يملأ التركيز مثل الصحة والستامينا.
        [Test]
        public void Respawn_RefillsFocus()
        {
            var bootstrap = BuildBootstrap();
            var player = bootstrap.Player;

            Assert.IsTrue(player.Focus.TrySpend(30f));
            player.Damageable.Health.TakeDamage(10000f);

            bootstrap.Loop.Respawn();

            Assert.AreEqual(player.Focus.Max, player.Focus.Current,
                "الإحياء يعيد التركيز كاملاً ككل المجمّعات");
        }

        // المهمة 7: لا التقاط أثناء الموت — لا إشعار فوق شاشة YOU DIED
        // ولا تجهيز يرفضه العقل الميت.
        [Test]
        public void GearPickup_RejectedWhileDead()
        {
            var bootstrap = BuildBootstrap();
            var player = bootstrap.Player;
            var pickup = bootstrap.Pickups[0];

            player.Damageable.Health.TakeDamage(10000f);
            Assert.AreEqual(0, pickup.TryCollect(pickup.transform.position),
                "الميت لا يلتقط");
            Assert.AreEqual(1, player.Weapons.Count, "المخزون لم يتغير");

            bootstrap.Loop.Respawn();
            Assert.AreEqual(1, pickup.TryCollect(pickup.transform.position),
                "بعد الإحياء يعمل الالتقاط");
        }
    }
}
