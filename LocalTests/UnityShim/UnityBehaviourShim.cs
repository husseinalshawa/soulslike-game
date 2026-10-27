// Compile-only stubs for the MonoBehaviour layer. These members never run in
// local tests (only pure logic is executed here); they exist so the whole
// game compiles and type errors are caught before the project reaches Unity.
using System;

namespace UnityEngine
{
    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion LookRotation(Vector3 forward) => default;
        public static Quaternion LookRotation(Vector3 forward, Vector3 up) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => default;
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta) => default;
        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
        public static Quaternion operator *(Quaternion a, Quaternion b) => default;
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);
        public static float Clamp(float v, float min, float max) => Math.Clamp(v, min, max);
        public static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Abs(float v) => Math.Abs(v);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float MoveTowards(float current, float target, float maxDelta)
            => Math.Abs(target - current) <= maxDelta ? target : current + Math.Sign(target - current) * maxDelta;
        public static float LerpAngle(float a, float b, float t) => Lerp(a, b, t);
        public static float DeltaAngle(float current, float target)
        {
            float delta = (target - current) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }

    public static class Time
    {
        public static float deltaTime => 0f;
        public static float time => 0f;
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public T GetComponent<T>() => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
    }

    public class Behaviour : Component
    {
        public bool enabled;
        public bool isActiveAndEnabled => enabled;
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public class Transform : Component
    {
        public Vector3 position;
        public Vector3 localPosition;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 forward;
        public Vector3 right;
        public Vector3 up;
        public Transform parent;
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void LookAt(Vector3 worldPosition) { }
        public void Rotate(Vector3 eulers) { }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public Transform transform => null;
        public bool activeSelf => true;
        public int layer;
        public void SetActive(bool value) { }
        public T GetComponent<T>() => default;
        public T AddComponent<T>() where T : Component, new() => new T();
        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject(type.ToString());
        public static GameObject Find(string name) => null;
        public static T FindObjectOfType<T>() => default;
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public class Collider : Component
    {
        public bool enabled;
        public bool isTrigger;
    }

    public class BoxCollider : Collider
    {
        public Vector3 size;
        public Vector3 center;
    }

    public class CapsuleCollider : Collider
    {
        public float radius;
        public float height;
        public Vector3 center;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type requiredComponent) { }
        public RequireComponentAttribute(Type c1, Type c2) { }
    }
}
