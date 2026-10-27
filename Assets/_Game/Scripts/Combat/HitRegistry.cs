using System.Collections.Generic;

namespace SoulsGame.Combat
{
    // يضمن أن الضربة الواحدة (swing) تصيب كل هدف مرة واحدة فقط.
    public class HitRegistry
    {
        private readonly HashSet<object> hitThisSwing = new HashSet<object>();

        public void BeginSwing() => hitThisSwing.Clear();

        public bool TryRegister(object target) => hitThisSwing.Add(target);
    }
}
