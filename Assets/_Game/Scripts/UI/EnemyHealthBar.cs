using SoulsGame.Enemies;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.UI
{
    // شريط صحة واحد يتبع هدف الـ lock-on الحالي أياً كان (جندي/مرتّل) —
    // البوس مستثنى (له شريطه الكبير)، ويواجه الكاميرا دائماً (billboard).
    public class EnemyHealthBar : MonoBehaviour
    {
        public LockOnSystem lockOn;
        public RectTransform fill;
        public GameObject root;

        // يطبّق الظهور والامتلاء لهدف معيّن ويعيد هل الشريط ظاهر —
        // مفصولة عن LateUpdate لتُختبر في الشيم.
        public bool Refresh(ITargetable target)
        {
            var enemy = target as EnemyAvatar; // البوس ليس EnemyAvatar — يُستبعد
            bool visible = enemy != null && enemy.IsAlive;
            if (root != null)
                root.SetActive(visible);
            if (!visible)
                return false;

            transform.position = enemy.transform.position + Vector3.up * 2.2f;
            float pct = Mathf.Clamp01(enemy.Damageable.Health.Current / enemy.Damageable.Health.Max);
            if (fill != null)
                fill.localScale = new Vector3(pct, 1f, 1f);
            return true;
        }

        private void LateUpdate()
        {
            if (!Refresh(lockOn != null ? lockOn.CurrentTarget : null))
                return;
            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(
                    transform.position - Camera.main.transform.position, Vector3.up);
        }
    }
}
