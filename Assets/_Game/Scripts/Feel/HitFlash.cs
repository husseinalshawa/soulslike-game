using SoulsGame.Combat;
using UnityEngine;

namespace SoulsGame.Feel
{
    // وميض الإصابة: المضروب يومض أبيض 0.1ث ثم يعود للون نوعه —
    // ولون الجثة يغلب: لا ارتداد فوق ميت.
    public class HitFlash : MonoBehaviour
    {
        public const float Duration = 0.1f;

        public Renderer target;
        public Color baseColor;
        public Damageable owner;

        private float remaining;

        public bool Active => remaining > 0f;

        public void Begin()
        {
            if (owner != null && owner.Health.IsDead)
                return;
            remaining = Duration;
            if (target != null)
                target.material.color = Color.white;
        }

        public void Tick(float dt)
        {
            if (remaining <= 0f)
                return;
            remaining -= dt;
            if (remaining <= 0f && target != null &&
                (owner == null || !owner.Health.IsDead))
                target.material.color = baseColor;
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
