using NUnit.Framework;
using SoulsGame.Core;
using UnityEngine;

namespace SoulsGame.Tests
{
    // يحاكي سلوك Unity الحقيقي: AddComponent على كائن نشط يستدعي Awake فوراً
    // قبل أن يضبط المستدعي الحقول. البناء البرمجي للساحة يجب أن ينجو من ذلك
    // (نمط الكائن المعطّل: أضف المكونات، اربط الحقول، ثم فعّل).
    public class BootstrapLifecycleTests
    {
        [Test]
        public void GameBootstrap_BuildsEverything_WithUnityAwakeOrdering()
        {
            var go = new GameObject("Bootstrap");

            Assert.DoesNotThrow(() => go.AddComponent<GameBootstrap>());

            var bootstrap = go.GetComponent<GameBootstrap>();
            Assert.IsNotNull(bootstrap.Player, "اللاعب بُني");
            Assert.IsNotNull(bootstrap.Player.Brain, "عقل اللاعب تهيأ بعد ضبط الحقول");
            Assert.IsNotNull(bootstrap.Player.Damageable.Health, "مكونات الضرر تهيأت");
            Assert.IsNotNull(bootstrap.Enemy, "العدو بُني");
            Assert.IsNotNull(bootstrap.Enemy.Brain, "عقل العدو تهيأ");
        }
    }
}
