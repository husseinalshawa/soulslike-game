using System;

namespace SoulsGame.Combat
{
    public class HealthPool
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public event Action Died;

        public HealthPool(float max)
        {
            Max = max;
            Current = max;
        }

        // رفع المستوى: حد أقصى جديد مع ملء كامل (يحدث عند الراحة فقط).
        public void SetMax(float newMax)
        {
            Max = newMax;
            Current = newMax;
            IsDead = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
                return;

            Current = Math.Max(0f, Current - amount);
            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        public void ResetToFull()
        {
            Current = Max;
            IsDead = false;
        }
    }
}
