using NUnit.Framework;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.Tests.Sim
{
    // مُفرِغ تقرير الموازنة — يُشغَّل يدوياً بالفلتر (Explicit لا يدخل الحزمة):
    // dotnet test --filter BalanceReport_Dump
    public class BalanceReportDump
    {
        [Test, Explicit("تفريغ يدوي لجداول التقرير")]
        public void BalanceReport_Dump()
        {
            var weapons = new[]
            {
                GearLibrary.Longsword(), GearLibrary.Greatsword(), GearLibrary.TwinDaggers(),
                GearLibrary.Spear(), GearLibrary.Warhammer()
            };

            var soldier = ScriptableObject.CreateInstance<CharacterStats>();
            soldier.maxHealth = 150f; soldier.maxPoise = 30f;
            var soldierWeapon = ScriptableObject.CreateInstance<WeaponData>();
            soldierWeapon.lightDamage = 20f; soldierWeapon.lightPoiseDamage = 20f;
            soldierWeapon.lightStaminaCost = 0f;
            soldierWeapon.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);

            var chanter = ScriptableObject.CreateInstance<CharacterStats>();
            chanter.maxHealth = 200f; chanter.maxPoise = 40f;
            var chanterWeapon = ScriptableObject.CreateInstance<WeaponData>();
            chanterWeapon.lightDamage = 30f; chanterWeapon.lightPoiseDamage = 30f;
            chanterWeapon.lightStaminaCost = 0f;
            chanterWeapon.light = new AttackTimeline(0.8f, 0.3f, 1.2f, 1.2f);

            var boss = ScriptableObject.CreateInstance<CharacterStats>();
            boss.maxHealth = 1000f; boss.maxPoise = 120f;

            var cloth = GearLibrary.Cloth();
            System.Console.WriteLine("| Weapon | lightTotal | vsSoldier s (hits taken, stalls) | vsChanter s | vsBoss s |");
            foreach (var w in weapons)
            {
                var s = CombatSim.PlayerVsEnemy(w, cloth, Copy(soldier), soldierWeapon);
                var c = CombatSim.PlayerVsEnemy(w, cloth, Copy(chanter), chanterWeapon);
                var b = CombatSim.PlayerVsBoss(w, cloth, Copy(boss));
                System.Console.WriteLine(
                    $"| {w.weaponName} | {w.light.Total:0.00} | {s.Seconds:0.0} won={s.PlayerWon} (taken {s.PlayerHitsTaken}, stalls {s.StaminaStalls}) | {c.Seconds:0.0} won={c.PlayerWon} | {b.Seconds:0.0} won={b.PlayerWon} taken={b.PlayerHitsTaken} |");
            }

            System.Console.WriteLine("| Armor | hits to die vs 20 / 30 / 40 |");
            foreach (var a in new[] { GearLibrary.Cloth(), GearLibrary.Leather(), GearLibrary.Plate() })
                System.Console.WriteLine($"| {a.armorName} | {CombatSim.HitsToDie(20f, a)} / {CombatSim.HitsToDie(30f, a)} / {CombatSim.HitsToDie(40f, a)} |");
        }

        private static CharacterStats Copy(CharacterStats source)
        {
            var copy = ScriptableObject.CreateInstance<CharacterStats>();
            copy.maxHealth = source.maxHealth;
            copy.maxPoise = source.maxPoise;
            return copy;
        }
    }
}
