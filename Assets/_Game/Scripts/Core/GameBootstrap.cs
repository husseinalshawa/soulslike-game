using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Enemies;
using SoulsGame.Player;
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

        public PlayerAvatar Player { get; private set; }
        public EnemyAvatar Enemy { get; private set; }

        private void Awake()
        {
            if (playerStats == null) playerStats = ScriptableObject.CreateInstance<CharacterStats>();
            if (playerWeapon == null) playerWeapon = ScriptableObject.CreateInstance<WeaponData>();
            if (enemyStats == null)
            {
                enemyStats = ScriptableObject.CreateInstance<CharacterStats>();
                enemyStats.maxHealth = 150f;
                enemyStats.maxPoise = 30f;
            }
            if (enemyWeapon == null)
            {
                enemyWeapon = ScriptableObject.CreateInstance<WeaponData>();
                enemyWeapon.lightDamage = 20f;
                enemyWeapon.lightPoiseDamage = 20f;
                enemyWeapon.lightStaminaCost = 0f;
                enemyWeapon.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            }

            BuildArena();
            Player = BuildPlayer(new Vector3(0f, 1.1f, -12f));
            BuildCamera(Player);
            Enemy = BuildEnemy(new Vector3(0f, 1.1f, 5f),
                new Vector3(-5f, 1.1f, 5f), new Vector3(5f, 1.1f, 5f));
            BuildHudAndGameLoop();
        }

        private void BuildHudAndGameLoop()
        {
            var (_, _, deathScreen) = UI.HudBuilder.BuildPlayerHud(Player);
            UI.HudBuilder.BuildEnemyBar(Enemy, Player.lockOn);

            var gameLoop = gameObject.AddComponent<GameLoop>();
            gameLoop.Initialize(Player, new[] { Enemy }, deathScreen);
        }

        private EnemyAvatar BuildEnemy(Vector3 position, Vector3 patrolA, Vector3 patrolB)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "EnemyMelee";
            go.transform.position = position;
            Tint(go, new Color(0.6f, 0.2f, 0.2f));

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

        private void BuildArena()
        {
            CreateBox("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), new Color(0.25f, 0.25f, 0.28f));

            CreateBox("Wall_N", new Vector3(0f, 2f, 20.5f), new Vector3(42f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_S", new Vector3(0f, 2f, -20.5f), new Vector3(42f, 4f, 1f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_E", new Vector3(20.5f, 2f, 0f), new Vector3(1f, 4f, 42f), new Color(0.2f, 0.2f, 0.22f));
            CreateBox("Wall_W", new Vector3(-20.5f, 2f, 0f), new Vector3(1f, 4f, 42f), new Color(0.2f, 0.2f, 0.22f));

            CreateBox("Crate_A", new Vector3(5f, 1f, 3f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));
            CreateBox("Crate_B", new Vector3(-6f, 1f, -2f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));
            CreateBox("Crate_C", new Vector3(2f, 1f, 9f), new Vector3(2f, 2f, 2f), new Color(0.35f, 0.3f, 0.25f));

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            light.color = new Color(0.8f, 0.78f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.12f, 0.12f, 0.16f);
            RenderSettings.fogDensity = 0.015f;
            RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.32f);
        }

        private PlayerAvatar BuildPlayer(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = position;
            Tint(go, new Color(0.75f, 0.75f, 0.85f));

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
            avatar.weaponHitbox = hitbox;

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
