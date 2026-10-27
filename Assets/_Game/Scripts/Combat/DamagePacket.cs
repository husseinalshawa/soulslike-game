using UnityEngine;

namespace SoulsGame.Combat
{
    // حزمة الضرر التي تنتقل من الـ Hitbox إلى الـ Hurtbox كوحدة واحدة.
    public readonly struct DamagePacket
    {
        public readonly float HealthDamage;
        public readonly float PoiseDamage;
        public readonly Vector3 SourcePosition;

        public DamagePacket(float healthDamage, float poiseDamage, Vector3 sourcePosition)
        {
            HealthDamage = healthDamage;
            PoiseDamage = poiseDamage;
            SourcePosition = sourcePosition;
        }
    }
}
