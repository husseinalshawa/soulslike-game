using System;

namespace SoulsGame.Combat
{
    // مورد التركيز (FP): وقود التعاويذ ومهارات الأسلحة. تجدد طبيعي بطيء —
    // انحراف مقصود عن عرف السولز إلى أن تضاف القوارير (مرحلة لاحقة).
    public class FocusPool
    {
        private const float RegenPerSecond = 3f;
        private const float RegenDelay = 2f;

        private float delayRemaining;

        public float Max { get; private set; }
        public float Current { get; private set; }

        public FocusPool(float max)
        {
            Max = max;
            Current = max;
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f)
                return true; // مهارة مجانية/فارغة لا تصفّر مؤقت التجدد
            if (amount > Current)
                return false;
            Current -= amount;
            delayRemaining = RegenDelay;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (delayRemaining > 0f)
            {
                delayRemaining -= deltaTime;
                if (delayRemaining >= 0f)
                    return;
                deltaTime = -delayRemaining; // باقي الإطار بعد انتهاء المهلة يتجدد
                delayRemaining = 0f;
            }
            Current = Math.Min(Max, Current + RegenPerSecond * deltaTime);
        }

        public void ResetToFull()
        {
            Current = Max;
            delayRemaining = 0f;
        }

        public void SetMax(float newMax)
        {
            Max = newMax;
            Current = newMax;
            delayRemaining = 0f;
        }
    }
}
