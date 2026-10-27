using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // إشعار قصير وسط الشاشة (اكتشاف موقع راحة، رسائل عامة) يختفي بعد ثانيتين.
    public class Toast : MonoBehaviour
    {
        private const float HoldSeconds = 2f;

        public Text label;

        private float remaining = -1f;

        public void Show(string message)
        {
            if (label == null)
                return;
            label.text = message;
            label.gameObject.SetActive(true);
            remaining = HoldSeconds;
        }

        private void Update()
        {
            if (remaining < 0f)
                return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                remaining = -1f;
                if (label != null)
                    label.gameObject.SetActive(false);
            }
        }
    }
}
