using SoulsGame.Enemies;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.UI
{
    // شريط صحة فوق رأس العدو — يظهر فقط عندما يكون هدف الـ lock-on الحالي،
    // ويواجه الكاميرا دائماً (billboard).
    public class EnemyHealthBar : MonoBehaviour
    {
        public EnemyAvatar owner;
        public LockOnSystem lockOn;
        public RectTransform fill;
        public GameObject root;

        private void LateUpdate()
        {
            if (owner == null || root == null)
                return;

            bool visible = lockOn != null && ReferenceEquals(lockOn.CurrentTarget, owner) && owner.IsAlive;
            root.SetActive(visible);
            if (!visible)
                return;

            float pct = Mathf.Clamp01(owner.Damageable.Health.Current / owner.Damageable.Health.Max);
            if (fill != null)
                fill.localScale = new Vector3(pct, 1f, 1f);

            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        }
    }
}
