using UnityEngine;

namespace SoulsGame.Data
{
    // تعويذة: قذيفة (isBuff=false) أو تعزيز سلاح (isBuff=true).
    [CreateAssetMenu(menuName = "SoulsGame/Spell Data", fileName = "Spell")]
    public class SpellData : ScriptableObject
    {
        public string spellName = "Glint Bolt";
        public float fpCost = 12f;
        public bool isBuff;

        // قذيفة
        public float damage = 35f;
        public float poiseDamage = 10f;
        public float projectileSpeed = 20f;
        public float maxRange = 30f;

        // تعزيز
        public float buffBonusDamage = 10f;
        public float buffDuration = 30f;
    }
}
