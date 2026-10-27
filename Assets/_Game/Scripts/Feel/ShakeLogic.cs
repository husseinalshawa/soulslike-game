using UnityEngine;

namespace SoulsGame.Feel
{
    // اهتزاز كاميرا حتمي: جيبيتان بترددين مختلفين × سعة تتضاءل خطياً —
    // نفس المدخل نفس الاهتزاز، بلا عشوائية (قابل للاختبار).
    public class ShakeLogic
    {
        private float amplitude;
        private float duration;
        private float elapsed;

        public bool Active => duration > 0f && elapsed < duration;

        public void Shake(float shakeAmplitude, float shakeDuration)
        {
            // الأقوى يسود إن تزامن اهتزازان
            if (shakeAmplitude * shakeDuration < amplitude * (duration - elapsed))
                return;
            amplitude = shakeAmplitude;
            duration = shakeDuration;
            elapsed = 0f;
        }

        public Vector3 Tick(float dt)
        {
            if (!Active)
                return Vector3.zero;

            elapsed += dt;
            if (elapsed >= duration)
            {
                duration = 0f;
                return Vector3.zero;
            }

            float falloff = 1f - elapsed / duration; // تضاؤل خطي
            float a = amplitude * falloff;
            return new Vector3(
                Mathf.Sin(elapsed * 47f) * a,
                Mathf.Sin(elapsed * 61f + 1.3f) * a,
                0f);
        }
    }
}
