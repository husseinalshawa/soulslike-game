namespace SoulsGame.Feel
{
    // تجميد الإصابة الوجيز: الضربة تلحق فيتباطأ الزمن للحظة — الوزن المحسوس.
    // يُدار بزمن غير مقيس (لا يجمد نفسه)، والأطول يسود ولا يقصَّر أبداً.
    public class HitStopLogic
    {
        public const float StopScale = 0.05f;

        private float remaining;

        public bool Active => remaining > 0f;
        public float CurrentScale => Active ? StopScale : 1f;

        public void Request(float seconds)
        {
            if (seconds > remaining)
                remaining = seconds;
        }

        public void Tick(float unscaledDt)
        {
            if (remaining <= 0f)
                return;
            remaining -= unscaledDt;
            if (remaining < 1e-4f) // بقايا الفاصلة العائمة على الحدود لا تمدد التجميد
                remaining = 0f;
        }
    }
}
