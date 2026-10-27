using System;

namespace SoulsGame.Combat
{
    // يتتبع ضرر التوازن المتراكم؛ عند بلوغ العتبة ينكسر التوازن (Stagger)
    // ويُعاد الضبط. التراكم يتعافى نحو الصفر مع الوقت.
    public class PoiseMeter
    {
        private readonly float max;
        private readonly float regenPerSecond;
        private float accumulated;
        private float thresholdMultiplier = 1f;

        public PoiseMeter(float max, float regenPerSecond)
        {
            this.max = max;
            this.regenPerSecond = regenPerSecond;
        }

        // مضاد سبام الترنح: ×1.5 مؤقتاً بعد كل كسر (يستخدمه البوس).
        public void SetThresholdMultiplier(float multiplier) => thresholdMultiplier = multiplier;

        // يعيد true عندما تكسر هذه الضربة التوازن.
        public bool TakePoiseDamage(float amount)
        {
            accumulated += amount;
            if (accumulated >= max * thresholdMultiplier)
            {
                accumulated = 0f;
                return true;
            }
            return false;
        }

        public void Tick(float deltaTime)
        {
            accumulated = Math.Max(0f, accumulated - regenPerSecond * deltaTime);
        }

        public void Reset() => accumulated = 0f;
    }
}
