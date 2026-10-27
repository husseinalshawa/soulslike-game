using SoulsGame.Player;
using SoulsGame.UI;
using UnityEngine;

namespace SoulsGame.Story
{
    // قطعة قصة ملقاة في العالم (الجرس الصدئ): لمسها يضيفها لقطع اللاعب
    // ويرفع علمها — مرة واحدة للأبد، والوصف المبهم يظهر في الإشعار.
    public class KeyItemPickup : MonoBehaviour
    {
        public const float CollectRadius = 1.5f;

        public string itemName;
        public string description;
        public StoryFlag flagOnCollect;
        public PlayerAvatar player;
        public StoryState story;
        public Toast toast;

        private bool collected;

        // يعيد 1 عند أول التقاط ضمن النطاق، وإلا 0.
        public int TryCollect(Vector3 playerPosition)
        {
            if (collected || player == null ||
                player.Damageable.Health.IsDead || // لا التقاط فوق شاشة الموت
                Vector3.Distance(playerPosition, transform.position) > CollectRadius)
                return 0;

            collected = true;
            player.KeyItems.Add(itemName);
            story?.Set(flagOnCollect);
            toast?.Show($"{itemName} — {description}");
            var renderer = GetComponentInChildren<Renderer>() ?? GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            return 1;
        }

        private void Update()
        {
            if (!collected && player != null)
                TryCollect(player.transform.position);
        }
    }
}
