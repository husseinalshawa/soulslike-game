using UnityEngine.InputSystem;

namespace SoulsGame.Player
{
    // خريطة الإدخال مبنية بالكود (بدل ملف .inputactions) حتى تكون قابلة
    // للمراجعة والترجمة بدون محرر. كيبورد/ماوس ويد تحكم معاً، بنمط Elden Ring.
    public class PlayerControls
    {
        public readonly InputAction Move;
        public readonly InputAction Look;
        public readonly InputAction Sprint;
        public readonly InputAction Dodge;
        public readonly InputAction LightAttack;
        public readonly InputAction HeavyAttack;
        public readonly InputAction Block;
        public readonly InputAction LockOn;

        private readonly InputActionMap map;

        public PlayerControls()
        {
            map = new InputActionMap("Gameplay");

            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = map.AddAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick");

            Sprint = map.AddAction("Sprint", InputActionType.Button);
            Sprint.AddBinding("<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/leftStickPress");

            Dodge = map.AddAction("Dodge", InputActionType.Button);
            Dodge.AddBinding("<Keyboard>/space");
            Dodge.AddBinding("<Gamepad>/buttonEast");

            LightAttack = map.AddAction("LightAttack", InputActionType.Button);
            LightAttack.AddBinding("<Mouse>/leftButton");
            LightAttack.AddBinding("<Gamepad>/rightShoulder");

            HeavyAttack = map.AddAction("HeavyAttack", InputActionType.Button);
            HeavyAttack.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Keyboard>/leftShift")
                .With("Binding", "<Mouse>/leftButton");
            HeavyAttack.AddBinding("<Gamepad>/rightTrigger");

            Block = map.AddAction("Block", InputActionType.Button);
            Block.AddBinding("<Mouse>/rightButton");
            Block.AddBinding("<Gamepad>/leftShoulder");

            LockOn = map.AddAction("LockOn", InputActionType.Button);
            LockOn.AddBinding("<Keyboard>/q");
            LockOn.AddBinding("<Gamepad>/rightStickPress");
        }

        public void Enable() => map.Enable();
        public void Disable() => map.Disable();
    }
}
