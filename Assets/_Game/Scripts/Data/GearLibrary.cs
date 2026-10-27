using UnityEngine;

namespace SoulsGame.Data
{
    // مصدر أرقام العتاد الواحد (جداول المواصفة §2/§5/§6) — يستعمله بناء
    // المشهد وقائمة المحرر معاً، ومثبت بالاختبارات.
    public static class GearLibrary
    {
        // ---- الأسلحة ----

        public static WeaponData Longsword()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>(); // افتراضيات المرحلة 1
            w.weaponName = "Longsword";
            w.reach = 1.6f;
            w.skill = new SkillData
            {
                skillName = "Piercing Dash",
                fpCost = 15f,
                timeline = new AttackTimeline(0.30f, 0.25f, 0.55f, 0.80f),
                damage = 40f,
                poiseDamage = 30f,
                dashDistance = 4f,
                radius = 0f,
                hitCount = 1
            };
            return w;
        }

        public static WeaponData Greatsword()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.weaponName = "Greatsword";
            w.reach = 2.0f;
            w.hitboxWidth = 0.4f;
            w.lightDamage = 45f; w.lightPoiseDamage = 35f; w.lightStaminaCost = 22f;
            w.light = new AttackTimeline(0.45f, 0.30f, 0.70f, 0.85f);
            w.heavyDamage = 85f; w.heavyPoiseDamage = 60f; w.heavyStaminaCost = 38f;
            w.heavy = new AttackTimeline(0.80f, 0.30f, 1.00f, 1.30f);
            w.comboLength = 2;
            w.skill = new SkillData
            {
                skillName = "Quake",
                fpCost = 25f,
                timeline = new AttackTimeline(0.60f, 0.25f, 0.90f, 1.20f),
                damage = 55f,
                poiseDamage = 70f,
                dashDistance = 0f,
                radius = 3f,
                hitCount = 1
            };
            return w;
        }

        public static WeaponData TwinDaggers()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.weaponName = "Twin Daggers";
            w.reach = 1.0f;
            w.hitboxWidth = 0.18f;
            w.lightDamage = 12f; w.lightPoiseDamage = 6f; w.lightStaminaCost = 8f;
            w.light = new AttackTimeline(0.15f, 0.12f, 0.25f, 0.30f);
            w.heavyDamage = 30f; w.heavyPoiseDamage = 15f; w.heavyStaminaCost = 18f;
            w.heavy = new AttackTimeline(0.35f, 0.20f, 0.45f, 0.60f);
            w.comboLength = 5;
            w.skill = new SkillData
            {
                skillName = "Flurry",
                fpCost = 18f,
                timeline = new AttackTimeline(0.25f, 0.70f, 0.50f, 1.10f),
                damage = 15f,
                poiseDamage = 8f,
                dashDistance = 0f,
                radius = 0f,
                hitCount = 3
            };
            return w;
        }

        public static WeaponData Spear()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.weaponName = "Spear";
            w.reach = 2.4f;
            w.hitboxWidth = 0.15f;
            w.lightDamage = 22f; w.lightPoiseDamage = 12f; w.lightStaminaCost = 14f;
            w.light = new AttackTimeline(0.30f, 0.22f, 0.45f, 0.60f);
            w.heavyDamage = 40f; w.heavyPoiseDamage = 25f; w.heavyStaminaCost = 26f;
            w.heavy = new AttackTimeline(0.55f, 0.25f, 0.70f, 0.90f);
            w.comboLength = 3;
            w.skill = new SkillData
            {
                skillName = "Piercing Charge",
                fpCost = 15f,
                timeline = new AttackTimeline(0.35f, 0.30f, 0.60f, 0.90f),
                damage = 45f,
                poiseDamage = 35f,
                dashDistance = 2f,
                radius = 0f,
                hitCount = 1
            };
            return w;
        }

        public static WeaponData Warhammer()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.weaponName = "Warhammer";
            w.reach = 1.5f;
            w.hitboxWidth = 0.45f;
            w.lightDamage = 50f; w.lightPoiseDamage = 45f; w.lightStaminaCost = 26f;
            w.light = new AttackTimeline(0.55f, 0.28f, 0.85f, 1.00f);
            w.heavyDamage = 95f; w.heavyPoiseDamage = 80f; w.heavyStaminaCost = 42f;
            w.heavy = new AttackTimeline(0.95f, 0.30f, 1.20f, 1.50f);
            w.comboLength = 2;
            w.skill = new SkillData
            {
                skillName = "Skyfall",
                fpCost = 25f,
                timeline = new AttackTimeline(0.70f, 0.25f, 1.00f, 1.30f),
                damage = 70f,
                poiseDamage = 100f,
                dashDistance = 0f,
                radius = 2.5f,
                hitCount = 1
            };
            return w;
        }

        // ---- الدروع ----

        public static ArmorData Cloth()
        {
            var a = ScriptableObject.CreateInstance<ArmorData>();
            a.armorName = "Wanderer Cloth";
            a.damageReduction = 0f;
            a.dodgeDurationMultiplier = 1.00f;
            a.dodgeDistanceMultiplier = 1.00f;
            a.tint = new Color(0.75f, 0.75f, 0.85f);
            return a;
        }

        public static ArmorData Leather()
        {
            var a = ScriptableObject.CreateInstance<ArmorData>();
            a.armorName = "Stalker Leather";
            a.damageReduction = 0.15f;
            a.dodgeDurationMultiplier = 1.10f;
            a.dodgeDistanceMultiplier = 0.95f;
            a.tint = new Color(0.5f, 0.4f, 0.3f);
            return a;
        }

        public static ArmorData Plate()
        {
            var a = ScriptableObject.CreateInstance<ArmorData>();
            a.armorName = "Warden Plate";
            a.damageReduction = 0.30f;
            a.dodgeDurationMultiplier = 1.30f;
            a.dodgeDistanceMultiplier = 0.85f;
            a.tint = new Color(0.55f, 0.55f, 0.62f);
            return a;
        }

        // ---- التعاويذ ----

        public static SpellData GlintBolt()
        {
            var s = ScriptableObject.CreateInstance<SpellData>();
            s.spellName = "Glint Bolt";
            s.isBuff = false;
            s.fpCost = 12f;
            s.damage = 35f;
            s.poiseDamage = 10f;
            s.projectileSpeed = 20f;
            s.maxRange = 30f;
            return s;
        }

        public static SpellData WeaponBuffSpell()
        {
            var s = ScriptableObject.CreateInstance<SpellData>();
            s.spellName = "Glintblade Edge";
            s.isBuff = true;
            s.fpCost = 20f;
            s.buffBonusDamage = 10f;
            s.buffDuration = 30f;
            return s;
        }
    }
}
