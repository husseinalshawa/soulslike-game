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

        public int TerrainChunkCount { get; private set; }

        // مواضع محتوى المرج (يستهلكها البناء والنثر كمناطق إقصاء)
        public static readonly Vector3 Camp1Center = new Vector3(60f, 0f, -60f);
        public static readonly Vector3 Camp2Center = new Vector3(-70f, 0f, -130f);

        public PlayerAvatar Player { get; private set; }
        public EnemyAvatar Enemy { get; private set; }
        public EnemyAvatar Enemy2 { get; private set; }
        public EnemyAvatar[] AllEnemies { get; private set; }
        public BossAvatar Boss { get; private set; }
        public SoulsCache Cache { get; private set; }
        public GameLoop Loop { get; private set; }

        private void Awake()
        {
            if (playerStats == null) playerStats = ScriptableObject.CreateInstance<CharacterStats>();
            if (playerWeapon == null) playerWeapon = ScriptableObject.CreateInstance<WeaponData>();
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
            AllEnemies = new[]
            {
                Enemy, Enemy2,
                camp1[0], camp1[1], camp2[0], camp2[1],
                caveEnemies[0], caveEnemies[1]
            };

            BuildHudAndGameLoop();
            BuildBossArea(fy);
            BuildSoulsCache();
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
            var midGrace = WorldBuilder.OnGround(WorldHeightmap.MidGraceCenter.x,
                WorldHeightmap.MidGraceCenter.z, 0f);
            var caveGrace = WorldBuilder.OnGround(WorldHeightmap.CaveMouthCenter.x,
                WorldHeightmap.CaveMouthCenter.z, 0f);
            Loop.Graces.AddSite(1, midGrace, discovered: false);
            Loop.Graces.AddSite(2, caveGrace, discovered: false);

            Loop.graceMarkers = new[]
            {
                BuildGraceMarker(0, Player.transform.position, lit: true),
                BuildGraceMarker(1, midGrace, lit: false),
                BuildGraceMarker(2, caveGrace, lit: false)
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

            var banner = UI.HudBuilder.BuildBanner();
            UI.HudBuilder.BuildBossBar(Boss);
            Boss.Defeated += () =>
            {
                Player.Wallet.Add(bossStats.soulsReward);
                gate.Seal(false);
                // الضباب يختفي مع النصر (المواصفة §5)
                var fogRenderer = fogVisual.GetComponent<Renderer>();
                if (fogRenderer != null) fogRenderer.enabled = false;
                if (banner != null)
                    banner.Show("GREAT ENEMY FELLED");
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

        private EnemyAvatar BuildEnemy(Vector3 position, Vector3 patrolA, Vector3 patrolB)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "EnemyMelee";
            go.transform.position = position;
            Tint(go, new Color(0.6f, 0.2f, 0.2f));

            go.SetActive(false); // نفس نمط اللاعب: اربط ثم فعّل

            // جسم العدو: CharacterController بدل الـ collider الصلب حتى لا يخترق
            // الجدران والصناديق واللاعب أثناء المطاردة
            Destroy(go.GetComponent<CapsuleCollider>());
            var controller = go.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;

            var damageable = go.AddComponent<Damageable>();
            damageable.stats = enemyStats;

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
            enemy.stats = enemyStats;
            enemy.weapon = enemyWeapon;
            enemy.weaponHitbox = hitbox;
            enemy.player = Player.transform;
            enemy.patrolA = patrolA;
            enemy.patrolB = patrolB;

            go.SetActive(true);
            enemy.Damageable.Died += () => Player.Wallet.Add(enemyStats.soulsReward);
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
