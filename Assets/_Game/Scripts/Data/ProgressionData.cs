using UnityEngine;

namespace SoulsGame.Data
{
    // موازنة نظام التقدم: قيم البداية وأثر كل نقطة ومنحنى التكلفة.
    [CreateAssetMenu(menuName = "SoulsGame/Progression Data", fileName = "Progression")]
    public class ProgressionData : ScriptableObject
    {
        public int baseStatValue = 10;
        public float vigorHpPerPoint = 12f;
        public float enduranceStaminaPerPoint = 6f;
        public float strengthDamagePerPoint = 2f;
        public int baseCost = 100;
        public int costIncrement = 25;
    }
}
