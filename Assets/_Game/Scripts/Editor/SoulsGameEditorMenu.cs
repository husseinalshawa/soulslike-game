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
            CreateGearAssetIfMissing("Assets/_Game/Data/Sword_Basic.asset", GearLibrary.Longsword());
            // ترحيل أصل سيف موجود من المرحلة 1 بلا مهارة (fpCost=0 = struct فارغ)
            var existingSword = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Sword_Basic.asset");
            if (existingSword != null && existingSword.skill.fpCost <= 0f)
            {
                var template = GearLibrary.Longsword();
                existingSword.skill = template.skill;
                Object.DestroyImmediate(template);
                EditorUtility.SetDirty(existingSword);
            }
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/EnemyMeleeStats.asset", s =>
            {
                s.maxHealth = 150f;
                s.maxPoise = 30f;
                s.soulsReward = 100;
            });
            // ترحيل أصول موجودة من المرحلة 1 لا تملك مكافأة أرواح
            var existingEnemyStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/_Game/Data/EnemyMeleeStats.asset");
            if (existingEnemyStats != null && existingEnemyStats.soulsReward == 0)
            {
                existingEnemyStats.soulsReward = 100;
                EditorUtility.SetDirty(existingEnemyStats);
            }
            CreateAssetIfMissing<WeaponData>("Assets/_Game/Data/Enemy_Sword.asset", w =>
            {
                w.lightDamage = 20f;
                w.lightPoiseDamage = 20f;
                w.lightStaminaCost = 0f;
                w.light = new AttackTimeline(0.6f, 0.3f, 1.2f, 1.2f);
            });
            CreateAssetIfMissing<ProgressionData>("Assets/_Game/Data/Progression.asset", null);
            // المرتّلون الغارقون (المرحلة 5)
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/ChanterStats.asset", s =>
            {
                s.maxHealth = 200f;
                s.maxPoise = 40f;
                s.runSpeed = 2.5f;
                s.soulsReward = 150;
            });
            CreateAssetIfMissing<WeaponData>("Assets/_Game/Data/Chanter_Maul.asset", w =>
            {
                w.lightDamage = 30f;
                w.lightPoiseDamage = 30f;
                w.lightStaminaCost = 0f;
                w.light = new AttackTimeline(0.8f, 0.3f, 1.2f, 1.2f);
            });
            CreateAssetIfMissing<WorldTheme>("Assets/_Game/Data/WorldTheme.asset", null);
            CreateAssetIfMissing<CharacterStats>("Assets/_Game/Data/BossStats.asset", s =>
            {
                s.maxHealth = 1000f;
                s.maxPoise = 120f;
                s.runSpeed = 3.5f;
                s.soulsReward = 2000;
            });
            // عتاد المرحلة 4 — من GearLibrary (مصدر الأرقام الواحد المُختبر)
            CreateGearAssetIfMissing("Assets/_Game/Data/Weapon_Greatsword.asset", GearLibrary.Greatsword());
            CreateGearAssetIfMissing("Assets/_Game/Data/Weapon_TwinDaggers.asset", GearLibrary.TwinDaggers());
            CreateGearAssetIfMissing("Assets/_Game/Data/Weapon_Spear.asset", GearLibrary.Spear());
            CreateGearAssetIfMissing("Assets/_Game/Data/Weapon_Warhammer.asset", GearLibrary.Warhammer());
            CreateGearAssetIfMissing("Assets/_Game/Data/Armor_Cloth.asset", GearLibrary.Cloth());
            CreateGearAssetIfMissing("Assets/_Game/Data/Armor_Leather.asset", GearLibrary.Leather());
            CreateGearAssetIfMissing("Assets/_Game/Data/Armor_Plate.asset", GearLibrary.Plate());
            CreateGearAssetIfMissing("Assets/_Game/Data/Spell_GlintBolt.asset", GearLibrary.GlintBolt());
            CreateGearAssetIfMissing("Assets/_Game/Data/Spell_GlintbladeEdge.asset", GearLibrary.WeaponBuffSpell());

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
            bootstrap.worldTheme = AssetDatabase.LoadAssetAtPath<WorldTheme>("Assets/_Game/Data/WorldTheme.asset");
            bootstrap.chanterStats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/_Game/Data/ChanterStats.asset");
            bootstrap.chanterWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Chanter_Maul.asset");
            // عتاد المرحلة 4: المشهد يشير للأصول كي يكون تعديلها من الـ Inspector فعالاً
            bootstrap.greatswordData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapon_Greatsword.asset");
            bootstrap.twinDaggersData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapon_TwinDaggers.asset");
            bootstrap.spearData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapon_Spear.asset");
            bootstrap.warhammerData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapon_Warhammer.asset");
            bootstrap.clothArmorData = AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Game/Data/Armor_Cloth.asset");
            bootstrap.leatherArmorData = AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Game/Data/Armor_Leather.asset");
            bootstrap.plateArmorData = AssetDatabase.LoadAssetAtPath<ArmorData>("Assets/_Game/Data/Armor_Plate.asset");
            bootstrap.boltSpellData = AssetDatabase.LoadAssetAtPath<SpellData>("Assets/_Game/Data/Spell_GlintBolt.asset");
            bootstrap.buffSpellData = AssetDatabase.LoadAssetAtPath<SpellData>("Assets/_Game/Data/Spell_GlintbladeEdge.asset");

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Arena.unity");
            Debug.Log("SoulsGame: Arena scene saved — press Play to build and play the arena.");
        }

        private static void CreateGearAssetIfMissing(string path, ScriptableObject instance)
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
            {
                Object.DestroyImmediate(instance);
                return;
            }
            AssetDatabase.CreateAsset(instance, path);
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
