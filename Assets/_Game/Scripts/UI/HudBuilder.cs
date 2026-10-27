using SoulsGame.Enemies;
using SoulsGame.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SoulsGame.UI
{
    // يبني الـ HUD برمجياً (امتداد لنهج المشهد-ككود): شريطا اللاعب،
    // شاشة الموت، وشريط صحة فوق العدو.
    public static class HudBuilder
    {
        public static (ResourceBar health, ResourceBar stamina, DeathScreen death) BuildPlayerHud(PlayerAvatar player)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            var health = BuildBar(canvasGo.transform, "HealthBar",
                new Vector2(30f, 70f), new Vector2(300f, 18f), new Color(0.75f, 0.15f, 0.15f));
            health.Bind(() => player.Damageable.Health.Current, () => player.Damageable.Health.Max);

            var stamina = BuildBar(canvasGo.transform, "StaminaBar",
                new Vector2(30f, 44f), new Vector2(240f, 14f), new Color(0.2f, 0.6f, 0.25f));
            stamina.Bind(() => player.Stamina.Current, () => player.Stamina.Max);
            player.Brain.StaminaRejected += stamina.Flash;

            var death = BuildDeathScreen(canvasGo.transform);
            BuildSoulsCounter(canvasGo.transform, player);
            return (health, stamina, death);
        }

        private static void BuildSoulsCounter(Transform parent, PlayerAvatar player)
        {
            var background = CreateImage(parent, "SoulsCounter", new Color(0f, 0f, 0f, 0.6f));
            var rect = background.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-30f, 30f);
            rect.sizeDelta = new Vector2(160f, 34f);

            var textGo = new GameObject("Value");
            textGo.transform.SetParent(background.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = "0";
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleRight;
            text.color = new Color(0.85f, 0.8f, 0.6f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.sizeDelta = new Vector2(-16f, 0f);

            var counter = background.gameObject.AddComponent<SoulsCounter>();
            counter.label = text;
            counter.Bind(() => player.Wallet.Current);
        }

        private static ResourceBar BuildBar(Transform parent, string name,
            Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var background = CreateImage(parent, name, new Color(0f, 0f, 0f, 0.6f));
            SetupRect(background.rectTransform, Vector2.zero, anchoredPosition, size);

            var fill = CreateImage(background.transform, "Fill", color);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            var bar = background.gameObject.AddComponent<ResourceBar>();
            bar.fill = fillRect;
            bar.fillImage = fill;
            bar.baseColor = color;
            return bar;
        }

        private static DeathScreen BuildDeathScreen(Transform parent)
        {
            var overlay = CreateImage(parent, "DeathScreen", new Color(0f, 0f, 0f, 0.85f));
            var rect = overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(overlay.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = "YOU DIED";
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.7f, 0.1f, 0.1f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.sizeDelta = Vector2.zero;

            overlay.gameObject.SetActive(false);

            var screenGo = new GameObject("DeathScreenController");
            screenGo.transform.SetParent(parent, false);
            var screen = screenGo.AddComponent<DeathScreen>();
            screen.panel = overlay.gameObject;
            return screen;
        }

        public static EnemyHealthBar BuildEnemyBar(EnemyAvatar enemy, LockOnSystem lockOn)
        {
            var canvasGo = new GameObject("EnemyHealthBar");
            canvasGo.transform.SetParent(enemy.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160f, 20f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var background = CreateImage(canvasGo.transform, "Background", new Color(0f, 0f, 0f, 0.6f));
            SetupRect(background.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 16f));

            var fill = CreateImage(background.transform, "Fill", new Color(0.75f, 0.15f, 0.15f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            var bar = canvasGo.AddComponent<EnemyHealthBar>();
            bar.owner = enemy;
            bar.lockOn = lockOn;
            bar.fill = fillRect;
            bar.root = background.gameObject;
            return bar;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetupRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
