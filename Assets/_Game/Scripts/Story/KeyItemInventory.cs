using System.Collections.Generic;

namespace SoulsGame.Story
{
    // قطع القصة الفريدة (الجرس، الرماد): خارج اقتصاد الأرواح كلياً —
    // لا تُسقط بالموت ولا تُباع.
    public class KeyItemInventory
    {
        private readonly HashSet<string> items = new HashSet<string>();

        public int Count => items.Count;

        public bool Has(string name) => items.Contains(name);

        public void Add(string name) => items.Add(name);

        public bool Remove(string name) => items.Remove(name);
    }
}
