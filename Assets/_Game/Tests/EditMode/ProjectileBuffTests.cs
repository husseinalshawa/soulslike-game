using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.Combat;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class ProjectileBuffTests
    {
        private static List<(object target, Vector3 position, float radius)> Targets(
            params (object, Vector3, float)[] items) =>
            new List<(object, Vector3, float)>(items);

        [Test]
        public void Projectile_HitsNearestInPath()
        {
            var near = new object();
            var far = new object();
            var p = new ProjectileLogic(Vector3.zero, Vector3.forward, speed: 20f, maxRange: 30f);
            var targets = Targets((far, new Vector3(0, 0, 10f), 0.6f), (near, new Vector3(0, 0, 5f), 0.6f));

            object hit = null;
            for (int i = 0; i < 60 && hit == null && !p.Expired; i++)
                hit = p.Step(0.05f, targets);

            Assert.AreSame(near, hit, "الأقرب في المسار يُصاب أولاً");
        }

        [Test]
        public void Projectile_ExpiresAtRange()
        {
            var p = new ProjectileLogic(Vector3.zero, Vector3.forward, 20f, 30f);
            for (int i = 0; i < 100 && !p.Expired; i++)
                p.Step(0.05f, Targets());

            Assert.IsTrue(p.Expired);
            Assert.LessOrEqual(p.Position.z, 31f, "تتوقف عند المدى");
        }

        [Test]
        public void Projectile_MissesOffPath()
        {
            var side = new object();
            var p = new ProjectileLogic(Vector3.zero, Vector3.forward, 20f, 30f);
            var targets = Targets((side, new Vector3(5f, 0, 10f), 0.6f)); // بعيد جانبياً

            object hit = null;
            for (int i = 0; i < 100 && !p.Expired; i++)
            {
                var h = p.Step(0.05f, targets);
                if (h != null) hit = h;
            }

            Assert.IsNull(hit);
        }

        [Test]
        public void Buff_ExpiresAndRefreshes()
        {
            var buff = new WeaponBuff();
            buff.Activate(10f, 30f);
            Assert.AreEqual(10f, buff.CurrentBonus);

            buff.Tick(29f);
            buff.Activate(10f, 30f); // تجديد قبل الانتهاء
            buff.Tick(29f);
            Assert.AreEqual(10f, buff.CurrentBonus, "التجديد أعاد المدة");

            buff.Tick(2f);
            Assert.AreEqual(0f, buff.CurrentBonus, "انتهى");
        }

        [Test]
        public void Buff_ClearOnDeath()
        {
            var buff = new WeaponBuff();
            buff.Activate(10f, 30f);
            buff.Clear();
            Assert.AreEqual(0f, buff.CurrentBonus);
        }
    }
}
