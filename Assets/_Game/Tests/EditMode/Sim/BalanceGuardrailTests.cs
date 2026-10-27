using NUnit.Framework;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Tests.Sim
{
    // حواجز الموازنة (مواصفة المرحلة 6 §3) — سولز كلاسيكية.
    // تعمل على المحاكي الحتمي: كسرها يعني انحراف أرقام لا كسر كود.
    public class BalanceGuardrailTests
    {
        private static WeaponData[] AllWeapons() => new[]
        {
            GearLibrary.Longsword(), GearLibrary.Greatsword(), GearLibrary.TwinDaggers(),
            GearLibrary.Spear(), GearLibrary.Warhammer()
        };

        private static CharacterStats SoldierStats()
        {
            var s = ScriptableObject.CreateInstance<CharacterStats>();
            s.maxHealth = 150f; s.maxPoise = 30f;
            return s;
        }

        private static WeaponData SoldierWeapon()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.lightDamage = 20f; w.lightPoiseDamage = 20f; w.lightStaminaCost = 0f;
            w.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            return w;
        }

        private static CharacterStats BossStats()
        {
            var s = ScriptableObject.CreateInstance<CharacterStats>();
            s.maxHealth = 1000f; s.maxPoise = 120f;
            return s;
        }

        [Test]
        public void Guardrail1_EveryWeaponKillsSoldier_3to25s()
        {
            foreach (var weapon in AllWeapons())
            {
                var result = CombatSim.PlayerVsEnemy(weapon, GearLibrary.Cloth(),
                    SoldierStats(), SoldierWeapon());
                Assert.IsTrue(result.PlayerWon, $"{weapon.weaponName} ينتصر على الجندي");
                Assert.GreaterOrEqual(result.Seconds, 3f, $"{weapon.weaponName} ليس سحقاً فورياً");
                Assert.LessOrEqual(result.Seconds, 25f, $"{weapon.weaponName} لا يطيل بلا معنى");
            }
        }

        // حكم مسجل: الهيمنة تقاس على نزال البوس (اختبار الجدارة العام ≤2.5)؛
        // ضد الجنود التخصص مقصود (المطرقة تقفل كسرهم) فسقفه أرخى (≤6).
        [Test]
        public void Guardrail2_WeaponSpread_BossAtMost2_5x_SoldierAtMost6x()
        {
            float bossFastest = float.MaxValue, bossSlowest = 0f;
            float soldierFastest = float.MaxValue, soldierSlowest = 0f;
            foreach (var weapon in AllWeapons())
            {
                float boss = CombatSim.PlayerVsBoss(weapon, GearLibrary.Cloth(), BossStats()).Seconds;
                bossFastest = Mathf.Min(bossFastest, boss);
                bossSlowest = Mathf.Max(bossSlowest, boss);

                float soldier = CombatSim.PlayerVsEnemy(weapon, GearLibrary.Cloth(),
                    SoldierStats(), SoldierWeapon()).Seconds;
                soldierFastest = Mathf.Min(soldierFastest, soldier);
                soldierSlowest = Mathf.Max(soldierSlowest, soldier);
            }
            Assert.LessOrEqual(bossSlowest / bossFastest, 2.5f,
                "تمايز لا هيمنة في نزال الجدارة — البوس");
            Assert.LessOrEqual(soldierSlowest / soldierFastest, 6f,
                "وحتى ضد الجنود التخصص لا ينقلب سحقاً");
        }

        [Test]
        public void Guardrail3_BossTTK_Longsword_45to240s()
        {
            var result = CombatSim.PlayerVsBoss(GearLibrary.Longsword(), GearLibrary.Cloth(),
                BossStats());
            Assert.IsTrue(result.PlayerWon, "اللعب النظيف ينتصر على الحارس");
            Assert.GreaterOrEqual(result.Seconds, 45f, "البوس ليس وجبة خفيفة");
            Assert.LessOrEqual(result.Seconds, 240f, "ولا حائط مللٍ");
        }

        [Test]
        public void Guardrail4_PlayerHitsToDie_Classic()
        {
            int vsSoldier = CombatSim.HitsToDie(20f, GearLibrary.Cloth());
            Assert.IsTrue(vsSoldier == 5 || vsSoldier == 6,
                $"5-6 ضربات جندي تقتل بالقماش (الفعلي: {vsSoldier})");
            Assert.AreEqual(4, CombatSim.HitsToDie(30f, GearLibrary.Cloth()),
                "4 ضربات مرتّل");
            Assert.AreEqual(3, CombatSim.HitsToDie(40f, GearLibrary.Cloth()),
                "3 ساحقات بوس");
        }

        [Test]
        public void Guardrail5_PlateAddsAtLeast2Hits()
        {
            int cloth = CombatSim.HitsToDie(20f, GearLibrary.Cloth());
            int plate = CombatSim.HitsToDie(20f, GearLibrary.Plate());
            Assert.GreaterOrEqual(plate - cloth, 2,
                "الصفائح تشترى بثمنها — ضربتان إضافيتان على الأقل");
        }
    }
}
