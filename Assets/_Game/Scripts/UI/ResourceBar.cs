using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // شريط مورد (صحة/ستامينا): تعبئة بالنسبة بين مزوّدي قيمة، مع وميض
    // عند رفض فعل لنقص الستامينا (مؤشر المواصفة البصري).
    public class ResourceBar : MonoBehaviour
    {
        private const float FlashDuration = 0.25f;

        public RectTransform fill;
        public Image fillImage;
        public Color baseColor = Color.white;

        private Func<float> current;
        private Func<float> max;
        private float flashRemaining;

        public void Bind(Func<float> currentProvider, Func<float> maxProvider)
        {
            current = currentProvider;
            max = maxProvider;
        }

        public void Flash() => flashRemaining = FlashDuration;

        private void Update()
        {
            if (current == null || max == null || fill == null)
                return;

            float maxValue = max();
            float pct = maxValue > 0f ? Mathf.Clamp01(current() / maxValue) : 0f;
            fill.localScale = new Vector3(pct, 1f, 1f);

            if (fillImage != null)
            {
                if (flashRemaining > 0f)
                {
                    flashRemaining -= Time.deltaTime;
                    fillImage.color = Color.white;
                }
                else
                {
                    fillImage.color = baseColor;
                }
            }
        }
    }
}
