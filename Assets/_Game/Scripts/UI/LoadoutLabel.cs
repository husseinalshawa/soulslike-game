using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // سطر العتاد الحالي (سلاح ⟡ تعويذة ⟡ درع) فوق بارات الموارد.
    public class LoadoutLabel : MonoBehaviour
    {
        public Text label;

        private Func<string> provider;
        private string lastShown;

        public void Bind(Func<string> textProvider) => provider = textProvider;

        private void Update()
        {
            if (provider == null || label == null)
                return;
            string text = provider();
            if (text != lastShown)
            {
                lastShown = text;
                label.text = text;
            }
        }
    }
}
