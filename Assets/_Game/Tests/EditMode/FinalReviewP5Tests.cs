using NUnit.Framework;
using SoulsGame.Core;
using SoulsGame.Story;
using SoulsGame.World;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.Tests
{
    // إصلاحات المراجعة النهائية للمرحلة 5 — كل اختبار يثبت ملاحظة «مهمة»
    // من تقرير المراجع.
    public class FinalReviewP5Tests
    {
        private static NpcAvatar PlaceNpcAtPlayer(GameBootstrap bootstrap, NpcQuestline questline)
        {
            var npcGo = new GameObject($"Npc_{questline.NpcName}");
            npcGo.transform.position = bootstrap.Player.transform.position;
            var npc = npcGo.AddComponent<NpcAvatar>();
            npc.Questline = questline;
            bootstrap.Loop.npcs = new[] { npc };
            return npc;
        }

        // مهمة 1: لا حوار وعدو عدائي قريب — قفل الإدخال وسط مطاردة = موت بلا دفاع.
        [Test]
        public void Dialogue_RefusedWhileEnemyHostileNearby()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;
            PlaceNpcAtPlayer(bootstrap, StoryLibrary.Valion());

            // عدو مطارد على بعد 3 أمتار
            var enemy = bootstrap.Enemy;
            enemy.Brain.Tick(0.1f, 5f, 0f);
            Assert.AreEqual(Enemies.EnemyStateId.Chase, enemy.Brain.CurrentState);
            enemy.transform.position = bootstrap.Player.transform.position + new Vector3(3f, 0f, 0f);

            loop.HandleInteract();
            Assert.IsFalse(loop.Dialogue.IsActive, "لا حديث والسيوف مشهورة");
            Assert.IsFalse(bootstrap.Player.InputLocked, "ولا قفل إدخال");

            // موت العدو يرفع الحظر
            enemy.Damageable.Health.TakeDamage(10000f);
            loop.HandleInteract();
            Assert.IsTrue(loop.Dialogue.IsActive, "بعد موته يعود الحديث ممكناً");
        }

        // مهمة 2: سرعة المطاردة من إحصاءات العدو — «أبطأ» المرتّل رقم حي لا ميت.
        [Test]
        public void ChaseSpeed_ComesFromStats()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Assert.AreEqual(5f, bootstrap.AllEnemies[0].ChaseSpeed, "جندي الحصن يطارد بـ5 كما كان");
            Assert.AreEqual(2.5f, bootstrap.AllEnemies[8].ChaseSpeed,
                "المرتّل الغارق أبطأ فعلاً — اللاعب المبطأ بالماء (3.25) يسبقه");
        }

        // مهمة 3: موقع راحة الكنيسة على أرض جافة خارج الماء — لا راحة غارقة.
        [Test]
        public void ChapelGrace_OnDryLand_OutsideWater()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            Vector3 site = bootstrap.Loop.Graces.PositionOf(3);
            float dx = site.x - WorldHeightmap.MarshCenter.x;
            float dz = site.z - WorldHeightmap.MarshCenter.z;
            Assert.Greater(Mathf.Sqrt(dx * dx + dz * dz), WorldHeightmap.MarshRadius + 12f,
                "خارج الحوض ومنطقة الدمج كلياً — أرض جافة");
            // خارج مربع سطح الماء المرئي بهامش ≥ 4م: لا بلاطة ماء فوق الموقع
            Assert.IsTrue(Mathf.Abs(dx) > WorldHeightmap.MarshRadius + 4f ||
                          Mathf.Abs(dz) > WorldHeightmap.MarshRadius + 4f,
                "بعيد عن مربع الماء — الشعلة مرئية لا مغمورة");
        }

        // مهمة 4: تسليم الرماد يظهر الاسم والوصف — ثلث السر لا يضيع صامتاً.
        [Test]
        public void AshesGrant_ShowsDescription()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var loop = bootstrap.Loop;

            var toastGo = new GameObject("TestToast");
            var toast = toastGo.AddComponent<UI.Toast>();
            var labelGo = new GameObject("Label");
            toast.label = labelGo.AddComponent<Text>();
            loop.toast = toast;

            loop.Story.Set(StoryFlag.BossDefeated);
            PlaceNpcAtPlayer(bootstrap, StoryLibrary.Valion());

            loop.HandleInteract(); // بدء (مرحلة الرماد: 3 أسطر)
            loop.HandleInteract();
            loop.HandleInteract();
            loop.HandleInteract(); // الإكمال: يعطي الرماد

            Assert.IsTrue(bootstrap.Player.KeyItems.Has(StoryLibrary.WardensAshes));
            StringAssert.Contains("Still warm", toast.label.text,
                "وصف الرماد ظهر في الإشعار");
        }

        // مهمة 5أ: المرتّلون فوق أرضية الكنيسة الحجرية لا داخلها.
        [Test]
        public void Chanters_SpawnAboveChapelFloorSlab()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            float floorTop = WorldHeightmap.MarshFloorY + 0.3f;
            for (int i = 8; i < 11; i++)
                Assert.GreaterOrEqual(bootstrap.AllEnemies[i].transform.position.y, floorTop + 1f,
                    $"المرتّل {i - 8} أقدامه فوق سطح البلاطة");
        }

        // مهمة 5ب: درجة أرضية الكنيسة (0.3م) أقل من stepOffset صريح — الصعود مضمون.
        [Test]
        public void PlayerController_StepOffsetClearsChapelLip()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();

            var controller = bootstrap.Player.GetComponent<CharacterController>();
            Assert.AreEqual(0.5f, controller.stepOffset, 0.001f,
                "stepOffset صريح أعلى من حافة أرضية الكنيسة (0.3)");
        }

        // مهمة 6: الركوب إلى داخل المستنقع = ترجّل إجباري — الماء يمسك ما يلتقطه.
        [Test]
        public void RidingIntoMarsh_ForcesDismount()
        {
            var go = new GameObject("Bootstrap");
            var bootstrap = go.AddComponent<GameBootstrap>();
            var player = bootstrap.Player;

            player.transform.position = new Vector3(50f, 5f, -80f); // مرج مفتوح
            Assert.IsTrue(player.TrySummonHorse());
            Assert.IsTrue(player.IsRiding);

            player.transform.position = new Vector3(
                WorldHeightmap.MarshCenter.x, WorldHeightmap.MarshFloorY,
                WorldHeightmap.MarshCenter.z);
            player.EnforceMarshRules();

            Assert.IsFalse(player.IsRiding, "دخول الماء راكباً ينزلك");
        }
    }
}
