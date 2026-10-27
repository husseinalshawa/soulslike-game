using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Player
{
    // حركة اللاعب: نسبية للكاميرا، مع اندفاع التفادي والتجمد أثناء الالتزام
    // بالهجوم/الترنح/الموت. PlayerAvatar يغذيه بالإدخال والحالة كل إطار.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public const float DodgeDistance = 3.5f;
        private const float RotationSpeedDegrees = 720f;
        private const float Gravity = -20f;

        public CharacterStats stats;
        public Transform cameraTransform;

        // يغذيها PlayerAvatar كل إطار
        public Vector2 MoveInput { get; set; }
        public bool SprintActive { get; set; }

        // مغادرة حالة المهارة (كسر توازن مثلاً) تصفّر بقية الاندفاعة —
        // وإلا تسربت لمهارة لاحقة بلا اندفاع (Quake/Flurry).
        public PlayerStateId State
        {
            get => state;
            set
            {
                if (value != PlayerStateId.Skill)
                    skillDashRemaining = 0f;
                state = value;
            }
        }
        private PlayerStateId state = PlayerStateId.Locomotion;

        public float SkillDashRemaining => skillDashRemaining;

        // هدف القفل: عند وجوده يتحول اللاعب لوضع strafe (يواجه الهدف)
        public Transform StrafeTarget { get; set; }

        // أثناء الركوب: الحصان هو من يتحرك — المحرك متجمد تماماً
        public bool Frozen { get; set; }

        // ملف تفادي الدرع: المسافة والمدة تُضربان بمضاعفي الدرع المجهز
        public float DodgeDistanceMultiplier { get; set; } = 1f;
        public float DodgeDurationMultiplier { get; set; } = 1f;

        private CharacterController controller;
        private Vector3 dodgeDirection;
        private float verticalVelocity;
        private float skillDashRemaining;
        private float skillDashSpeed;

        public Vector3 LastPlanarDirection { get; private set; } = Vector3.forward;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        // اندفاع مهارة السلاح (اندفاعة قاطعة/شحنة خارقة): للأمام خلال ربع ثانية.
        public void BeginSkillDash(float distance)
        {
            const float dashDuration = 0.25f;
            skillDashRemaining = dashDuration;
            skillDashSpeed = distance / dashDuration;
        }

        public void BeginDodge()
        {
            // اتجاه التفادي: اتجاه الإدخال الحالي، وإلا للخلف (مثل السولز)
            Vector3 input = WorldMoveDirection();
            dodgeDirection = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward * -1f;
        }

        private void Update()
        {
            if (Frozen)
                return;

            float dt = Time.deltaTime;
            Vector3 planar = Vector3.zero;

            switch (State)
            {
                case PlayerStateId.Locomotion:
                case PlayerStateId.Block:
                {
                    Vector3 moveDir = WorldMoveDirection();
                    float speed = State == PlayerStateId.Block ? stats.walkSpeed
                        : SprintActive ? stats.sprintSpeed
                        : stats.runSpeed;
                    planar = moveDir * speed;

                    if (StrafeTarget != null)
                        FaceTowards(StrafeTarget.position - transform.position, dt);
                    else if (moveDir.sqrMagnitude > 0.01f)
                        FaceTowards(moveDir, dt);

                    if (moveDir.sqrMagnitude > 0.01f)
                        LastPlanarDirection = moveDir.normalized;
                    break;
                }
                case PlayerStateId.Dodge:
                    planar = dodgeDirection * (DodgeDistance * DodgeDistanceMultiplier
                        / (PlayerBrain.DodgeDuration * DodgeDurationMultiplier));
                    break;
                // Attack/Cast/Stagger/Death: لا حركة أفقية — الالتزام بالحركة
                case PlayerStateId.Skill:
                    if (skillDashRemaining > 0f)
                    {
                        skillDashRemaining -= dt;
                        planar = transform.forward * skillDashSpeed;
                    }
                    break;
            }

            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + Gravity * dt;
            Vector3 velocity = planar + Vector3.up * verticalVelocity;
            controller.Move(velocity * dt);
        }

        private Vector3 WorldMoveDirection()
        {
            if (MoveInput.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            Vector3 dir = forward.normalized * MoveInput.y + right.normalized * MoveInput.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private void FaceTowards(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, RotationSpeedDegrees * dt);
        }
    }
}
