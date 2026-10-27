using UnityEngine;

namespace SoulsGame.Progression
{
    // التمثيل البصري لموقع الراحة: أسطوانة ذهبية خافتة تسطع عند الاكتشاف.
    public class GraceSiteMarker : MonoBehaviour
    {
        public int siteId;

        private static readonly Color Undiscovered = new Color(0.35f, 0.3f, 0.15f);
        private static readonly Color Lit = new Color(0.95f, 0.8f, 0.35f);

        public void SetLit(bool lit)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = lit ? Lit : Undiscovered;
        }
    }
}
