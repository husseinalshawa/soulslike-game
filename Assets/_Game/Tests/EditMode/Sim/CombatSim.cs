using SoulsGame.Combat;
using SoulsGame.Data;
using SoulsGame.Enemies;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests.Sim
{
    // محاكي نزالات حتمي (كود اختبارات فقط): يدير العقول الحقيقية تكّة-تكّة
    // بسياسة لاعب نظيفة — تفادٍ موقوت قبل الصدمة، عقاب في الاستراحة —
    // والمسافة مجردة: الضربة تلحق ما لم يكن الخصم في i-frames.
    public static class CombatSim
    {
        private const float Dt = 1f / 60f;
        private const float TimeCap = 600f;
        // التفادي قبل الصدمة بـ0.25ث: نافذة الحصانة [+0.1, +0.4] تغطيها
        private const float DodgeLead = 0.25f;
        // اللاعب النظيف لا يصرف ستامينا التفادي على الهجوم أبداً
        private const float DodgeReserve = 20f; // كلفة التفادي بالضبط

        public struct DuelResult
        {
            public float Seconds;
            public bool PlayerWon;
            public int PlayerHitsTaken;
            public int StaminaStalls;
        }

        // عدد ضربات موت اللاعب (صحة 100) تحت تخفيض الدرع — حساب مباشر.
        public static int HitsToDie(float enemyDamage, ArmorData armor, float maxHealth = 100f)
        {
            float perHit = enemyDamage * (1f - armor.damageReduction);
            return Mathf.CeilToInt(maxHealth / perHit);
        }

        public static DuelResult PlayerVsEnemy(WeaponData weapon, ArmorData armor,
            CharacterStats enemyStats, WeaponData enemyWeapon)
        {
            var arena = new Arena(weapon, armor);
            // نظيف إن وسعت التصويبة أوسع نافذة (استراحة + إعلان − مهلة تفادٍ)
            arena.TradeMode = weapon.light.Total >
                enemyWeapon.light.recovery + enemyWeapon.light.windup - DodgeLead - 0.02f;
            var enemy = new EnemyBrain(enemyWeapon);
            var enemyHealth = new HealthPool(enemyStats.maxHealth);
            var enemyPoise = new PoiseMeter(enemyStats.maxPoise, 10f);

            arena.OnPlayerHitLands = (damage, poise) =>
            {
                if (enemyHealth.IsDead) return;
                enemyHealth.TakeDamage(damage);
                if (enemyHealth.IsDead) { enemy.NotifyDied(); return; }
                if (enemyPoise.TakePoiseDamage(poise)) enemy.NotifyPoiseBroken();
            };
            enemy.HitboxOpen += () => arena.EnemyStrikes(enemyWeapon.lightDamage);

            float impactLead = enemyWeapon.light.windup - DodgeLead;
            var window = new PunishTracker();
            while (!enemyHealth.IsDead && !arena.PlayerDead && arena.Time < TimeCap)
            {
                arena.PlanDodge(enemy.CurrentState == EnemyStateId.AttackWindup, impactLead);
                float remaining = window.Remaining(arena.Time,
                    enemy.CurrentState == EnemyStateId.AttackRecovery, enemyWeapon.light.recovery,
                    enemy.CurrentState == EnemyStateId.Stagger, 0.8f);
                arena.Step(remaining, enemyWeapon.light.windup);
                enemy.Tick(Dt, 2f, 0f);
                enemyPoise.Tick(Dt);
            }

            return arena.Result(!enemyHealth.IsDead ? false : !arena.PlayerDead);
        }

        public static DuelResult PlayerVsBoss(WeaponData weapon, ArmorData armor,
            CharacterStats bossStats)
        {
            var arena = new Arena(weapon, armor);
            var boss = new BossBrain();
            var bossHealth = new HealthPool(bossStats.maxHealth);
            var bossPoise = new PoiseMeter(bossStats.maxPoise, 10f);

            arena.OnPlayerHitLands = (damage, poise) =>
            {
                if (bossHealth.IsDead) return;
                bossHealth.TakeDamage(damage);
                if (bossHealth.IsDead) { boss.NotifyDied(); return; }
                if (bossPoise.TakePoiseDamage(poise)) boss.NotifyPoiseBroken();
            };
            // ضرر هجمات البوس من جدول BossAvatar المثبت
            boss.HitboxOpen += attack => arena.EnemyStrikes(
                attack == BossAttack.Smash ? 40f : attack == BossAttack.Sweep ? 25f : 35f);

            boss.Activate();
            float smashImpactLead = 0.9f - DodgeLead; // الساحقة (الوحيدة ضمن مدى 2م مع الأرضية)
            float slamImpactLead = 1.1f - DodgeLead;
            var window = new PunishTracker();
            while (!bossHealth.IsDead && !arena.PlayerDead && arena.Time < TimeCap)
            {
                bool windup = boss.CurrentState == BossStateId.Windup;
                arena.PlanDodge(windup,
                    boss.CurrentAttack == BossAttack.Slam ? slamImpactLead : smashImpactLead);
                float recoveryDuration = boss.CurrentAttack == BossAttack.Smash ? 1.6f
                    : boss.CurrentAttack == BossAttack.Sweep ? 1.2f : 1.4f;
                float remaining = window.Remaining(arena.Time,
                    boss.CurrentState == BossStateId.Recovery, recoveryDuration,
                    boss.CurrentState == BossStateId.Stagger, 0.8f);
                arena.Step(remaining, 0.9f); // أقصر إعلانات البوس (الساحقة)
                boss.Tick(Dt, 2f);
                bossPoise.Tick(Dt);
            }

            return arena.Result(!bossHealth.IsDead ? false : !arena.PlayerDead);
        }

        // تتبع نافذة العقاب: متى بدأت وكم بقي منها.
        private class PunishTracker
        {
            private float startedAt = -1f;

            public float Remaining(float now, bool inRecovery, float recoveryDuration,
                bool inStagger, float staggerDuration)
            {
                if (!inRecovery && !inStagger)
                {
                    startedAt = -1f;
                    return 0f;
                }
                if (startedAt < 0f)
                    startedAt = now;
                float duration = inStagger ? staggerDuration : recoveryDuration;
                return duration - (now - startedAt);
            }
        }

        // الساحة المشتركة: عقل اللاعب ومجمّعاته وسياسته.
        private class Arena
        {
            private readonly WeaponData weapon;
            private readonly ArmorData armor;
            private readonly PlayerBrain brain;
            private readonly StaminaPool stamina;
            private readonly HealthPool health;

            private bool invulnerable;
            private float dodgeAt = -1f;
            private float windupStartedAt = -1f;
            private bool stalledThisWindow;
            private bool comboRequested;
            private float attackEndsAt;

            public float Time { get; private set; }
            public bool PlayerDead => health.IsDead;
            public bool TradeMode { get; set; }
            public System.Action<float, float> OnPlayerHitLands;

            private int hitsTaken;
            private int staminaStalls;

            public Arena(WeaponData playerWeapon, ArmorData playerArmor)
            {
                weapon = playerWeapon;
                armor = playerArmor;
                var stats = ScriptableObject.CreateInstance<CharacterStats>();
                stamina = new StaminaPool(stats.maxStamina, stats.staminaRegenPerSecond,
                    stats.staminaRegenDelay, stats.blockingRegenMultiplier);
                brain = new PlayerBrain(stamina, playerWeapon, stats, new FocusPool(60f));
                brain.SetDodgeProfile(playerArmor.dodgeDurationMultiplier);
                health = new HealthPool(stats.maxHealth);

                brain.InvulnerabilityChanged += v => invulnerable = v;
                brain.HitboxOpen += type =>
                {
                    float damage = type == AttackType.Heavy ? weapon.heavyDamage : weapon.lightDamage;
                    float poise = type == AttackType.Heavy ? weapon.heavyPoiseDamage : weapon.lightPoiseDamage;
                    OnPlayerHitLands?.Invoke(damage, poise);
                };
            }

            // ضربة خصم تصل الآن: i-frames تنجّي، وإلا ضرر مخفض بالدرع.
            public void EnemyStrikes(float damage)
            {
                if (invulnerable || health.IsDead)
                    return;
                health.TakeDamage(damage * (1f - armor.damageReduction));
                hitsTaken++;
                if (brain.CurrentState != PlayerStateId.Death && health.IsDead)
                    brain.NotifyDied();
            }

            // جدولة تفادٍ واحد لكل إعلان: قبل الصدمة بـDodgeLead.
            // خروج الخصم من الإعلان (ضُرب توازنه مثلاً) يلغي الجدولة كلياً —
            // وإلا بقي تفادٍ يتيم يمنع جدولة الإعلان التالي.
            public void PlanDodge(bool enemyInWindup, float impactLeadFromWindupStart)
            {
                if (enemyInWindup)
                {
                    if (windupStartedAt < 0f)
                    {
                        windupStartedAt = Time;
                        dodgeAt = Time + Mathf.Max(0.02f, impactLeadFromWindupStart);
                    }
                    return;
                }
                windupStartedAt = -1f;
                dodgeAt = -1f;
            }

            // punishRemaining: الزمن المتبقي من نافذة العقاب (≤0 = لا نافذة) —
            // اللاعب النظيف لا يبدأ ضربة لا تكتمل قبل إعلان الخصم التالي.
            public void Step(float punishRemaining, float nextWindup)
            {
                if (dodgeAt >= 0f && Time >= dodgeAt)
                {
                    brain.RequestDodge();
                    dodgeAt = -1f;
                }

                bool punishWindow = punishRemaining > 0f;
                // ميزانية الضربة: بقية النافذة + إعلان الخصم التالي − مهلة التفادي —
                // هكذا تدخل الأسلحة الثقيلة نوافذ أقصر من تصويبتها (كما يفعل اللاعبون)
                float budget = punishRemaining + nextWindup - DodgeLead - 0.02f;
                bool idle = brain.CurrentState == PlayerStateId.Locomotion;
                bool inLightAttack = brain.CurrentState == PlayerStateId.AttackLight;
                if (!inLightAttack)
                    comboRequested = false;
                // تسلسل الكومبو: أثناء الضربة يُطلب التالي (مرة) إن انتهى
                // التسلسل الممدد قبل موعد التفادي — الخناجر تعيش على هذا
                bool comboing = inLightAttack && punishWindow && !comboRequested &&
                                (attackEndsAt - Time) + weapon.light.Total <= budget;
                // وضع المقايضة: سلاح لا يسع أي نافذة نظيفة يُلوَّح به متى أمكن
                // ويقبل التبادل — أسلوب المطرقة الحقيقي (الكسر يعوض الضربات المقبوضة)
                bool wantsAttack = TradeMode
                    ? idle
                    : (idle && punishWindow && weapon.light.Total <= budget) || comboing;
                if (wantsAttack)
                {
                    if (stamina.Current >= weapon.lightStaminaCost + DodgeReserve)
                    {
                        brain.RequestLightAttack();
                        if (comboing)
                        {
                            comboRequested = true;
                            attackEndsAt += weapon.light.Total;
                        }
                        else
                        {
                            attackEndsAt = Time + weapon.light.Total;
                        }
                        stalledThisWindow = false;
                    }
                    else if (!stalledThisWindow)
                    {
                        staminaStalls++;
                        stalledThisWindow = true;
                    }
                }
                if (!punishWindow)
                    stalledThisWindow = false;

                brain.Tick(Dt);
                stamina.Tick(Dt, brain.IsBlocking, false);
                Time += Dt;
            }

            public DuelResult Result(bool won) => new DuelResult
            {
                Seconds = Time,
                PlayerWon = won,
                PlayerHitsTaken = hitsTaken,
                StaminaStalls = staminaStalls
            };
        }
    }
}
