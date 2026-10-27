using System;

namespace SoulsGame.Combat
{
    public class HealthPool
    {
        public float Max { get; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public event Action Died;

        public HealthPool(float max)
        {
            Max = max;
            Current = max;
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
