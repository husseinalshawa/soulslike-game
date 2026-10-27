using System;
using UnityEngine;

namespace SoulsGame.Data
{
    // مدد مراحل الهجوم بالثواني. تقود نوافذ فتح/إغلاق الـ Hitbox ونافذة الإلغاء؛
    // أحداث الأنيميشن تستطيع لاحقاً استدعاء نفس النوافذ عند توفر أنيميشنات حقيقية.
    [Serializable]
    public struct AttackTimeline
    {
        public float windup;
        public float active;
        public float recovery;
        public float cancelWindowStart;

        public AttackTimeline(float windup, float active, float recovery, float cancelWindowStart)
        {
            this.windup = windup;
            this.active = active;
            this.recovery = recovery;
            this.cancelWindowStart = cancelWindowStart;
        }

        public float Total => windup + active + recovery;
    }

    [CreateAssetMenu(menuName = "SoulsGame/Weapon Data", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        public float lightDamage = 25f;
        public float lightPoiseDamage = 15f;
        public float lightStaminaCost = 15f;
        public AttackTimeline light = new AttackTimeline(0.25f, 0.20f, 0.45f, 0.55f);

        public float heavyDamage = 50f;
        public float heavyPoiseDamage = 35f;
        public float heavyStaminaCost = 30f;
        public AttackTimeline heavy = new AttackTimeline(0.60f, 0.25f, 0.80f, 1.00f);

        public int comboLength = 3;
    }
}
