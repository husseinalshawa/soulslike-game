using UnityEngine;

namespace SoulsGame.Combat
{
    // جسم الاستقبال: Collider (isTrigger) على جسم الشخصية يمرر الضرر لمالكه.
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        public Damageable owner;
    }
}
