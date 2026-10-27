using System;
using System.Linq;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring.Tests
{
    public sealed class EmberLevelAuthoringTests
    {
        private Scene _scene;
        private Tilemap _map;
        private EmberTerrainTile _tile;
        private Texture2D _texture;
        private Sprite _sprite;
        [SetUp] public void Setup()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            var grid = new GameObject("TestGrid"); SceneManager.MoveGameObjectToScene(grid, _scene); grid.AddComponent<Grid>().cellSize = new Vector3(.5f,.5f,1);
            var go = new GameObject("Map"); go.transform.SetParent(grid.transform); _map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); go.AddComponent<TilemapCollider2D>();
            _texture = new Texture2D(16,16); _sprite = Sprite.Create(_texture,new Rect(0,0,16,16),new Vector2(.5f,.5f),32);
            _tile = ScriptableObject.CreateInstance<EmberTerrainTile>();
            _tile.Configure("test", "group", new Vector2(.5f,.5f),new Vector3(.5f,.5f,0),EmberTerrainPhysics.Solid,_sprite,Color.white,
                Enumerable.Range(0,256).Select(m=>new EmberTerrainTile.Rule { required=m, forbidden=255^m, sprite=_sprite }).ToArray());
        }
        [TearDown] public void Teardown()
        {
            Undo.ClearAll(); if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            UnityEngine.Object.DestroyImmediate(_tile); UnityEngine.Object.DestroyImmediate(_sprite); UnityEngine.Object.DestroyImmediate(_texture);
        }
        [TestCase(.5f,.5f,1,1)] [TestCase(1,1,2,2)] [TestCase(.5f,10,1,20)] [TestCase(10,.5f,20,1)] [TestCase(3.5f,.5f,7,1)]
        public void DimensionsAreExact(float w,float h,int x,int y) => Assert.AreEqual(new Vector2Int(x,y),EmberTerrainEditing.GetCellCount(new Vector2(w,h),new Vector2(.5f,.5f)));
        [Test] public void RejectsFractionalAndNonFiniteDimensions()
        {
            Assert.Throws<ArgumentException>(()=>EmberTerrainEditing.GetCellCount(new Vector2(.6f,1),new Vector2(.5f,.5f)));
            Assert.Throws<ArgumentException>(()=>EmberTerrainEditing.GetCellCount(new Vector2(float.NaN,1),Vector2.one));
            Assert.Throws<ArgumentException>(()=>EmberTerrainEditing.GetCellCount(new Vector2(float.PositiveInfinity,1),Vector2.one));
        }
        [Test] public void AllEightNeighborMasksResolveDeterministically()
        {
            for(int m=0;m<256;m++) { Assert.AreEqual(m,_tile.Evaluate(m,out bool conflict)); Assert.IsFalse(conflict); }
            Assert.IsEmpty(EmberTerrainEditing.ValidateRules(_tile));
        }
        [Test] public void ConflictingRulesAreReported()
        {
            _tile.Configure("test","group",new Vector2(.5f,.5f),_tile.Anchor,_tile.Physics,_sprite,Color.white,new[]{new EmberTerrainTile.Rule{sprite=_sprite},new EmberTerrainTile.Rule{sprite=_sprite}});
            _tile.Evaluate(0,out bool conflict); Assert.IsTrue(conflict); Assert.AreEqual(256,EmberTerrainEditing.ValidateRules(_tile).Length);
        }
        [Test] public void PaintEraseAndUndoPreserveOccupancy()
        {
            var region = new BoundsInt(-2,-1,0,6,3,1); EmberTerrainEditing.Paint(_map,region,_tile);
            Assert.AreEqual(18,_map.GetTilesBlock(region).Count(t=>t!=null));
            Assert.IsFalse(_map.HasTile(new Vector3Int(4,0,0)));
            EmberTerrainEditing.Paint(_map,new BoundsInt(0,0,0,1,1,1),_tile,true); Assert.IsFalse(_map.HasTile(Vector3Int.zero));
            Undo.PerformUndo(); Assert.AreSame(_tile,_map.GetTile(Vector3Int.zero));
            Undo.PerformRedo(); Assert.IsFalse(_map.HasTile(Vector3Int.zero));
        }
        [Test] public void MoveUpdatesOldAndNewOccupancy()
        {
            var region=new BoundsInt(0,0,0,2,2,1); EmberTerrainEditing.Paint(_map,region,_tile);
            EmberTerrainEditing.Move(_map,region,new Vector3Int(3,0,0),_tile);
            Assert.IsFalse(_map.HasTile(Vector3Int.zero)); Assert.AreSame(_tile,_map.GetTile(new Vector3Int(3,0,0)));
            Undo.PerformUndo(); Assert.AreSame(_tile,_map.GetTile(Vector3Int.zero)); Assert.IsFalse(_map.HasTile(new Vector3Int(3,0,0)));
        }
        [Test] public void IncompatibleMapIsNotTransformed()
        {
            _map.tileAnchor=Vector3.zero;
            Assert.Throws<InvalidOperationException>(()=>EmberTerrainEditing.Paint(_map,new BoundsInt(0,0,0,1,1,1),_tile));
            Assert.AreEqual(Vector3.zero,_map.tileAnchor); Assert.IsFalse(_map.HasTile(Vector3Int.zero));
        }
        [TestCase(2f, .5f, 4, 8)]
        [TestCase(-2f, 2f, 4, 2)]
        [TestCase(.5f, .5f, 16, 8)]
        public void ScaledGridUsesWorldDimensionsWithoutChangingTransform(float sx, float sy, int columns, int rows)
        {
            _map.layoutGrid.transform.localScale = new Vector3(sx, sy, 1);
            var preset = ScriptableObject.CreateInstance<EmberTerrainPreset>();
            try
            {
                preset.Configure("scaled", new Vector2(4, 2), new Vector2Int(1, 1), _tile);
                EmberTerrainEditing.Stamp(_map, Vector3Int.zero, preset);
                var region = new BoundsInt(-1,-1,0,columns,rows,1);
                Assert.AreEqual(columns * rows, _map.GetTilesBlock(region).Count(t => t != null));
                Assert.AreEqual(new Vector3(sx,sy,1), _map.layoutGrid.transform.localScale);
                Undo.PerformUndo(); Assert.IsFalse(_map.HasTile(new Vector3Int(-1,-1,0)));
                Undo.PerformRedo(); Assert.AreSame(_tile, _map.GetTile(new Vector3Int(-1,-1,0)));
            }
            finally { UnityEngine.Object.DestroyImmediate(preset); }
        }
        [Test] public void RotatedScaledGridMeasuresAxesRatherThanWorldAabb()
        {
            _map.layoutGrid.transform.SetPositionAndRotation(new Vector3(3,-5,0),Quaternion.Euler(0,0,37));
            _map.layoutGrid.transform.localScale = new Vector3(2,.5f,1);
            Assert.That(Vector2.Distance(new Vector2(1,.25f),EmberTerrainEditing.GetWorldCellSize(_map)),Is.LessThan(.00001f));
        }
        [Test] public void FractionalScaledStampFailsBeforeMutatingMap()
        {
            _map.layoutGrid.transform.localScale = new Vector3(3,1,1);
            var preset = ScriptableObject.CreateInstance<EmberTerrainPreset>();
            try { preset.Configure("bad",Vector2.one,Vector2Int.zero,_tile);
                Assert.Throws<ArgumentException>(()=>EmberTerrainEditing.Stamp(_map,Vector3Int.zero,preset));
                Assert.IsFalse(_map.HasTile(Vector3Int.zero)); }
            finally { UnityEngine.Object.DestroyImmediate(preset); }
        }
        [TestCase(16f, 0, false)]
        [TestCase(64f, 1, false)]
        [TestCase(32f, 3, true)]
        public void SpriteAndCollisionMatchNonSquareGridWithCustomPivot(float ppu, int turns, bool mirror)
        {
            var sprite = Sprite.Create(_texture,new Rect(0,0,16,16),new Vector2(.1f,.8f),ppu,0,SpriteMeshType.FullRect);
            try
            {
                _map.layoutGrid.cellSize = new Vector3(1,.5f,1);
                _map.layoutGrid.transform.localScale = new Vector3(-2,.5f,1);
                _map.tileAnchor = new Vector3(.2f,.7f,0);
                _tile.Configure("geometry","group",new Vector2(1,.5f),_map.tileAnchor,EmberTerrainPhysics.Solid,sprite,Color.white,
                    new[]{new EmberTerrainTile.Rule { sprite=sprite,quarterTurns=turns,mirrorX=mirror }});
                var cell = new Vector3Int(2,3,0);
                EmberTerrainEditing.Paint(_map,new BoundsInt(cell,Vector3Int.one),_tile);
                Physics2D.SyncTransforms();
                Vector3 a = _map.CellToWorld(cell), b = _map.CellToWorld(cell + new Vector3Int(1,1,0));
                var expected = new Bounds((a+b)*.5f,new Vector3(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y),0));
                var actual = _map.GetComponent<TilemapCollider2D>().bounds;
                Assert.That(Vector2.Distance(expected.center,actual.center),Is.LessThan(.001f),"collision center");
                Assert.That(Vector2.Distance(expected.size,actual.size),Is.LessThan(.001f),"collision size");
                var selected = _map.GetSprite(cell);
                var matrix = _map.GetTransformMatrix(cell);
                var vertices = selected.vertices.Select(v=>_map.transform.TransformPoint(_map.GetCellCenterLocal(cell)+matrix.MultiplyPoint3x4(v))).ToArray();
                var visual = new Bounds(vertices[0],Vector3.zero);
                foreach(var v in vertices) visual.Encapsulate(v);
                Assert.That(Vector2.Distance(expected.center,visual.center),Is.LessThan(.001f),"visual center");
                Assert.That(Vector2.Distance(expected.size,visual.size),Is.LessThan(.001f),"visual size");
                Assert.AreNotSame(sprite,selected,"original art physics must not be mutated");
            }
            finally { UnityEngine.Object.DestroyImmediate(sprite); }
        }
        [Test] public void AssetConfigureFailureRollsBackExistingAndNewAssets()
        {
            string folder = "Assets/EmberLevelFailureTest-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
            string path=folder+"/Material.asset";
            try
            {
                Assert.Throws<InvalidOperationException>(()=>EmberLevelAssetService.Upsert<EmberLevelMaterial>(path,m=>throw new InvalidOperationException("injected-new")));
                Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(path));
                // 无外部引用的资产作为事务载体，故障前修改真实序列化名称。
                var asset=EmberLevelAssetService.Upsert<EmberLevelMaterial>(path,m=>m.name="before");
                string before=EditorJsonUtility.ToJson(asset);
                string guid=AssetDatabase.AssetPathToGUID(path);
                Assert.Throws<InvalidOperationException>(()=>EmberLevelAssetService.Upsert<EmberLevelMaterial>(path,m=>{m.name="partial";throw new InvalidOperationException("injected-update");}));
                Assert.AreEqual(before,EditorJsonUtility.ToJson(asset)); Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
        [Test] public void MergePreservesBusinessDisabledAndUnknownEntries()
        {
            var result=EmberBuildSceneRegistry.Merge(new[]{new EditorBuildSettingsScene("Assets/Business/A.unity",false),new EditorBuildSettingsScene("Assets/Missing.unity",true)},new[]{"Assets/Game/Scenes/Main.unity"},"Assets/Game/Scenes/FrameworkScene.unity");
            Assert.AreEqual("Assets/Game/Scenes/FrameworkScene.unity",result[0].path);
            Assert.IsFalse(result[1].enabled); Assert.AreEqual("Assets/Missing.unity",result[2].path);
            Assert.AreEqual(4,result.Length);
        }
        [Test] public void ParallaxIsAbsoluteAndReturnsToBaseline()
        {
            var go=new GameObject("Layer"); SceneManager.MoveGameObjectToScene(go,_scene); var layer=go.AddComponent<EmberParallaxLayer>();
            layer.Configure(null,new Vector2(.5f,1),new Vector3(2,3,4));
            Assert.AreEqual(new Vector3(7,13,4),layer.Evaluate(new Vector3(10,10,0),Vector3.zero));
            Assert.AreEqual(new Vector3(2,3,4),layer.Evaluate(Vector3.zero,Vector3.zero));
        }
    }
}
