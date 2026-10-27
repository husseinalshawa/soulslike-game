using System.Collections.Generic;
using UnityEngine;

namespace SoulsGame.Combat
{
    // منطق قذيفة خالص: تتقدم خطياً وتصيب أقرب هدف يتقاطع مع مقطع حركتها
    // هذا الإطار، وتنتهي عند أقصى المدى.
    public class ProjectileLogic
    {
        private readonly Vector3 direction;
        private readonly float speed;
        private readonly float maxRange;
        private float traveled;

        public Vector3 Position { get; private set; }
        public bool Expired { get; private set; }

        public ProjectileLogic(Vector3 origin, Vector3 direction, float speed, float maxRange)
        {
            Position = origin;
            this.direction = direction.normalized;
            this.speed = speed;
            this.maxRange = maxRange;
        }

        // يعيد الهدف المُصاب هذا الإطار (الأقرب على المسار) أو null.
        public object Step(float deltaTime,
            IReadOnlyList<(object target, Vector3 position, float radius)> targets)
        {
            if (Expired)
                return null;

            float stepLength = Mathf.Min(speed * deltaTime, maxRange - traveled);
            Vector3 start = Position;
            Position = start + direction * stepLength;
            traveled += stepLength;
            if (traveled >= maxRange)
                Expired = true;

            object best = null;
            float bestAlong = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                var (target, center, radius) = targets[i];
                // أقرب نقطة على مقطع الحركة لمركز الهدف
                Vector3 toCenter = center - start;
                float along = Mathf.Clamp(Vector3.Dot(toCenter, direction), 0f, stepLength);
                Vector3 closest = start + direction * along;
                if (Vector3.Distance(closest, center) <= radius && along < bestAlong)
                {
                    bestAlong = along;
                    best = target;
                }
            }

            if (best != null)
                Expired = true;
            return best;
        }
    }
}
