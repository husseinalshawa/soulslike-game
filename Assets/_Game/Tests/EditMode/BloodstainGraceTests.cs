using NUnit.Framework;
using SoulsGame.Progression;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class BloodstainGraceTests
    {
        // ---- BloodstainLedger ----

        [Test]
        public void Drop_ZeroAmount_NoStain()
        {
            var ledger = new BloodstainLedger();
            ledger.Drop(new Vector3(1, 0, 1), 0);
            Assert.IsFalse(ledger.HasStain);
        }

        [Test]
        public void Drop_ReplacesOldStain_OldAmountLost()
        {
            var ledger = new BloodstainLedger();
            ledger.Drop(new Vector3(1, 0, 1), 500);
            ledger.Drop(new Vector3(9, 0, 9), 70); // موت ثانٍ قبل الاستعادة

            Assert.AreEqual(70, ledger.Amount, "القديم ضاع نهائياً");
            Assert.AreEqual(9f, ledger.Position.x);
        }

        [Test]
        public void TryRecover_WithinRadius_ReturnsAndClears()
        {
            var ledger = new BloodstainLedger();
            ledger.Drop(new Vector3(5, 0, 5), 300);

            Assert.AreEqual(300, ledger.TryRecover(new Vector3(5.5f, 0, 5), 1.5f));
            Assert.IsFalse(ledger.HasStain);
        }

        [Test]
        public void TryRecover_TooFar_ReturnsZeroKeepsStain()
        {
            var ledger = new BloodstainLedger();
            ledger.Drop(new Vector3(5, 0, 5), 300);

            Assert.AreEqual(0, ledger.TryRecover(new Vector3(10, 0, 5), 1.5f));
            Assert.IsTrue(ledger.HasStain);
        }

        // ---- GraceSystem ----

        private static GraceSystem NewGraces()
        {
            var g = new GraceSystem();
            g.AddSite(0, new Vector3(0, 0, -12), discovered: true);
            g.AddSite(1, new Vector3(0, 0, 14), discovered: false);
            return g;
        }

        [Test]
        public void Discover_WithinRange_Once()
        {
            var g = NewGraces();

            Assert.AreEqual(-1, g.TryDiscover(new Vector3(0, 0, 5)), "بعيد عن الموقعين");
            Assert.AreEqual(1, g.TryDiscover(new Vector3(0, 0, 13)), "ضمن 2م من الموقع 1");
            Assert.AreEqual(-1, g.TryDiscover(new Vector3(0, 0, 13)), "مكتشف مسبقاً — لا يتكرر");
        }

        [Test]
        public void CanRest_RequiresDiscovered()
        {
            var g = NewGraces();

            Assert.IsFalse(g.CanRest(new Vector3(0, 0, 14), out _), "غير مكتشف");
            g.TryDiscover(new Vector3(0, 0, 14));
            Assert.IsTrue(g.CanRest(new Vector3(0, 0, 14), out int siteId));
            Assert.AreEqual(1, siteId);
        }

        [Test]
        public void RestAt_SetsRespawn()
        {
            var g = NewGraces();
            Assert.AreEqual(new Vector3(0, 0, -12), g.RespawnPosition, "الافتراضي: الموقع 0");

            g.TryDiscover(new Vector3(0, 0, 14));
            g.RestAt(1);
            Assert.AreEqual(new Vector3(0, 0, 14), g.RespawnPosition);
        }

        [Test]
        public void Rest_DoesNotTouchStain()
        {
            var ledger = new BloodstainLedger();
            ledger.Drop(new Vector3(3, 0, 3), 400);
            var g = NewGraces();

            g.RestAt(0); // الراحة لا تلغي البقعة

            Assert.IsTrue(ledger.HasStain);
            Assert.AreEqual(400, ledger.Amount);
        }
    }
}
