using System;

namespace SoulsGame.Combat
{
    // منطق ستامينا خالص (بلا MonoBehaviour): كل استهلاك يعيد ضبط مهلة التجدد،
    // والتجدد أبطأ أثناء رفع الدرع. القيم تأتي من CharacterStats عبر الغلاف.
    public class StaminaPool
    {
        private readonly float regenPerSecond;
        private readonly float regenDelay;
        private readonly float blockingRegenMultiplier;
        private float sprintCostPerSecond;
        private float delayRemaining;

        public float Max { get; private set; }
        public float Current { get; private set; }

        public StaminaPool(float max, float regenPerSecond, float regenDelay, float blockingRegenMultiplier)
        {
            Max = max;
            Current = max;
            this.regenPerSecond = regenPerSecond;
            this.regenDelay = regenDelay;
            this.blockingRegenMultiplier = blockingRegenMultiplier;
        }

        public void SetSprintCost(float costPerSecond) => sprintCostPerSecond = costPerSecond;

        public void ResetToFull()
        {
            Current = Max;
            delayRemaining = 0f;
        }

        // رفع المستوى: حد أقصى جديد مع ملء كامل.
        public void SetMax(float newMax)
        {
            Max = newMax;
            Current = newMax;
            delayRemaining = 0f;
        }

        public bool TryConsume(float amount)
        {
            if (amount > Current)
                return false;

            Current -= amount;
            delayRemaining = regenDelay;
            return true;
        }

        public void Tick(float deltaTime, bool isBlocking, bool isSprinting)
        {
            if (isSprinting && sprintCostPerSecond > 0f && Current > 0f)
            {
                Current = Math.Max(0f, Current - sprintCostPerSecond * deltaTime);
                delayRemaining = regenDelay;
                return;
            }

            if (delayRemaining > 0f)
            {
                delayRemaining -= deltaTime;
                return;
            }

            float rate = isBlocking ? regenPerSecond * blockingRegenMultiplier : regenPerSecond;
            Current = Math.Min(Max, Current + rate * deltaTime);
        }
    }
}
