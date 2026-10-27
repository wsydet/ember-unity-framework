using System.Collections;
using System.Reflection;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring.Tests
{
    /// <summary>缓存失效与延迟恢复的定向用例；真实程序集重载另按 B02 验收步骤执行。</summary>
    public sealed class EmberTerrainCacheLifecycleTests
    {
        #region 内部参数
        private Scene _scene;
        private Tilemap _map;
        private EmberTerrainTile _tile;
        private Tile _ordinary;
        private Texture2D _texture;
        private Sprite _source;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void QueueLifecycleRestore()
        {
            var type = typeof(EmberTerrainEditing).Assembly.GetType("Ember.LevelAuthoring.Editor.EmberTerrainCacheLifecycle", true);
            type.GetMethod("OnScriptsReloaded", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void Setup()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            var grid = new GameObject("CacheTestGrid", typeof(Grid));
            SceneManager.MoveGameObjectToScene(grid, _scene);
            grid.GetComponent<Grid>().cellSize = new Vector3(1.5f, .75f, 1);
            var go = new GameObject("CacheTestMap", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            go.transform.SetParent(grid.transform);
            _map = go.GetComponent<Tilemap>();
            _map.tileAnchor = new Vector3(.2f, .7f, 0);
            _map.GetComponent<TilemapRenderer>().sortingOrder = 7;
            _texture = new Texture2D(16, 8);
            _source = Sprite.Create(_texture, new Rect(0, 0, 16, 8), new Vector2(.1f, .8f), 32);
            _tile = ScriptableObject.CreateInstance<EmberTerrainTile>();
            // 右邻居命中旋转/镜像规则；最右格使用 fallback。
            _tile.Configure("cache", "cache", new Vector2(1.5f, .75f), _map.tileAnchor,
                EmberTerrainPhysics.Solid, _source, Color.cyan,
                new[] { new EmberTerrainTile.Rule { required = 1, sprite = _source, quarterTurns = 1, mirrorX = true } });
            _ordinary = ScriptableObject.CreateInstance<Tile>();
            _ordinary.sprite = _source;
            _map.SetTile(Vector3Int.zero, _tile);
            _map.SetTile(Vector3Int.right, _tile);
            _map.SetTile(new Vector3Int(3, 0, 0), _ordinary);
            _map.RefreshAllTiles();
        }

        [TearDown]
        public void Teardown()
        {
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            Object.DestroyImmediate(_tile);
            Object.DestroyImmediate(_ordinary);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_texture);
        }

        [UnityTest]
        public IEnumerator TwoCacheInvalidationsRestoreReferencesWithoutChangingTileState()
        {
            var bounds = _map.cellBounds;
            var tiles = _map.GetTilesBlock(bounds);
            var vertices = _source.vertices;
            var uv = _source.uv;
            var ruleVertices = _map.GetSprite(Vector3Int.zero).vertices;
            var fallbackVertices = _map.GetSprite(Vector3Int.right).vertices;
            var collider = _map.GetComponent<TilemapCollider2D>();
            collider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            var collision = collider.bounds;
            bool dirty = _scene.isDirty;
            var cacheField = typeof(EmberTerrainTile).GetField("_renderSprites", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                var oldRule = _map.GetSprite(Vector3Int.zero);
                var oldFallback = _map.GetSprite(Vector3Int.right);
                _tile.OnDisable();
                Assert.IsTrue(oldRule == null && oldFallback == null, "旧临时 Sprite 必须释放");
                _tile.OnEnable();
                // 只模拟生命周期调度；不调用任何 Tilemap 修复性刷新。
                QueueLifecycleRestore();
                QueueLifecycleRestore();
                yield return null;
                yield return null;
                Assert.IsNotNull(_map.GetSprite(Vector3Int.zero), "规则格应自动恢复");
                Assert.IsNotNull(_map.GetSprite(Vector3Int.right), "fallback 格应自动恢复");
                CollectionAssert.AreEqual(ruleVertices, _map.GetSprite(Vector3Int.zero).vertices);
                CollectionAssert.AreEqual(fallbackVertices, _map.GetSprite(Vector3Int.right).vertices);
                CollectionAssert.AreEqual(tiles, _map.GetTilesBlock(bounds));
                Assert.AreEqual(bounds, _map.cellBounds);
                Assert.AreEqual(Color.cyan, _map.GetColor(Vector3Int.zero));
                Assert.AreEqual(Matrix4x4.identity, _map.GetTransformMatrix(Vector3Int.zero));
                Assert.AreEqual(new Vector3(1.5f, .75f, 1), _map.layoutGrid.cellSize);
                Assert.AreEqual(new Vector3(.2f, .7f, 0), _map.tileAnchor);
                Assert.AreEqual(7, _map.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.AreSame(_source, _map.GetSprite(new Vector3Int(3, 0, 0)));
                CollectionAssert.AreEqual(vertices, _source.vertices);
                CollectionAssert.AreEqual(uv, _source.uv);
                Assert.AreEqual(2, ((IDictionary)cacheField.GetValue(_tile)).Count, "仅 fallback 与规则两个缓存");
                Assert.AreEqual(dirty, _scene.isDirty);
                if (collider.hasTilemapChanges) collider.ProcessTilemapChanges();
                Physics2D.SyncTransforms();
                Assert.AreEqual(collision, collider.bounds);
            }
        }
        #endregion
    }
}
