using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Enemies
{
    // غلاف العدو: يملك EnemyBrain وDamageable وHitbox، يتحرك حسب الحالة،
    // ويُنفّذ ITargetable ليكون هدفاً للـ lock-on.
    [RequireComponent(typeof(Damageable))]
    public class EnemyAvatar : MonoBehaviour, ITargetable
    {
        // مكبوح أثناء قائمة رفع المستوى: لا اكتشاف للاعب الجالس العاجز عن الرد
        public static bool AggroSuppressed;

        // سرعة المطاردة من إحصاءات النوع: جنود 5، المرتّلون الغارقون 2.5 (أبطأ وأقسى)
        public float ChaseSpeed => stats != null ? stats.runSpeed : 5f;
        private const float PatrolSpeed = 1.5f;
        private const float TurnSpeedDegrees = 360f;
        private const float PatrolPointTolerance = 0.5f;

        public CharacterStats stats;
        public WeaponData weapon;
        public Hitbox weaponHitbox;
        public Transform player;
        public Vector3 patrolA;
        public Vector3 patrolB;
        public Color baseTint = new Color(0.6f, 0.2f, 0.2f); // لون النوع (جندي/مرتّل)

        public EnemyBrain Brain { get; private set; }
        public Damageable Damageable { get; private set; }

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 currentPatrolGoal;
        private CharacterController controller;

        // ITargetable — نقطة القفل في منتصف الجسم
        public Vector3 Position => transform.position + Vector3.up * 1f;
        public bool IsAlive => Damageable != null && !Damageable.Health.IsDead;

        private void Awake()
        {
            Damageable = GetComponent<Damageable>();
            controller = GetComponent<CharacterController>();
            Brain = new EnemyBrain(weapon);

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            currentPatrolGoal = patrolB;

            Brain.HitboxOpen += () =>
            {
                if (weaponHitbox != null)
                    weaponHitbox.Activate(new DamagePacket(weapon.lightDamage, weapon.lightPoiseDamage,
                        transform.position));
            };
            Brain.HitboxClose += () =>
            {
                if (weaponHitbox != null) weaponHitbox.Deactivate();
            };

            Damageable.PoiseBroken += () => Brain.NotifyPoiseBroken();
            Damageable.Died += OnDied;
        }

        private void OnEnable() => LockOnSystem.Register(this);

        private void OnDisable() => LockOnSystem.Unregister(this);

        private void OnDied()
        {
            Brain.NotifyDied();
            // الجثة تبقى لكنها ليست هدفاً ولا عائقاً
            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            GameBootstrapTint();
        }

        private void GameBootstrapTint()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.15f, 0.1f, 0.1f);
        }

        private void Update()
        {
            if (player == null)
                return;

            float dt = Time.deltaTime;
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            float angle = Vector3.Angle(PlanarForward(), toPlayer);

            if (AggroSuppressed && Brain.CurrentState == EnemyStateId.Patrol)
                distance = float.MaxValue; // لا اكتشاف أثناء القائمة

            Vector3 fromHome = spawnPosition - transform.position;
            fromHome.y = 0f;
            Brain.Tick(dt, distance, angle, fromHome.magnitude);

            switch (Brain.CurrentState)
            {
                case EnemyStateId.Patrol:
                    PatrolMove(dt);
                    break;
                case EnemyStateId.Chase:
                    MoveTowards(player.position, ChaseSpeed, dt);
                    break;
                case EnemyStateId.Returning:
                    MoveTowards(spawnPosition, PatrolSpeed, dt);
                    break;
                case EnemyStateId.AttackWindup:
                    // الإعلان المسبق: يواصل تعقب اللاعب بالدوران فقط (مقروء وعادل)
                    FaceTowards(toPlayer, dt);
                    break;
                // Active/Recovery/Stagger/Dead: ملتزم — لا حركة ولا دوران
            }
        }

        private void PatrolMove(float dt)
        {
            Vector3 toGoal = currentPatrolGoal - transform.position;
            toGoal.y = 0f;
            if (toGoal.magnitude < PatrolPointTolerance)
            {
                currentPatrolGoal = currentPatrolGoal == patrolA ? patrolB : patrolA;
                return;
            }
            MoveTowards(currentPatrolGoal, PatrolSpeed, dt);
        }

        private void MoveTowards(Vector3 goal, float speed, float dt)
        {
            Vector3 direction = goal - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            direction = direction.normalized;
            if (controller != null && controller.enabled)
                controller.SimpleMove(direction * speed); // يصطدم بالجدران والصناديق واللاعب
            else
                transform.position += direction * speed * dt;
            FaceTowards(direction, dt);
        }

        private void FaceTowards(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeedDegrees * dt);
        }

        private Vector3 PlanarForward()
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f;
        }

        public void ResetToSpawn()
        {
            // CharacterController مفعّل قد يتجاهل تغيير الموضع المباشر — عطّله أولاً
            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            if (bodyCollider != null) bodyCollider.enabled = true;

            currentPatrolGoal = patrolB;
            Damageable.ResetToFull();
            Brain.ResetToPatrol();
            if (weaponHitbox != null) weaponHitbox.Deactivate();
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = baseTint;
            LockOnSystem.Register(this);
        }
    }
}
