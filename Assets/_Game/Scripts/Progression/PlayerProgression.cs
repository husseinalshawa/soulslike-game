using SoulsGame.Data;

namespace SoulsGame.Progression
{
    // تقدم اللاعب: ثلاث إحصائيات، مستوى، ومنحنى تكلفة — منطق خالص.
    // القيم المشتقة تُبنى من CharacterStats الأساسية + النقاط المشتراة.
    public class PlayerProgression
    {
        private readonly ProgressionData data;
        private readonly CharacterStats baseStats;
        private int vigor;
        private int endurance;
        private int strength;

        public PlayerProgression(ProgressionData data, CharacterStats baseStats)
        {
            this.data = data;
            this.baseStats = baseStats;
            vigor = endurance = strength = data.baseStatValue;
        }

        public int Level => 1 + (vigor + endurance + strength) - 3 * data.baseStatValue;

        public int NextLevelCost => data.baseCost + data.costIncrement * (Level - 1);

        public int GetStat(StatId stat) => stat switch
        {
            StatId.Vigor => vigor,
            StatId.Endurance => endurance,
            _ => strength
        };

        public float MaxHealth => baseStats.maxHealth + (vigor - data.baseStatValue) * data.vigorHpPerPoint;
        public float MaxStamina => baseStats.maxStamina + (endurance - data.baseStatValue) * data.enduranceStaminaPerPoint;
        public float BonusDamage => (strength - data.baseStatValue) * data.strengthDamagePerPoint;

        public bool TryLevelUp(StatId stat, SoulsWallet wallet)
        {
            if (!wallet.TrySpend(NextLevelCost))
                return false;

            switch (stat)
            {
                case StatId.Vigor: vigor++; break;
                case StatId.Endurance: endurance++; break;
                default: strength++; break;
            }
            return true;
        }
    }
}
