using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.World
{
    // «الرفيق»: حصان التنقل. يظهر عند الاستدعاء ويُركَب فوراً، يختفي عند
    // الترجّل. الراكب يُثبّت على السرج ويقود بإدخال الحركة.
    public class HorseAvatar : MonoBehaviour
    {
        public const float GallopSpeed = 16f;
        private const float Acceleration = 24f;
        private const float TurnSpeedDegrees = 240f;
        private const float JumpVelocity = 9f;
        private const float Gravity = -22f;

        public Transform cameraTransform;

        public Vector2 MoveInput { get; set; }
        public bool HasRider { get; private set; }

        private CharacterController controller;
        private PlayerAvatar rider;
        private Vector3 planarVelocity;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            gameObject.SetActive(false); // مخفي حتى أول استدعاء
        }

        public void SummonAndMount(PlayerAvatar player)
        {
            rider = player;
            HasRider = true;

            Vector3 p = player.transform.position;
            transform.position = WorldBuilder.OnGround(p.x, p.z, 0.9f);
            transform.rotation = player.transform.rotation;
            gameObject.SetActive(true);

            var riderController = player.GetComponent<CharacterController>();
            if (riderController != null) riderController.enabled = false;
            player.transform.SetParent(transform, false);
            player.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            player.transform.localRotation = Quaternion.identity;
        }

        public void DismountRider()
        {
            if (rider == null)
                return;

            var player = rider;
            rider = null;
            HasRider = false;

            player.transform.SetParent(null);
            Vector3 beside = transform.position + transform.right * 1.5f;
            player.transform.position = WorldBuilder.OnGround(beside.x, beside.z, 1.1f);
            var riderController = player.GetComponent<CharacterController>();
            if (riderController != null) riderController.enabled = true;

            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            gameObject.SetActive(false); // يختفي حتى الاستدعاء التالي
        }

        public void Jump()
        {
            if (HasRider && controller != null && controller.isGrounded)
                verticalVelocity = JumpVelocity;
        }

        private void Update()
        {
            if (!HasRider || controller == null)
                return;

            float dt = Time.deltaTime;

            Vector3 desired = Vector3.zero;
            if (MoveInput.sqrMagnitude > 0.01f)
            {
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
                forward.y = 0f; right.y = 0f;
                desired = (forward.normalized * MoveInput.y + right.normalized * MoveInput.x);
                if (desired.sqrMagnitude > 1f) desired = desired.normalized;
                desired *= GallopSpeed;
            }

            planarVelocity = new Vector3(
                Mathf.MoveTowards(planarVelocity.x, desired.x, Acceleration * dt),
                0f,
                Mathf.MoveTowards(planarVelocity.z, desired.z, Acceleration * dt));

            if (planarVelocity.sqrMagnitude > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeedDegrees * dt);
            }

            verticalVelocity = controller.isGrounded && verticalVelocity < 0f
                ? -1f : verticalVelocity + Gravity * dt;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
        }
    }
}
