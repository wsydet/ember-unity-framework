using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ember.LevelAuthoring.Tests
{
    public sealed class EmberRoomGraphTests
    {
        private sealed class Adapter : IEmberRoomGraphAdapter
        {
            public string Id => "test.room-graph";
            public bool fail;
            public bool CanRead(UnityEngine.Object graph) => graph is EmberRoomGraphTestAsset;
            public IEnumerable<UnityEngine.Object> Targets(UnityEngine.Object graph) { yield return graph; }
            public EmberRoomGraphSnapshot Read(UnityEngine.Object graph)
            {
                var asset = (EmberRoomGraphTestAsset)graph;
                return new EmberRoomGraphSnapshot(asset.rooms.Select(r => Node(r.id, r.graphPosition)), asset.links.Select(e => Edge(e.from, e.to)), true, true);
            }
            public void WriteLayout(UnityEngine.Object graph, IReadOnlyDictionary<string, Vector2> positions)
            { foreach (var room in ((EmberRoomGraphTestAsset)graph).rooms) room.graphPosition = positions[room.id]; }
            public void AddConnection(UnityEngine.Object graph, EmberRoomGraphConnection edge)
            { var asset = (EmberRoomGraphTestAsset)graph; asset.links = asset.links.Append(new EmberRoomGraphTestAsset.Link { from = edge.FromRoom, to = edge.ToRoom }).ToArray(); }
            public void RemoveConnection(UnityEngine.Object graph, EmberRoomGraphConnection edge)
            { var asset = (EmberRoomGraphTestAsset)graph; asset.links = asset.links.Where(l => l.from != edge.FromRoom || l.to != edge.ToRoom).ToArray(); }
            public IEnumerable<EmberLevelDiagnostic> Validate(UnityEngine.Object graph)
            { if (fail) yield return new EmberLevelDiagnostic("test.product", "injected product validation", EmberLevelSeverity.Error); }
        }
        private string _configPath, _config, _folder;
        private Adapter _adapter;
        private EmberRoomGraphTestAsset _asset;
        private IDisposable _registration;
        private bool _module;
        private static EmberRoomGraphNode Node(string id, Vector2 position = default) => new(id, id, position, new Vector2(270, 270),
            new[] { new EmberRoomGraphPort("in", new Vector2(0, .5f), Vector2.left, worldAnchor: Vector2.zero), new EmberRoomGraphPort("out", new Vector2(1, .5f), Vector2.right, worldAnchor: Vector2.zero) });
        private static EmberRoomGraphConnection Edge(string from = "a", string to = "b") => new(from, "out", to, "in");
        [SetUp] public void Setup()
        {
            _configPath = EmberExtensionProject.ConfigurationPath; _config = File.Exists(_configPath) ? File.ReadAllText(_configPath) : null;
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = new[] { EmberExtensionProject.RoomGraph, EmberExtensionProject.FsmBlueprint, EmberExtensionProject.LevelTrial } });
            _folder = "Assets/__EmberRoomGraphTest_" + Guid.NewGuid().ToString("N"); AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            _asset = ScriptableObject.CreateInstance<EmberRoomGraphTestAsset>(); AssetDatabase.CreateAsset(_asset, _folder + "/ExistingGraph.asset");
            _adapter = new Adapter(); _module = true;
            _registration = EmberRoomGraphService.Register(_adapter, EmberFsmBlueprintAccess.CurrentTemplateId ?? "", () => _module);
            Undo.ClearAll();
        }
        [TearDown] public void Teardown()
        {
            _registration?.Dispose(); Undo.ClearAll();
            if (_folder != null) AssetDatabase.DeleteAsset(_folder);
            if (_config == null) File.Delete(_configPath); else File.WriteAllText(_configPath, _config);
            EmberConditionalMenu.Refresh();
        }
        [Test] public void ArbitraryPortsValidateWithoutTwelveSlotRestriction()
        { Assert.IsEmpty(EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(new[] { Node("a"), Node("b") }, new[] { Edge() }, true, true))); Assert.AreEqual(12, EmberRoomGraphRules.TwelveSlotPreset().Length); }
        [Test] public void RejectsOccupiedWrongDirectionDisabledAndUnalignedWorldPorts()
        {
            var a = Node("a"); var b = Node("b");
            Assert.IsNotEmpty(EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(new[] { a, b }, new[] { Edge(), Edge() })));
            Assert.IsNotEmpty(EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(new[] { a, b }, new[] { new EmberRoomGraphConnection("a", "out", "b", "out") }, true)));
            var disabled = new EmberRoomGraphNode("b", "b", Vector2.zero, Vector2.one, new[] { new EmberRoomGraphPort("in", Vector2.zero, Vector2.left, false) });
            Assert.IsNotEmpty(EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(new[] { a, disabled }, new[] { Edge() })));
            var unaligned = new EmberRoomGraphNode("b", "b", Vector2.zero, Vector2.one, new[] { new EmberRoomGraphPort("in", Vector2.zero, Vector2.left, worldAnchor: Vector2.one) });
            StringAssert.Contains("实际世界", string.Join("", EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(new[] { a, unaligned }, new[] { Edge() }, true, true))));
        }
        [Test] public void ArrangementPreservesInputGraphAndHandlesCyclesAndDisconnectedRooms()
        {
            var graph = new EmberRoomGraphSnapshot(new[] { Node("a"), Node("b"), Node("c"), Node("isolated") }, new[] { Edge("a", "b"), Edge("b", "c"), Edge("c", "a") });
            var layout = EmberRoomGraphRules.Arrange(graph); Assert.AreEqual(4, layout.Count); Assert.IsTrue(layout.Values.All(v => float.IsFinite(v.x) && float.IsFinite(v.y)));
            Assert.IsTrue(graph.Nodes.All(n => n.GraphPosition == Vector2.zero));
        }
        [Test] public void ExistingAssetLayoutAndConnectionsUndoWithoutChangingGuidOrWorldOrigins()
        {
            string path = AssetDatabase.GetAssetPath(_asset), guid = AssetDatabase.AssetPathToGUID(path);
            Vector2[] origins = _asset.rooms.Select(r => r.authoredWorldOrigin).ToArray();
            var layout = EmberRoomGraphRules.Arrange(_adapter.Read(_asset));
            EmberRoomGraphService.ApplyLayout(_adapter.Id, _asset, layout, EmberRoomGraphService.Revision(_adapter.Id, _asset));
            Assert.AreEqual(layout["a"], _asset.rooms[0].graphPosition); Undo.PerformUndo(); Assert.AreEqual(Vector2.zero, _asset.rooms[0].graphPosition);
            Undo.PerformRedo(); Assert.AreEqual(layout["a"], _asset.rooms[0].graphPosition);
            EmberRoomGraphService.Connect(_adapter.Id, _asset, Edge(), EmberRoomGraphService.Revision(_adapter.Id, _asset)); Assert.AreEqual(1, _asset.links.Length);
            Undo.PerformUndo(); Assert.AreEqual(0, _asset.links.Length); Undo.PerformRedo(); Assert.AreEqual(1, _asset.links.Length);
            EmberRoomGraphService.Disconnect(_adapter.Id, _asset, Edge(), EmberRoomGraphService.Revision(_adapter.Id, _asset)); Assert.IsEmpty(_asset.links);
            CollectionAssert.AreEqual(origins, _asset.rooms.Select(r => r.authoredWorldOrigin)); Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
        }
        [Test] public void ProductFailureRollsBackAndExternalChangesRejectStaleDraft()
        {
            var revision = EmberRoomGraphService.Revision(_adapter.Id, _asset); _adapter.fail = true;
            Assert.Throws<InvalidOperationException>(() => EmberRoomGraphService.Connect(_adapter.Id, _asset, Edge(), revision)); Assert.IsEmpty(_asset.links);
            _adapter.fail = false; _asset.rooms[0].authoredWorldOrigin = Vector2.one;
            Assert.Throws<InvalidOperationException>(() => EmberRoomGraphService.ApplyLayout(_adapter.Id, _asset, EmberRoomGraphRules.Arrange(_adapter.Read(_asset)), revision));
        }
        [Test] public void DisabledExtensionBlocksPublicApisAndOldWindowPreservesAssetAndDraft()
        {
            var window = ScriptableObject.CreateInstance<EmberRoomGraphWindow>();
            try
            {
                window.SetGraph(_asset, _adapter.Id); window.Arrange();
                var fields = BindingFlags.NonPublic | BindingFlags.Instance;
                var draft = EditorJsonUtility.ToJson(window);
                EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
                Assert.IsFalse(window.HasAccess); Assert.IsFalse(EmberFsmBlueprintAccess.IsAvailable);
                Assert.Throws<InvalidOperationException>(() => window.ApplyLayout());
                Assert.Throws<InvalidOperationException>(() => EmberRoomGraphService.Read(_adapter.Id, _asset));
                Assert.Throws<InvalidOperationException>(() => EmberLevelTrialSession.Start(null, EmberLevelStartMode.Entrance, Vector2.zero));
                Assert.Throws<InvalidOperationException>(() => EmberLevelWorkbench.Open());
                Assert.Throws<InvalidOperationException>(() => EmberFsmBlueprintWindow.Open(null));
                Assert.AreEqual(draft, EditorJsonUtility.ToJson(window)); Assert.AreSame(_asset, window.GetType().GetField("_graph", fields).GetValue(window));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
        [Test] public void ModuleAndTemplateScopesRejectAccessDespiteGenericExtensionEnabled()
        {
            _module = false; Assert.IsEmpty(EmberRoomGraphService.FindAdapters(_asset));
            Assert.Throws<InvalidOperationException>(() => EmberRoomGraphService.Read(_adapter.Id, _asset));
            _module = true;
            using var other = EmberRoomGraphService.Register(new OtherAdapter(), "missing-template", () => true);
            Assert.IsFalse(EmberRoomGraphService.FindAdapters(_asset).Contains("test.other"));
        }
        private sealed class OtherAdapter : IEmberRoomGraphAdapter
        {
            public string Id => "test.other";
            public bool CanRead(UnityEngine.Object g) => true;
            public EmberRoomGraphSnapshot Read(UnityEngine.Object g) => throw new Exception("must not read");
            public IEnumerable<UnityEngine.Object> Targets(UnityEngine.Object g) => Array.Empty<UnityEngine.Object>();
            public void WriteLayout(UnityEngine.Object g, IReadOnlyDictionary<string, Vector2> p) { }
            public void AddConnection(UnityEngine.Object g, EmberRoomGraphConnection e) { }
            public void RemoveConnection(UnityEngine.Object g, EmberRoomGraphConnection e) { }
            public IEnumerable<EmberLevelDiagnostic> Validate(UnityEngine.Object g) => Array.Empty<EmberLevelDiagnostic>();
        }
        [Test] public void FormalConfigurationActuallyAddsAndRemovesMenusAndGuardsAuthoringApis()
        {
            bool Exists(string path) => (bool)typeof(EditorApplication).Assembly.GetType("UnityEditor.Menu").GetMethod("MenuItemExists", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { path });
            EmberConditionalMenu.Refresh();
            Assert.IsTrue(Exists(EmberRoomGraphService.MenuPath)); Assert.IsTrue(Exists(EmberFsmBlueprintAccess.MenuPath)); Assert.IsTrue(Exists("Ember/Level/2D 关卡工作台"));
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
            Assert.IsFalse(Exists(EmberRoomGraphService.MenuPath)); Assert.IsFalse(Exists(EmberFsmBlueprintAccess.MenuPath)); Assert.IsFalse(Exists("Ember/Level/2D 关卡工作台"));
            Assert.IsNull(EmberLevelAdapters.Get("topdown.level"));
            Assert.Throws<InvalidOperationException>(() => EmberLevelAssetService.Upsert<EmberRoomGraphTestAsset>(_folder + "/Rejected.asset", _ => { }));
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(_folder + "/Rejected.asset"));
            Assert.Throws<InvalidOperationException>(() => EmberLevelObjectEditing.Duplicate(null));
            Assert.Throws<InvalidOperationException>(() => EmberTerrainEditing.Paint(null, default, null));
            using var legacy = EmberFsmBlueprintAccess.Register(EmberFsmBlueprintAccess.CurrentTemplateId, () => true);
            Assert.IsFalse(EmberFsmBlueprintAccess.IsAvailable, "旧提供者登记不能绕过正式配置。");
        }
        [Test] public void UnknownConfigurationFailsClosedAndRetainsOriginalBytes()
        {
            string invalid = "{\"schemaVersion\":99,\"enabled\":[\"unknown.id\"]}";
            File.WriteAllText(_configPath, invalid);
            Assert.IsFalse(EmberExtensionProject.IsEnabled(EmberExtensionProject.RoomGraph));
            Assert.Throws<InvalidDataException>(() => EmberExtensionProject.Load());
            Assert.AreEqual(invalid, File.ReadAllText(_configPath));
        }
        [UnityTest] public IEnumerator CanvasRendersPortsAndMovementKeepsDraftOutOfOriginalAsset()
        {
            var window = ScriptableObject.CreateInstance<EmberRoomGraphWindow>(); window.position = new Rect(30, 30, 1100, 650); window.Show();
            try
            {
                window.SetGraph(_asset, _adapter.Id); yield return null;
                Assert.AreSame(_asset, window.rootVisualElement.Q<UnityEditor.UIElements.ObjectField>().value);
                var board = window.rootVisualElement.Q<GraphView>();
                Assert.AreEqual(2, board.nodes.Count()); Assert.AreEqual(8, board.ports.Count());
                var node = board.nodes.First(); var at = node.GetPosition(); at.position += new Vector2(50, 60); node.SetPosition(at);
                board.graphViewChanged(new GraphViewChange { movedElements = new List<GraphElement> { node } });
                Assert.IsTrue(_asset.rooms.All(r => r.graphPosition == Vector2.zero));
                var serializedDraft = EditorJsonUtility.ToJson(window);
                var restored = ScriptableObject.CreateInstance<EmberRoomGraphWindow>();
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(serializedDraft, restored);
                    Assert.AreEqual(serializedDraft, EditorJsonUtility.ToJson(restored), "窗口序列化保留源资产、精确模板身份和图位置草稿。");
                }
                finally { UnityEngine.Object.DestroyImmediate(restored); }
                _module = false;
                typeof(EmberRoomGraphWindow).GetMethod("RefreshAccess", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(window, null);
                Assert.IsFalse(board.enabledInHierarchy); Assert.AreEqual(serializedDraft, EditorJsonUtility.ToJson(window));
            }
            finally { window.Close(); }
        }
        [Test] public void DisabledExtensionKeepsOwnedTrialStopAndCleanupAvailable()
        {
            Assert.IsFalse(EmberLevelTrialSession.Active, "不覆盖用户实际试玩会话。");
            var adapter = new CleanupAdapter();
            EmberLevelAdapters.Register(adapter, EmberFsmBlueprintAccess.CurrentTemplateId ?? "", () => true);
            var type = typeof(EmberLevelTrialSession); const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var field = type.GetField("_state", flags);
            var state = Activator.CreateInstance(type.GetNestedType("Snapshot", BindingFlags.NonPublic), true);
            state.GetType().GetField("request").SetValue(state, new EmberLevelTrialRequest { adapterId = adapter.Id });
            string snapshot = SessionState.GetString("Ember.LevelAuthoring.Trial", "");
            string retry = SessionState.GetString("Ember.LevelAuthoring.Trial.Retry", "");
            try
            {
                field.SetValue(null, state);
                EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
                Assert.IsNull(EmberLevelAdapters.Get(adapter.Id));
                type.GetMethod("Tick", flags).Invoke(null, null);
                Assert.AreEqual(1, adapter.stopped, "条件失效自动停止，但不丢掉拥有的清理适配器。");
                Assert.IsTrue((bool)state.GetType().GetField("returning").GetValue(state));
                EmberLevelTrialSession.Stop(); Assert.AreEqual(2, adapter.stopped, "关闭后仍允许显式 Stop。");
            }
            finally
            {
                field.SetValue(null, null); EmberLevelAdapters.Unregister(adapter);
                if (snapshot.Length == 0) SessionState.EraseString("Ember.LevelAuthoring.Trial"); else SessionState.SetString("Ember.LevelAuthoring.Trial", snapshot);
                if (retry.Length == 0) SessionState.EraseString("Ember.LevelAuthoring.Trial.Retry"); else SessionState.SetString("Ember.LevelAuthoring.Trial.Retry", retry);
            }
        }
        private sealed class CleanupAdapter : IEmberLevelAdapter
        {
            public int stopped;
            public string Id => "test.cleanup";
            public string UnavailableReason => null;
            public string TrialStatus => "cleanup fixture";
            public bool TrialReady => false;
            public bool IsCleaning => true; // 禁止测试触碰真实编辑场景恢复。
            public IEnumerable<EmberLevelDescriptor> Discover() => Array.Empty<EmberLevelDescriptor>();
            public IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor l, EmberLevelTrialRequest r) => Array.Empty<EmberLevelDiagnostic>();
            public EmberLevelDescriptor Create(string folder, string name, EmberLevelDescriptor source) => throw new NotSupportedException();
            public void BeginTrial(EmberLevelTrialRequest request) => throw new NotSupportedException();
            public void StopTrial() => stopped++;
        }
        [Test] public void ExplicitProjectObservationScopeStillRequiresExtensionAndModule()
        {
            int calls = 0; bool module = true;
            EmberFsmObservationSources.Register("test.project-scope", () => { calls++; return Array.Empty<EmberFsmObservedInstance>(); }, ownerTemplateId: "", enabled: () => module);
            try
            {
                EmberFsmObservationSources.Read(null); Assert.AreEqual(1, calls);
                module = false; EmberFsmObservationSources.Read(null); Assert.AreEqual(1, calls);
                module = true; EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
                EmberFsmObservationSources.Read(null); Assert.AreEqual(1, calls);
            }
            finally { EmberFsmObservationSources.Remove("test.project-scope"); }
        }
    }
}
