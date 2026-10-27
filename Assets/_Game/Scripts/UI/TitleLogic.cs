using UnityEngine;

namespace SoulsGame.UI
{
    // منطق شاشة العنوان: حساسية كاميرا مقصوصة بخطوات، وبدء يقع مرة واحدة.
    public class TitleLogic
    {
        public const float Min = 0.5f;
        public const float Max = 2.0f;
        public const float Step = 0.25f;

        public float Sensitivity { get; private set; } = 1f;
        public bool Started { get; private set; }

        public void AdjustSensitivity(int direction)
        {
            Sensitivity = Mathf.Clamp(Sensitivity + Step * Mathf.Sign(direction), Min, Max);
        }

        // يعيد true أول مرة فقط — E المزدوجة لا تبدأ مرتين.
        public bool RequestStart()
        {
            if (Started)
                return false;
            Started = true;
            return true;
        }
    }
}
