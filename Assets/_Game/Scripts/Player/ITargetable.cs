using UnityEngine;

namespace SoulsGame.Player
{
    // ما يمكن للـ lock-on أن يستهدفه (ينفّذها EnemyAvatar).
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
    }
}
