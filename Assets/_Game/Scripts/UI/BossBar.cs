using SoulsGame.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // بار البوس العريض أسفل وسط الشاشة — يظهر مع بدء المعركة فقط.
    public class BossBar : MonoBehaviour
    {
        public BossAvatar boss;
        public GameObject root;
        public RectTransform fill;
        public Text nameLabel;

        private void LateUpdate()
        {
            if (boss == null || root == null)
                return;

            bool visible = boss.FightActive;
            root.SetActive(visible);
            if (!visible)
                return;

            float pct = Mathf.Clamp01(boss.Damageable.Health.Current / boss.Damageable.Health.Max);
            if (fill != null)
                fill.localScale = new Vector3(pct, 1f, 1f);
        }
    }
}
