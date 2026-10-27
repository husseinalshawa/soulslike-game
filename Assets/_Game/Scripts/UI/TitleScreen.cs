using SoulsGame.Feel;
using SoulsGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // شاشة العنوان: THE WITHERED MEADOW فوق غطاء أسود + سطر حساسية الكاميرا.
    // E تبدأ (مرة واحدة) فيتلاشى الغطاء 0.8ث؛ أسهم يمين/يسار أو dpad تضبط
    // الحساسية وتطبقها فوراً. المنطق في TitleLogic الصافي المختبَر.
    public class TitleScreen : MonoBehaviour
    {
        public const float FadeOutSeconds = 0.8f;

        public GameObject panel;
        public Image cover;
        public Text sensitivityLabel;
        public ThirdPersonCamera cameraRig;

        public TitleLogic Logic { get; } = new TitleLogic();
        public FadeLogic Fade { get; } = new FadeLogic();

        public bool IsOpen => !Logic.Started;

        public void AdjustSensitivity(int direction)
        {
            Logic.AdjustSensitivity(direction);
            if (cameraRig != null)
                cameraRig.SensitivityMultiplier = Logic.Sensitivity;
            if (sensitivityLabel != null)
                sensitivityLabel.text = $"Camera Sensitivity  ◀ {Logic.Sensitivity:0.00} ▶";
        }

        // يعيد true عند البدء الفعلي (المرة الأولى فقط).
        public bool BeginGame()
        {
            if (!Logic.RequestStart())
                return false;
            Fade.FadeTo(0f, FadeOutSeconds);
            return true;
        }

        private void Update()
        {
            // ضبط الحساسية بالأسهم/الـ dpad وهي مفتوحة فقط
            if (IsOpen)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.rightArrowKey.wasPressedThisFrame) AdjustSensitivity(+1);
                    if (keyboard.leftArrowKey.wasPressedThisFrame) AdjustSensitivity(-1);
                }
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    if (gamepad.dpad.right.wasPressedThisFrame) AdjustSensitivity(+1);
                    if (gamepad.dpad.left.wasPressedThisFrame) AdjustSensitivity(-1);
                }
                return;
            }

            // بعد البدء: تلاشي الغطاء ثم إخفاء اللوحة كلها
            Fade.Tick(Time.deltaTime);
            if (cover != null)
                cover.color = new Color(0f, 0f, 0f, Fade.Alpha);
            if (Fade.Done && panel != null && panel.activeSelf)
                panel.SetActive(false);
        }
    }
}
