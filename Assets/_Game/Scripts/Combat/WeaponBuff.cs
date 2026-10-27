using System;

namespace SoulsGame.Combat
{
    // تعزيز السلاح: ضرر إضافي مؤقت. إعادة التفعيل تجدد المدة، والموت يصفّره.
    public class WeaponBuff
    {
        private float bonus;
        private float remaining;

        public float CurrentBonus => remaining > 0f ? bonus : 0f;
        public bool IsActive => remaining > 0f;

        public void Activate(float bonusDamage, float duration)
        {
            bonus = bonusDamage;
            remaining = duration;
        }

        public void Tick(float deltaTime) => remaining = Math.Max(0f, remaining - deltaTime);

        public void Clear() => remaining = 0f;
    }
}
