using UnityEngine;

namespace SoulsGame.Progression
{
    // بقعة الدم: أرواح اللاعب الساقطة عند موته. واحدة كحد أقصى —
    // موت جديد قبل الاستعادة يستبدلها ويضيّع القديمة نهائياً (عرف السولز).
    public class BloodstainLedger
    {
        public bool HasStain { get; private set; }
        public Vector3 Position { get; private set; }
        public int Amount { get; private set; }

        public void Drop(Vector3 position, int amount)
        {
            if (amount <= 0)
            {
                HasStain = false;
                Amount = 0;
                return;
            }
            HasStain = true;
            Position = position;
            Amount = amount;
        }

        // ضمن النطاق: يعيد المبلغ ويمسح البقعة؛ وإلا صفر.
        public int TryRecover(Vector3 playerPosition, float radius)
        {
            if (!HasStain || Vector3.Distance(playerPosition, Position) > radius)
                return 0;

            int recovered = Amount;
            HasStain = false;
            Amount = 0;
            return recovered;
        }
    }
}
