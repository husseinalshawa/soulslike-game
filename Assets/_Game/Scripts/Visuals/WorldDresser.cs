using System.Collections.Generic;
using SoulsGame.Core;
using UnityEngine;

namespace SoulsGame.Visuals
{
    // يُلبس العالم المبني برمجياً فناً حقيقياً: الأشكال البدائية تبقى (تصادمها
    // ومنطقها والاختبارات كما هي) لكن رسمها يُخفى ويحل محله نموذج واقعي.
    // كل الاختيارات حتمية من بذرة — نفس العالم في كل تشغيل.
    public static class WorldDresser
    {
        private const int ForestTrees = 60;
        private const int ForestDeadTrees = 30;
        private const int ForestUndergrowth = 70;
        public const float ForestRadius = 48f;

        public static void Dress(ArtLibrary art)
        {
            if (art == null) return;
            var rng = new Rng(777);

            var all = Object.FindObjectsByType<Transform>();
            var deadTrees = new List<Transform>();
            var rocks = new List<Transform>();
            foreach (var t in all)
            {
                string n = t.name;
                if (n.StartsWith("Terrain_")) DressTerrain(t, art);
                else if (n == "DeadTree") deadTrees.Add(t);
                else if (n == "Rock") rocks.Add(t);
                else if (n == "Campfire") DressCampfire(t, art);
                else if (IsStone(n)) ApplyTiled(t, art.stone, 3f);
                else if (n.StartsWith("Cave")) ApplyTiled(t, art.rockFace, 4f);
            }

            // أشجار العالم: تنوب داكن في الغالب، وبينها جذوع ميتة واقفة — أطول من الأصل
            foreach (var t in deadTrees)
            {
                bool hasDead = art.deadTrees != null && art.deadTrees.Length > 0;
                bool fir = art.forestTrees != null && art.forestTrees.Length > 0 &&
                           (!hasDead || rng.Range(0f, 1f) < 0.6f);
                Replace(t, fir ? Pick(art.forestTrees, rng) : Pick(art.deadTrees, rng),
                    t.localScale.y * 2f * (fir ? 2.6f : 1.6f), rng, true);
            }
            foreach (var t in rocks) Replace(t, Pick(art.rocks, rng), t.localScale.y * 1.6f, rng, false);

            BuildDarkForest(art, rng);
            ApplySky(art);
        }

        private static bool IsStone(string n) =>
            n.StartsWith("Wall_") || n.StartsWith("Boss") || n.StartsWith("Corridor") ||
            n.StartsWith("RuinBlock") || n.StartsWith("Chapel") || n.StartsWith("Arena");

        // التضاريس بلا UV: نولّدها من موضع العالم (بلاطة كل 6م) ثم مادة أرض حقيقية
        private static void DressTerrain(Transform t, ArtLibrary art)
        {
            var filter = t.GetComponent<MeshFilter>();
            var renderer = t.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null || art.ground == null) return;
            var mesh = filter.sharedMesh;
            var verts = mesh.vertices;
            var uv = new Vector2[verts.Length];
            Vector3 origin = t.position;
            for (int i = 0; i < verts.Length; i++)
                uv[i] = new Vector2((verts[i].x + origin.x) / 6f, (verts[i].z + origin.z) / 6f);
            mesh.uv = uv;
            mesh.RecalculateTangents();
            renderer.sharedMaterial = InForest(origin + new Vector3(25f, 0f, 25f)) && art.forestFloor != null
                ? art.forestFloor : art.ground;
        }

        private static bool InForest(Vector3 p)
        {
            var c = GameBootstrap.DarkForestCenter;
            float dx = p.x - c.x, dz = p.z - c.z;
            return dx * dx + dz * dz < ForestRadius * ForestRadius * 1.3f;
        }

        // المكعبات تمدد UV على كل وجه — ننسخ الشبكة ونضرب UV كل وجه بأبعاده كي
        // يبقى الحجر بحجمه الحقيقي على الجدران الطويلة.
        private static readonly Dictionary<Vector3, Mesh> tiledCubes = new Dictionary<Vector3, Mesh>();

        private static void ApplyTiled(Transform t, Material material, float tileMeters)
        {
            var renderer = t.GetComponent<MeshRenderer>();
            var filter = t.GetComponent<MeshFilter>();
            if (renderer == null || filter == null || material == null) return;
            Vector3 s = t.lossyScale;
            Vector3 key = new Vector3(Mathf.Round(s.x * 10f), Mathf.Round(s.y * 10f), Mathf.Round(s.z * 10f));
            if (!tiledCubes.TryGetValue(key, out var mesh))
            {
                mesh = Object.Instantiate(filter.sharedMesh);
                var verts = mesh.vertices;
                var normals = mesh.normals;
                var uv = new Vector2[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = Vector3.Scale(verts[i], s);
                    Vector3 n = normals[i];
                    if (Mathf.Abs(n.x) > 0.5f) uv[i] = new Vector2(v.z, v.y);
                    else if (Mathf.Abs(n.y) > 0.5f) uv[i] = new Vector2(v.x, v.z);
                    else uv[i] = new Vector2(v.x, v.y);
                    uv[i] /= tileMeters;
                }
                mesh.uv = uv;
                mesh.RecalculateTangents();
                tiledCubes[key] = mesh;
            }
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
        }

