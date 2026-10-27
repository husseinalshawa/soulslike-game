using SoulsGame.Core;
using SoulsGame.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoulsGame.EditorTools
{
    // أدوات الإعداد لمرة واحدة — تعمل داخل محرر Unity فقط (قائمة Tools/SoulsGame).
    public static class SoulsGameEditorMenu
    {
        [MenuItem("Tools/SoulsGame/0 - Setup URP")]
        public static void SetupUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                Debug.Log("SoulsGame: URP already active.");
                return;
            }

            System.IO.Directory.CreateDirectory("Assets/_Game/Data");
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/_Game/Data/URP_Renderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, "Assets/_Game/Data/URP_Pipeline.asset");

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("SoulsGame: URP pipeline created and assigned.");
        }

        [MenuItem("Tools/SoulsGame/1 - Create Data Assets")]
        public static void CreateDataAssets()
        {
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/PlayerStats.asset", null);
            CreateAssetIfMissing<WeaponData>("Assets/_Game/Data/Sword_Basic.asset", null);
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/EnemyMeleeStats.asset", s =>
            {
                s.maxHealth = 150f;
                s.maxPoise = 30f;
            });
            CreateAssetIfMissing<WeaponData>("Assets/_Game/Data/Enemy_Sword.asset", w =>
            {
                w.lightDamage = 20f;
                w.lightPoiseDamage = 20f;
                w.lightStaminaCost = 0f;
                w.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            });
            CreateAssetIfMissing<ProgressionData>("Assets/_Game/Data/Progression.asset", null);
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/BossStats.asset", s =>
            {
                s.maxHealth = 1000f;
                s.maxPoise = 120f;
                s.runSpeed = 3.5f;
                s.soulsReward = 2000;
            });
            AssetDatabase.SaveAssets();
            Debug.Log("SoulsGame: data assets ready under Assets/_Game/Data/");
        }

        [MenuItem("Tools/SoulsGame/2 - Create Arena Scene")]
        public static void CreateArenaScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var bootstrapGo = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();
            bootstrap.playerStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/_Game/Data/PlayerStats.asset");
            bootstrap.playerWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Sword_Basic.asset");
            bootstrap.enemyStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/_Game/Data/EnemyMeleeStats.asset");
            bootstrap.enemyWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Enemy_Sword.asset");
            bootstrap.progressionData = AssetDatabase.LoadAssetAtPath<ProgressionData>("Assets/_Game/Data/Progression.asset");
            bootstrap.bossStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/_Game/Data/BossStats.asset");

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Arena.unity");
            Debug.Log("SoulsGame: Arena scene saved — press Play to build and play the arena.");
        }

        private static void CreateAssetIfMissing<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return;
            var asset = ScriptableObject.CreateInstance<T>();
            configure?.Invoke(asset);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
