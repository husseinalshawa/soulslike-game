using NUnit.Framework;
using SoulsGame.Combat;
using SoulsGame.Core;
using SoulsGame.Feel;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class FeelWiringTests
    {
        private static GameBootstrap Build()
        {
            var go = new GameObject("Bootstrap");
            return go.AddComponent<GameBootstrap>();
        }

        [Test]
        public void EnemyHit_TriggersHitStop_TierByPlayerState()
        {
            var bootstrap = Build();

            bootstrap.Enemy.Damageable.ApplyDamage(new DamagePacket(10f, 0f, Vector3.zero));
            Assert.IsTrue(bootstrap.HitStop.Active, "إصابة العدو تطلب تجميداً");
            bootstrap.HitStop.Tick(0.07f);
            Assert.IsFalse(bootstrap.HitStop.Active, "خفيفة = 60ms");

            // ثقيلة: عقل اللاعب في AttackHeavy لحظة الإصابة
            Assert.IsTrue(bootstrap.Player.Brain.RequestHeavyAttack());
            bootstrap.Enemy.Damageable.ApplyDamage(new DamagePacket(10f, 0f, Vector3.zero));
            bootstrap.HitStop.Tick(0.07f);
            Assert.IsTrue(bootstrap.HitStop.Active, "ثقيلة = 90ms — ما زال مجمداً بعد 70ms");
            bootstrap.HitStop.Tick(0.03f);
            Assert.IsFalse(bootstrap.HitStop.Active);
        }

        [Test]
        public void PlayerHit_RequestsStop50ms()
        {
            var bootstrap = Build();

            bootstrap.Player.Damageable.ApplyDamage(new DamagePacket(5f, 0f, Vector3.zero));
            Assert.IsTrue(bootstrap.HitStop.Active);
            bootstrap.HitStop.Tick(0.04f);
            Assert.IsTrue(bootstrap.HitStop.Active, "تلقّي ضربة = 50ms");
            bootstrap.HitStop.Tick(0.02f);
            Assert.IsFalse(bootstrap.HitStop.Active);
        }

        [Test]
        public void HitFlash_RestoresBaseTint_DeathWins()
        {
            var bootstrap = Build();
            var enemy = bootstrap.Enemy;
            var flash = enemy.GetComponent<HitFlash>();
            Assert.IsNotNull(flash, "كل عدو يحمل وميض إصابة");

            var renderer = enemy.GetComponent<Renderer>();
            Color baseColor = renderer.material.color;

            enemy.Damageable.ApplyDamage(new DamagePacket(10f, 0f, Vector3.zero));
            Assert.AreEqual(Color.white, renderer.material.color, "يومض أبيض عند الضرب");

            flash.Tick(0.11f);
            Assert.AreEqual(baseColor, renderer.material.color, "يعود للون نوعه بعد 0.1ث");

            // الموت منتصف الوميض: لون الجثة يغلب — التكة لا تكتب فوقه
            enemy.Damageable.ApplyDamage(new DamagePacket(10000f, 0f, Vector3.zero));
            Color deathColor = renderer.material.color;
            flash.Tick(0.11f);
            Assert.AreEqual(deathColor, renderer.material.color, "لا ارتداد لوني فوق الجثة");
        }

        [Test]
        public void GuardBreak_ShakesCamera()
        {
            var bootstrap = Build();
            var player = bootstrap.Player;
            var shake = player.cameraRig.Shake;

            // استنزف الستامينا ثم صد ضربة: كسر حرس → اهتزاز
            player.Brain.SetBlockHeld(true);
            player.Stamina.TryConsume(player.Stamina.Current - 1f);
            player.Damageable.ApplyDamage(new DamagePacket(50f, 20f, Vector3.zero));

            Assert.IsTrue(shake.Active, "كسر الحرس يهز الكاميرا");
        }
    }
}
