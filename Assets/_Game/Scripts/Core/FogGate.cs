using System;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Core
{
    // جدار الضباب: عبوره يبدأ معركة البوس ويقفل المدخل خلفك.
    public class FogGate : MonoBehaviour
    {
        public Action OnPlayerEntered;

        // حاجز صلب يمنع الخروج أثناء المعركة
        public GameObject barrier;

        private bool armed = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!armed || other.GetComponent<PlayerAvatar>() == null)
                return;
            armed = false;
            Seal(true);
            OnPlayerEntered?.Invoke();
        }

        public void Seal(bool sealed_)
        {
            if (barrier != null)
                barrier.SetActive(sealed_);
        }

        // بعد موت اللاعب: الحاجز يفتح والعبور يبدأ المعركة من جديد
        public void Reopen()
        {
            Seal(false);
            armed = true;
        }
    }
}
