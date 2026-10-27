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

        public PoiseMeter(float max, float regenPerSecond)
        {
            this.max = max;
            this.regenPerSecond = regenPerSecond;
        }

        // يعيد true عندما تكسر هذه الضربة التوازن.
        public bool TakePoiseDamage(float amount)
        {
            accumulated += amount;
            if (accumulated >= max)
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
