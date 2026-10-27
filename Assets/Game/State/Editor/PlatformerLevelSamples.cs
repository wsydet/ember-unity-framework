using System;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using Ember.LevelAuthoring;
using Ember.LevelAuthoring.Editor;
using Game.Entity;
using Game.LevelAuthoring;
using Game.Player;
using Game.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.State.Editor
{
    /// <summary>正式模板样例生成入口；只创建缺失资产，已存在内容不覆盖。</summary>
    public static class PlatformerLevelSamples
    {
        #region 内部参数
        public const string ROOT = "Assets/Game/Development/LevelAuthoring";
        public const string SCENE = ROOT + "/AuthoringSample.unity";
        public const string LEVEL = ROOT + "/AuthoringSampleLevel.asset";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static Sprite[] Sprites()
        {
            string path = ROOT + "/WhiteboxAtlas.asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false) { name = "WhiteboxAtlas", filterMode = FilterMode.Point };
            for (int mask = 0; mask < 256; mask++)
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                {
                    bool edge = (x == 15 && (mask & 1) == 0) || (y == 15 && (mask & 2) == 0) || (x == 0 && (mask & 4) == 0) || (y == 0 && (mask & 8) == 0);
                    bool corner = (x >= 14 && y >= 14 && (mask & 3) == 3 && (mask & 16) == 0) ||
                        (x <= 1 && y >= 14 && (mask & 6) == 6 && (mask & 32) == 0) ||
                        (x <= 1 && y <= 1 && (mask & 12) == 12 && (mask & 64) == 0) ||
                        (x >= 14 && y <= 1 && (mask & 9) == 9 && (mask & 128) == 0);
                    texture.SetPixel((mask % 16) * 16 + x, (mask / 16) * 16 + y, edge || corner ? new Color(.15f, .25f, .3f) : Color.white);
                }
            texture.Apply(); AssetDatabase.CreateAsset(texture, path);
            var sprites = new Sprite[256];
            for (int mask = 0; mask < 256; mask++)
            {
                var sprite = Sprite.Create(texture, new Rect(mask % 16 * 16, mask / 16 * 16, 16, 16), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
                sprite.name = "mask-" + mask.ToString("D3"); AssetDatabase.AddObjectToAsset(sprite, texture); sprites[mask] = sprite;
            }
            AssetDatabase.SaveAssets(); return sprites;
        }
        private static EmberTerrainTile Terrain(string name, EmberTerrainPhysics physics, Sprite[] sprites, Color color)
        {
            string path = ROOT + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<EmberTerrainTile>(path); if (existing != null) return existing;
            return EmberLevelAssetService.Upsert<EmberTerrainTile>(path, tile => tile.Configure("whitebox." + name, "whitebox." + name,
                new Vector2(.5f, .5f), new Vector3(.5f, .5f, 0), physics, sprites[0], color,
                Enumerable.Range(0, 256).Select(mask => new EmberTerrainTile.Rule { required = mask, forbidden = 255 ^ mask, priority = 1, sprite = sprites[mask] }).ToArray()));
        }
        private static Tilemap Map(Scene scene, string name, EmberTerrainPhysics physics)
        {
            var grid = new GameObject(name + "Grid"); SceneManager.MoveGameObjectToScene(grid, scene); grid.AddComponent<Grid>().cellSize = new Vector3(.5f, .5f, 1);
            var go = new GameObject(name); go.transform.SetParent(grid.transform, false); var map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>();
            if (physics != EmberTerrainPhysics.Decoration)
            {
                var collider = go.AddComponent<TilemapCollider2D>();
                if (physics == EmberTerrainPhysics.OneWay) { var effector = go.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; collider.usedByEffector = true; }
            }
            return map;
        }
        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        { using var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string PrepareBuildProbe()
        {
            if (!EmberProjectSetup.IsTemplateActive("platformer-2d")) throw new InvalidOperationException("需要活动平台模板。");
            string path = ROOT + "/AuthoringBuildBoot.unity";
            if (File.Exists(path)) return path;
            if (!AssetDatabase.CopyAsset("Assets/Game/Scenes/FrameworkScene.unity", path)) throw new IOException("复制正式引导场景失败。");
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var go = new GameObject("Explicit Build Acceptance"); SceneManager.MoveGameObjectToScene(go, scene);
                go.AddComponent<PlatformerAuthoringBuildProbe>().Configure(AssetDatabase.LoadAssetAtPath<PlatformerAuthoringLevel>(LEVEL).Config, SCENE);
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
            return path;
        }

        public static string Create()
        {
            if (!EmberProjectSetup.IsTemplateActive("platformer-2d")) throw new InvalidOperationException("需要活动平台模板。");
            Directory.CreateDirectory(ROOT); AssetDatabase.Refresh();
            var sprites = Sprites();
            var solid = Terrain("Solid", EmberTerrainPhysics.Solid, sprites, new Color(.65f, .83f, .9f));
            var oneWay = Terrain("OneWay", EmberTerrainPhysics.OneWay, sprites, new Color(1, .8f, .3f));
            var decoration = Terrain("Decoration", EmberTerrainPhysics.Decoration, sprites, new Color(.7f, .65f, .85f));
            var dimensions = new[] { new Vector2(.5f,.5f), Vector2.one, new Vector2(.5f,10), new Vector2(10,.5f), new Vector2(3,.5f) };
            for (int i = 0; i < dimensions.Length; i++)
            {
                string id = "whitebox.size." + i, path = ROOT + "/Size" + i + ".asset";
                var preset = AssetDatabase.LoadAssetAtPath<EmberTerrainPreset>(path) ?? EmberLevelAssetService.SaveSize(path, id, dimensions[i], Vector2Int.zero, solid);
                string materialPath = ROOT + "/Size" + i + "Material.asset";
                if (AssetDatabase.LoadMainAssetAtPath(materialPath) == null) EmberLevelAssetService.SaveMaterial(materialPath, id,
                    m => m.Configure(id, $"实体 {dimensions[i].x}×{dimensions[i].y}", EmberLevelMaterialKind.Terrain, null, null, preset, description: "实心轮廓 · 实体碰撞 · 0.5网格"));
            }
            foreach (var tile in new[] { oneWay, decoration })
            {
                string path = ROOT + "/" + tile.name + "Size.asset";
                var preset = AssetDatabase.LoadAssetAtPath<EmberTerrainPreset>(path) ?? EmberLevelAssetService.SaveSize(path, tile.Id, new Vector2(3,.5f), Vector2Int.zero, tile);
                string materialPath = ROOT + "/" + tile.name + "TerrainMaterial.asset";
                if (AssetDatabase.LoadMainAssetAtPath(materialPath) == null) EmberLevelAssetService.SaveMaterial(materialPath, tile.Id,
                    m => m.Configure(tile.Id, tile.Physics == EmberTerrainPhysics.OneWay ? "单向支撑条 3×0.5" : "无碰撞装饰条 3×0.5", EmberLevelMaterialKind.Terrain, null, null, preset));
            }
            foreach (var kind in new[] { EmberLevelMaterialKind.Background, EmberLevelMaterialKind.Decoration })
            {
                string path = ROOT + "/" + kind + "Material.asset";
                if (AssetDatabase.LoadMainAssetAtPath(path) == null) EmberLevelAssetService.SaveMaterial(path, "whitebox.object." + kind,
                    m => m.Configure("whitebox.object." + kind, kind == EmberLevelMaterialKind.Background ? "白盒背景层" : "白盒装饰块", kind, null, sprites[0], null, rotation: true, scale: true, description: "无碰撞 · 可调整排序与色调"));
            }
            string launchPath = ROOT + "/LaunchPad.prefab";
            if (AssetDatabase.LoadMainAssetAtPath(launchPath) == null)
            {
                var go = new GameObject("LaunchPad");
                try { go.AddComponent<SpriteRenderer>().sprite = sprites[0]; go.AddComponent<BoxCollider2D>().isTrigger = true; go.AddComponent<LaunchZone2D>(); PrefabUtility.SaveAsPrefabAsset(go, launchPath); }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            if (AssetDatabase.LoadMainAssetAtPath(ROOT + "/LaunchMaterial.asset") == null)
                EmberLevelAssetService.SaveMaterial(ROOT + "/LaunchMaterial.asset", "whitebox.launch", m => m.Configure("whitebox.launch", "现有弹射机关", EmberLevelMaterialKind.Mechanism,
                    AssetDatabase.LoadAssetAtPath<GameObject>(launchPath), null, null, "platformer-2d.mechanism", hasPhysics: true, description: "LaunchZone2D · 黄色范围为真实Trigger"));
            if (AssetDatabase.LoadMainAssetAtPath(LEVEL) != null) return "样例已存在；保留所有用户修改。";
            if (File.Exists(SCENE) || File.Exists(ROOT + "/AuthoringSampleConfig.asset")) throw new InvalidOperationException("样例部分资产已存在，禁止覆盖。");
            const string source = "Assets/Game/Development/LevelAcceptance/LevelA.unity";
            if (!AssetDatabase.CopyAsset(source, SCENE)) throw new InvalidOperationException("缺少现有平台场景骨架：" + source);
            var config = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayerConfig>("Assets/Game/Development/LevelAcceptance/ConfigA.asset"));
            using (var data = new SerializedObject(config)) { data.FindProperty("_scene").stringValue = "AuthoringSample"; data.ApplyModifiedPropertiesWithoutUndo(); }
            AssetDatabase.CreateAsset(config, ROOT + "/AuthoringSampleConfig.asset");
            var scene = EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Additive);
            try
            {
                var scope = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSceneScope>(true)).Single();
                foreach (var old in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerCheckpointAcceptanceParticipant>(true))) UnityEngine.Object.DestroyImmediate(old);
                using (var data = new SerializedObject(scope))
                {
                    var property = data.FindProperty("_participants");
                    var references = Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue).Where(v => v != null).ToArray();
                    property.arraySize = references.Length; for (int i = 0; i < references.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = references[i]; data.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (var root in scene.GetRootGameObjects().Where(r => r.name == "Ground")) UnityEngine.Object.DestroyImmediate(root);
                foreach (var player in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerController>(true)))
                    if (player.GetComponent<PlayerLaunchTarget2D>() == null) player.gameObject.AddComponent<PlayerLaunchTarget2D>();
                var map = Map(scene, "实体地形", EmberTerrainPhysics.Solid);
                EmberTerrainEditing.Paint(map, new BoundsInt(new Vector3Int(-24,-2,0), new Vector3Int(60,2,1)), solid);
                EmberTerrainEditing.Paint(map, new BoundsInt(new Vector3Int(0,0,0), new Vector3Int(2,2,1)), solid);
                EmberTerrainEditing.Paint(map, new BoundsInt(new Vector3Int(4,0,0), new Vector3Int(2,4,1)), solid);
                var platform = Map(scene, "单向平台", EmberTerrainPhysics.OneWay);
                EmberTerrainEditing.Paint(platform, new BoundsInt(new Vector3Int(8,6,0), new Vector3Int(8,1,1)), oneWay);
                var checkpoint = new GameObject("Checkpoint · 世界初态"); SceneManager.MoveGameObjectToScene(checkpoint, scene); checkpoint.transform.position = new Vector3(-2, 0, 0);
                checkpoint.AddComponent<BoxCollider2D>().isTrigger = true; checkpoint.AddComponent<PlatformerAuthoringCheckpoint>().Configure("sample.checkpoint", new Vector2(0,1));
                var marker = checkpoint.AddComponent<SpriteRenderer>(); marker.sprite = sprites[0]; marker.color = Color.green;
                var launch = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(launchPath), scene); launch.transform.position = new Vector3(6, .25f, 0);
                var background = EmberLevelObjectEditing.Place(AssetDatabase.LoadAssetAtPath<EmberLevelMaterial>(ROOT + "/BackgroundMaterial.asset"), scene, new Vector3(0,3,2), Quaternion.identity, new Vector3(60,24,1));
                background.GetComponent<SpriteRenderer>().color = new Color(.12f,.18f,.27f); background.GetComponent<SpriteRenderer>().sortingOrder = -100;
                var parallax = background.AddComponent<EmberParallaxLayer>(); parallax.Configure(null, new Vector2(.15f,.1f), background.transform.position);
                var presentation = scope.gameObject.AddComponent<PlatformerLevelPresentation>();
                presentation.Configure(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerCheckpointRecovery>(true)).Single(), new[] { parallax });
                using (var data = new SerializedObject(scope))
                { var list = data.FindProperty("_participants"); list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = presentation; data.ApplyModifiedPropertiesWithoutUndo(); }
                var decor = EmberLevelObjectEditing.Place(AssetDatabase.LoadAssetAtPath<EmberLevelMaterial>(ROOT + "/DecorationMaterial.asset"), scene, new Vector3(-6,2,0), Quaternion.identity, new Vector3(2,2,1));
                decor.GetComponent<SpriteRenderer>().color = new Color(.5f,.5f,.8f); decor.GetComponent<SpriteRenderer>().sortingOrder = -1;
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
            var level = ScriptableObject.CreateInstance<PlatformerAuthoringLevel>(); level.name = "白盒关卡制作样例";
            level.Configure("sample.authoring", AssetDatabase.AssetPathToGUID(SCENE), config); AssetDatabase.CreateAsset(level, LEVEL); AssetDatabase.SaveAssets();
            return "创建白盒样例、五种尺寸、三种物理语义、256邻域规则、机关/背景/装饰。草稿未加入发行集合。";
        }
        #endregion
    }
}
