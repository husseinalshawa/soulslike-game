using UnityEngine;

namespace SoulsGame.World
{
    // كنز أرواح الكهف: بلورة تُلمس مرة واحدة — لا تعود بالراحة
    // (علم في الجلسة؛ الحفظ للقرص خارج النطاق).
    public class SoulsCache : MonoBehaviour
    {
        public const float CollectRadius = 1.5f;

        public int amount = 800;

        private bool collected;

        // يعيد المبلغ عند أول لمسة ضمن النطاق، وإلا صفراً.
        public int TryCollect(Vector3 playerPosition)
        {
            if (collected || Vector3.Distance(playerPosition, transform.position) > CollectRadius)
                return 0;

            collected = true;
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            return amount;
        }
    }
}
