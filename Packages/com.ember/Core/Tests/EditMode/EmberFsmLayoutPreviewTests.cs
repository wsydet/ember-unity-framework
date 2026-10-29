using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        #region 内部方法
        private static Dictionary<string, Vector2> LayoutPositions(EmberFsmBlueprintWindow window) =>
            new((Dictionary<string, Vector2>)typeof(EmberFsmBlueprintWindow)
                .GetField("_positions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window));
        private EmberFsmBlueprintWindow IsolatedLayoutWindow(EmberFsmGraph graph)
        {
            _blueprintScope ??= EmberFsmBlueprintAccess.Register(EmberFsmBlueprintAccess.CurrentTemplateId, () => true);
            var window = ScriptableObject.CreateInstance<EmberFsmBlueprintWindow>();
            window.Show(); window.SetGraph(graph); return window;
        }
        private System.IDisposable _blueprintScope;
        private static void AssertLayout(Dictionary<string, Vector2> expected, EmberFsmBlueprintWindow window)
        { CollectionAssert.AreEquivalent(expected, LayoutPositions(window)); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [UnityTest]
        public IEnumerator LayoutRuntimeObservationAndPresetChangesKeepCoordinates()
        {
            Start();
            const string provider = "layout-observation-test";
            EmberFsmObservationSources.Register(provider, () => new[]
                { new EmberFsmObservedInstance(_graph, _graph, EmberFsmObservation.Capture(_fsm)) });
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.ObserveInstance(_fsm.InstanceId);
                var before = LayoutPositions(window);
                window.PreviewLayout("all");
                Request("Idle", EmberFsmTestState.Move);
                var log = _context.Log.ToArray(); var guards = _context.GuardCount;
                window.RefreshRuntimeObservation(); AssertLayout(before, window);
                CollectionAssert.AreEqual(log, _context.Log); Assert.That(_context.GuardCount, Is.EqualTo(guards));
                Assert.That(window.rootVisualElement.Q<Button>("apply-layout-preview").enabledSelf, Is.True);
                window.CancelLayoutPreview();
                window.ParameterPreset = _definition; window.RefreshGraph(); AssertLayout(before, window);
                window.ParameterPreset = null; window.SetGraph(_graph); AssertLayout(before, window);
            }
            finally { window.Close(); EmberFsmObservationSources.Remove(provider); }
        }

        [UnityTest]
        public IEnumerator LayoutLongLabelsAndOutOfScopeConflictRemainExplicit()
        {
            var data = new SerializedObject(_graph);
            string title = string.Concat(Enumerable.Repeat("很长的复合状态显示名称", 12));
            data.FindProperty("_nodes").GetArrayElementAtIndex(1).FindPropertyRelative("_displayName").stringValue = title;
            data.ApplyModifiedPropertiesWithoutUndo(); data.Dispose();
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas = window.rootVisualElement.Q<GraphView>();
                Assert.That(canvas.nodes.Single(n => n.viewDataKey == "Ground").titleContainer.tooltip, Does.Contain(title));
                BlueprintMove(canvas, "Move", new Vector2(60, 90)); window.RefreshGraph();
                canvas.AddToSelection(canvas.nodes.Single(n => n.viewDataKey == "Ground"));
                var before = LayoutPositions(window);
                Assert.That(window.PreviewLayout("branch"), Is.Not.Empty, "空间不足必须说明冲突而非挪走邻接分支");
                Assert.That(window.ApplyLayoutPreview(), Is.False); AssertLayout(before, window);
                window.CancelLayoutPreview();
                Assert.That(window.PreviewLayout("all"), Is.Empty); window.ApplyLayoutPreview();
                var arranged = LayoutPositions(window);
                Assert.That(window.PreviewLayout("all"), Is.Empty);
                Assert.That(window.ApplyLayoutPreview(), Is.False, "重复整理应稳定"); AssertLayout(arranged, window);
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator LayoutPreviewCancelApplyUndoRedoAndRefresh()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas = window.rootVisualElement.Q<GraphView>();
                BlueprintMove(canvas, "Move", new Vector2(350, -150)); window.RefreshGraph();
                var before = LayoutPositions(window); var graphBefore = EditorJsonUtility.ToJson(_graph);
                Assert.That(window.PreviewLayout("all"), Is.Empty); AssertLayout(before, window);
                window.CancelLayoutPreview(); AssertLayout(before, window);
                Assert.That(window.PreviewLayout("all"), Is.Empty);
                Assert.That(window.ApplyLayoutPreview(), Is.True);
                var applied = LayoutPositions(window);
                Assert.That(applied.Any(p => p.Value != before[p.Key]), Is.True);
                Undo.PerformUndo(); AssertLayout(before, window);
                Undo.PerformRedo(); AssertLayout(applied, window);
                window.RefreshGraph(); AssertLayout(applied, window);
                Assert.That(EditorJsonUtility.ToJson(_graph), Is.EqualTo(graphBefore));
                window.PreviewLayout("all"); window.RefreshGraph();
                Assert.That(window.ApplyLayoutPreview(), Is.False, "刷新后不能应用过期候选");
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator LayoutBranchKeepsOutsideAndPinnedCompositeProtectsDescendants()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                Assert.That(window.PreviewLayout("all"), Is.Empty);
                window.ApplyLayoutPreview();
                var canvas = window.rootVisualElement.Q<GraphView>();
                BlueprintMove(canvas, "Move", new Vector2(60, 90)); window.RefreshGraph();
                canvas.AddToSelection(canvas.nodes.Single(n => n.viewDataKey == "Ground"));
                var before = LayoutPositions(window);
                Assert.That(window.PreviewLayout("branch"), Is.Empty);
                Assert.That(window.ApplyLayoutPreview(), Is.True);
                foreach (var id in new[] { "Alive", "Air", "Jump", "Fall", "Dead", "Ground" })
                    Assert.That(LayoutPositions(window)[id], Is.EqualTo(before[id]), id);
                window.SetLayoutPinned(new[] { "Ground" }, true);
                var pinned = LayoutPositions(window);
                var conflicts = window.PreviewLayout("all");
                if (conflicts.Length == 0) window.ApplyLayoutPreview();
                else Assert.That(window.ApplyLayoutPreview(), Is.False);
                foreach (var id in new[] { "Ground", "Idle", "Move" })
                    Assert.That(LayoutPositions(window)[id], Is.EqualTo(pinned[id]), id);
                Assert.That(canvas.nodes.Single(n => n.viewDataKey == "Idle").capabilities.HasFlag(Capabilities.Movable), Is.False);
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator LayoutPinnedConflictRejectsInsteadOfUnlocking()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas = window.rootVisualElement.Q<GraphView>();
                var idle = BlueprintPosition(canvas, "Idle");
                var move = canvas.nodes.Single(n => n.viewDataKey == "Move");
                move.SetPosition(new Rect(idle, new Vector2(320, 160))); window.RefreshGraph();
                window.SetLayoutPinned(new[] { "Idle", "Move" }, true);
                var before = LayoutPositions(window);
                Assert.That(window.PreviewLayout("overlaps"), Is.Not.Empty);
                Assert.That(window.ApplyLayoutPreview(), Is.False); AssertLayout(before, window);
                window.CancelLayoutPreview(); AssertLayout(before, window);
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator LayoutPinPersistenceAndCollapsedDescendantsRoundTrip()
        {
            string path = "Assets/FsmLayoutPreview-" + System.Guid.NewGuid().ToString("N") + ".asset";
            AssetDatabase.CreateAsset(_graph, path);
            string key = "Ember.Fsm.Layout." + Application.dataPath + "." + AssetDatabase.AssetPathToGUID(path) + ".v4.exterior.";
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetLayoutPinned(new[] { "Ground" }, true);
                window.SetCollapsed("Ground", true);
                var before = LayoutPositions(window);
                window.PreviewLayout("all"); window.CancelLayoutPreview(); AssertLayout(before, window);
                window.SaveLayout(); window.Close(); window = IsolatedLayoutWindow(_graph);
                yield return null;
                AssertLayout(before, window);
                Assert.That(EditorPrefs.GetBool(key + "Ground.pinned"), Is.True);
                window.SetCollapsed("Ground", false);
                foreach (var id in new[] { "Ground", "Idle", "Move" })
                    Assert.That(LayoutPositions(window)[id], Is.EqualTo(before[id]));
            }
            finally
            {
                window.Close();
                foreach (var id in _graph.Nodes.Select(n => n.Id))
                    foreach (var suffix in new[] { ".x", ".y", ".fold", ".redirect", ".pinned" }) EditorPrefs.DeleteKey(key + id + suffix);
                foreach (var suffix in new[] { "saved", "redirects", "panX", "panY", "zoom" }) EditorPrefs.DeleteKey(key + suffix);
                _assets.Remove(_graph); AssetDatabase.DeleteAsset(path);
            }
        }
        #endregion
    }
}
