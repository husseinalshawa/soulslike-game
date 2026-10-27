using UnityEngine;

namespace SoulsGame.Data
{
    // لوحة «المرج الذابل»: كل ألوان وأجواء العالم من ملف واحد —
    // غيّر المزاج كله من هنا بدون لمس الكود.
    [CreateAssetMenu(menuName = "SoulsGame/World Theme", fileName = "WorldTheme")]
    public class WorldTheme : ScriptableObject
    {
        [Tooltip("ضوء الغروب الذهبي الشاحب")]
        public Color sunColor = new Color(1.0f, 0.85f, 0.6f);
        public float sunIntensity = 0.9f;
        public float sunPitchDegrees = 18f; // شمس منخفضة
        public float sunYawDegrees = -60f;

        public Color fogColor = new Color(0.75f, 0.68f, 0.55f);
        public float fogDensity = 0.008f;
        public Color ambient = new Color(0.45f, 0.42f, 0.38f);
        public Color skyColor = new Color(0.82f, 0.72f, 0.58f);

        public Color grassColor = new Color(0.45f, 0.42f, 0.25f);
        public Color highGroundColor = new Color(0.55f, 0.5f, 0.42f);
        public Color rockColor = new Color(0.4f, 0.38f, 0.34f);
        public Color deadTreeColor = new Color(0.3f, 0.25f, 0.2f);
        public Color ruinColor = new Color(0.42f, 0.4f, 0.36f);
        public Color caveColor = new Color(0.16f, 0.15f, 0.14f);
    }
}
