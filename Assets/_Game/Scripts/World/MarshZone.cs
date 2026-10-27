using UnityEngine;

namespace SoulsGame.World
{
    // منطقة الماء الضحل حول الكنيسة الغارقة: داخلها حركة اللاعب أبطأ —
    // التفادي واندفاعة المهارة لا يتأثران (نافذة الحصانة مقدسة).
    public class MarshZone
    {
        public const float SpeedMultiplier = 0.65f;

        private readonly Vector3 center;
        private readonly float radius;

        public MarshZone(Vector3 zoneCenter, float zoneRadius)
        {
            center = zoneCenter;
            radius = zoneRadius;
        }

        // مقارنة أفقية فقط (xz) — الارتفاع لا يهم في مستنقع.
        public bool Contains(Vector3 p)
        {
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
