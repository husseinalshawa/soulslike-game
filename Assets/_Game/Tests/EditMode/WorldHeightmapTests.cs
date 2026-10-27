using NUnit.Framework;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class WorldHeightmapTests
    {
        [Test]
        public void Height_Deterministic()
        {
            for (float x = -180f; x <= 180f; x += 37f)
                for (float z = -180f; z <= 180f; z += 41f)
                    Assert.AreEqual(WorldHeightmap.GetHeight(x, z), WorldHeightmap.GetHeight(x, z),
                        $"نفس المدخل نفس الناتج عند ({x},{z})");
        }

        [Test]
        public void Height_WithinBounds()
        {
            for (float x = -195f; x <= 195f; x += 19.5f)
                for (float z = -195f; z <= 195f; z += 19.5f)
                {
                    float h = WorldHeightmap.GetHeight(x, z);
                    Assert.GreaterOrEqual(h, 0f, $"لا ارتفاع سالب عند ({x},{z})");
                    Assert.LessOrEqual(h, 25f, $"حتى الحواف تحت 25 عند ({x},{z})");

                    bool inInterior = Mathf.Abs(x) < 180f && Mathf.Abs(z) < 180f;
                    if (inInterior)
                        Assert.LessOrEqual(h, WorldHeightmap.MaxHeight, $"الداخل ≤ {WorldHeightmap.MaxHeight} عند ({x},{z})");
                }
        }

        [Test]
        public void FortPlateau_Flat()
        {
            float center = WorldHeightmap.GetHeight(0f, 0f);
            for (float angle = 0f; angle < 360f; angle += 45f)
            {
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * 30f;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * 30f;
                Assert.AreEqual(center, WorldHeightmap.GetHeight(x, z), 0.01f,
                    "هضبة الحصن مسطحة ضمن نصف قطر 30");
            }
        }

        [Test]
        public void Edges_Rise()
        {
            float interiorAverage = 0f;
            int samples = 0;
            for (float x = -100f; x <= 100f; x += 50f)
                for (float z = -100f; z <= 100f; z += 50f)
                {
                    interiorAverage += WorldHeightmap.GetHeight(x, z);
                    samples++;
                }
            interiorAverage /= samples;

            Assert.Greater(WorldHeightmap.GetHeight(198f, 0f), interiorAverage + 3f, "الحافة الشرقية ترتفع");
            Assert.Greater(WorldHeightmap.GetHeight(0f, -198f), interiorAverage + 3f, "الحافة الجنوبية ترتفع");
        }
    }
}
