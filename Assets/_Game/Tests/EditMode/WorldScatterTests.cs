using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class WorldScatterTests
    {
        private static readonly List<Vector3> Exclusions = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),      // الحصن
            new Vector3(0f, 0f, -100f),   // موقع راحة المنتصف
            new Vector3(-140f, 0f, 20f),  // فم الكهف
            new Vector3(60f, 0f, -60f)    // معسكر
        };
        private static readonly List<float> Radii = new List<float> { 40f, 6f, 6f, 6f };

        [Test]
        public void Generate_Deterministic()
        {
            var a = WorldScatter.Generate(Exclusions, Radii);
            var b = WorldScatter.Generate(Exclusions, Radii);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].Position.x, b[i].Position.x);
                Assert.AreEqual(a[i].Position.z, b[i].Position.z);
            }
        }

        [Test]
        public void Generate_RespectsExclusions()
        {
            var items = WorldScatter.Generate(Exclusions, Radii);
            foreach (var item in items)
                for (int i = 0; i < Exclusions.Count; i++)
                {
                    float dx = item.Position.x - Exclusions[i].x;
                    float dz = item.Position.z - Exclusions[i].z;
                    Assert.Greater(Mathf.Sqrt(dx * dx + dz * dz), Radii[i],
                        $"عنصر {item.Kind} داخل منطقة إقصاء {i}");
                }
        }

        [Test]
        public void Generate_MinSpacing()
        {
            var items = WorldScatter.Generate(Exclusions, Radii);
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    float dx = items[i].Position.x - items[j].Position.x;
                    float dz = items[i].Position.z - items[j].Position.z;
                    Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), 4f,
                        $"عنصران متلاصقان ({i},{j})");
                }
        }

        [Test]
        public void Generate_Counts()
        {
            var items = WorldScatter.Generate(Exclusions, Radii);
            int trees = 0, rocks = 0, ruins = 0;
            foreach (var item in items)
            {
                if (item.Kind == ScatterKind.DeadTree) trees++;
                else if (item.Kind == ScatterKind.Rock) rocks++;
                else ruins++;
            }
            Assert.GreaterOrEqual(trees, 108, "90% من 120 شجرة على الأقل");
            Assert.GreaterOrEqual(rocks, 72, "90% من 80 صخرة");
            Assert.GreaterOrEqual(ruins, 27, "90% من 30 طللاً");
            Assert.LessOrEqual(items.Count, 230);
        }
    }
}
