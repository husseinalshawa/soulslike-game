using UnityEngine;

namespace SoulsGame.Player
{
    // كاميرا شخص ثالث مكتوبة يدوياً (بديل Cinemachine): مدار حر بالماوس/العصا،
    // وعند القفل تؤطر اللاعب والهدف معاً.
    public class ThirdPersonCamera : MonoBehaviour
    {
        private const float Distance = 5f;
        private const float Height = 2f;
        private const float AimHeight = 1.5f;
        private const float Sensitivity = 0.15f;
        private const float MinPitch = -30f;
        private const float MaxPitch = 60f;
        private const float LockOnPitch = 12f;
        private const float FollowLerp = 12f;

        public Transform followTarget;
        public LockOnSystem lockOn;

        // يغذيه PlayerAvatar كل إطار من إدخال النظرة.
        public Vector2 LookInput { get; set; }

        private float yaw;
        private float pitch = 15f;

        private void LateUpdate()
        {
            if (followTarget == null)
                return;

            var lockTarget = lockOn != null ? lockOn.CurrentTarget : null;
            if (lockTarget != null)
            {
                // أثناء القفل: الكاميرا خلف اللاعب على محور اللاعب→الهدف
                Vector3 toTarget = lockTarget.Position - followTarget.position;
                yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                pitch = Mathf.Lerp(pitch, LockOnPitch, Time.deltaTime * FollowLerp);
            }
            else
            {
                yaw += LookInput.x * Sensitivity * 60f * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - LookInput.y * Sensitivity * 60f * Time.deltaTime, MinPitch, MaxPitch);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 aimPoint = followTarget.position + Vector3.up * AimHeight;
            transform.position = aimPoint - orbit * Vector3.forward * Distance + Vector3.up * (Height - AimHeight);
            transform.rotation = Quaternion.LookRotation(aimPoint - transform.position, Vector3.up);
        }
    }
}
