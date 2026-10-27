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
            // بدون GraphicRaycaster + EventSystem لا تعمل أزرار uGUI إطلاقاً
            canvasGo.AddComponent<GraphicRaycaster>();
            BuildEventSystem();

            var health = BuildBar(canvasGo.transform, "HealthBar",
                new Vector2(30f, 70f), new Vector2(300f, 18f), new Color(0.75f, 0.15f, 0.15f));
            health.Bind(() => player.Damageable.Health.Current, () => player.Damageable.Health.Max);

            var stamina = BuildBar(canvasGo.transform, "StaminaBar",
                new Vector2(30f, 44f), new Vector2(240f, 14f), new Color(0.2f, 0.6f, 0.25f));
            stamina.Bind(() => player.Stamina.Current, () => player.Stamina.Max);
            player.Brain.StaminaRejected += stamina.Flash;

            // بار التركيز (FP) الأزرق
            var focus = BuildBar(canvasGo.transform, "FocusBar",
                new Vector2(30f, 22f), new Vector2(190f, 10f), new Color(0.25f, 0.4f, 0.8f));
            focus.Bind(() => player.Focus.Current, () => player.Focus.Max);
            player.Brain.FocusRejected += focus.Flash;

            BuildLoadoutLabel(canvasGo.transform, player);

            var death = BuildDeathScreen(canvasGo.transform);
            BuildSoulsCounter(canvasGo.transform, player);
            return (health, stamina, death);
        }

        private static void BuildEventSystem()
        {
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        public static Toast BuildToast(PlayerAvatar player)
        {
            var canvas = GameObject.Find("HUD");
            var parent = canvas != null ? canvas.transform : player.transform;

            var textGo = new GameObject("Toast");
            textGo.transform.SetParent(parent, false);
            var text = textGo.AddComponent<Text>();
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.85f, 0.65f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(600f, 40f);
            textGo.SetActive(false);

            var holderGo = new GameObject("ToastController");
            holderGo.transform.SetParent(parent, false);
            var toast = holderGo.AddComponent<Toast>();
            toast.label = text;
            return toast;
        }

        public static LevelUpMenu BuildLevelUpMenu(PlayerAvatar player)
        {
            var canvas = GameObject.Find("HUD");
            var parent = canvas != null ? canvas.transform : player.transform;

            var panelImage = CreateImage(parent, "LevelUpMenu", new Color(0.04f, 0.04f, 0.07f, 0.92f));
            var rect = panelImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(460f, 340f);

            var menuGo = new GameObject("LevelUpMenuController");
            menuGo.transform.SetParent(parent, false);
            var menu = menuGo.AddComponent<LevelUpMenu>();
            menu.panel = panelImage.gameObject;

            menu.header = MakeLabel(panelImage.transform, new Vector2(0f, 130f), 18);
            menu.vigorValue = MakeLabel(panelImage.transform, new Vector2(-40f, 80f), 18);
            menu.vigorButton = MakeButton(panelImage.transform, new Vector2(170f, 80f));
            menu.enduranceValue = MakeLabel(panelImage.transform, new Vector2(-40f, 35f), 18);
            menu.enduranceButton = MakeButton(panelImage.transform, new Vector2(170f, 35f));
            menu.strengthValue = MakeLabel(panelImage.transform, new Vector2(-40f, -10f), 18);
            menu.strengthButton = MakeButton(panelImage.transform, new Vector2(170f, -10f));
            menu.mindValue = MakeLabel(panelImage.transform, new Vector2(-40f, -55f), 18);
            menu.mindButton = MakeButton(panelImage.transform, new Vector2(170f, -55f));

            // زر «انهض» للإغلاق
            var rise = MakeButton(panelImage.transform, new Vector2(0f, -130f), 140f, "Rise");
            rise.onClick.AddListener(menu.Close);

            menu.Initialize(player);
            menu.panel.SetActive(false);
            return menu;
        }

        // غطاء التلاشي الأسود — فوق كل عناصر الواجهة.
        public static FadeOverlay BuildFadeOverlay()
        {
            var canvas = GameObject.Find("HUD");
            if (canvas == null) return null;

            var cover = CreateImage(canvas.transform, "FadeOverlay", new Color(0f, 0f, 0f, 0f));
            var rect = cover.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            cover.transform.SetAsLastSibling();
            cover.raycastTarget = false;

            var overlay = cover.gameObject.AddComponent<FadeOverlay>();
            overlay.image = cover;
            return overlay;
        }

        // شريط الحوار أسفل الشاشة: اسم ذهبي + سطر + تلميح E.
        public static DialoguePanel BuildDialoguePanel()
        {
            var canvas = GameObject.Find("HUD");
            if (canvas == null) return null;

            var background = CreateImage(canvas.transform, "DialoguePanel",
                new Color(0.04f, 0.04f, 0.07f, 0.88f));
            var rect = background.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 95f);
            rect.sizeDelta = new Vector2(820f, 110f);

            var speaker = MakeLabel(background.transform, new Vector2(-230f, 32f), 18);
            speaker.color = new Color(0.85f, 0.7f, 0.25f); // اسم ذهبي
            var line = MakeLabel(background.transform, new Vector2(0f, -6f), 18);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.sizeDelta = new Vector2(760f, 60f);
            var hint = MakeLabel(background.transform, new Vector2(340f, -38f), 14);
            hint.text = "E ▸";
            hint.alignment = TextAnchor.MiddleRight;

            var holderGo = new GameObject("DialoguePanelController");
            holderGo.transform.SetParent(canvas.transform, false);
            var dialoguePanel = holderGo.AddComponent<DialoguePanel>();
            dialoguePanel.panel = background.gameObject;
            dialoguePanel.speakerLabel = speaker;
            dialoguePanel.lineLabel = line;
            background.gameObject.SetActive(false);
            return dialoguePanel;
        }

        // لافتة النصر الكبيرة (GREAT ENEMY FELLED) — Toast بخط ضخم ذهبي.
        public static Toast BuildBanner()
        {
            var canvas = GameObject.Find("HUD");
            if (canvas == null) return null;

            var textGo = new GameObject("Banner");
            textGo.transform.SetParent(canvas.transform, false);
            var text = textGo.AddComponent<Text>();
            text.fontSize = 44;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.85f, 0.7f, 0.25f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(900f, 70f);
            textGo.SetActive(false);

            var holderGo = new GameObject("BannerController");
            holderGo.transform.SetParent(canvas.transform, false);
            var toast = holderGo.AddComponent<Toast>();
            toast.label = text;
            return toast;
        }

        public static BossBar BuildBossBar(SoulsGame.Enemies.BossAvatar boss)
        {
            var canvas = GameObject.Find("HUD");
            if (canvas == null) return null;

            var background = CreateImage(canvas.transform, "BossBar", new Color(0f, 0f, 0f, 0.7f));
            var rect = background.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 60f);
            rect.sizeDelta = new Vector2(700f, 18f);

            var fill = CreateImage(background.transform, "Fill", new Color(0.65f, 0.55f, 0.2f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            var name = new GameObject("Name");
            name.transform.SetParent(background.transform, false);
            var nameText = name.AddComponent<Text>();
            nameText.text = "ARENA WARDEN";
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.LowerCenter;
            nameText.color = new Color(0.85f, 0.8f, 0.65f);
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var nameRect = name.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 4f);
            nameRect.sizeDelta = new Vector2(400f, 22f);

            var controllerGo = new GameObject("BossBarController");
            controllerGo.transform.SetParent(canvas.transform, false);
            var bar = controllerGo.AddComponent<BossBar>();
            bar.boss = boss;
            bar.root = background.gameObject;
            bar.fill = fillRect;
            bar.nameLabel = nameText;
            background.gameObject.SetActive(false);
            return bar;
        }

        private static Text MakeLabel(Transform parent, Vector2 position, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.85f, 0.82f, 0.72f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 36f);
            return text;
        }

        private static Button MakeButton(Transform parent, Vector2 position, float width = 44f, string caption = "+")
        {
            var image = CreateImage(parent, "Button", new Color(0.25f, 0.22f, 0.12f));
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 36f);

            var label = MakeLabel(image.transform, Vector2.zero, 18);
            label.alignment = TextAnchor.MiddleCenter;
            label.text = caption;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(width, 36f);

            return image.gameObject.AddComponent<Button>();
        }

        private static void BuildLoadoutLabel(Transform parent, PlayerAvatar player)
        {
            var go = new GameObject("Loadout");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerLeft;
            text.color = new Color(0.75f, 0.72f, 0.6f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(30f, 94f);
            rect.sizeDelta = new Vector2(420f, 20f);

            var label = go.AddComponent<LoadoutLabel>();
            label.label = text;
            // لا بناء نص كل إطار: يُبنى فقط حين يتغير أحد الأسماء الثلاثة
            string cachedWeapon = null, cachedSpell = null, cachedArmor = null, cachedText = "";
            label.Bind(() =>
            {
                string w = player.Weapons.Current?.weaponName;
                string s = player.Spells.Current?.spellName;
                string a = player.Armors.Current?.armorName;
                if (!ReferenceEquals(w, cachedWeapon) || !ReferenceEquals(s, cachedSpell) ||
                    !ReferenceEquals(a, cachedArmor))
                {
                    cachedWeapon = w; cachedSpell = s; cachedArmor = a;
                    cachedText = $"{w}  |  {s}  |  {a}";
                }
                return cachedText;
            });
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

        // شريط واحد حر في العالم — يتموضع فوق هدف القفل الحالي كل إطار.
        public static EnemyHealthBar BuildEnemyBar(LockOnSystem lockOn)
        {
            var canvasGo = new GameObject("LockOnHealthBar");
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
