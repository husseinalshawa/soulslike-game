using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // عداد الأرواح أسفل يمين الشاشة.
    public class SoulsCounter : MonoBehaviour
    {
        public Text label;

        private Func<int> provider;
        private int lastShown = -1;

        public void Bind(Func<int> soulsProvider) => provider = soulsProvider;

        private void Update()
        {
            if (provider == null || label == null)
                return;
            int value = provider();
            if (value != lastShown)
            {
                lastShown = value;
                label.text = value.ToString();
            }
        }
    }
}
