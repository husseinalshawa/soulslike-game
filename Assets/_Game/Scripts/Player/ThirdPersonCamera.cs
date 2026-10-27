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
        private const float MouseSensitivity = 0.12f;     // درجة لكل بكسل — بلا dt
        private const float StickSensitivityPerSecond = 160f; // درجة/ثانية عند انحراف كامل
        private const float MinPitch = -30f;
        private const float MaxPitch = 60f;
        private const float LockOnPitch = 12f;
        private const float FollowLerp = 12f;

        public Transform followTarget;
        public LockOnSystem lockOn;

        // يغذيهما PlayerAvatar كل إطار من إدخال النظرة.
        public Vector2 MouseLook { get; set; }
        public Vector2 StickLook { get; set; }

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
                // دلتا الماوس لكل-إطار أصلاً — ضربها في dt يجعل الحساسية تتبع الـ FPS
                yaw += MouseLook.x * MouseSensitivity + StickLook.x * StickSensitivityPerSecond * Time.deltaTime;
                float pitchDelta = MouseLook.y * MouseSensitivity + StickLook.y * StickSensitivityPerSecond * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - pitchDelta, MinPitch, MaxPitch);
            }

            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 aimPoint = followTarget.position + Vector3.up * AimHeight;
            transform.position = aimPoint - orbit * Vector3.forward * Distance + Vector3.up * (Height - AimHeight);
            transform.rotation = Quaternion.LookRotation(aimPoint - transform.position, Vector3.up);
        }
    }
}