        private static void DressCampfire(Transform t, ArtLibrary art)
        {
            if (art.firePit == null) return;
            var r = t.GetComponent<Renderer>();
            // الصندوق البرتقالي يبقى صغيراً كجمر داخل حلقة الحجر
            t.localScale = new Vector3(0.5f, 0.25f, 0.5f);
            var pit = Object.Instantiate(art.firePit, t.position - Vector3.up * 0.15f, Quaternion.identity);
            pit.name = "FirePit";
            FitHeight(pit, 0.6f);
            if (r != null) r.enabled = true;
        }

        // يخفي رسم الشكل البدائي (وأبنائه) ويضع النموذج الحقيقي بنفس الارتفاع
        private static void Replace(Transform placeholder, GameObject prefab, float targetHeight,
            Rng rng, bool groundAtBase)
        {
            if (prefab == null) return;
            foreach (var r in placeholder.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            Vector3 basePos = placeholder.position;
            if (groundAtBase) basePos.y -= placeholder.lossyScale.y; // أسطوانة: الأصل في المنتصف
            else basePos.y -= placeholder.lossyScale.y * 0.5f;
            var go = Object.Instantiate(prefab, basePos, Quaternion.Euler(0f, rng.Range(0f, 360f), 0f));
            go.name = prefab.name;
            FitHeight(go, targetHeight * rng.Range(0.85f, 1.2f));
            go.transform.SetParent(placeholder.parent, true);
            StripColliders(go);
        }

        private static void BuildDarkForest(ArtLibrary art, Rng rng)
        {
            var parent = new GameObject("DarkForest").transform;
            var c = GameBootstrap.DarkForestCenter;
            var placed = new List<Vector3>();
            Place(art.forestTrees, ForestTrees, 3.5f, 9f, 16f, true, false);
            Place(art.deadTrees, ForestDeadTrees, 3f, 6f, 10f, true, false);
            Place(art.fallenLogs, 18, 4f, 4f, 7f, false, true);
            Place(art.undergrowth, ForestUndergrowth, 1.2f, 0.6f, 1.4f, false, false);

            void Place(GameObject[] prefabs, int count, float spacing, float minH, float maxH, bool solid, bool byLength)
            {
                if (prefabs == null || prefabs.Length == 0) return;
                int made = 0, attempts = 0;
                while (made < count && attempts++ < count * 30)
                {
                    float a = rng.Range(0f, Mathf.PI * 2f);
                    float d = Mathf.Sqrt(rng.Range(0f, 1f)) * ForestRadius;
                    var p = new Vector3(c.x + Mathf.Cos(a) * d, 0f, c.z + Mathf.Sin(a) * d);
                    if (solid && NearPatrol(p)) continue;
                    bool clear = true;
                    foreach (var q in placed)
                        if ((q - p).sqrMagnitude < spacing * spacing) { clear = false; break; }
                    if (!clear) continue;
                    p.y = World.WorldHeightmap.GetHeight(p.x, p.z) - 0.1f;
                    var prefab = prefabs[rng.Int(prefabs.Length)];
                    var go = Object.Instantiate(prefab, p, Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), parent);
                    go.name = prefab.name;
                    if (byLength) FitLength(go, rng.Range(minH, maxH));
                    else FitHeight(go, rng.Range(minH, maxH));
                    StripColliders(go);
                    if (solid)
                    {
                        // جذع صلب بسيط — الأشجار تحجب الطريق دون تكلفة تصادم الشبكة
                        var col = go.AddComponent<CapsuleCollider>();
                        float s = go.transform.lossyScale.y;
                        col.radius = 0.35f / s;
                        col.height = 4f / s;
                        col.center = new Vector3(0f, 2f / s, 0f);
                    }
                    placed.Add(p);
                    made++;
                }
            }
        }

        // ممرات دوريات حرّاس الظل تبقى خالية من الأشجار
        private static bool NearPatrol(Vector3 p)
        {
            var c = GameBootstrap.DarkForestCenter;
            Vector3[] anchors =
            {
                c, new Vector3(c.x - 14f, 0f, c.z + 10f), new Vector3(c.x + 12f, 0f, c.z - 12f)
            };
            foreach (var a in anchors)
            {
                float dx = p.x - a.x, dz = p.z - a.z;
                if (dx * dx + dz * dz < 11f * 11f) return true;
            }
            return false;
        }

        private static void ApplySky(ArtLibrary art)
        {
            if (art.skybox == null) return;
            RenderSettings.skybox = art.skybox;
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            DynamicGI.UpdateEnvironment();
        }

        private static void FitHeight(GameObject go, float height)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            if (b.size.y < 0.001f) return;
            go.transform.localScale *= height / b.size.y;
        }

        private static void FitLength(GameObject go, float length)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (longest < 0.001f) return;
            go.transform.localScale *= length / longest;
        }

        private static void StripColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
        }

        private static GameObject Pick(GameObject[] options, Rng rng) =>
            options == null || options.Length == 0 ? null : options[rng.Int(options.Length)];

        private sealed class Rng
        {
            private uint state;
            public Rng(uint seed) { state = seed; }
            public float Range(float min, float max)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return min + (state & 0xFFFFFF) / 16777216f * (max - min);
            }
            public int Int(int count) => Mathf.Min(count - 1, (int)Range(0f, count));
        }
    }
}
