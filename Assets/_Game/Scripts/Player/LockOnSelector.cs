using System.Collections.Generic;
using UnityEngine;

namespace SoulsGame.Player
{
    // منطق اختيار أهداف الـ lock-on — خالص وقابل للاختبار.
    public static class LockOnSelector
    {
        // الأقرب داخل مخروط الرؤية (المسافة والزاوية)، أو null.
        public static ITargetable Select(Vector3 origin, Vector3 forward,
            IReadOnlyList<ITargetable> candidates, float maxDistance, float maxAngleDegrees)
        {
            ITargetable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!IsValid(candidate, origin, forward, maxDistance, maxAngleDegrees))
                    continue;

                float distance = Vector3.Distance(origin, candidate.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        // الهدف التالي في الاتجاه المطلوب (+1 يمين، -1 يسار) بالنسبة للهدف الحالي؛
        // إن لم يوجد هدف صالح في ذلك الجانب يبقى الحالي.
        public static ITargetable Switch(ITargetable current, int direction, Vector3 origin,
            Vector3 forward, IReadOnlyList<ITargetable> candidates, float maxDistance, float maxAngleDegrees)
        {
            if (current == null)
                return Select(origin, forward, candidates, maxDistance, maxAngleDegrees);

            float currentAngle = SignedYawAngle(forward, current.Position - origin);
            ITargetable best = null;
            float bestDelta = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (ReferenceEquals(candidate, current))
                    continue;
                if (!IsValid(candidate, origin, forward, maxDistance, maxAngleDegrees))
                    continue;

                float delta = SignedYawAngle(forward, candidate.Position - origin) - currentAngle;
                if (direction > 0 ? delta <= 0f : delta >= 0f)
                    continue; // على الجانب الآخر

                float magnitude = Mathf.Abs(delta);
                if (magnitude < bestDelta)
                {
                    bestDelta = magnitude;
                    best = candidate;
                }
            }
            return best ?? current;
        }

        private static bool IsValid(ITargetable candidate, Vector3 origin, Vector3 forward,
            float maxDistance, float maxAngleDegrees)
        {
            if (candidate == null || !candidate.IsAlive)
                return false;

            Vector3 toTarget = candidate.Position - origin;
            if (toTarget.magnitude > maxDistance)
                return false;

            return Vector3.Angle(Planar(forward), Planar(toTarget)) <= maxAngleDegrees;
        }

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // زاوية الانحراف حول المحور العمودي: موجبة يميناً، سالبة يساراً.
        private static float SignedYawAngle(Vector3 forward, Vector3 toTarget)
        {
            Vector3 f = Planar(forward);
            Vector3 t = Planar(toTarget);
            float angle = Vector3.Angle(f, t);
            float sign = Vector3.Cross(f, t).y >= 0f ? 1f : -1f;
            return angle * sign;
        }
    }
}
