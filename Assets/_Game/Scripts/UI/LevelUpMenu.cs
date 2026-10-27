using SoulsGame.Player;
using SoulsGame.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // قائمة رفع المستوى عند الراحة: الأرواح، المستوى، ثلاث إحصائيات بأزرار +،
    // وتكلفة المستوى التالي. تقفل إدخال القتال وهي مفتوحة.
    public class LevelUpMenu : MonoBehaviour
    {
        public GameObject panel;
        public Text header;
        public Text vigorValue;
        public Text enduranceValue;
        public Text strengthValue;
        public Text mindValue;
        public Button vigorButton;
        public Button enduranceButton;
        public Button strengthButton;
        public Button mindButton;

        private PlayerAvatar player;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Initialize(PlayerAvatar playerAvatar)
        {
            player = playerAvatar;
            vigorButton.onClick.AddListener(() => Buy(StatId.Vigor));
            enduranceButton.onClick.AddListener(() => Buy(StatId.Endurance));
            strengthButton.onClick.AddListener(() => Buy(StatId.Strength));
            mindButton.onClick.AddListener(() => Buy(StatId.Mind));
        }

        public void Open()
        {
            panel.SetActive(true);
            player.InputLocked = true;
            Enemies.EnemyAvatar.AggroSuppressed = true; // لا اعتداء والقائمة مفتوحة
            Refresh();
        }

        public void Close()
        {
            panel.SetActive(false);
            player.InputLocked = false;
            Enemies.EnemyAvatar.AggroSuppressed = false;
        }

        private void Buy(StatId stat)
        {
            if (player.Progression.TryLevelUp(stat, player.Wallet))
            {
                player.ApplyProgression();
                Refresh();
            }
        }

        private void Refresh()
        {
            if (player == null || header == null)
                return;
            var p = player.Progression;
            header.text = $"Level {p.Level}   Souls {player.Wallet.Current}   Next: {p.NextLevelCost}";
            vigorValue.text = $"Vigor {p.GetStat(StatId.Vigor)}  (+12 HP)";
            enduranceValue.text = $"Endurance {p.GetStat(StatId.Endurance)}  (+6 ST)";
            strengthValue.text = $"Strength {p.GetStat(StatId.Strength)}  (+2 DMG)";
            mindValue.text = $"Mind {p.GetStat(StatId.Mind)}  (+5 FP)";

            bool affordable = player.Wallet.Current >= p.NextLevelCost;
            vigorButton.interactable = affordable;
            enduranceButton.interactable = affordable;
            strengthButton.interactable = affordable;
            mindButton.interactable = affordable;
        }
    }
}
