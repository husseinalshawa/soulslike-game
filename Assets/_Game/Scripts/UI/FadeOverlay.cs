using SoulsGame.Feel;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // غطاء أسود ملء الشاشة فوق كل شيء — تلاشي الموت/الإحياء وشاشة العنوان.
    public class FadeOverlay : MonoBehaviour
    {
        public Image image;

        public FadeLogic Fade { get; } = new FadeLogic();

        private void Update()
        {
            Fade.Tick(Time.deltaTime);
            Apply();
        }

        // تطبيق الشفافية الحالية (تستدعى أيضاً من الاختبارات بعد تكة يدوية)
        public void Apply()
        {
            if (image != null)
                image.color = new Color(0f, 0f, 0f, Fade.Alpha);
        }
    }
}
