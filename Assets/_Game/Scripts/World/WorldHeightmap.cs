using UnityEngine;

namespace SoulsGame.World
{
    // دالة ارتفاع العالم: حتمية بالكامل (موجات جيبية فقط — نفس المدخل نفس
    // الناتج دائماً، في الاختبارات وفي اللعبة). الهضاب تُسطّح مواضع البناء،
    // والحواف ترتفع كتلال حدودية طبيعية.
    public static class WorldHeightmap
    {
        public const float HalfSize = 200f;
        public const float MaxHeight = 12f;
        public const float FortRadius = 35f;
        public const float FortPlateauHeight = 4f;
        public const float EdgeBandWidth = 15f;

        private const float PlateauBlend = 12f;
        private const float EdgeStart = HalfSize - EdgeBandWidth; // 185
        private const float EdgeRise = 14f;

        public static readonly Vector3 FortCenter = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 MidGraceCenter = new Vector3(0f, 0f, -100f);
        public static readonly Vector3 CaveMouthCenter = new Vector3(-140f, 0f, 20f);
        public const float SmallPlateauRadius = 8f;

        public static bool IsInsideWorld(float x, float z) =>
            Mathf.Abs(x) <= HalfSize && Mathf.Abs(z) <= HalfSize;

        public static float GetHeight(float x, float z)
        {
            // أساس متموج لطيف ضمن [0, MaxHeight]
            float n = 0.5f
                + 0.25f * Mathf.Sin(x * 0.021f + 1.7f) * Mathf.Sin(z * 0.017f + 0.4f)
                + 0.15f * Mathf.Sin(x * 0.043f + 3.1f) * Mathf.Cos(z * 0.049f + 2.2f)
                + 0.10f * Mathf.Sin((x + z) * 0.011f + 5.0f);
            float height = Mathf.Clamp01(n) * MaxHeight;

            // هضاب البناء (بدمج ناعم مع الجوار)
            height = FlattenAround(height, x, z, FortCenter, FortRadius, FortPlateauHeight);
            height = FlattenAround(height, x, z, MidGraceCenter, SmallPlateauRadius,
                BaseHeightAt(MidGraceCenter.x, MidGraceCenter.z));
            height = FlattenAround(height, x, z, CaveMouthCenter, SmallPlateauRadius,
                BaseHeightAt(CaveMouthCenter.x, CaveMouthCenter.z));

            // تلال الحواف: حدود طبيعية بدل جدران خفية
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge > EdgeStart)
            {
                float t = Mathf.Clamp01((edge - EdgeStart) / EdgeBandWidth);
                height += t * t * EdgeRise;
            }

            return height;
        }

        private static float BaseHeightAt(float x, float z)
        {
            float n = 0.5f
                + 0.25f * Mathf.Sin(x * 0.021f + 1.7f) * Mathf.Sin(z * 0.017f + 0.4f)
                + 0.15f * Mathf.Sin(x * 0.043f + 3.1f) * Mathf.Cos(z * 0.049f + 2.2f)
                + 0.10f * Mathf.Sin((x + z) * 0.011f + 5.0f);
            return Mathf.Clamp01(n) * MaxHeight;
        }

        private static float FlattenAround(float height, float x, float z,
            Vector3 center, float radius, float plateauHeight)
        {
            float dx = x - center.x, dz = z - center.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            if (distance >= radius + PlateauBlend)
                return height;
            if (distance <= radius)
                return plateauHeight;
            float t = (distance - radius) / PlateauBlend;
            return Mathf.Lerp(plateauHeight, height, t);
        }
    }
}
