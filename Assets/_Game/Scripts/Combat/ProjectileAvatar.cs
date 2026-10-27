using System.Collections.Generic;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Combat
{
    // غلاف القذيفة المتوهجة: يقود ProjectileLogic ضد أهداف الـ lock-on
    // المسجلة، ويطبق الضرر عبر ApplyDamage ثم يفنى.
    public class ProjectileAvatar : MonoBehaviour
    {
        private const float TargetRadius = 0.8f;

        private ProjectileLogic logic;
        private DamagePacket packet;

        public void Launch(Vector3 origin, Vector3 direction, float speed, float maxRange, DamagePacket damagePacket)
        {
            logic = new ProjectileLogic(origin, direction, speed, maxRange);
            packet = damagePacket;
            transform.position = origin;
        }

        private void Update()
        {
            if (logic == null)
                return;

            var candidates = new List<(object, Vector3, float)>();
            var targets = LockOnSystem.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!targets[i].IsAlive)
                    continue;
                var component = targets[i] as Component;
                var damageable = component != null ? component.GetComponent<Damageable>() : null;
                if (damageable != null)
                    candidates.Add((damageable, targets[i].Position, TargetRadius));
            }

            object hit = logic.Step(Time.deltaTime, candidates);
            transform.position = logic.Position;

            if (hit is Damageable target)
                target.ApplyDamage(packet);

            if (logic.Expired)
                Destroy(gameObject);
        }
    }
}
