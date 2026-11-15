using UnityEngine;

namespace SoulsGame.Data
{
    // لوحة «المرج الذابل»: كل ألوان وأجواء العالم من ملف واحد —
    // غيّر المزاج كله من هنا بدون لمس الكود.
    // الافتراضي: ليل مقمر بارد وضباب كثيف (طلب الأجواء المظلمة). الخاتمة
    // (ThemeShift) ما زالت تعيد للعالم دفئه بعد المذبح.
    [CreateAssetMenu(menuName = "SoulsGame/World Theme", fileName = "WorldTheme")]
    public class WorldTheme : ScriptableObject
    {
        [Tooltip("ضوء القمر البارد")]
        public Color sunColor = new Color(0.55f, 0.62f, 0.85f);
        public float sunIntensity = 0.45f;
        public float sunPitchDegrees = 35f;
        public float sunYawDegrees = -40f;

        public Color fogColor = new Color(0.07f, 0.08f, 0.1f);
        public float fogDensity = 0.022f; // الرؤية تتلاشى بعد ~60م — الخطر يخرج من الظلام
        public Color ambient = new Color(0.12f, 0.13f, 0.17f);
        public Color skyColor = new Color(0.04f, 0.05f, 0.07f);

        public Color grassColor = new Color(0.17f, 0.19f, 0.12f);
        public Color highGroundColor = new Color(0.23f, 0.22f, 0.19f);
        public Color rockColor = new Color(0.21f, 0.21f, 0.22f);
        public Color deadTreeColor = new Color(0.13f, 0.11f, 0.1f);
        public Color ruinColor = new Color(0.24f, 0.23f, 0.22f);
        public Color caveColor = new Color(0.06f, 0.06f, 0.06f);

        // نسخ القيم إلى أصل موجود (أداة المحرر تحدّث أصل المشروع بهذه الافتراضيات)
        public void CopyFrom(WorldTheme other)
        {
            sunColor = other.sunColor; sunIntensity = other.sunIntensity;
            sunPitchDegrees = other.sunPitchDegrees; sunYawDegrees = other.sunYawDegrees;
            fogColor = other.fogColor; fogDensity = other.fogDensity;
            ambient = other.ambient; skyColor = other.skyColor;
            grassColor = other.grassColor; highGroundColor = other.highGroundColor;
            rockColor = other.rockColor; deadTreeColor = other.deadTreeColor;
            ruinColor = other.ruinColor; caveColor = other.caveColor;
        }
    }
}
