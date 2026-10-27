using NUnit.Framework;
using SoulsGame.Feel;
using SoulsGame.UI;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class FeelLogicTests
    {
        [Test]
        public void HitStop_LongestWins_RestoresExactlyOne()
        {
            var stop = new HitStopLogic();
            Assert.AreEqual(1f, stop.CurrentScale);

            stop.Request(0.06f);
            stop.Request(0.09f); // الأطول يسود
            Assert.IsTrue(stop.Active);
            Assert.AreEqual(HitStopLogic.StopScale, stop.CurrentScale);

            stop.Tick(0.06f);
            Assert.IsTrue(stop.Active, "طلب الـ90ms يسود على الـ60");

            stop.Tick(0.03f);
            Assert.IsFalse(stop.Active);
            Assert.AreEqual(1f, stop.CurrentScale, "يعود 1 تماماً — لا slow-mo دائم");
        }

        [Test]
        public void HitStop_RequestDuringStop_NeverShortens()
        {
            var stop = new HitStopLogic();
            stop.Request(0.09f);
            stop.Tick(0.02f);
            stop.Request(0.05f); // أقصر من المتبقي — لا يقصّر
            stop.Tick(0.05f);
            Assert.IsTrue(stop.Active, "المتبقي 0.07 لا ينزل لـ0.05");
            stop.Tick(0.02f);
            Assert.IsFalse(stop.Active);
        }

        [Test]
        public void Shake_DecaysToExactZero()
        {
            var shake = new ShakeLogic();
            Assert.AreEqual(Vector3.zero, shake.Tick(0.016f), "بلا طلب لا إزاحة");

            shake.Shake(0.15f, 0.25f);
            Vector3 mid = shake.Tick(0.1f);
            Assert.IsTrue(shake.Active);
            Assert.Greater(mid.magnitude, 0f, "منتصف المدة يهتز");
            Assert.LessOrEqual(mid.magnitude, 0.15f * 1.5f, "ضمن حدود السعة");

            Vector3 after = shake.Tick(0.2f); // تجاوز المدة
            Assert.AreEqual(Vector3.zero, after, "بعد المدة صفر تماماً");
            Assert.IsFalse(shake.Active);
        }

        [Test]
        public void Fade_LinearAlpha()
        {
            var fade = new FadeLogic();
            Assert.AreEqual(0f, fade.Alpha);

            fade.FadeTo(1f, 0.6f);
            fade.Tick(0.3f);
            Assert.AreEqual(0.5f, fade.Alpha, 0.01f, "منتصف المدة = منتصف الشفافية");
            Assert.IsFalse(fade.Done);

            fade.Tick(0.3f);
            Assert.AreEqual(1f, fade.Alpha, 0.001f);
            Assert.IsTrue(fade.Done);

            fade.FadeTo(0f, 0.5f);
            fade.Tick(0.25f);
            Assert.AreEqual(0.5f, fade.Alpha, 0.01f, "النزول خطي أيضاً");
        }

        [Test]
        public void Title_SensitivityClampAndSingleStart()
        {
            var title = new TitleLogic();
            Assert.AreEqual(1f, title.Sensitivity);

            for (int i = 0; i < 8; i++) title.AdjustSensitivity(+1);
            Assert.AreEqual(TitleLogic.Max, title.Sensitivity, "مقصوص عند 2.0");

            for (int i = 0; i < 12; i++) title.AdjustSensitivity(-1);
            Assert.AreEqual(TitleLogic.Min, title.Sensitivity, "مقصوص عند 0.5");

            Assert.IsFalse(title.Started);
            Assert.IsTrue(title.RequestStart(), "أول E تبدأ");
            Assert.IsTrue(title.Started);
            Assert.IsFalse(title.RequestStart(), "E ثانية لا تبدأ مرتين");
        }
    }
}
