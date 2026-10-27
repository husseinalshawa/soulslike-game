using System.Collections.Generic;
using SoulsGame.Core;
using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.World
{
    // يبني العالم المفتوح برمجياً: قطع التضاريس (Mesh + MeshCollider)،
    // ثيم المرج الذابل، النثر، والكهف. كل المواضع من WorldHeightmap.
    public static class WorldBuilder
    {
        public const float ChunkSize = 50f;
        public const float Resolution = 2f;

        // مركز غرفة الكهف — يستخدمه بناء محتوى الكهف (أعداء، كنز، موقع راحة)
        public static readonly Vector3 CaveRoomCenter = new Vector3(-162f, 0f, 20f);

        public static Vector3 OnGround(float x, float z, float yOffset) =>
            new Vector3(x, WorldHeightmap.GetHeight(x, z) + yOffset, z);

        // يعيد عدد قطع التضاريس المبنية.
        public static int Build(WorldTheme theme,
            IReadOnlyList<Vector3> exclusionCenters, IReadOnlyList<float> exclusionRadii)
        {
            ApplyTheme(theme);
            int chunks = BuildTerrain(theme);
            BuildScatter(theme, exclusionCenters, exclusionRadii);
            BuildCave(theme);
            return chunks;
        }

        public static void ApplyTheme(WorldTheme theme)
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = theme.sunIntensity;
            sun.color = theme.sunColor;
            sunGo.transform.rotation = Quaternion.Euler(theme.sunPitchDegrees, theme.sunYawDegrees, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;
            RenderSettings.ambientLight = theme.ambient;
        }

        private static int BuildTerrain(WorldTheme theme)
        {
            int chunksPerSide = (int)(WorldHeightmap.HalfSize * 2f / ChunkSize); // 8
            int count = 0;
            for (int cx = 0; cx < chunksPerSide; cx++)
                for (int cz = 0; cz < chunksPerSide; cz++)
                {
                    float originX = -WorldHeightmap.HalfSize + cx * ChunkSize;
                    float originZ = -WorldHeightmap.HalfSize + cz * ChunkSize;
                    BuildChunk($"Terrain_{cx}_{cz}", originX, originZ, theme);
                    count++;
                }
            return count;
        }

        private static void BuildChunk(string name, float originX, float originZ, WorldTheme theme)
        {
            int quads = (int)(ChunkSize / Resolution); // 25
            int vertsPerSide = quads + 1;

            var vertices = new Vector3[vertsPerSide * vertsPerSide];
            var colors = new Color[vertices.Length];
            for (int vz = 0; vz < vertsPerSide; vz++)
                for (int vx = 0; vx < vertsPerSide; vx++)
                {
                    float worldX = originX + vx * Resolution;
                    float worldZ = originZ + vz * Resolution;
                    float height = WorldHeightmap.GetHeight(worldX, worldZ);
                    int index = vz * vertsPerSide + vx;
                    // إحداثيات محلية نسبة لأصل القطعة
                    vertices[index] = new Vector3(vx * Resolution, height, vz * Resolution);
                    // عشب ذابل في المنخفض، صخر شاحب في المرتفع
                    float t = Mathf.Clamp01(height / WorldHeightmap.MaxHeight);
                    colors[index] = LerpColor(theme.grassColor, theme.highGroundColor, t);
                }

            var triangles = new int[quads * quads * 6];
            int tri = 0;
            for (int vz = 0; vz < quads; vz++)
                for (int vx = 0; vx < quads; vx++)
                {
                    int a = vz * vertsPerSide + vx;
                    int b = a + 1;
                    int c = a + vertsPerSide;
                    int d = c + 1;
                    triangles[tri++] = a; triangles[tri++] = c; triangles[tri++] = b;
                    triangles[tri++] = b; triangles[tri++] = c; triangles[tri++] = d;
                }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.position = new Vector3(originX, 0f, originZ);
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.material.color = theme.grassColor;
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private static Color LerpColor(Color a, Color b, float t) =>
            new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t);

        private static void BuildScatter(WorldTheme theme,
            IReadOnlyList<Vector3> exclusionCenters, IReadOnlyList<float> exclusionRadii)
        {
            var parent = new GameObject("Scatter");
            foreach (var item in WorldScatter.Generate(exclusionCenters, exclusionRadii))
            {
                switch (item.Kind)
                {
                    case ScatterKind.DeadTree: BuildDeadTree(item, theme, parent.transform); break;
                    case ScatterKind.Rock: BuildRock(item, theme, parent.transform); break;
                    default: BuildRuin(item, theme, parent.transform); break;
                }
            }
        }

        private static void BuildDeadTree(ScatterItem item, WorldTheme theme, Transform parent)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "DeadTree";
            trunk.transform.SetParent(parent, false);
            float height = 3.5f * item.Scale;
            trunk.transform.position = item.Position + Vector3.up * (height * 0.5f);
            trunk.transform.localScale = new Vector3(0.3f * item.Scale, height * 0.5f, 0.3f * item.Scale);
            trunk.transform.rotation = Quaternion.Euler(0f, item.Rotation, 0f);
            GameBootstrap.Tint(trunk, theme.deadTreeColor);

            for (int i = 0; i < 2; i++)
            {
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "Branch";
                branch.transform.SetParent(trunk.transform, false);
                branch.transform.localPosition = new Vector3(0f, 0.5f + i * 0.3f, 0f);
                branch.transform.localRotation = Quaternion.Euler(50f + i * 30f, item.Rotation + i * 140f, 0f);
                branch.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
                GameBootstrap.Tint(branch, theme.deadTreeColor);
                var branchCollider = branch.GetComponent<Collider>();
                if (branchCollider != null) branchCollider.enabled = false;
            }
        }

        private static void BuildRock(ScatterItem item, WorldTheme theme, Transform parent)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            float size = 1.2f * item.Scale;
            rock.transform.position = item.Position + Vector3.up * (size * 0.35f);
            rock.transform.localScale = new Vector3(size, size * 0.7f, size * 0.9f);
            rock.transform.rotation = Quaternion.Euler(item.Rotation * 0.1f, item.Rotation, item.Rotation * 0.08f);
            GameBootstrap.Tint(rock, theme.rockColor);
        }

        private static void BuildRuin(ScatterItem item, WorldTheme theme, Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "RuinBlock";
                block.transform.SetParent(parent, false);
                float angle = item.Rotation + i * 120f;
                var offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.2f;
                block.transform.position = item.Position + offset + Vector3.up * 0.5f;
                block.transform.localScale = new Vector3(1f, 1f + (i % 2) * 0.8f, 1f) * item.Scale;
                block.transform.rotation = Quaternion.Euler(0f, angle, i * 8f);
                GameBootstrap.Tint(block, theme.ruinColor);
            }
        }

        private static void BuildCave(WorldTheme theme)
        {
            Vector3 mouth = WorldHeightmap.CaveMouthCenter;
            float floorY = WorldHeightmap.GetHeight(mouth.x, mouth.z);
            Vector3 room = CaveRoomCenter;

            // أرضية النفق والغرفة (مستوية على ارتفاع الفم)
            Box("CaveFloor", new Vector3((mouth.x + room.x) * 0.5f, floorY - 0.5f, mouth.z), new Vector3(34f, 1f, 8f), theme.caveColor);
            Box("CaveRoomFloor", new Vector3(room.x, floorY - 0.5f, room.z), new Vector3(14f, 1f, 14f), theme.caveColor);
            // جدران وسقف النفق
            Box("CaveWall_N", new Vector3((mouth.x + room.x) * 0.5f, floorY + 2f, mouth.z + 4.5f), new Vector3(34f, 4f, 1f), theme.caveColor);
            Box("CaveWall_S", new Vector3((mouth.x + room.x) * 0.5f, floorY + 2f, mouth.z - 4.5f), new Vector3(34f, 4f, 1f), theme.caveColor);
            Box("CaveCeiling", new Vector3((mouth.x + room.x) * 0.5f, floorY + 4.5f, mouth.z), new Vector3(34f, 1f, 10f), theme.caveColor);
            // غرفة الكهف
            Box("CaveRoom_N", new Vector3(room.x, floorY + 2f, room.z + 7.5f), new Vector3(16f, 4f, 1f), theme.caveColor);
            Box("CaveRoom_S", new Vector3(room.x, floorY + 2f, room.z - 7.5f), new Vector3(16f, 4f, 1f), theme.caveColor);
            Box("CaveRoom_W", new Vector3(room.x - 7.5f, floorY + 2f, room.z), new Vector3(1f, 4f, 16f), theme.caveColor);
            Box("CaveRoomCeiling", new Vector3(room.x, floorY + 4.5f, room.z), new Vector3(16f, 1f, 16f), theme.caveColor);
        }

        private static void Box(string name, Vector3 position, Vector3 scale, Color color) =>
            GameBootstrap.CreateBox(name, position, scale, color);
    }
}
