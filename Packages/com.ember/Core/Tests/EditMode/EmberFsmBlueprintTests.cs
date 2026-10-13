using System.Collections;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        [UnityTest]
        public IEnumerator BlueprintExplicitOverlapRepairIncludesReferencesAndSurvivesCloneRoundTrip()
        {
            string path = "Assets/FsmOverlapTest-" + System.Guid.NewGuid().ToString("N") + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(_graph, path);
            string key = "Ember.Fsm.Layout." + Application.dataPath + "."
                + UnityEditor.AssetDatabase.AssetPathToGUID(path) + ".v4.exterior.";
            var clone = Object.Instantiate(_graph);
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas = window.rootVisualElement.Q<GraphView>();
                var idle = BlueprintPosition(canvas, "Idle");
                var move = canvas.nodes.Single(n => n.viewDataKey == "Move");
                // 正文不相交，但前一个状态右侧引用卡片与 Move 左侧来源重叠。
                move.SetPosition(new Rect(idle + new Vector2(350, 0), new Vector2(320, 160)));
                window.RefreshGraph(); window.SaveLayout();
                var before = BlueprintPosition(canvas, "Move");
                window.RefreshGraph();
                Assert.That(BlueprintPosition(canvas, "Move"), Is.EqualTo(before), "刷新不能暗中修复布局");
                var envelope = typeof(EmberFsmBlueprintWindow).GetMethod("Envelope",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(((Rect)envelope.Invoke(window, new object[] { "Idle" }))
                    .Overlaps((Rect)envelope.Invoke(window, new object[] { "Move" })), Is.True);
                var moved = window.RepairLayoutOverlaps();
                Assert.That(moved, Does.Contain("Move"));
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(idle));
                Assert.That(((Rect)envelope.Invoke(window, new object[] { "Idle" }))
                    .Overlaps((Rect)envelope.Invoke(window, new object[] { "Move" })), Is.False);
                var repaired = BlueprintPosition(canvas, "Move");
                Assert.That(window.RepairLayoutOverlaps(), Is.Empty, "重复修复必须幂等");
                canvas.UpdateViewTransform(new Vector3(123, 234, 0), new Vector3(.65f, .65f, 1));
                yield return null;
                window.SaveLayout();
                float savedPan = UnityEditor.EditorPrefs.GetFloat(key + "panX");
                window.SetGraph(clone);
                yield return null;
                canvas.UpdateViewTransform(Vector3.zero, Vector3.one);
                window.SetGraph(_graph);
                yield return null;
                Assert.That(BlueprintPosition(canvas, "Move"), Is.EqualTo(repaired));
                Assert.That(canvas.contentViewContainer.resolvedStyle.translate.x, Is.EqualTo(savedPan).Within(.01));
                Assert.That(canvas.contentViewContainer.resolvedStyle.scale.value.x, Is.EqualTo(.65f).Within(.01));
                window.Close(); window = IsolatedLayoutWindow(_graph);
                yield return null;
                canvas = window.rootVisualElement.Q<GraphView>();
                Assert.That(BlueprintPosition(canvas, "Move"), Is.EqualTo(repaired));
                Assert.That(_definition.CreateCount, Is.Zero);
            }
            finally
            {
                window.Close(); Object.DestroyImmediate(clone);
                foreach (var node in _graph.Nodes)
                    foreach (var suffix in new[] { ".x", ".y", ".fold", ".redirect" })
                        UnityEditor.EditorPrefs.DeleteKey(key + node.Id + suffix);
                foreach (var suffix in new[] { "saved", "redirects", "panX", "panY", "zoom" })
                    UnityEditor.EditorPrefs.DeleteKey(key + suffix);
                _assets.Remove(_graph); UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }

        [UnityTest]
        public IEnumerator BlueprintFitUsesNewBoundsOnSameFrameNavigationAndMove()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                window.position = new Rect(80, 80, 1500, 900);
                yield return null;
                window.Navigate("Ground");
                var canvas = window.rootVisualElement.Q<GraphView>();
                BlueprintMove(canvas, "Idle", new Vector2(270, 360));
                window.RefreshGraph(); window.FitVisible(); // 同帧，不依赖上帧 layout
                yield return null;
                foreach (var node in canvas.nodes)
                {
                    Assert.That(canvas.worldBound.Contains(node.worldBound.min), Is.True, node.viewDataKey);
                    Assert.That(canvas.worldBound.Contains(node.worldBound.max), Is.True, node.viewDataKey);
                }
                window.SetFullNested(true); window.FitVisible();
                yield return null;
                foreach (var node in canvas.nodes)
                {
                    Assert.That(canvas.worldBound.Contains(node.worldBound.min), Is.True, node.viewDataKey);
                    Assert.That(canvas.worldBound.Contains(node.worldBound.max), Is.True, node.viewDataKey);
                }
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintLocalReferencesPreserveIdentityAndNavigateWithoutRunning()
        {
            Start(); _fsm.Update(2); var json = JsonUtility.ToJson(_graph); var log = _context.Log.ToArray();
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas = window.rootVisualElement.Q<GraphView>();
                Assert.That(window.IsFullNested, Is.True);
                Assert.That(window.DeclarationKeys.Length, Is.EqualTo(48));
                Assert.That(window.DeclarationKeys.Distinct().Count(), Is.EqualTo(48));
                Assert.That(canvas.Query<VisualElement>("source-reference").ToList().All(e=>e.GetFirstAncestorOfType<Node>()==null),Is.True);
                Assert.That(canvas.Query<VisualElement>("target-reference").ToList().All(e=>e.GetFirstAncestorOfType<Node>()==null),Is.True);
                Assert.That(canvas.edges.Count(), Is.Zero); // 不再创建跨画布关系边
                foreach (var node in canvas.nodes)
                    Assert.That(node.Query<VisualElement>("local-entry-anchor").ToList().Count, Is.EqualTo(1));
                window.SelectDeclaration("Ground", false, "jump");
                Assert.That(window.SelectedPathSegmentCount, Is.EqualTo(2));
                Assert.That(window.IncomingKeys("Jump"), Does.Contain("6:Ground:T:jump"));
                window.SetCollapsed("Alive", true); window.NavigateToState("Jump");
                Assert.That(canvas.nodes.Any(n=>n.viewDataKey=="Jump"), Is.True);
                Assert.That(window.FocusId, Is.Null);
                window.SetRedirectsForNode("Ground",true);
                Assert.That(window.VisibleRedirectCount, Is.EqualTo(2));
                Assert.That(window.IncomingKeys("Jump").Length, Is.EqualTo(8)); // 预检不混入普通来源
                Assert.That(JsonUtility.ToJson(_graph), Is.EqualTo(json));
                Assert.That(_context.GuardCount, Is.Zero); CollectionAssert.AreEqual(log,_context.Log);
                AssertPath("Alive","Ground","Idle");
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintNavigationPreservesSingleCanvasCoordinates()
        {
            var window=IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas=window.rootVisualElement.Q<GraphView>();
                var expected=BlueprintPosition(canvas,"Idle")+new Vector2(73,49);
                BlueprintMove(canvas,"Idle",new Vector2(73,49));
                window.Navigate("Air"); window.Navigate("Ground"); window.RefreshGraph();
                Assert.That(BlueprintPosition(canvas,"Idle"),Is.EqualTo(expected));
                Assert.That(window.IsFullNested,Is.True);
            }
            finally { window.Close(); }
        }

        [Test]
        public void ExecutionPreviewResolvesInheritedAndOverriddenBodiesWithoutConstruction()
        {
            var data=EmberFsmExecutionPreview.Inspect(typeof(PreviewDerived));
            Assert.That(data.Count,Is.EqualTo(9));
            var enter=data.Single(m=>m.Name=="Enter");
            Assert.That(enter.ImplementationKind,Does.Contain("继承"));
            Assert.That(enter.Declaration,Does.Contain("EmberFsmState"));
            Assert.That(enter.Source,Does.Contain("Activation = activation"),enter.Status);
            var tick=data.Single(m=>m.Name=="FixedUpdate");
            Assert.That(tick.ImplementationKind,Does.Contain("重写"));
            Assert.That(tick.Source,Does.Contain("if (deltaTime > 0) return;"),tick.Status);
            Assert.That(tick.Source,Does.Not.Contain("WRONG_OVERLOAD"));
            Assert.That(tick.Path,Does.EndWith("EmberFsmExecutionPreviewFixture.cs"));
            Assert.That(_definition.CreateCount,Is.Zero);
        }
        private sealed partial class PreviewDerived : EmberFsmState<object>
        {
            public void FixedUpdate(string text) { throw new System.Exception("WRONG_OVERLOAD"); }
        }
        [Test]
        public void ExecutionPreviewUnavailableTypeDoesNotInventCode()
        {
            var data=EmberFsmExecutionPreview.Inspect(typeof(string));
            Assert.That(data.All(m=>m.Source==null && m.Status.Contains("不可用")),Is.True);
        }

        [UnityTest]
        public IEnumerator BlueprintDragGridSnapKeepsParentAndSelectedDescendantTogether()
        {
            var window=IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                var canvas=window.rootVisualElement.Q<GraphView>();
                var before=BlueprintPosition(canvas,"Idle");
                var parent=canvas.nodes.Single(n=>n.viewDataKey=="Alive");
                var child=canvas.nodes.Single(n=>n.viewDataKey=="Idle");
                var original=BlueprintPosition(canvas,"Alive");
                var wanted=new Vector2(51,83); var delta=wanted-original;
                BlueprintMove(canvas,"Alive",delta); BlueprintMove(canvas,"Idle",delta);
                canvas.AddToSelection(parent); canvas.AddToSelection(child);
                typeof(EmberFsmBlueprintWindow).GetMethod("PreviewDragGuides",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(window,null);
                Assert.That(BlueprintPosition(canvas,"Alive").x%16,Is.EqualTo(0).Within(.001));
                Assert.That(BlueprintPosition(canvas,"Alive").y%16,Is.EqualTo(0).Within(.001));
                canvas.graphViewChanged(new GraphViewChange { movedElements=new System.Collections.Generic.List<GraphElement>{parent,child} });
                var parentAfter=BlueprintPosition(canvas,"Alive");
                Assert.That(parentAfter.x%16,Is.EqualTo(0).Within(.001));
                Assert.That(parentAfter.y%16,Is.EqualTo(0).Within(.001));
                Assert.That(BlueprintPosition(canvas,"Idle"),Is.EqualTo(before+parentAfter-original));
                foreach(var node in canvas.nodes)
                {
                    Assert.That(node.mainContainer.style.height.value.value,Is.EqualTo(node.style.height.value.value));
                    Assert.That(node.mainContainer.style.width.value.value,Is.EqualTo(node.style.width.value.value));
                }
                window.RefreshGraph();
                Assert.That(BlueprintPosition(canvas,"Idle"),Is.EqualTo(before+parentAfter-original));
            }
            finally { window.Close(); }
        }

        private static Vector2 BlueprintPosition(GraphView canvas, string id)
        {
            var node = canvas.nodes.Single(n => n.viewDataKey == id);
            return new Vector2(node.style.left.value.value, node.style.top.value.value);
        }

        private static void BlueprintMove(GraphView canvas, string id, Vector2 delta)
        {
            var node = canvas.nodes.Single(n => n.viewDataKey == id);
            node.SetPosition(new Rect(BlueprintPosition(canvas, id) + delta,
                new Vector2(node.style.width.value.value, node.style.height.value.value)));
        }

        [UnityTest]
        public IEnumerator BlueprintCompositeBoundsContainChildrenAndEntriesAreNotEdges()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                var canvas = window.rootVisualElement.Q<GraphView>();
                foreach (var info in window.Description.Nodes.Where(n => !string.IsNullOrEmpty(n.ParentId)))
                {
                    var parent = canvas.nodes.Single(n => n.viewDataKey == info.ParentId);
                    var child = canvas.nodes.Single(n => n.viewDataKey == info.Id);
                    var bounds = new Rect(BlueprintPosition(canvas, info.ParentId),
                        new Vector2(parent.style.width.value.value, parent.style.height.value.value));
                    Assert.That(bounds.Contains(BlueprintPosition(canvas, info.Id)), Is.True, info.Id);
                    Assert.That(bounds.Contains(BlueprintPosition(canvas, info.Id) +
                        new Vector2(child.style.width.value.value, child.style.height.value.value)), Is.True, info.Id);
                }
                Assert.That(window.DeclarationKeys.Length, Is.EqualTo(48));
                Assert.That(window.VisibleRedirectCount, Is.EqualTo(16));
                Assert.That(canvas.nodes.Count(n => n.title.Contains("复合状态")), Is.EqualTo(3));
                Assert.That(canvas.nodes.Single(n => n.viewDataKey == "Alive").title, Does.Contain("图入口"));
                Assert.That(window.StructuralEntryCount, Is.EqualTo(4));
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintParentAndNestedMultiMoveApplyExactlyOnceIncludingHiddenChildren()
        {
            Start(); _fsm.Update(2);
            var json = JsonUtility.ToJson(_graph); var log = _context.Log.ToArray();
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                var canvas = window.rootVisualElement.Q<GraphView>();
                var initial = BlueprintPosition(canvas, "Idle"); var delta = new Vector2(90, 45);
                BlueprintMove(canvas, "Alive", delta);
                // 跨层多选：中间 Ground 未选中，Idle 不能重复继承 Alive 位移。
                BlueprintMove(canvas, "Idle", delta);
                window.RefreshGraph();
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(initial + delta));
                window.SetCollapsed("Ground", true);
                BlueprintMove(canvas, "Ground", delta);
                window.RefreshGraph(); window.SetCollapsed("Ground", false);
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(initial + delta * 2));
                var move = BlueprintPosition(canvas, "Move");
                BlueprintMove(canvas, "Idle", delta); window.RefreshGraph();
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(initial + delta * 3));
                Assert.That(BlueprintPosition(canvas, "Move"), Is.EqualTo(move));
                Assert.That(JsonUtility.ToJson(_graph), Is.EqualTo(json));
                AssertPath("Alive", "Ground", "Idle");
                Assert.That(_fsm.GetElapsedTime("Idle"), Is.EqualTo(2));
                Assert.That(_context.GuardCount, Is.Zero); CollectionAssert.AreEqual(log, _context.Log);
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintCollapsedProxiesPreserveEveryCrossBoundaryIdentity()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                var canvas = window.rootVisualElement.Q<GraphView>();
                var all=window.DeclarationKeys.OrderBy(s=>s).ToArray();
                window.SetCollapsed("Ground",true);
                CollectionAssert.AreEqual(all,window.DeclarationKeys.OrderBy(s=>s).ToArray());
                Assert.That(canvas.nodes.Any(n=>n.viewDataKey=="Idle"),Is.False);
                Assert.That(canvas.nodes.Single(n=>n.viewDataKey=="Ground").Query<Foldout>().ToList().Any(),Is.True);
                window.NavigateToState("Idle");
                Assert.That(canvas.nodes.Any(n=>n.viewDataKey=="Idle"),Is.True);
                CollectionAssert.AreEqual(all,window.DeclarationKeys.OrderBy(s=>s).ToArray());
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintCyclesMissingParentAndDefaultRemainDiagnosable()
        {
            _graph.Configure("A", Node("A", "B", "B"), Node("B", "A", "A"),
                Node("orphan", "missing"), Node("parent"), Node("child", "parent"));
            var json = JsonUtility.ToJson(_graph);
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                window.RefreshGraph(); window.SetCollapsed("A", true);
                Assert.That(window.Description.IsValid, Is.False);
                Assert.That(window.Description.Errors.Any(e => e.Contains("cycle")), Is.True);
                Assert.That(window.Description.Errors.Any(e => e.Contains("invalid parent")), Is.True);
                Assert.That(window.Description.Errors.Any(e => e.Contains("missing default")), Is.True);
                Assert.That(window.VisibleNodeCount, Is.EqualTo(5));
                Assert.That(JsonUtility.ToJson(_graph), Is.EqualTo(json));
                Assert.That(_definition.CreateCount, Is.Zero);
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintLegacyLayoutSurvivesMigrationAndSameFrameSaveReopen()
        {
            string path = "Assets/FsmBlueprintLayoutTest-" + System.Guid.NewGuid().ToString("N") + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(_graph, path);
            string key = "Ember.Fsm.Layout." + Application.dataPath + "." + UnityEditor.AssetDatabase.AssetPathToGUID(path) + ".";
            UnityEditor.EditorPrefs.SetFloat(key + "Idle.x", 713);
            UnityEditor.EditorPrefs.SetFloat(key + "Idle.y", 479);
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                var canvas = window.rootVisualElement.Q<GraphView>();
                var migrated = BlueprintPosition(canvas, "Idle");
                Assert.That(float.IsFinite(migrated.y), Is.True);
                Assert.That(float.IsFinite(migrated.x), Is.True);
                BlueprintMove(canvas, "Ground", new Vector2(60, 30));
                window.RefreshGraph(); window.SaveLayout(); // 不等待 UI 布局
                BlueprintMove(canvas, "Idle", new Vector2(23, 17));
                // 不点击保存：关闭/域重载路径必须捕获同帧 inline 位置。
                window.Close(); window = IsolatedLayoutWindow(_graph);
                yield return null;
                canvas = window.rootVisualElement.Q<GraphView>();
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(migrated + new Vector2(83,47)));
                var preserved = BlueprintPosition(canvas, "Idle");
                window.Close();
                // 图新增状态等价于已有 v4 布局缺少该 ID；不得重排已保存兄弟。
                UnityEditor.EditorPrefs.DeleteKey(key + "v4.exterior.Move.x");
                UnityEditor.EditorPrefs.DeleteKey(key + "v4.exterior.Move.y");
                window = IsolatedLayoutWindow(_graph);
                canvas = window.rootVisualElement.Q<GraphView>();
                Assert.That(BlueprintPosition(canvas, "Idle"), Is.EqualTo(preserved));
                var envelope = typeof(EmberFsmBlueprintWindow).GetMethod("Envelope",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var idleBounds = (Rect)envelope.Invoke(window, new object[] { "Idle" });
                var moveBounds = (Rect)envelope.Invoke(window, new object[] { "Move" });
                Assert.That(idleBounds.Overlaps(moveBounds), Is.False);
                Assert.That(UnityEditor.EditorPrefs.GetFloat(key + "Idle.x"), Is.EqualTo(713));
                Assert.That(UnityEditor.EditorPrefs.GetFloat(key + "Idle.y"), Is.EqualTo(479));
                Assert.That(UnityEditor.EditorUtility.IsDirty(_graph), Is.False);
            }
            finally
            {
                window.Close();
                foreach (var node in _graph.Nodes)
                    foreach (var prefix in new[] { key, key + "v2.canvas.", key + "v3.nested.", key + "v4.exterior." })
                        foreach (var suffix in new[] { ".x", ".y", ".fold" }) UnityEditor.EditorPrefs.DeleteKey(prefix + node.Id + suffix);
                foreach (var suffix in new[] { "v2.canvas.saved", "v3.nested.saved", "v3.nested.redirects", "v3.nested.panX", "v3.nested.panY", "v3.nested.zoom", "v3.lastNested", "v3.lastFocus", "v4.exterior.saved", "v4.exterior.redirects", "v4.exterior.panX", "v4.exterior.panY", "v4.exterior.zoom" }) UnityEditor.EditorPrefs.DeleteKey(key + suffix);
                _assets.Remove(_graph); UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }

        [UnityTest]
        public IEnumerator BlueprintImmediateRefreshPreservesUnrenderedNodePosition()
        {
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                var canvas = window.rootVisualElement.Q<GraphView>();
                var node = canvas.nodes.Single(n => n.viewDataKey == "Idle");
                node.SetPosition(new Rect(713, 479, 270, 160));
                // 刻意不等待 UI 下一帧布局，覆盖同步刷新/折叠/保存链。
                window.RefreshGraph(); window.RefreshGraph();
                node = canvas.nodes.Single(n => n.viewDataKey == "Idle");
                Assert.That(node.style.left.value.value, Is.EqualTo(713));
                Assert.That(node.style.top.value.value, Is.EqualTo(479));
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintRefreshDoesNotConstructOrMutateGraph()
        {
            var json = JsonUtility.ToJson(_graph);
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                for (int i = 0; i < 10; i++) window.RefreshGraph();
                Assert.That(window.Description.IsValid, Is.True);
                Assert.That(window.VisibleNodeCount, Is.EqualTo(8));
                Assert.That(window.VisibleEdgeCount, Is.EqualTo(64)); // 8 × (6 代码转换 + 2 静态重定向)，不再绘制层级边
                Assert.That(_definition.CreateCount, Is.Zero);
                Assert.That(_context.GuardCount, Is.Zero);
                Assert.That(JsonUtility.ToJson(_graph), Is.EqualTo(json));
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintCollapseAndRefreshCannotControlRunningMachine()
        {
            Start(); _fsm.Update(2);
            var json = JsonUtility.ToJson(_graph);
            var log = _context.Log.ToArray();
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                window.SetCollapsed("Alive", true);
                Assert.That(window.VisibleNodeCount, Is.EqualTo(2));
                window.SetCollapsed("Alive", false);
                Assert.That(window.VisibleNodeCount, Is.EqualTo(8));
                window.RefreshGraph();
                AssertPath("Alive", "Ground", "Idle");
                Assert.That(_fsm.GetElapsedTime("Idle"), Is.EqualTo(2));
                Assert.That(_context.GuardCount, Is.Zero);
                CollectionAssert.AreEqual(log, _context.Log);
                Assert.That(JsonUtility.ToJson(_graph), Is.EqualTo(json));
            }
            finally { window.Close(); }
        }

        [UnityTest]
        public IEnumerator BlueprintInvalidGraphReportsErrorsWithoutRuntimeCreation()
        {
            _graph.Configure("missing", new EmberFsmNode("broken", "broken", null));
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                yield return null;
                window.SetFullNested(true); window.SetShowRedirects(true);
                window.RefreshGraph();
                Assert.That(window.Description.IsValid, Is.False);
                Assert.That(window.Description.Errors.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(window.VisibleNodeCount, Is.EqualTo(1));
                Assert.That(_definition.CreateCount, Is.Zero);
            }
            finally { window.Close(); }
        }
    }
}
