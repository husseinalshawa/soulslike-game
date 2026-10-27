using System.Collections.Generic;
using UnityEngine;

namespace SoulsGame.World
{
    public enum ScatterKind { DeadTree, Rock, Ruin }

    public struct ScatterItem
    {
        public ScatterKind Kind;
        public Vector3 Position;
        public float Scale;
        public float Rotation;
    }

    // نثر العالم: أشجار ميتة وصخور وأطلال بمواضع حتمية من بذرة ثابتة —
    // نفس العالم في كل تشغيل، بعيداً عن المباني والمواقع وبتباعد مضمون.
    public static class WorldScatter
    {
        private const int Seed = 12345;
        private const float MinSpacing = 4f;
        private const float PlacementRange = 190f;
        private const int TreeTarget = 120, RockTarget = 80, RuinTarget = 30;
        private const int MaxAttemptsPerItem = 40;

        public static List<ScatterItem> Generate(
            IReadOnlyList<Vector3> exclusionCenters, IReadOnlyList<float> exclusionRadii)
        {
            var items = new List<ScatterItem>();
            uint state = Seed;

            Place(items, ScatterKind.DeadTree, TreeTarget, exclusionCenters, exclusionRadii, ref state);
            Place(items, ScatterKind.Rock, RockTarget, exclusionCenters, exclusionRadii, ref state);
            Place(items, ScatterKind.Ruin, RuinTarget, exclusionCenters, exclusionRadii, ref state);
            return items;
        }

        private static void Place(List<ScatterItem> items, ScatterKind kind, int target,
            IReadOnlyList<Vector3> centers, IReadOnlyList<float> radii, ref uint state)
        {
            int placed = 0, attempts = 0;
            int maxAttempts = target * MaxAttemptsPerItem;
            while (placed < target && attempts < maxAttempts)
            {
                attempts++;
                float x = (Next01(ref state) * 2f - 1f) * PlacementRange;
                float z = (Next01(ref state) * 2f - 1f) * PlacementRange;

                if (!IsClear(x, z, items, centers, radii))
                    continue;

                items.Add(new ScatterItem
                {
                    Kind = kind,
                    Position = new Vector3(x, WorldHeightmap.GetHeight(x, z), z),
                    Scale = 0.7f + Next01(ref state) * 0.8f,
                    Rotation = Next01(ref state) * 360f
                });
                placed++;
            }
        }

        private static bool IsClear(float x, float z, List<ScatterItem> items,
            IReadOnlyList<Vector3> centers, IReadOnlyList<float> radii)
        {
            for (int i = 0; i < centers.Count; i++)
            {
                float dx = x - centers[i].x, dz = z - centers[i].z;
                if (dx * dx + dz * dz <= radii[i] * radii[i])
                    return false;
            }
            for (int i = 0; i < items.Count; i++)
            {
                float dx = x - items[i].Position.x, dz = z - items[i].Position.z;
                if (dx * dx + dz * dz < MinSpacing * MinSpacing)
                    return false;
            }
            // تجنب الميول الحادة: فرق ارتفاع الجوار القريب محدود
            float h = WorldHeightmap.GetHeight(x, z);
            float hx = WorldHeightmap.GetHeight(x + 2f, z);
            float hz = WorldHeightmap.GetHeight(x, z + 2f);
            return Mathf.Abs(hx - h) < 1.6f && Mathf.Abs(hz - h) < 1.6f;
        }

        // مولد حتمي بسيط (xorshift) — لا System.Random ولا UnityEngine.Random
        private static float Next01(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / 16777216f;
        }
    }
}
