namespace SoulsGame.Progression
{
    // محفظة الأرواح: عملة اللعبة الوحيدة — تُكسب بالقتل، تُصرف في رفع المستوى،
    // وتُؤخذ كلها عند الموت (تسقط كبقعة دم).
    public class SoulsWallet
    {
        public int Current { get; private set; }

        public void Add(int amount)
        {
            if (amount > 0)
                Current += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount > Current)
                return false;
            Current -= amount;
            return true;
        }

        public int TakeAll()
        {
            int taken = Current;
            Current = 0;
            return taken;
        }
    }
}
