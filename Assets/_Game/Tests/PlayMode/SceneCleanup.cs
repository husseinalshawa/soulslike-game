using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

// اختبارات التكامل تبني الساحة كاملة (GameBootstrap) وتحتاج دورة حياة Unity الحقيقية:
// AddComponent يستدعي Awake فوراً فقط في وضع اللعب، ولذلك تعيش هنا في PlayMode.
// بعد كل اختبار نمحو كائنات المشهد حتى لا تتراكم عوالم كاملة بين الاختبارات.
[assembly: SoulsGame.Tests.SceneCleanup]

namespace SoulsGame.Tests
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SceneCleanupAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) { }

        public void AfterTest(ITest test)
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    // مشغّل الاختبارات نفسه يعيش في المشهد — لا نلمسه
                    if (root.name.Contains("tests runner")) continue;
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }
    }
}
