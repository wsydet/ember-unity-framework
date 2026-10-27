using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.LevelAuthoring.Tests
{
    public sealed partial class EmberLevelObjectResolutionTests
    {
        private Scene _scene;
        private EmberLevelObjectTestNode _source, _owner;
        private Adapter _adapter, _other;
        private EmberLevelWorkbench _window;
        private UnityEngine.Object _selection;
        private string _folder;

        private sealed class Adapter : IEmberLevelObjectAdapter
        {
            public string Id { get; } = Guid.NewGuid().ToString("N");
            public EmberLevelObjectTestNode owner;
            public bool failCreate, failDelete, failDraw;
            public bool drawHandles;
            public int created, deleted, drawn;
            public Vector2 drawnRange;
            public void ValidatePlacement(EmberLevelMaterial material, Scene scene) { }
            public void OnCreated(GameObject instance, bool duplicate)
            {
                created++;
                var node = instance.GetComponent<EmberLevelObjectTestNode>();
                Undo.RecordObject(node, "分配身份"); node.stableId = Guid.NewGuid().ToString("N");
                Undo.RecordObject(owner, "维护关联"); owner.link = node;
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                if (failCreate) throw new InvalidOperationException("injected-create");
            }
            public void BeforeDelete(GameObject instance)
            {
                deleted++; Undo.RecordObject(owner, "删除关联"); owner.link = null;
                if (failDelete) throw new InvalidOperationException("injected-delete");
            }
            public IEnumerable<EmberLevelDiagnostic> Validate(GameObject instance) { yield break; }
            public void DrawScene(GameObject instance)
            {
                drawn++; drawnRange = instance.GetComponent<EmberLevelObjectTestNode>().range;
                if (failDraw) throw new InvalidOperationException("injected-draw");
                if (drawHandles && Event.current != null)
                {
                    using (new Handles.DrawingScope(Color.magenta, instance.transform.localToWorldMatrix))
                        Handles.DrawWireCube(Vector3.zero, new Vector3(drawnRange.x, drawnRange.y, 0));
                    Handles.Label(instance.transform.position, "Raw checkpoint " + drawnRange + " ID=" + instance.GetComponent<EmberLevelObjectTestNode>().stableId);
                }
            }
        }

        [SetUp] public void Setup()
        {
            _guiShown = false; _folder = null; _clickedRect = default;
            _selection = Selection.activeObject;
            _scene = EditorSceneManager.NewPreviewScene();
            var parent = new GameObject("ExistingHierarchy"); SceneManager.MoveGameObjectToScene(parent, _scene);
            var go = new GameObject("ExistingCheckpoint"); go.transform.SetParent(parent.transform);
            go.transform.localPosition = new Vector3(2, 3, 4); go.transform.localRotation = Quaternion.Euler(0, 0, 21); go.transform.localScale = new Vector3(2, 1, 1);
            _source = go.AddComponent<EmberLevelObjectTestNode>(); _source.stableId = "A"; _source.range = new Vector2(3, 7);
            var owner = new GameObject("ReferenceOwner"); SceneManager.MoveGameObjectToScene(owner, _scene);
            _owner = owner.AddComponent<EmberLevelObjectTestNode>(); _owner.link = _source;
            _adapter = new Adapter { owner = _owner }; _other = new Adapter { owner = _owner };
            EmberLevelAdapters.Register(_adapter); EmberLevelAdapters.Register(_other);
            EmberLevelAdapters.RegisterObjectResolver("test.raw", go => go.GetComponent<EmberLevelObjectTestNode>() != null ? _adapter : null);
            _window = ScriptableObject.CreateInstance<EmberLevelWorkbench>();
            Selection.activeGameObject = go;
            Undo.ClearAll();
        }

        [TearDown] public void Teardown()
        {
            EmberLevelAdapters.UnregisterObjectResolver("test.raw"); EmberLevelAdapters.UnregisterObjectResolver("test.other");
            EmberLevelAdapters.Unregister(_adapter); EmberLevelAdapters.Unregister(_other);
            if (_window != null) { if (_guiShown) _window.Close(); else UnityEngine.Object.DestroyImmediate(_window); }
            Undo.ClearAll(); Selection.activeObject = _selection;
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            if (_folder != null) AssetDatabase.DeleteAsset(_folder);
        }

        private void Invoke(string method)
        {
            try { typeof(EmberLevelWorkbench).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_window, null); }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); }
        }

        [Test] public void WorkbenchDuplicatePreservesSourceAndUndoesIdentityAndReferencesTogether()
        {
            var parent = _source.transform.parent; var position = _source.transform.localPosition;
            var rotation = _source.transform.localRotation; var scale = _source.transform.localScale;
            Invoke("DuplicateSelection"); Undo.FlushUndoRecordObjects();
            var copy = _owner.link; string id = copy.stableId;
            Assert.AreNotSame(_source, copy); Assert.AreNotEqual("A", id); Assert.AreEqual(1, _adapter.created);
            Assert.AreEqual("A", _source.stableId); Assert.AreEqual(position, _source.transform.localPosition);
            Assert.AreEqual(rotation, _source.transform.localRotation); Assert.AreEqual(scale, _source.transform.localScale);
            Assert.AreSame(parent, _source.transform.parent); Assert.AreSame(parent, copy.transform.parent);
            Assert.IsFalse(PrefabUtility.IsPartOfAnyPrefab(_source)); Assert.IsFalse(PrefabUtility.IsPartOfAnyPrefab(copy));
            Undo.PerformUndo(); Assert.IsTrue(copy == null); Assert.AreSame(_source, _owner.link);
            Undo.PerformRedo(); Assert.AreEqual(id, _owner.link.stableId); Assert.AreEqual(2, parent.childCount);
        }

        [Test] public void WorkbenchDeleteUndoesObjectAndReferenceTogether()
        {
            Invoke("DeleteSelection"); Undo.FlushUndoRecordObjects();
            Assert.AreEqual(1, _adapter.deleted); Assert.IsTrue(_source == null); Assert.IsNull(_owner.link);
            Undo.PerformUndo(); Assert.IsNotNull(_owner.link); Assert.AreEqual("A", _owner.link.stableId);
            Undo.PerformRedo(); Assert.IsNull(_owner.link);
        }

        [TestCase(true)] [TestCase(false)] public void WorkbenchCallbackFailureRollsBack(bool create)
        {
            _adapter.failCreate = create; _adapter.failDelete = !create;
            Assert.Throws<InvalidOperationException>(() => Invoke(create ? "DuplicateSelection" : "DeleteSelection"));
            Assert.AreSame(_source, _owner.link); Assert.AreEqual("A", _source.stableId); Assert.AreEqual(1, _source.transform.parent.childCount);
        }

        [Test] public void WorkbenchRangeReadsRealComponentAndReportsCallbackFailure()
        {
            Invoke("DrawSelectedScene"); Assert.AreEqual(new Vector2(3, 7), _adapter.drawnRange);
            _source.range = new Vector2(9, 2); Invoke("DrawSelectedScene"); Assert.AreEqual(_source.range, _adapter.drawnRange);
            _adapter.failDraw = true; Assert.Throws<InvalidOperationException>(() => Invoke("DrawSelectedScene"));
        }

        [Test] public void PublicServiceUsesSameResolverEvenWithExplicitNull()
        {
            var copy = EmberLevelObjectEditing.Duplicate(_source.gameObject, null);
            Assert.AreNotEqual("A", copy.GetComponent<EmberLevelObjectTestNode>().stableId);
            EmberLevelObjectEditing.Delete(copy, null); Assert.AreEqual(1, _adapter.deleted);
        }

        [Test] public void ResolverConflictBlocksWorkbenchBeforeMutation()
        {
            EmberLevelAdapters.RegisterObjectResolver("test.other", go => _other);
            var error = Assert.Throws<InvalidOperationException>(() => Invoke("DuplicateSelection"));
            StringAssert.Contains("test.other", error.Message); StringAssert.Contains("test.raw", error.Message);
            Assert.AreEqual(1, _source.transform.parent.childCount); Assert.AreEqual(0, _adapter.created);
            Assert.Throws<InvalidOperationException>(() => Invoke("DeleteSelection")); Assert.AreSame(_source, _owner.link);
        }

        [Test] public void StaleResolverAndThrownResolverCannotFallBackToNull()
        {
            EmberLevelAdapters.Unregister(_adapter);
            StringAssert.Contains("test.raw", Assert.Throws<InvalidOperationException>(() => Invoke("DuplicateSelection")).Message);
            EmberLevelAdapters.Register(_adapter);
            EmberLevelAdapters.RegisterObjectResolver("test.other", go => throw new Exception("injected-resolver"));
            StringAssert.Contains("injected-resolver", Assert.Throws<InvalidOperationException>(() => Invoke("DeleteSelection")).Message);
            Assert.AreSame(_source, _owner.link); Assert.AreEqual(1, _source.transform.parent.childCount);
        }

        [Test] public void RegistrationRejectsDuplicateNamesAndAllowsResolverAgreement()
        {
            Assert.Throws<InvalidOperationException>(() => EmberLevelAdapters.RegisterObjectResolver("test.raw", go => _other));
            Assert.Throws<ArgumentNullException>(() => EmberLevelAdapters.RegisterObjectResolver("test.other", null));
            EmberLevelAdapters.RegisterObjectResolver("test.other", go => _adapter);
            Assert.AreSame(_adapter, EmberLevelAdapters.ResolveObject(_source.gameObject));
        }

        [Test] public void PlainObjectsRemainEditableWithoutAdapter()
        {
            var plain = new GameObject("Plain"); SceneManager.MoveGameObjectToScene(plain, _scene);
            Assert.IsNull(EmberLevelAdapters.ResolveObject(plain));
            var copy = EmberLevelObjectEditing.Duplicate(plain); EmberLevelObjectEditing.Delete(copy);
            Assert.IsTrue(copy == null); Assert.AreEqual(0, _adapter.created);
        }

        [Test] public void PrefabFallbackPlacementOverridesDuplicateDeleteAndPriorityRemainValid()
        {
            _folder = "Assets/EmberObjectResolutionTest-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(_folder));
            var prefab = PrefabUtility.SaveAsPrefabAsset(_source.gameObject, _folder + "/Checkpoint.prefab");
            var material = ScriptableObject.CreateInstance<EmberLevelMaterial>();
            material.Configure("test", "Test", EmberLevelMaterialKind.Mechanism, prefab, null, null, _other.Id, rotation: true, scale: true);
            AssetDatabase.CreateAsset(material, _folder + "/Material.asset");
            // 项目匹配优先于素材；放置保持素材声明的事务行为。
            var placed = EmberLevelObjectEditing.Place(material, _scene, Vector3.one, Quaternion.identity, Vector3.one);
            Assert.AreEqual(1, _other.created); Assert.AreSame(_adapter, EmberLevelAdapters.ResolveObject(placed));
            EmberLevelAdapters.UnregisterObjectResolver("test.raw");
            Assert.AreSame(_other, EmberLevelAdapters.ResolveObject(placed));
            var node = placed.GetComponent<EmberLevelObjectTestNode>(); node.range = new Vector2(11, 12);
            PrefabUtility.RecordPrefabInstancePropertyModifications(node);
            Selection.activeGameObject = placed; Invoke("DuplicateSelection"); var copy = Selection.activeGameObject;
            Assert.AreSame(prefab, PrefabUtility.GetCorrespondingObjectFromOriginalSource(copy));
            Assert.AreEqual(node.range, copy.GetComponent<EmberLevelObjectTestNode>().range);
            Assert.AreNotEqual(node.stableId, copy.GetComponent<EmberLevelObjectTestNode>().stableId);
            Invoke("DeleteSelection"); Assert.AreEqual(1, _other.deleted);
            EmberLevelAdapters.Unregister(_other);
            Assert.Throws<InvalidOperationException>(() => EmberLevelObjectEditing.Duplicate(placed));
            EmberLevelAdapters.Register(_other);
            var conflict = ScriptableObject.CreateInstance<EmberLevelMaterial>();
            conflict.Configure("conflict", "Conflict", EmberLevelMaterialKind.Mechanism, prefab, null, null, _adapter.Id);
            AssetDatabase.CreateAsset(conflict, _folder + "/Conflict.asset");
            StringAssert.Contains("冲突", Assert.Throws<InvalidOperationException>(() => EmberLevelAdapters.ResolveObject(placed)).Message);
        }
    }
}
