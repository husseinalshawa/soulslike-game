using System;
using UnityEngine;

namespace SoulsGame.UI
{
    // شاشة "YOU DIED": تظهر عند موت اللاعب، وبعد مهلة قصيرة تستدعي الإعادة.
    public class DeathScreen : MonoBehaviour
    {
        private const float HoldSeconds = 2f;

        public GameObject panel;
        public Action OnRespawnRequested;

        private float remaining = -1f;

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
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
                if (panel != null)
                    panel.SetActive(false);
                OnRespawnRequested?.Invoke();
            }
        }
    }
}
