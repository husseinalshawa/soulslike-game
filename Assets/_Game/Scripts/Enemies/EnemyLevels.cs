using System.Collections.Generic;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Enemies
{
    // مستويات الوحوش: كل منطقة أبعد عن نقطة البداية أقسى من التي قبلها.
    // المستوى 1 = القيم الأصلية كما هي؛ ما فوقه نسخ مضخّمة من نفس الأصول،
    // فتبقى الموازنة كلها من ملفات البيانات والمضاعفات هنا فقط.
    //
    //   المستوى 1: الحصن (البداية)
    //   المستوى 2: المعسكر الشرقي والكهف
    //   المستوى 3: المعسكر الجنوبي البعيد والكنيسة الغارقة
    //   المستوى 4: نخبة الغابة المظلمة (أخطر ما في العالم قبل البوس)
    public static class EnemyLevels
    {
        public const int MinTier = 1;
        public const int MaxTier = 4;

        // مضاعفات لكل مستوى فوق الأول — مركّبة (أُسّية) حتى يحس اللاعب بالقفزة
        public const float HealthGrowth = 1.6f;
        public const float DamageGrowth = 1.3f;
        public const float PoiseGrowth = 1.3f;
        public const float SoulsGrowth = 1.8f;
        // الوحوش الأعلى تنتظر أقل بين الضربات (إعلان أقصر = رد فعل أسرع مطلوب)
        public const float WindupShrinkPerTier = 0.08f;
        public const float MinWindupFactor = 0.75f;

        public static int Clamp(int tier) => Mathf.Clamp(tier, MinTier, MaxTier);

        public static float HealthMultiplier(int tier) => Mathf.Pow(HealthGrowth, Clamp(tier) - 1);
        public static float DamageMultiplier(int tier) => Mathf.Pow(DamageGrowth, Clamp(tier) - 1);
        public static float PoiseMultiplier(int tier) => Mathf.Pow(PoiseGrowth, Clamp(tier) - 1);
        public static float SoulsMultiplier(int tier) => Mathf.Pow(SoulsGrowth, Clamp(tier) - 1);
        public static float WindupFactor(int tier) =>
            Mathf.Max(MinWindupFactor, 1f - WindupShrinkPerTier * (Clamp(tier) - 1));

        // الأرواح تُقرّب لأقرب 10 — أرقام نظيفة في العداد
        public static int ScaleSouls(int baseSouls, int tier) =>
            Mathf.RoundToInt(baseSouls * SoulsMultiplier(tier) / 10f) * 10;

        private static readonly Dictionary<(CharacterStats, int), CharacterStats> statsCache =
            new Dictionary<(CharacterStats, int), CharacterStats>();
        private static readonly Dictionary<(WeaponData, int), WeaponData> weaponCache =
            new Dictionary<(WeaponData, int), WeaponData>();

        // نسخة مضخّمة واحدة لكل (أصل، مستوى) — كل وحوش المنطقة تتشارك نفس الأرقام
        public static CharacterStats Scale(CharacterStats source, int tier)
        {
            tier = Clamp(tier);
            if (source == null || tier == MinTier)
                return source;
            if (statsCache.TryGetValue((source, tier), out var cached) && cached != null)
                return cached;

            var s = ScriptableObject.CreateInstance<CharacterStats>();
            s.name = $"{source.name}_L{tier}";
            s.maxHealth = source.maxHealth * HealthMultiplier(tier);
            s.maxStamina = source.maxStamina;
            s.maxPoise = source.maxPoise * PoiseMultiplier(tier);
            s.poiseRegenPerSecond = source.poiseRegenPerSecond * PoiseMultiplier(tier);
            s.staminaRegenPerSecond = source.staminaRegenPerSecond;
            s.staminaRegenDelay = source.staminaRegenDelay;
            s.blockingRegenMultiplier = source.blockingRegenMultiplier;
            s.walkSpeed = source.walkSpeed;
            s.runSpeed = source.runSpeed;
            s.sprintSpeed = source.sprintSpeed;
            s.sprintStaminaPerSecond = source.sprintStaminaPerSecond;
            s.dodgeStaminaCost = source.dodgeStaminaCost;
            s.soulsReward = ScaleSouls(source.soulsReward, tier);
            statsCache[(source, tier)] = s;
            return s;
        }

        public static WeaponData Scale(WeaponData source, int tier)
        {
            tier = Clamp(tier);
            if (source == null || tier == MinTier)
                return source;
            if (weaponCache.TryGetValue((source, tier), out var cached) && cached != null)
                return cached;

            float dmg = DamageMultiplier(tier);
            float poise = PoiseMultiplier(tier);
            float windup = WindupFactor(tier);
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.name = $"{source.name}_L{tier}";
            w.weaponName = source.weaponName;
            w.reach = source.reach;
            w.hitboxWidth = source.hitboxWidth;
            w.skill = source.skill;
            w.lightDamage = source.lightDamage * dmg;
            w.lightPoiseDamage = source.lightPoiseDamage * poise;
            w.lightStaminaCost = source.lightStaminaCost;
            w.light = Faster(source.light, windup);
            w.heavyDamage = source.heavyDamage * dmg;
            w.heavyPoiseDamage = source.heavyPoiseDamage * poise;
            w.heavyStaminaCost = source.heavyStaminaCost;
            w.heavy = Faster(source.heavy, windup);
            w.comboLength = source.comboLength;
            weaponCache[(source, tier)] = w;
            return w;
        }

        private static AttackTimeline Faster(AttackTimeline t, float windupFactor)
        {
            float newWindup = t.windup * windupFactor;
            float shift = t.windup - newWindup;
            // نافذة الإلغاء تتحرك مع الإعلان حتى تبقى بعد الطور النشط
            return new AttackTimeline(newWindup, t.active, t.recovery,
                Mathf.Max(newWindup + t.active, t.cancelWindowStart - shift));
        }
    }
}
