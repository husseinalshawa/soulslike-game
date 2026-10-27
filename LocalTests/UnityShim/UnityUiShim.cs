// Compile-only stubs for uGUI / Canvas types used by the HUD layer.
using System;

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode;
        public Camera worldCamera;
        public int sortingOrder;
    }

    public class CanvasGroup : Behaviour
    {
        public float alpha;
    }

    public class RectTransform : Transform
    {
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
    }

    public class Font : Object
    {
        public Material material = new Material();
    }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }

    public static class Resources
    {
        private static readonly Font builtinFont = new Font();
        public static T GetBuiltinResource<T>(string path) where T : Object =>
            typeof(T) == typeof(Font) ? builtinFont as T : null;
        public static T Load<T>(string path) where T : Object => null;
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour { }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : Behaviour { }
}

namespace UnityEngine.UI
{
    public class CanvasScaler : Behaviour
    {
        public Vector2 referenceResolution;
    }

    public class GraphicRaycaster : Behaviour { }

    public class Graphic : Behaviour
    {
        public Color color;
        public bool raycastTarget = true;
        public RectTransform rectTransform => GetComponent<RectTransform>();
    }

    public class Image : Graphic
    {
    }

    public class Text : Graphic
    {
        public string text;
        public Font font;
        public int fontSize;
        public TextAnchor alignment;
    }

    public class Button : Behaviour
    {
        public class ButtonClickedEvent
        {
            public void AddListener(Action call) { }
            public void Invoke() { }
        }

        public ButtonClickedEvent onClick = new ButtonClickedEvent();
        public bool interactable = true;
    }
}
