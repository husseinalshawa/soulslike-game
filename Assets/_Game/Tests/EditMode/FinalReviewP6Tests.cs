using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Core;
using SoulsGame.Feel;
using UnityEngine;

namespace SoulsGame.Tests
{
    // إصلاحات المراجعة النهائية للمرحلة 6.
    public class FinalReviewP6Tests
    {
        private static GameBootstrap Build()
        {
            var go = new GameObject("Bootstrap");
            return go.AddComponent<GameBootstrap>();
        }

        // الحرجة: TextMesh بلا خط لا يرسم شيئاً في Unity الحقيقي.
        [Test]
        public void DamageNumber_HasFontAndMaterial()
        {
            var number = DamageNumber.Spawn(Vector3.zero, 25f);
            Assert.IsNotNull(number.label.font, "خط مسند — وإلا الرقم غير مرئي");
            var renderer = number.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreSame(number.label.font.material, renderer.material,
                "مادة الخط على الـ renderer — شرط ظهور TextMesh");
        }

        // مهمة: تلاشي العنوان 0.8ث لا قطعة فورية — الغطاء يبدأ معتماً منطقياً.
        [Test]
        public void TitleFade_IsGradualAfterBegin()
        {
            var bootstrap = Build();
            var title = bootstrap.Loop.titleScreen;
            Assert.AreEqual(1f, title.Fade.Alpha, 0.001f, "الغطاء معتم قبل البدء");

            bootstrap.Loop.BeginGame();
            title.Fade.Tick(0.4f);
            Assert.AreEqual(0.5f, title.Fade.Alpha, 0.01f, "منتصف المدة = نصف شفافية");
            Assert.IsFalse(title.Fade.Done);
            title.Fade.Tick(0.4f);
            Assert.AreEqual(0f, title.Fade.Alpha, 0.001f);
        }

        // مهمة: YOU DIED تظهر بعد اكتمال التعتيم لا تحته.
        [Test]
        public void DeathScreen_ShowsOnlyAfterFadeCompletes()
        {
            var bootstrap = Build();
            var loop = bootstrap.Loop;
            var overlayGo = new GameObject("FadeOverlay");
            var overlay = overlayGo.AddComponent<UI.FadeOverlay>();
            loop.fadeOverlay = overlay;

            bootstrap.Player.Damageable.Health.TakeDamage(100000f);
            loop.ProcessDeathSequence();
            Assert.IsFalse(loop.deathScreen.panel.activeSelf,
                "لا YOU DIED قبل اكتمال التعتيم");

            overlay.Fade.Tick(0.6f);
            loop.ProcessDeathSequence();
            Assert.IsTrue(loop.deathScreen.panel.activeSelf, "تظهر فوق الأسود الكامل");
        }

        // مهمة (رفعتها من ثانوية): أرقام البوس فوق رأسه لا داخل صدره.
        [Test]
        public void DamageNumbers_SpawnAboveTargetHead()
        {
            var bootstrap = Build();

            bootstrap.Enemy.Damageable.ApplyDamage(new DamagePacket(10f, 0f, Vector3.zero));
            Assert.AreEqual(bootstrap.Enemy.transform.position.y + 1.6f,
                DamageNumber.LastSpawnPosition.y, 0.01f, "الجندي: فوق الرأس بقليل");

            bootstrap.Boss.Damageable.ApplyDamage(new DamagePacket(10f, 0f, Vector3.zero));
            Assert.AreEqual(bootstrap.Boss.transform.position.y + 3.8f,
                DamageNumber.LastSpawnPosition.y, 0.01f, "البوس ضعف الطول — الرقم فوقه");
        }

        // مهمة: جدول الاقتصاد مثبت من ثوابت الكود — لا ينحرف حسابياً ثانية.
        [Test]
        public void EconomyTable_PinnedFromCodeConstants()
        {
            var bootstrap = Build();
            int total =
                8 * bootstrap.enemyStats.soulsReward +
                3 * bootstrap.chanterStats.soulsReward +
                bootstrap.Cache.amount +
                Story.StoryLibrary.BellReward +
                bootstrap.bossStats.soulsReward +
                GameLoop.AltarReward;
            Assert.AreEqual(7550, total, "مجموع الأرواح المتاحة في مسار واحد");

            var data = ScriptableObject.CreateInstance<Data.ProgressionData>(); // الافتراضيات المثبتة
            int cumulative = 0, affordable = 0;
            for (int level = 1; level <= 40; level++)
            {
                cumulative += data.baseCost + data.costIncrement * (level - 1);
                if (cumulative <= total) affordable = level;
            }
            Assert.AreEqual(21, affordable, "المسار الكامل يوصلك حوالي مستوى 21");
        }
    }
}
