using UnityEngine;

namespace SoulsGame.Data
{
    // الدرع: مقايضة حماية ↔ خفة تفادي. نافذة i-frames تبقى مطلقة [0.1, 0.4]؛
    // الثقيل يطيل ذيل الالتزام بعد الحصانة لا الحصانة نفسها.
    [CreateAssetMenu(menuName = "SoulsGame/Armor Data", fileName = "Armor")]
    public class ArmorData : ScriptableObject
    {
        public string armorName = "Wanderer Cloth";
        [Range(0f, 0.9f)] public float damageReduction = 0f;
        public float dodgeDurationMultiplier = 1f;
        public float dodgeDistanceMultiplier = 1f;
        public Color tint = new Color(0.75f, 0.75f, 0.85f);
    }
}
