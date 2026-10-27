// Compile-only stubs (never executed locally): input system, physics,
// rendering types used by the MonoBehaviour wiring layer.
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0);
        public static Color clear => new Color(0, 0, 0, 0);
    }

    public class Material : Object
    {
        public Color color;
    }

    public class Renderer : Component
    {
        public Material material = new Material();
        public bool enabled;
    }

    public class MeshRenderer : Renderer { }

    public enum LightType { Directional, Point, Spot, Area }

    public class Light : Behaviour
    {
        public LightType type;
        public float intensity;
        public Color color;
    }

    public static class RenderSettings
    {
        public static bool fog;
        public static Color fogColor;
        public static float fogDensity;
        public static Color ambientLight;
    }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public float fieldOfView;
    }

    public class Rigidbody : Component
    {
        public bool isKinematic;
        public bool useGravity;
    }

    public class CharacterController : Collider
    {
        public float height;
        public float radius;
        public Vector3 center;
        public bool isGrounded => true;
        public void Move(Vector3 motion) { }
    }
}

namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }

    public class InputActionMap
    {
        public InputActionMap(string name) { }
        public InputAction AddAction(string name, InputActionType type = InputActionType.Value) => new InputAction();
        public void Enable() { }
        public void Disable() { }
    }

    public class InputAction
    {
        public struct CallbackContext
        {
            public T ReadValue<T>() => default;
            public bool performed => false;
        }

        public event Action<CallbackContext> started { add { } remove { } }
        public event Action<CallbackContext> performed { add { } remove { } }
        public event Action<CallbackContext> canceled { add { } remove { } }

        public T ReadValue<T>() => default;
        public bool IsPressed() => false;
        public bool WasPressedThisFrame() => false;

        public BindingSyntax AddBinding(string path) => default;
        public CompositeSyntax AddCompositeBinding(string composite) => default;

        public struct BindingSyntax { }

        public struct CompositeSyntax
        {
            public CompositeSyntax With(string name, string path) => this;
        }
    }
}
