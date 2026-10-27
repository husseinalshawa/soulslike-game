using System.Collections.Generic;
using UnityEngine;

namespace SoulsGame.Player
{
    // إدارة القفل على الأهداف: اكتساب، تبديل يمين/يسار، فك تلقائي عند
    // موت الهدف أو تجاوز المسافة. يضع هدف الـ strafe على الـ PlayerMotor.
    public class LockOnSystem : MonoBehaviour
    {
        public const float MaxAcquireDistance = 15f;
        public const float MaxAcquireAngleDegrees = 60f;
        public const float ReleaseDistance = 20f;
        private const float SwitchThreshold = 0.6f;
        private const float SwitchCooldownSeconds = 0.35f;

        private static readonly List<ITargetable> registry = new List<ITargetable>();

        public static void Register(ITargetable target)
        {
            if (!registry.Contains(target))
                registry.Add(target);
        }

        public static void Unregister(ITargetable target) => registry.Remove(target);

        public PlayerMotor motor;
        public Transform cameraTransform;

        public ITargetable CurrentTarget { get; private set; }
        public bool IsLocked => CurrentTarget != null;

        private float switchCooldown;

        public void Toggle()
        {
            if (IsLocked)
            {
                CurrentTarget = null;
                return;
            }

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            CurrentTarget = LockOnSelector.Select(transform.position, forward, registry,
                MaxAcquireDistance, MaxAcquireAngleDegrees);
        }

        // يستقبل إدخال النظرة من الـ Avatar لتبديل الأهداف أثناء القفل.
        public void ReportLookInput(Vector2 look)
        {
            if (!IsLocked || switchCooldown > 0f || Mathf.Abs(look.x) < SwitchThreshold)
                return;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            var next = LockOnSelector.Switch(CurrentTarget, look.x > 0f ? 1 : -1,
                transform.position, forward, registry, MaxAcquireDistance, MaxAcquireAngleDegrees);
            if (!ReferenceEquals(next, CurrentTarget))
            {
                CurrentTarget = next;
                switchCooldown = SwitchCooldownSeconds;
            }
        }

        private void Update()
        {
            if (switchCooldown > 0f)
                switchCooldown -= Time.deltaTime;

            if (IsLocked)
            {
                bool tooFar = Vector3.Distance(transform.position, CurrentTarget.Position) > ReleaseDistance;
                if (!CurrentTarget.IsAlive || tooFar)
                    CurrentTarget = null;
            }

            if (motor != null)
                motor.StrafeTarget = TransformOf(CurrentTarget);
        }

        public static Transform TransformOf(ITargetable target) =>
            target is Component component ? component.transform : null;
    }
}
