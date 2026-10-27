using UnityEngine;

namespace SoulsGame.Feel
{
    // تلاشٍ خطي نحو شفافية هدف — غطاء الموت/الإحياء وتلاشي شاشة العنوان.
    public class FadeLogic
    {
        private float target;
        private float speed; // ألفا في الثانية

        public float Alpha { get; private set; }
        public bool Done => Mathf.Approximately(Alpha, target) || speed <= 0f;

        public void FadeTo(float targetAlpha, float duration)
        {
            target = Mathf.Clamp01(targetAlpha);
            speed = duration > 0f ? Mathf.Abs(target - Alpha) / duration : float.MaxValue;
        }

        public void Tick(float dt)
        {
            Alpha = Mathf.MoveTowards(Alpha, target, speed * dt);
        }
    }
}
