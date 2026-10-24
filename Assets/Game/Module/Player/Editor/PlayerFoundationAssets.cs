using System;
using System.IO;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Module;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player.Editor
{
    /// <summary>阶段 3 方块验证装配。只在明确调用时创建，已有产物拒绝覆盖。</summary>
    public static class PlayerFoundationAssets
    {
        public static string Create()
        {
            const string directory = "Assets/GameResource/Resources/Config/Player";
            const string prefabPath = "Assets/GameResource/Player/PlayerBlock.prefab";
            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(directory + "/PlayerConfig.asset") != null)
                throw new InvalidOperationException("玩家基础资产已存在；修改应保留 GUID，不能重新覆盖。");
            var gameplay = SceneManager.GetSceneByPath("Assets/Game/Scenes/GameplayScene.unity");
            if (gameplay.IsValid() && gameplay.isDirty) throw new InvalidOperationException("请先保护未保存的 GameplayScene。");
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory("Assets/GameResource/Player");
            AssetDatabase.Refresh();
            var definition = ScriptableObject.CreateInstance<PlayerBasicStateDefinition>();
            AssetDatabase.CreateAsset(definition, directory + "/PlayerBasicState.asset");
            var graph = ScriptableObject.CreateInstance<EmberFsmGraph>();
            graph.Configure("basic-drive", new EmberFsmNode("basic-drive", "基础驱动验证", definition));
            AssetDatabase.CreateAsset(graph, directory + "/PlayerGraph.asset");
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            config.Configure(graph, new Vector2(0, 1.05f));
            AssetDatabase.CreateAsset(config, directory + "/PlayerConfig.asset");

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            const string texturePath = "Assets/GameResource/Player/Block.png";
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

            var root = new GameObject("PlayerBlock");
            try
            {
                root.AddComponent<Entity2D>();
                var body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 3; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var shape = root.AddComponent<BoxCollider2D>(); shape.size = new Vector2(1, 1.75f);
                var material = new PhysicsMaterial2D("PlayerSurface") { friction = 0, bounciness = 0 };
                AssetDatabase.CreateAsset(material, "Assets/GameResource/Player/PlayerSurface.physicsMaterial2D");
                shape.sharedMaterial = material;
                root.AddComponent<EntityMotor2D>();
                root.AddComponent<EntitySensors2D>().Configure(shape, 1);
                var visual = MakeBlock("Visual", sprite, new Color(0.2f, 0.8f, 1), new Vector2(1, 1.75f));
                visual.transform.SetParent(root.transform, false);
                root.AddComponent<EntityFacing2D>().Configure(visual.transform);
                root.AddComponent<PlayerController>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            bool opened = !gameplay.IsValid();
            if (opened) gameplay = EditorSceneManager.OpenScene("Assets/Game/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
            try
            {
                var group = new GameObject("PlatformerFoundation");
                SceneManager.MoveGameObjectToScene(group, gameplay);
                var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), gameplay);
                player.transform.SetParent(group.transform); player.transform.position = config.Spawn;
                var floor = MakeBlock("Ground", sprite, new Color(0.24f, 0.29f, 0.36f), new Vector2(24, 1));
                SceneManager.MoveGameObjectToScene(floor, gameplay);
                floor.transform.SetParent(group.transform); floor.transform.position = new Vector3(0, -0.5f, 0);
                floor.AddComponent<BoxCollider2D>().size = Vector2.one;
                EditorSceneManager.MarkSceneDirty(gameplay);
                EditorSceneManager.SaveScene(gameplay);
            }
            finally { if (opened) EditorSceneManager.CloseScene(gameplay, true); }

            var storage = AssetDatabase.LoadAssetAtPath<PlayerDataConfig>("Assets/GameResource/Resources/Config/PlayerData/PlayerDataConfig.asset");
            storage.Configure("Platformer2D", "ember.platformer2d.progress", new[] { 0, 1, 2 });
            EditorUtility.SetDirty(storage);
            AssetDatabase.SaveAssets();
            return "Created block player 1 x 1.75, graph, config and GameplayScene foundation.";
        }

        private static GameObject MakeBlock(string name, Sprite sprite, Color color, Vector2 size)
        {
            var result = new GameObject(name);
            var renderer = result.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            result.transform.localScale = new Vector3(size.x, size.y, 1);
            return result;
        }
    }
}
