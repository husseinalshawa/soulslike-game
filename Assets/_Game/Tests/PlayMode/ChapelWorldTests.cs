using NUnit.Framework;
using SoulsGame.Data;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class ChapelWorldTests
    {
        [Test]
        public void Marsh_FlatSunkenBasin_AtFloorHeight()
        {
            Vector3 c = WorldHeightmap.MarshCenter;
            Assert.AreEqual(WorldHeightmap.MarshFloorY, WorldHeightmap.GetHeight(c.x, c.z), 0.001f,
                "مركز المستنقع عند أرضية الحوض");
            Assert.AreEqual(WorldHeightmap.MarshFloorY, WorldHeightmap.GetHeight(c.x + 10f, c.z), 0.001f,
                "الحوض مسطح داخل نصف القطر (أرضية الكنيسة)");
            Assert.AreEqual(35f, WorldHeightmap.MarshRadius);
        }

        [Test]
        public void Marsh_WaterBetweenFloorAndRim()
        {
            Assert.Greater(WorldHeightmap.MarshWaterY, WorldHeightmap.MarshFloorY,
                "الماء فوق الأرضية");
            Assert.Less(WorldHeightmap.MarshWaterY,
                WorldHeightmap.MarshFloorY + WorldHeightmap.MarshDepth,
                "الماء تحت حافة الحوض");
        }

        [Test]
        public void Scatter_ExcludesMarshCircle()
        {
            var items = WorldScatter.Generate(new Vector3[0], new float[0]);
            float limit = WorldHeightmap.MarshRadius + 4f;
            foreach (var item in items)
            {
                float dx = item.Position.x - WorldHeightmap.MarshCenter.x;
                float dz = item.Position.z - WorldHeightmap.MarshCenter.z;
                Assert.Greater(dx * dx + dz * dz, limit * limit,
                    "لا نثر داخل المستنقع — الكنيسة تحتاج أرضاً خالية");
            }
        }

        [Test]
        public void BuildChapel_ReturnsAltarAndWater()
        {
            var theme = ScriptableObject.CreateInstance<WorldTheme>();
            var parent = new GameObject("ChapelParent");

            var result = WorldBuilder.BuildChapel(parent.transform, theme);

            Assert.AreEqual(120f, result.AltarPosition.x, 0.001f);
            Assert.AreEqual(90f, result.AltarPosition.z, 0.001f, "المذبح في الصدر الشمالي");
            Assert.Greater(result.AltarPosition.y, WorldHeightmap.MarshFloorY,
                "المذبح فوق أرضية الحوض");
            Assert.IsNotNull(result.WaterPlane, "سطح الماء بُني");
            Assert.AreEqual(WorldHeightmap.MarshWaterY, result.WaterPlane.transform.position.y, 0.001f);
        }
    }
}
