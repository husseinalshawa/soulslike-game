using System;
using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Enemies
{
    // غلاف «حارس الساحة»: يعيد استخدام Damageable/Hitbox، ويترجم BossBrain
    // إلى حركة وضرر. الضربة الأرضية (Slam) نصف قطرية حول البوس.
    [RequireComponent(typeof(Damageable))]
    public class BossAvatar : MonoBehaviour, ITargetable
    {
        private const float TurnSpeedDegrees = 180f;
        private const float SlamRadius = 4f;
        private const float StaggerPoiseMultiplier = 1.5f;
        private const float StaggerPoiseWindowSeconds = 10f;

        public CharacterStats stats;
        public Hitbox weaponHitbox;
        public Transform player;
        public Damageable playerDamageable;

        public BossBrain Brain { get; } = new BossBrain();
        public Damageable Damageable { get; private set; }
        public bool DefeatedForever { get; private set; }
        public bool FightActive =>
            Brain.CurrentState != BossStateId.Dormant && Brain.CurrentState != BossStateId.Dead;

        public event Action Defeated;
        public event Action SlamLanded; // الأرضية الدائرية أصابت فعلاً — اهتزاز كبير

        private CharacterController controller;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float poiseWindowRemaining;

        private void Awake()
        {
            Damageable = GetComponent<Damageable>();
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            Brain.HitboxOpen += OnHitboxOpen;
            Brain.HitboxClose += () =>
            {
                if (weaponHitbox != null) weaponHitbox.Deactivate();
            };
            Brain.StaggerEnded += () => poiseWindowRemaining = StaggerPoiseWindowSeconds;

            Damageable.PoiseBroken += () => Brain.NotifyPoiseBroken();
            Damageable.Died += OnDied;

            var flash = gameObject.AddComponent<Feel.HitFlash>();
            var bossRenderer = GetComponent<Renderer>();
            flash.target = bossRenderer;
            flash.baseColor = bossRenderer != null ? bossRenderer.material.color : Color.white;
            flash.owner = Damageable;
            Damageable.Damaged += _ => flash.Begin();
        }

        private (float damage, float poise) PacketFor(BossAttack attack) => attack switch
        {
            BossAttack.Smash => (40f, 40f),
            BossAttack.Sweep => (25f, 25f),
            _ => (35f, 30f) // Slam
        };

        private void OnHitboxOpen(BossAttack attack)
        {
            var (damage, poise) = PacketFor(attack);
            var packet = new DamagePacket(damage, poise, transform.position);

            if (attack == BossAttack.Slam)
            {
                // نصف قطرية: الابتعاد هو النجاة — لكنها تمر عبر ApplyDamage
                // فتحترم i-frames والصد ككل ضربة أخرى
                if (playerDamageable != null && player != null &&
                    Vector3.Distance(transform.position, player.position) <= SlamRadius)
                {
                    float before = playerDamageable.Health.Current;
                    playerDamageable.ApplyDamage(packet);
                    if (playerDamageable.Health.Current < before)
                        SlamLanded?.Invoke();
                }
                return;
            }

            if (weaponHitbox != null)
                weaponHitbox.Activate(packet);
        }

        private void OnDied()
        {
            Brain.NotifyDied();
            DefeatedForever = true;
            if (weaponHitbox != null) weaponHitbox.Deactivate();
            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.12f, 0.1f, 0.14f);
            Defeated?.Invoke();
        }

        // يبدأ المعركة (يستدعيه جدار الضباب أو الاختبارات).
        public void BeginFight() => Brain.Activate();

        public void ResetFight()
        {
            if (DefeatedForever)
                return;
            Brain.ResetFight();
            Damageable.ResetToFull();
            Damageable.Poise.SetThresholdMultiplier(1f);
            poiseWindowRemaining = 0f;
            if (weaponHitbox != null) weaponHitbox.Deactivate();

            var bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            if (bodyCollider != null) bodyCollider.enabled = true;
        }

        // ITargetable
        public Vector3 Position => transform.position + Vector3.up * 1.5f;
        public bool IsAlive => Damageable != null && !Damageable.Health.IsDead;

        private void OnEnable() => LockOnSystem.Register(this);
        private void OnDisable() => LockOnSystem.Unregister(this);

        private void Update()
        {
            if (player == null)
                return;

            float dt = Time.deltaTime;

            if (poiseWindowRemaining > 0f)
            {
                Damageable.Poise.SetThresholdMultiplier(StaggerPoiseMultiplier);
                poiseWindowRemaining -= dt;
                if (poiseWindowRemaining <= 0f)
                    Damageable.Poise.SetThresholdMultiplier(1f);
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            Brain.Tick(dt, toPlayer.magnitude);

            switch (Brain.CurrentState)
            {
                case BossStateId.Approach:
                    if (toPlayer.sqrMagnitude > 0.01f)
                    {
                        Vector3 dir = toPlayer.normalized;
                        if (controller != null && controller.enabled)
                            controller.SimpleMove(dir * stats.runSpeed);
                        FaceTowards(dir, dt);
                    }
                    break;
                case BossStateId.Windup:
                    FaceTowards(toPlayer, dt); // الإعلان يتعقب بالدوران فقط
                    break;
                // Active/Recovery/Stagger/Dead: ملتزم
            }
        }

        private void FaceTowards(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeedDegrees * dt);
        }
    }
}
