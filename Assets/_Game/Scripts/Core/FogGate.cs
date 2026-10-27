using System;
using SoulsGame.Player;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Core
{
    // جدار الضباب: عبوره يبدأ معركة البوس ويقفل المدخل خلفك —
    // ماشياً أو راكباً (الحصان يُصرف إجبارياً: لا حصان في معركة البوس).
    public class FogGate : MonoBehaviour
    {
        public Action OnPlayerEntered;

        // حاجز صلب يمنع الخروج أثناء المعركة
        public GameObject barrier;

        private bool armed = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!armed)
                return;

            // راكباً: متحكم اللاعب معطّل — الحصان هو من يلمس المشغّل
            var player = other.GetComponentInParent<PlayerAvatar>();
            if (player == null)
            {
                var horse = other.GetComponentInParent<HorseAvatar>();
                if (horse == null || !horse.HasRider)
                    return;
                player = horse.GetComponentInChildren<PlayerAvatar>();
            }
            if (player == null)
                return;

            if (player.IsRiding)
                player.ForceDismount();

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
