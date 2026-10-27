using NUnit.Framework;
using SoulsGame.Data;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class MarshThemeTests
    {
        [Test]
        public void MarshZone_ContainsByPlanarDistance()
        {
            var zone = new MarshZone(new Vector3(120f, 0f, 80f), 35f);

            Assert.IsTrue(zone.Contains(new Vector3(120f + 34f, 50f, 80f)),
                "داخل أفقياً ولو اختلف الارتفاع");
            Assert.IsFalse(zone.Contains(new Vector3(120f + 36f, 0f, 80f)), "خارج النطاق");
            Assert.AreEqual(0.65f, MarshZone.SpeedMultiplier);
        }

        [Test]
        public void ThemeShift_LerpsAndFinishes()
        {
            var from = ScriptableObject.CreateInstance<WorldTheme>();
            from.skyColor = Color.black;
            var to = ScriptableObject.CreateInstance<WorldTheme>();
            to.skyColor = Color.white;
            var working = ScriptableObject.CreateInstance<WorldTheme>();

            var shift = new ThemeShift(from, to, 8f);
            Assert.IsFalse(shift.Done);

            shift.Tick(4f);
            Assert.AreEqual(0.5f, shift.T, 0.001f);
            var mid = shift.Blend(working);
            Assert.AreEqual(0.5f, mid.skyColor.r, 0.001f, "منتصف المدة = منتصف اللون");

            shift.Tick(4f);
            Assert.IsTrue(shift.Done);
            Assert.AreEqual(1f, shift.T, 0.001f);
            Assert.AreEqual(1f, shift.Blend(working).skyColor.r, 0.001f);
        }
    }
}
