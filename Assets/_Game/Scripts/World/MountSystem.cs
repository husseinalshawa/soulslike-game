using System.Collections.Generic;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.World
{
    public struct ForbiddenZone
    {
        public Vector3 Center;
        public float Radius;
    }

    // منطق الحصان الخالص: استدعاء فوري وركوب (مثل Torrent)، ممنوع في الأماكن
    // الضيقة وأثناء الالتزام القتالي، وضربة الراكب تُنزله إجبارياً.
    public class MountSystem
    {
        public bool IsMounted { get; private set; }

        public bool TrySummon(Vector3 playerPosition, IReadOnlyList<ForbiddenZone> zones,
            PlayerStateId playerState, bool menuOpen)
        {
            if (IsMounted || menuOpen)
                return false;

            if (playerState != PlayerStateId.Locomotion && playerState != PlayerStateId.Block)
                return false;

            if (zones != null)
                for (int i = 0; i < zones.Count; i++)
                {
                    float dx = playerPosition.x - zones[i].Center.x;
                    float dz = playerPosition.z - zones[i].Center.z;
                    if (dx * dx + dz * dz <= zones[i].Radius * zones[i].Radius)
                        return false;
                }

            IsMounted = true;
            return true;
        }

        public void Dismount() => IsMounted = false;

        // ضربة أثناء الركوب: ترجّل إجباري. يعيد true إن كان راكباً فعلاً.
        public bool NotifyRiderHit()
        {
            if (!IsMounted)
                return false;
            IsMounted = false;
            return true;
        }
    }
}
