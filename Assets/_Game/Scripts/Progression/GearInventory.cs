using System.Collections.Generic;

namespace SoulsGame.Progression
{
    // حوزة عتاد عامة (أسلحة/دروع/تعاويذ): بلا تكرار، وأول إضافة تُجهَّز.
    public class GearInventory<T> where T : class
    {
        private readonly List<T> items = new List<T>();
        private int currentIndex;

        public int Count => items.Count;
        public T Current => items.Count > 0 ? items[currentIndex] : null;

        public bool Add(T item)
        {
            if (item == null || items.Contains(item))
                return false;
            items.Add(item);
            if (items.Count == 1)
                currentIndex = 0;
            return true;
        }

        public T Cycle()
        {
            if (items.Count == 0)
                return null;
            currentIndex = (currentIndex + 1) % items.Count;
            return items[currentIndex];
        }

        // تجهيز قطعة مملوكة مباشرة (الالتقاط يجهز المُلتقط فوراً).
        public bool Select(T item)
        {
            int index = items.IndexOf(item);
            if (index < 0)
                return false;
            currentIndex = index;
            return true;
        }
    }
}
