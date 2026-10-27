using UnityEngine;

namespace SoulsGame.Data
{
    // القيم الافتراضية هي موازنة اللاعب الأولية من المواصفة؛ أصول الأعداء
    // تعدّلها لكل نوع عدو (تُنشأ الأصول من قائمة Tools/SoulsGame في المحرر).
    [CreateAssetMenu(menuName = "SoulsGame/Character Stats", fileName = "CharacterStats")]
    public class CharacterStats : ScriptableObject
    {
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        public float maxPoise = 40f;
        public float poiseRegenPerSecond = 10f;

        public float staminaRegenPerSecond = 25f;
        public float staminaRegenDelay = 1.0f;
        public float blockingRegenMultiplier = 0.33f;

        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float sprintSpeed = 8f;
        public float sprintStaminaPerSecond = 10f;
        public float dodgeStaminaCost = 20f;

        // أرواح تُمنح لقاتل هذه الشخصية (صفر للاعب)
        public int soulsReward = 0;
    }
}
