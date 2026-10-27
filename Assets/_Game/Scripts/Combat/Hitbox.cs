using UnityEngine;

namespace SoulsGame.Combat
{
    // صندوق الضرر على السلاح. معطّل افتراضياً؛ يُفعَّل خلال نافذة الضربة فقط
    // (Activate/Deactivate تستدعيهما آلة الحالات حسب الـ AttackTimeline).
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        // مالك السلاح — لا يُصاب بسلاحه.
        public Damageable ignoreOwner;

        private readonly HitRegistry registry = new HitRegistry();
        private Collider hitCollider;
        private DamagePacket packet;

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }

        public void Activate(DamagePacket damagePacket)
        {
            packet = damagePacket;
            registry.BeginSwing();
            hitCollider.enabled = true;
        }

        public void Deactivate()
        {
            hitCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox == null || hurtbox.owner == null || hurtbox.owner == ignoreOwner)
                return;
            if (!registry.TryRegister(hurtbox.owner))
                return;

            hurtbox.owner.ApplyDamage(packet);
        }
    }
}
