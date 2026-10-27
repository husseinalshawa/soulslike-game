using System;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Combat
{
    // الواجهة الموحدة لاستقبال الضرر — يستخدمها اللاعب والأعداء بالتساوي.
    public class Damageable : MonoBehaviour
    {
        public CharacterStats stats;

        public HealthPool Health { get; private set; }
        public PoiseMeter Poise { get; private set; }

        // تُفعَّل أثناء i-frames التفادي: الضرر يُتجاهل بالكامل.
        public bool IsInvulnerable { get; set; }

        public event Action<DamagePacket> Damaged;
        public event Action PoiseBroken;
        public event Action Died;

        private void Awake()
        {
            Health = new HealthPool(stats.maxHealth);
            Poise = new PoiseMeter(stats.maxPoise, stats.poiseRegenPerSecond);
            Health.Died += () => Died?.Invoke();
        }

        private void Update()
        {
            Poise.Tick(Time.deltaTime);
        }

        public void ApplyDamage(DamagePacket packet)
        {
            if (IsInvulnerable || Health.IsDead)
                return;

            Health.TakeDamage(packet.HealthDamage);
            Damaged?.Invoke(packet);

            if (!Health.IsDead && Poise.TakePoiseDamage(packet.PoiseDamage))
                PoiseBroken?.Invoke();
        }

        public void ResetToFull()
        {
            Health.ResetToFull();
            Poise.Reset();
            IsInvulnerable = false;
        }
    }
}
