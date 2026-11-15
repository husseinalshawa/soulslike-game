using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SoulsGame.Visuals.EditorTools
{
    // يحوّل ملفات Poly Haven الخام (Assets/_Game/Art/PolyHaven) إلى مواد URP
    // ونماذج جاهزة، ويملأ كتالوج ArtLibrary الذي يقرؤه WorldDresser وقت اللعب.
    public static class ArtLibraryBuilder
    {
        private const string Root = "Assets/_Game/Art/PolyHaven";
        private const string MaterialsDir = "Assets/_Game/Art/Materials";
        private const string LibraryPath = "Assets/_Game/Art/Resources/ArtLibrary.asset";

        [MenuItem("Tools/SoulsGame/3 - Build Art Library")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            MarkNormalMaps();

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<ArtLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            lib.ground = SurfaceMaterial(lit, "rocky_terrain_02", "Ground", 0.12f);
            lib.forestFloor = SurfaceMaterial(lit, "forest_leaves_02", "ForestFloor", 0.1f);
            lib.stone = SurfaceMaterial(lit, "castle_brick_07", "Stone", 0.15f);
            lib.rockFace = SurfaceMaterial(lit, "brown_mud_leaves_01", "CaveRock", 0.1f);
            lib.skybox = SkyMaterial();

            // كل نماذج «الجذوع» في Poly Haven ممددة — تُستخدم جذوعاً ساقطة
            lib.deadTrees = new GameObject[0];
            lib.fallenLogs = Models(lit, "dead_tree_trunk", "dead_tree_trunk_02");
            // شتلة التنوب مكبّرة (شجرة التنوب الكاملة 7 ملايين مضلع — ثقيلة جداً للعب)
            lib.forestTrees = Models(lit, "fir_sapling");
            lib.rocks = Models(lit, "boulder_01", "rock_moss_set_01");
            lib.undergrowth = Models(lit, "fern_02", "tree_stump_01");
            lib.firePit = Models(lit, "stone_fire_pit").FirstOrDefault();
            lib.fortRuin = Models(lit, "modular_fort_01").FirstOrDefault();
            lib.castleDoor = Models(lit, "large_castle_door").FirstOrDefault();
            lib.ironGate = Models(lit, "large_iron_gate").FirstOrDefault();

            // تعتيم ما يبدو باهتاً تحت ضوء القمر: أرض الأوراق والجذوع المبيّضة
            Darken(lib.forestFloor, new Color(0.38f, 0.33f, 0.27f));
            foreach (var guid in AssetDatabase.FindAssets("dead_ t:Material", new[] { MaterialsDir }))
                Darken(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)),
                    new Color(0.4f, 0.36f, 0.32f));

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"SoulsGame: Art library ready — {lib.deadTrees.Length} dead trees, " +
                      $"{lib.forestTrees.Length} forest trees, {lib.rocks.Length} rocks.");
        }

        private static void Darken(Material mat, Color tint)
        {
            if (mat == null) return;
            mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
        }

        private static void DarkenModel(GameObject model, Color tint)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && AssetDatabase.GetAssetPath(m).StartsWith(MaterialsDir)) Darken(m, tint);
        }

        private static void MarkNormalMaps()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("_nor_gl")) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType == TextureImporterType.NormalMap) continue;
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static Material SurfaceMaterial(Shader lit, string texture, string name, float smoothness)
        {
            string dir = $"{Root}/Textures/{texture}";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_Diffuse_2k.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_nor_gl_2k.jpg");
            if (albedo == null) return null;
            return SaveMaterial(lit, $"{MaterialsDir}/{name}.mat", albedo, normal, smoothness);
        }

        private static Material SaveMaterial(Shader lit, string path, Texture2D albedo, Texture2D normal, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = lit;
            mat.SetTexture("_BaseMap", albedo);
            mat.SetColor("_BaseColor", Color.white);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // كل نموذج: مادة URP لكل مادة في الـ FBX، تُربط بالخامات المجاورة بالاسم
        private static GameObject[] Models(Shader lit, params string[] names)
        {
            var result = new List<GameObject>();
            foreach (var n in names)
            {
                string fbxPath = $"{Root}/Models/{n}/{n}.fbx";
                var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;

                var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { $"{Root}/Models/{n}" })
                    .Select(AssetDatabase.GUIDToAssetPath).ToArray();
                // Mesh LOD (Unity 6.2+): مستويات تفصيل تلقائية — نماذج المسح الضوئي ثقيلة جداً بدونها
                bool changed = !importer.generateMeshLods;
                importer.generateMeshLods = true;
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
                {
                    string prefix = BestPrefix(src.name, textures, n);
                    var albedo = Find(textures, prefix, "_diff_");
                    if (albedo == null) continue;
                    var alpha = Find(textures, prefix, "_alpha_");
                    if (alpha != null) albedo = MergeAlpha(albedo, alpha, prefix);
                    var mat = SaveMaterial(lit, $"{MaterialsDir}/{n}_{Sanitize(src.name)}.mat",
                        albedo, Find(textures, prefix, "_nor_gl_"), 0.1f);
                    // الأوراق والسرخس: قص الشفافية من قناة ألفا الخامة
                    if (alpha != null || prefix.Contains("leaf") || prefix.Contains("fern") ||
                        prefix.Contains("branch") || prefix.Contains("needle"))
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.SetFloat("_Cutoff", 0.4f);
                        mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetFloat("_Cull", 0f); // وجهان
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mat);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (model != null) result.Add(model);
            }
            return result.ToArray();
        }

        // الخامة الأقرب لاسم المادة (نماذج متعددة المواد مثل الشجرة: جذع + أوراق)
        private static string BestPrefix(string materialName, string[] textures, string model)
        {
            string m = materialName.ToLowerInvariant();
            var prefixes = textures.Select(p => Path.GetFileNameWithoutExtension(p))
                .Where(f => f.Contains("_diff_"))
                .Select(f => f.Substring(0, f.IndexOf("_diff_"))).Distinct().ToList();
            var exact = prefixes.FirstOrDefault(p => m.Contains(p.ToLowerInvariant()) || p.ToLowerInvariant().Contains(m));
            return exact ?? prefixes.FirstOrDefault(p => p == model) ?? prefixes.FirstOrDefault() ?? model;
        }

        // Poly Haven يفصل قناع الأوراق عن لونها — ندمجهما في PNG واحد بقناة ألفا
        private static Texture2D MergeAlpha(Texture2D diffuse, Texture2D alpha, string prefix)
        {
            string diffPath = AssetDatabase.GetAssetPath(diffuse);
            string outPath = $"{Path.GetDirectoryName(diffPath).Replace('\\', '/')}/{prefix}_diffalpha.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            if (existing != null) return existing;

            Color[] rgb = ReadPixels(diffuse, out int w, out int h);
            Color[] a = ReadPixels(alpha, out int aw, out int ah);
            var merged = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = rgb[y * w + x];
                    c.a = a[(y * ah / h) * aw + (x * aw / w)].r;
                    pixels[y * w + x] = c;
                }
            merged.SetPixels(pixels);
            File.WriteAllBytes(outPath, merged.EncodeToPNG());
            Object.DestroyImmediate(merged);
            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static Color[] ReadPixels(Texture2D tex, out int w, out int h)
        {
            // نسخة قابلة للقراءة عبر RenderTexture — بلا تعديل إعدادات الاستيراد
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            w = tex.width; h = tex.height;
            var px = copy.GetPixels();
            Object.DestroyImmediate(copy);
            return px;
        }

        private static Texture2D Find(string[] textures, string prefix, string kind)
        {
            var path = textures.FirstOrDefault(p => Path.GetFileName(p).StartsWith(prefix + kind));
            return path == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material SkyMaterial()
        {
            var hdr = AssetDatabase.FindAssets("t:Texture", new[] { $"{Root}/Sky" })
                .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (hdr == null) return null;
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(hdr);
            string path = $"{MaterialsDir}/NightSky.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Skybox/Panoramic");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Exposure", 0.35f); // سماء ليلية خافتة — لا تطغى على الضباب
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
