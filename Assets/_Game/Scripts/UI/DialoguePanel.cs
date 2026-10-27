using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // شريط الحوار أسفل الشاشة: اسم الشخصية + السطر الحالي + تلميح E —
    // يحدّثه GameLoop عبر حدث DialogueRunner.SessionChanged.
    public class DialoguePanel : MonoBehaviour
    {
        public GameObject panel;
        public Text speakerLabel;
        public Text lineLabel;

        public bool Visible => panel != null && panel.activeSelf;

        public void Show(string speaker, string line)
        {
            if (panel == null) return;
            panel.SetActive(true);
            if (speakerLabel != null) speakerLabel.text = speaker;
            if (lineLabel != null) lineLabel.text = line;
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
