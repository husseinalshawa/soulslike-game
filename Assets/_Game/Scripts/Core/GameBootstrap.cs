using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Enemies;
using SoulsGame.Player;
using SoulsGame.Progression;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Core
{
    // يبني ساحة الاختبار بالكامل برمجياً عند التشغيل: الأرضية والجدران
    // والإضاءة واللاعب. لا مشاهد ولا prefabs مكتوبة يدوياً — كل شيء كود
    // قابل للمراجعة، وقابل للاستبدال بأصول حقيقية لاحقاً.
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("اختياري: أصول موازنة من المشروع؛ إن تُركت فارغة تُستخدم القيم الافتراضية")]
        public CharacterStats playerStats;
        public WeaponData playerWeapon;
        public CharacterStats enemyStats;
        public WeaponData enemyWeapon;
        public ProgressionData progressionData;
        public CharacterStats bossStats;
        public WorldTheme worldTheme;

        [Tooltip("المرتّلون الغارقون (المرحلة 5): أبطأ وأقسى من الجنود")]
        public CharacterStats chanterStats;
        public WeaponData chanterWeapon;

        [Tooltip("عتاد المرحلة 4: أصول من المشروع تصل للعالم كما هي — الفارغ يُبنى من GearLibrary")]
        public WeaponData greatswordData;
        public WeaponData twinDaggersData;
        public WeaponData spearData;
        public WeaponData warhammerData;
        public ArmorData clothArmorData;
        public ArmorData leatherArmorData;
        public ArmorData plateArmorData;
        public SpellData boltSpellData;
        public SpellData buffSpellData;

        public int TerrainChunkCount { get; private set; }

        // مواضع محتوى المرج (يستهلكها البناء والنثر كمناطق إقصاء)
        public static readonly Vector3 Camp1Center = new Vector3(60f, 0f, -60f);
        public static readonly Vector3 Camp2Center = new Vector3(-70f, 0f, -130f);

        public PlayerAvatar Player { get; private set; }
        public EnemyAvatar Enemy { get; private set; }
        public EnemyAvatar Enemy2 { get; private set; }
        public EnemyAvatar[] AllEnemies { get; private set; }
        public BossAvatar Boss { get; private set; }
        public HorseAvatar Horse { get; private set; }
        public SoulsCache Cache { get; private set; }
        public GearPickup[] Pickups { get; private set; }
        public GameLoop Loop { get; private set; }
        public Story.KeyItemPickup BellPickup { get; private set; }
        public ThemeShift ActiveThemeShift { get; private set; }
        public Feel.HitStopLogic HitStop { get; } = new Feel.HitStopLogic();

        private UI.Toast victoryBanner;
        private WorldTheme workingTheme;

        private void Awake()
        {
            if (playerStats == null) playerStats = ScriptableObject.CreateInstance<CharacterStats>();
            if (playerWeapon == null) playerWeapon = GearLibrary.Longsword(); // بالاسم والمهارة
            if (enemyStats == null)
            {
                enemyStats = ScriptableObject.CreateInstance<CharacterStats>();
                enemyStats.maxHealth = 150f;
                enemyStats.maxPoise = 30f;
                enemyStats.soulsReward = 100;
            }
            if (enemyWeapon == null)
            {
                enemyWeapon = ScriptableObject.CreateInstance<WeaponData>();
                enemyWeapon.lightDamage = 20f;
                enemyWeapon.lightPoiseDamage = 20f;
                enemyWeapon.lightStaminaCost = 0f;
                enemyWeapon.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            }

            if (bossStats == null)
            {
                bossStats = ScriptableObject.CreateInstance<CharacterStats>();
                bossStats.maxHealth = 1000f;
                bossStats.maxPoise = 120f;
                bossStats.runSpeed = 3.5f;
                bossStats.soulsReward = 2000;
            }

            if (worldTheme == null) worldTheme = ScriptableObject.CreateInstance<WorldTheme>();

            if (chanterStats == null)
            {
                chanterStats = ScriptableObject.CreateInstance<CharacterStats>();
                chanterStats.maxHealth = 200f;
                chanterStats.maxPoise = 40f;
                chanterStats.runSpeed = 2.5f;
                chanterStats.soulsReward = 150;
            }
            if (chanterWeapon == null)
            {
                chanterWeapon = ScriptableObject.CreateInstance<WeaponData>();
                chanterWeapon.lightDamage = 30f;
                chanterWeapon.lightPoiseDamage = 30f;
                chanterWeapon.lightStaminaCost = 0f;
                chanterWeapon.light = new AttackTimeline(0.8f, 0.3f, 1.2f, 1.2f);
            }

            // عتاد المرحلة 4: الأصل المسند من المشهد أولاً، وإلا مكتبة الأرقام المختبرة
            if (greatswordData == null) greatswordData = GearLibrary.Greatsword();
            if (twinDaggersData == null) twinDaggersData = GearLibrary.TwinDaggers();
            if (spearData == null) spearData = GearLibrary.Spear();
            if (warhammerData == null) warhammerData = GearLibrary.Warhammer();
            if (clothArmorData == null) clothArmorData = GearLibrary.Cloth();
            if (leatherArmorData == null) leatherArmorData = GearLibrary.Leather();
            if (plateArmorData == null) plateArmorData = GearLibrary.Plate();
            if (boltSpellData == null) boltSpellData = GearLibrary.GlintBolt();
            if (buffSpellData == null) buffSpellData = GearLibrary.WeaponBuffSpell();

            // العالم أولاً: تضاريس + ثيم + نثر + كهف
            var exclusionCenters = new[]
            {
                Vector3.zero,                                    // الحصن وغرفة البوس
                WorldHeightmap.MidGraceCenter,                   // موقع راحة المنتصف
                WorldHeightmap.CaveMouthCenter,                  // فم الكهف
                WorldBuilder.CaveRoomCenter,                     // غرفة الكهف
                Camp1Center, Camp2Center                         // المعسكران
            };
            var exclusionRadii = new[] { 48f, 6f, 6f, 14f, 8f, 8f };
            TerrainChunkCount = WorldBuilder.Build(worldTheme, exclusionCenters, exclusionRadii);

            float fy = WorldHeightmap.FortPlateauHeight; // الحصن كله فوق الهضبة
            BuildArena(fy);
            Player = BuildPlayer(WorldBuilder.OnGround(0f, -12f, 1.1f));
            BuildCamera(Player);
            Enemy = BuildEnemy(WorldBuilder.OnGround(0f, 5f, 1.1f),
                WorldBuilder.OnGround(-5f, 5f, 1.1f), WorldBuilder.OnGround(5f, 5f, 1.1f));
            Enemy2 = BuildEnemy(WorldBuilder.OnGround(-4f, 11f, 1.1f),
                WorldBuilder.OnGround(-7f, 11f, 1.1f), WorldBuilder.OnGround(-1f, 11f, 1.1f));

            // المعسكران (جنديان لكل معسكر حول نار مخيم) وعدوا الكهف
            var camp1 = BuildCamp(Camp1Center);
            var camp2 = BuildCamp(Camp2Center);
            var caveEnemies = BuildCaveEnemies();
            var chanters = BuildChanters();
            AllEnemies = new[]
            {
                Enemy, Enemy2,
                camp1[0], camp1[1], camp2[0], camp2[1],
                caveEnemies[0], caveEnemies[1],
                chanters[0], chanters[1], chanters[2]
            };

            BuildHudAndGameLoop();
            BuildBossArea(fy);
            BuildSoulsCache();
            BuildHorse();
            BuildGearPickups();
            BuildChapelAndStory();
            WireFeel();
        }

        // ربط الإحساس: hit-stop حسب حالة عقل اللاعب، واهتزازات الكاميرا.
        private void WireFeel()
        {
            var rig = Player.cameraRig;
            System.Action<Vector3, float> onEnemyHit = (position, amount) =>
            {
                var state = Player.Brain.CurrentState;
                bool heavy = state == PlayerStateId.AttackHeavy || state == PlayerStateId.Skill;
                HitStop.Request(heavy ? 0.09f : 0.06f);
                if (heavy && rig != null)
                    rig.Shake.Shake(0.15f, 0.25f);
                Feel.DamageNumber.Spawn(position + Vector3.up * 1.6f, amount);
            };
            foreach (var enemy in AllEnemies)
                if (enemy != null)
                {
                    var self = enemy; // التقاط النسخة لا متغير الحلقة
                    self.Damageable.Damaged += packet =>
                        onEnemyHit(self.transform.position, packet.HealthDamage);
                }
            if (Boss != null)
            {
                Boss.Damageable.Damaged += packet =>
                    onEnemyHit(Boss.transform.position, packet.HealthDamage);
                Boss.SlamLanded += () => rig?.Shake.Shake(0.35f, 0.4f);
            }
            Loop.fadeOverlay = UI.HudBuilder.BuildFadeOverlay();
            Player.Damageable.Damaged += _ => HitStop.Request(0.05f);
            Player.Brain.GuardBroken += () => rig?.Shake.Shake(0.25f, 0.3f);
            // إشعار تبديل العتاد (Tab/G/V) باسم القطعة
            Player.GearChanged += message => Loop.toast?.Show(message);
        }

        // الكنيسة الغارقة والقصة: البناء، موقع الراحة الرابع، الماء الضحل،
        // الشخصيات الثلاث، الجرس، المذبح، وربط الأعلام وتحول اللون.
        private void BuildChapelAndStory()
        {
            var parent = new GameObject("SunkenChapel");
            var chapel = WorldBuilder.BuildChapel(parent.transform, worldTheme);

            // موقع الراحة الرابع (site 3) على أرض جافة قبالة المستنقع من الجنوب-الغرب
            // (خارج الحوض ومنطقة دمجه ومربع سطح الماء — لا راحة غارقة)
            var chapelGrace = WorldBuilder.OnGround(80f, 40f, 1.1f);
            Loop.Graces.AddSite(3, chapelGrace, discovered: false);
            Loop.graceMarkers = new[]
            {
                Loop.graceMarkers[0], Loop.graceMarkers[1], Loop.graceMarkers[2],
                BuildGraceMarker(3, chapelGrace - Vector3.up * 1.1f, lit: false)
            };

            // الماء الضحل يبطئ اللاعب، والحصان ممنوع في المستنقع
            Player.marsh = new MarshZone(WorldHeightmap.MarshCenter, WorldHeightmap.MarshRadius);
            Player.mountForbiddenZones.Add(new ForbiddenZone
            {
                Center = WorldHeightmap.MarshCenter,
                Radius = WorldHeightmap.MarshRadius
            });

            // الشخصيات الثلاث — ألوان مميزة، لا Damageable ولا lock-on
            float fy = WorldHeightmap.FortPlateauHeight;
            var mara = BuildNpc(Story.StoryLibrary.Mara(),
                WorldBuilder.OnGround(4f, -96f, 1.1f), new Color(0.75f, 0.55f, 0.3f));
            var valion = BuildNpc(Story.StoryLibrary.Valion(),
                new Vector3(8.5f, fy + 1.1f, 17f), new Color(0.5f, 0.55f, 0.65f));
            var sila = BuildNpc(Story.StoryLibrary.Sila(),
                WorldBuilder.OnGround(-40f, -70f, 1.1f), new Color(0.8f, 0.78f, 0.7f));
            // شجرة سيلا الميتة — معلم بصري يلتقطه البصر من بعيد
            CreateBox("SilaTree", WorldBuilder.OnGround(-41.5f, -70f, 2.5f),
                new Vector3(0.5f, 5f, 0.5f), worldTheme.deadTreeColor);
            Loop.npcs = new[] { mara, valion, sila };

            // الجرس الصدئ في غرفة كنز الكهف
            float caveFloorY = WorldHeightmap.GetHeight(
                WorldHeightmap.CaveMouthCenter.x, WorldHeightmap.CaveMouthCenter.z);
            var bellGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bellGo.name = "Pickup_RustedBell";
            bellGo.transform.position = new Vector3(
                WorldBuilder.CaveRoomCenter.x - 2f, caveFloorY + 0.6f, WorldBuilder.CaveRoomCenter.z - 2f);
            bellGo.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Tint(bellGo, new Color(0.65f, 0.6f, 0.45f));
            var bellCollider = bellGo.GetComponent<Collider>();
            if (bellCollider != null) bellCollider.enabled = false;
            BellPickup = bellGo.AddComponent<Story.KeyItemPickup>();
            BellPickup.itemName = Story.StoryLibrary.RustedBell;
            BellPickup.description = Story.StoryLibrary.RustedBellDescription;
            BellPickup.flagOnCollect = Story.StoryFlag.BellFound;
            BellPickup.player = Player;
            BellPickup.story = Loop.Story;
            BellPickup.toast = Loop.toast;

            // المذبح والواجهة والأعلام
            Loop.altarPosition = chapel.AltarPosition;
            Loop.dialoguePanel = UI.HudBuilder.BuildDialoguePanel();
            Loop.banner = victoryBanner;
            Boss.Defeated += () => Loop.Story.Set(Story.StoryFlag.BossDefeated);
            Loop.Story.FlagChanged += flag =>
            {
                if (flag == Story.StoryFlag.AshPlaced)
                    StartThemeShift();
            };
        }

        private Story.NpcAvatar BuildNpc(Story.NpcQuestline questline, Vector3 position, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Npc_{questline.NpcName}";
            go.transform.position = position;
            Tint(go, tint);
            // جسد صلب يقف في مكانه — لا Damageable ولا Hurtbox: لا يُقتل ولا يُستهدف
            var npc = go.AddComponent<Story.NpcAvatar>();
            npc.Questline = questline;
            return npc;
        }

        // الخاتمة: المرج يستعيد مسحة لون — تحول 8 ثوانٍ نحو سماء أدفأ وضباب أخف
        private void StartThemeShift()
        {
            var warm = ScriptableObject.CreateInstance<WorldTheme>();
            warm.sunColor = new Color(1f, 0.93f, 0.78f);
            warm.sunIntensity = 1.15f;
            warm.sunPitchDegrees = worldTheme.sunPitchDegrees;
            warm.sunYawDegrees = worldTheme.sunYawDegrees;
            warm.fogColor = new Color(0.8f, 0.8f, 0.7f);
            warm.fogDensity = 0.0045f;
            warm.ambient = new Color(0.52f, 0.52f, 0.46f);
            warm.skyColor = new Color(0.72f, 0.78f, 0.66f);
            warm.grassColor = new Color(0.38f, 0.48f, 0.28f);
            warm.highGroundColor = worldTheme.highGroundColor;
            warm.rockColor = worldTheme.rockColor;
            warm.deadTreeColor = worldTheme.deadTreeColor;
            warm.ruinColor = worldTheme.ruinColor;
            warm.caveColor = worldTheme.caveColor;

            workingTheme = ScriptableObject.CreateInstance<WorldTheme>();
            ActiveThemeShift = new ThemeShift(worldTheme, warm, 8f);
        }

        private void Update()
        {
            // hit-stop يُدار بزمن غير مقيس كي لا يجمد نفسه
            HitStop.Tick(Time.unscaledDeltaTime);
            Time.timeScale = HitStop.CurrentScale;

            if (ActiveThemeShift == null)
                return;
            ActiveThemeShift.Tick(Time.deltaTime);
            var blended = ActiveThemeShift.Blend(workingTheme);
            WorldBuilder.ApplyTheme(blended);
            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = blended.skyColor;
            if (ActiveThemeShift.Done)
                ActiveThemeShift = null; // اكتمل — السمة الدافئة مطبقة
        }

        // المرتّلون الغارقون الثلاثة: داخل المستنقع حول الكنيسة، بلون غارق
        private EnemyAvatar[] BuildChanters()
        {
            var tint = new Color(0.32f, 0.45f, 0.42f);
            // مواقعهم داخل مستطيل أرضية الكنيسة الحجرية (سمكها 0.3) —
            // الإزاحة 1.4 = 0.3 بلاطة + 1.1 نصف الجسم، كي لا يولدوا داخلها
            return new[]
            {
                BuildEnemy(WorldBuilder.OnGround(112f, 70f, 1.4f),
                    WorldBuilder.OnGround(110f, 68f, 1.4f), WorldBuilder.OnGround(114f, 72f, 1.4f),
                    chanterStats, chanterWeapon, tint),
                BuildEnemy(WorldBuilder.OnGround(128f, 72f, 1.4f),
                    WorldBuilder.OnGround(126f, 70f, 1.4f), WorldBuilder.OnGround(130f, 74f, 1.4f),
                    chanterStats, chanterWeapon, tint),
                BuildEnemy(WorldBuilder.OnGround(120f, 94f, 1.4f),
                    WorldBuilder.OnGround(118f, 93f, 1.4f), WorldBuilder.OnGround(122f, 95f, 1.4f),
                    chanterStats, chanterWeapon, tint)
            };
        }

        private void BuildGearPickups()
        {
            float fy = WorldHeightmap.FortPlateauHeight;
            Vector3 caveRoom = WorldBuilder.CaveRoomCenter;
            float caveFloorY = WorldHeightmap.GetHeight(
                WorldHeightmap.CaveMouthCenter.x, WorldHeightmap.CaveMouthCenter.z);

            // الترتيب مثبت: [0] السيف العظيم (يعتمده اختبار دورة الحياة)
            Pickups = new[]
            {
                BuildGearPickup(greatswordData, null, new Vector3(2f, fy + 0.6f, 35f)),
                BuildGearPickup(null, plateArmorData, new Vector3(-2f, fy + 0.6f, 35f)),
                BuildGearPickup(twinDaggersData, null,
                    new Vector3(caveRoom.x + 3f, caveFloorY + 0.6f, caveRoom.z + 3f)),
                BuildGearPickup(spearData, null,
                    WorldBuilder.OnGround(Camp1Center.x, Camp1Center.z + 3f, 0.6f)),
                BuildGearPickup(warhammerData, null,
                    WorldBuilder.OnGround(Camp2Center.x, Camp2Center.z + 3f, 0.6f)),
                BuildGearPickup(null, leatherArmorData,
                    WorldBuilder.OnGround(6f, -95f, 0.6f))
            };
        }

        private GearPickup BuildGearPickup(WeaponData weaponData, ArmorData armorData, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = weaponData != null ? $"Pickup_{weaponData.weaponName}" : $"Pickup_{armorData.armorName}";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Tint(go, weaponData != null ? new Color(0.85f, 0.75f, 0.4f) : new Color(0.6f, 0.65f, 0.8f));
            var pickupCollider = go.GetComponent<Collider>();
            if (pickupCollider != null) pickupCollider.enabled = false;

            var pickup = go.AddComponent<GearPickup>();
            pickup.weapon = weaponData;
            pickup.armor = armorData;
            pickup.player = Player;
            pickup.toast = Loop != null ? Loop.toast : null;
            return pickup;
        }

        private void BuildHorse()
        {
            // الجذر بمقياس 1 دائماً: الراكب يُربط به، فلا يتشوه مقياسه أبداً —
            // استطالة الحصان على ابن مرئي منفصل فقط
            var go = new GameObject("Steed");
            go.transform.position = WorldBuilder.OnGround(5f, -30f, 0.9f);
            go.SetActive(false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.9f, 0.9f, 1.8f);
            Tint(body, new Color(0.45f, 0.32f, 0.22f));
            Destroy(body.GetComponent<CapsuleCollider>());

            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;

            Horse = go.AddComponent<HorseAvatar>();
            Horse.cameraTransform = Camera.main != null ? Camera.main.transform
                : Player.cameraRig != null ? Player.cameraRig.transform : null;

            Player.horse = Horse;
            // 48 لا 38: هندسة الحصن تمتد حتى ~46.5م (غرفة البوس) بعد قرار الهضبة
            Player.mountForbiddenZones.Add(new ForbiddenZone { Center = Vector3.zero, Radius = 48f });
            Player.mountForbiddenZones.Add(new ForbiddenZone
            {
                Center = new Vector3((WorldHeightmap.CaveMouthCenter.x + WorldBuilder.CaveRoomCenter.x) * 0.5f,
                    0f, WorldHeightmap.CaveMouthCenter.z),
                Radius = 20f // النفق والغرفة
            });

            go.SetActive(true); // يجري Awake (الذي يخفيه فوراً حتى الاستدعاء)
        }

        private EnemyAvatar[] BuildCamp(Vector3 center)
        {
            // نار المخيم: صندوق متوهج يحدد الموقع من بعيد
            var fire = CreateBox("Campfire", WorldBuilder.OnGround(center.x, center.z, 0.3f),
                new Vector3(0.8f, 0.6f, 0.8f), new Color(0.9f, 0.45f, 0.15f));
            var fireCollider = fire.GetComponent<Collider>();
            if (fireCollider != null) fireCollider.enabled = false;

            return new[]
            {
                BuildEnemy(WorldBuilder.OnGround(center.x - 3f, center.z, 1.1f),
                    WorldBuilder.OnGround(center.x - 5f, center.z - 2f, 1.1f),
                    WorldBuilder.OnGround(center.x - 1f, center.z + 2f, 1.1f)),
                BuildEnemy(WorldBuilder.OnGround(center.x + 3f, center.z, 1.1f),
                    WorldBuilder.OnGround(center.x + 5f, center.z + 2f, 1.1f),
                    WorldBuilder.OnGround(center.x + 1f, center.z - 2f, 1.1f))
            };
        }

        private EnemyAvatar[] BuildCaveEnemies()
        {
            Vector3 room = WorldBuilder.CaveRoomCenter;
            float floorY = WorldHeightmap.GetHeight(
                WorldHeightmap.CaveMouthCenter.x, WorldHeightmap.CaveMouthCenter.z);
            return new[]
            {
                BuildEnemy(new Vector3(room.x - 2f, floorY + 1.1f, room.z + 2f),
                    new Vector3(room.x - 4f, floorY + 1.1f, room.z), new Vector3(room.x, floorY + 1.1f, room.z + 4f)),
                BuildEnemy(new Vector3(room.x + 2f, floorY + 1.1f, room.z - 2f),
                    new Vector3(room.x + 4f, floorY + 1.1f, room.z), new Vector3(room.x, floorY + 1.1f, room.z - 4f))
            };
        }

        private void BuildSoulsCache()
        {
            Vector3 room = WorldBuilder.CaveRoomCenter;
            float floorY = WorldHeightmap.GetHeight(
                WorldHeightmap.CaveMouthCenter.x, WorldHeightmap.CaveMouthCenter.z);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "SoulsCache";
            go.transform.position = new Vector3(room.x - 5f, floorY + 0.8f, room.z);
            go.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            Tint(go, new Color(0.6f, 0.95f, 0.75f));
            var cacheCollider = go.GetComponent<Collider>();
            if (cacheCollider != null) cacheCollider.enabled = false;

            Cache = go.AddComponent<SoulsCache>();
            Loop.cache = Cache;
        }

        private void BuildHudAndGameLoop()
        {
            var (_, _, deathScreen) = UI.HudBuilder.BuildPlayerHud(Player);
            UI.HudBuilder.BuildEnemyBar(Enemy, Player.lockOn);

            Loop = gameObject.AddComponent<GameLoop>();
            Loop.Initialize(Player, AllEnemies, deathScreen);
            Loop.toast = UI.HudBuilder.BuildToast(Player);
            Loop.levelUpMenu = UI.HudBuilder.BuildLevelUpMenu(Player);

            BuildGraceSites();
        }

        private void BuildGraceSites()
        {
            // الموقع 0 (مكتشف) أضافته Initialize عند نقطة البداية (بوابة الحصن)؛
            // 1 منتصف المرج؛ 2 فم الكهف
            // مواضع الإحياء بمركز اللاعب (+1.1) — موضع على سطح الأرض تماماً
            // يدفن الكبسولة نصفها في الـ MeshCollider عند الإحياء
            var midGrace = WorldBuilder.OnGround(WorldHeightmap.MidGraceCenter.x,
                WorldHeightmap.MidGraceCenter.z, 1.1f);
            var caveGrace = WorldBuilder.OnGround(WorldHeightmap.CaveMouthCenter.x,
                WorldHeightmap.CaveMouthCenter.z, 1.1f);
            Loop.Graces.AddSite(1, midGrace, discovered: false);
            Loop.Graces.AddSite(2, caveGrace, discovered: false);

            Loop.graceMarkers = new[]
            {
                BuildGraceMarker(0, Player.transform.position - Vector3.up * 1.1f, lit: true),
                BuildGraceMarker(1, midGrace - Vector3.up * 1.1f, lit: false),
                BuildGraceMarker(2, caveGrace - Vector3.up * 1.1f, lit: false)
            };
        }

        private void BuildBossArea(float fy)
        {
            // غرفة البوس 22×22 شمال الساحة خلف الفتحة — كلها فوق هضبة الحصن
            CreateBox("BossFloor", new Vector3(0f, fy - 0.5f, 31f), new Vector3(22f, 1f, 22f), new Color(0.22f, 0.2f, 0.26f));
            CreateBox("BossWall_N", new Vector3(0f, fy + 2f, 41.5f), new Vector3(24f, 4f, 1f), new Color(0.18f, 0.18f, 0.2f));
            CreateBox("BossWall_W", new Vector3(-10.5f, fy + 2f, 31f), new Vector3(1f, 4f, 22f), new Color(0.18f, 0.18f, 0.2f));
            CreateBox("BossWall_E", new Vector3(10.5f, fy + 2f, 31f), new Vector3(1f, 4f, 22f), new Color(0.18f, 0.18f, 0.2f));
            // سد جانبي الممر بين الساحة والغرفة
            CreateBox("Corridor_W", new Vector3(1.5f, fy + 2f, 20.9f), new Vector3(1f, 4f, 1.8f), new Color(0.18f, 0.18f, 0.2f));
            CreateBox("Corridor_E", new Vector3(6.5f, fy + 2f, 20.9f), new Vector3(1f, 4f, 1.8f), new Color(0.18f, 0.18f, 0.2f));

            Boss = BuildBoss(new Vector3(0f, fy + 1.8f, 33f));

            // جدار الضباب المرئي في الفتحة (x 2..6 عند z=20.5) — عبوره حر، بلا تصادم
            var fogVisual = CreateBox("FogVisual", new Vector3(4f, fy + 2f, 20.5f), new Vector3(4f, 4f, 0.3f), new Color(0.55f, 0.45f, 0.7f));
            var fogVisualCollider = fogVisual.GetComponent<Collider>();
            if (fogVisualCollider != null) fogVisualCollider.enabled = false;

            // المشغّل داخل الغرفة (z=22.5): لا يُقفل الحاجز إلا واللاعب داخلها فعلاً
            var triggerGo = new GameObject("FogGateTrigger");
            triggerGo.transform.position = new Vector3(4f, fy + 2f, 22.5f);
            var triggerCollider = triggerGo.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(4f, 4f, 0.6f);

            var barrier = CreateBox("FogBarrier", new Vector3(4f, fy + 2f, 20.5f), new Vector3(4f, 4f, 0.4f), new Color(0.35f, 0.25f, 0.5f));
            barrier.SetActive(false);
            var gate = triggerGo.AddComponent<FogGate>();
            gate.barrier = barrier;
            gate.OnPlayerEntered = () => Boss.BeginFight();

            Loop.boss = Boss;
            Loop.fogGate = gate;

            victoryBanner = UI.HudBuilder.BuildBanner();
            UI.HudBuilder.BuildBossBar(Boss);
            Boss.Defeated += () =>
            {
                Player.Wallet.Add(bossStats.soulsReward);
                gate.Seal(false);
                // الضباب يختفي مع النصر (المواصفة §5)
                var fogRenderer = fogVisual.GetComponent<Renderer>();
                if (fogRenderer != null) fogRenderer.enabled = false;
                if (victoryBanner != null)
                    victoryBanner.Show("GREAT ENEMY FELLED");
            };
        }

        private BossAvatar BuildBoss(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "ArenaWarden";
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            Tint(go, new Color(0.4f, 0.15f, 0.3f));

            go.SetActive(false); // نمط «اربط ثم فعّل»

            Destroy(go.GetComponent<CapsuleCollider>());
            var controller = go.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;

            var damageable = go.AddComponent<Damageable>();
            damageable.stats = bossStats;

            var hurtboxGo = new GameObject("Hurtbox");
            hurtboxGo.transform.SetParent(go.transform, false);
            var hurtCollider = hurtboxGo.AddComponent<CapsuleCollider>();
            hurtCollider.isTrigger = true;
            hurtCollider.height = 2f;
            hurtCollider.radius = 0.5f;
            var hurtbox = hurtboxGo.AddComponent<Hurtbox>();
            hurtbox.owner = damageable;

            var weaponGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponGo.name = "GreatClub";
            weaponGo.transform.SetParent(go.transform, false);
            weaponGo.transform.localPosition = new Vector3(0f, 0.2f, 1.1f);
            weaponGo.transform.localScale = new Vector3(0.35f, 0.35f, 1.4f);
            Tint(weaponGo, new Color(0.3f, 0.22f, 0.2f));
            var weaponCollider = weaponGo.GetComponent<Collider>();
            weaponCollider.isTrigger = true;
            var weaponBody = weaponGo.AddComponent<Rigidbody>();
            weaponBody.isKinematic = true;
            var hitbox = weaponGo.AddComponent<Hitbox>();
            hitbox.ignoreOwner = damageable;

            var boss = go.AddComponent<BossAvatar>();
            boss.stats = bossStats;
            boss.weaponHitbox = hitbox;
            boss.player = Player.transform;
            boss.playerDamageable = Player.Damageable;

            go.SetActive(true);
            return boss;
        }

        private GraceSiteMarker BuildGraceMarker(int siteId, Vector3 position, bool lit)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"Grace_{siteId}";
            go.transform.position = position + Vector3.up * 0.45f;
            go.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            var marker = go.AddComponent<GraceSiteMarker>();
            marker.siteId = siteId;
            marker.SetLit(lit);
            return marker;
        }

        private EnemyAvatar BuildEnemy(Vector3 position, Vector3 patrolA, Vector3 patrolB,
            CharacterStats statsOverride = null, WeaponData weaponOverride = null,
            Color? tintOverride = null)
        {
            var stats = statsOverride != null ? statsOverride : enemyStats;
            var weaponData = weaponOverride != null ? weaponOverride : enemyWeapon;
            Color tint = tintOverride ?? new Color(0.6f, 0.2f, 0.2f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = statsOverride != null ? "DrownedChanter" : "EnemyMelee";
            go.transform.position = position;
            Tint(go, tint);

            go.SetActive(false); // نفس نمط اللاعب: اربط ثم فعّل

            // جسم العدو: CharacterController بدل الـ collider الصلب حتى لا يخترق
            // الجدران والصناديق واللاعب أثناء المطاردة
            Destroy(go.GetComponent<CapsuleCollider>());
            var controller = go.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;

            var damageable = go.AddComponent<Damageable>();
            damageable.stats = stats;

            var hurtboxGo = new GameObject("Hurtbox");
            hurtboxGo.transform.SetParent(go.transform, false);
            var hurtCollider = hurtboxGo.AddComponent<CapsuleCollider>();
            hurtCollider.isTrigger = true;
            hurtCollider.height = 2f;
            hurtCollider.radius = 0.45f;
            var hurtbox = hurtboxGo.AddComponent<Hurtbox>();
            hurtbox.owner = damageable;

            var weaponGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponGo.name = "Weapon";
            weaponGo.transform.SetParent(go.transform, false);
            weaponGo.transform.localPosition = new Vector3(0f, 0.2f, 1.0f);
            weaponGo.transform.localScale = new Vector3(0.25f, 0.25f, 1.6f);
            Tint(weaponGo, new Color(0.4f, 0.25f, 0.25f));
            var weaponCollider = weaponGo.GetComponent<Collider>();
            weaponCollider.isTrigger = true;
            var weaponBody = weaponGo.AddComponent<Rigidbody>();
            weaponBody.isKinematic = true;
            var hitbox = weaponGo.AddComponent<Hitbox>();
            hitbox.ignoreOwner = damageable;

            var enemy = go.AddComponent<EnemyAvatar>();
            enemy.stats = stats;
            enemy.weapon = weaponData;
            enemy.weaponHitbox = hitbox;
            enemy.player = Player.transform;
            enemy.patrolA = patrolA;
            enemy.patrolB = patrolB;
            enemy.baseTint = tint;

            go.SetActive(true);
            enemy.Damageable.Died += () => Player.Wallet.Add(stats.soulsReward);
            return enemy;
        }

        private void BuildCamera(PlayerAvatar player)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = worldTheme.skyColor; // سماء الغروب الشاحبة

            var rig = camera.gameObject.AddComponent<ThirdPersonCamera>();
            rig.followTarget = player.transform;

            var lockOn = player.gameObject.AddComponent<LockOnSystem>();
            lockOn.motor = player.GetComponent<PlayerMotor>();
            lockOn.cameraTransform = camera.transform;
            rig.lockOn = lockOn;

            player.lockOn = lockOn;
            player.cameraRig = rig;
            player.GetComponent<PlayerMotor>().cameraTransform = camera.transform;
        }

        // الحصن المهجور فوق الهضبة — أرضيته هي سطح الهضبة نفسه (الإضاءة
        // والضباب صارا مسؤولية WorldBuilder.ApplyTheme).
        private void BuildArena(float fy)
        {
            // الجدار الشمالي بفتحة (x من 2 إلى 6) تقود لغرفة البوس
            CreateBox("Wall_N_Left", new Vector3(-9.5f, fy + 2f, 20.5f), new Vector3(23f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_N_Right", new Vector3(13.5f, fy + 2f, 20.5f), new Vector3(15f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            // الجدار الجنوبي ببوابة مفتوحة (x من -3 إلى 3) يدخل منها اللاعب من المرج
            CreateBox("Wall_S_Left", new Vector3(-12f, fy + 2f, -20.5f), new Vector3(18f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_S_Right", new Vector3(12f, fy + 2f, -20.5f), new Vector3(18f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_E", new Vector3(20.5f, fy + 2f, 0f), new Vector3(1f, 4f, 42f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_W", new Vector3(-20.5f, fy + 2f, 0f), new Vector3(1f, 4f, 42f), new Color(0.2f, 0.2f, 0.22f));

            CreateBox("Crate_A", new Vector3(5f, fy + 1f, 3f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));
            CreateBox("Crate_B", new Vector3(-6f, fy + 1f, -2f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));
            CreateBox("Crate_C", new Vector3(2f, fy + 1f, 9f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));
        }

        private PlayerAvatar BuildPlayer(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = position;
            Tint(go, new Color(0.75f, 0.75f, 0.85f));

            // مهم: AddComponent على كائن نشط يستدعي Awake قبل ضبط الحقول —
            // نبني معطّلاً، نربط كل الحقول، ثم نفعّل فيجري Awake بترتيب سليم.
            go.SetActive(false);

            // الكبسولة البدائية تأتي بـ CapsuleCollider صلب — نستبدله بـ CharacterController
            Destroy(go.GetComponent<CapsuleCollider>());
            var controller = go.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;
            // حافة أرضية الكنيسة 0.3م تساوي stepOffset الافتراضي بالضبط —
            // قيمة صريحة أعلى تضمن الصعود (وإلا صار المذبح/الخاتمة بلا طريق)
            controller.stepOffset = 0.5f;

            var damageable = go.AddComponent<Damageable>();
            damageable.stats = playerStats;

            // Hurtbox: trigger منفصل يستقبل ضربات العدو
            var hurtboxGo = new GameObject("Hurtbox");
            hurtboxGo.transform.SetParent(go.transform, false);
            var hurtCollider = hurtboxGo.AddComponent<CapsuleCollider>();
            hurtCollider.isTrigger = true;
            hurtCollider.height = 2f;
            hurtCollider.radius = 0.45f;
            var hurtbox = hurtboxGo.AddComponent<Hurtbox>();
            hurtbox.owner = damageable;

            // السلاح: صندوق أمام اللاعب، الـ Hitbox عليه
            var weaponGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weaponGo.name = "Weapon";
            weaponGo.transform.SetParent(go.transform, false);
            weaponGo.transform.localPosition = new Vector3(0f, 0.2f, 1.0f);
            weaponGo.transform.localScale = new Vector3(0.25f, 0.25f, 1.6f);
            Tint(weaponGo, new Color(0.6f, 0.6f, 0.65f));
            var weaponCollider = weaponGo.GetComponent<Collider>();
            weaponCollider.isTrigger = true;
            var weaponBody = weaponGo.AddComponent<Rigidbody>();
            weaponBody.isKinematic = true;
            var hitbox = weaponGo.AddComponent<Hitbox>();
            hitbox.ignoreOwner = damageable;

            var motor = go.AddComponent<PlayerMotor>();
            motor.stats = playerStats;
            motor.cameraTransform = Camera.main != null ? Camera.main.transform : null;

            var avatar = go.AddComponent<PlayerAvatar>();
            avatar.stats = playerStats;
            avatar.weapon = playerWeapon;
            avatar.progressionData = progressionData;
            avatar.weaponHitbox = hitbox;
            avatar.defaultArmor = clothArmorData;
            avatar.spellBolt = boltSpellData;
            avatar.spellBuff = buffSpellData;

            go.SetActive(true); // الآن تجري Awake وكل الحقول مضبوطة
            return avatar;
        }

        public static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        public static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            Tint(go, color);
            return go;
        }
    }
}
