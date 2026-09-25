using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeGraphInteractionTests
    {
        #region 内部参数
        private string _folder;
        private string _layoutPath;
        private NarrativeChapterSO _chapter;
        private readonly List<Object> _temporary = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private NarrativeDialogueSO MemoryNode(string id)
        {
            var node = ScriptableObject.CreateInstance<NarrativeDialogueSO>();
            // 临时子资产保证持久章节的引用有效；整个测试目录在 TearDown 中移除。
            AssetDatabase.AddObjectToAsset(node, _chapter);
            using var data = new SerializedObject(node);
            data.FindProperty("_chapterId").stringValue = _chapter.ChapterId;
            data.FindProperty("_nodeId").stringValue = id; data.ApplyModifiedPropertiesWithoutUndo(); return node;
        }
        private void Register(params NarrativeDialogueSO[] nodes)
        {
            using var data = new SerializedObject(_chapter);
            var list = data.FindProperty("_nodes"); list.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            data.FindProperty("_entry").objectReferenceValue = nodes[0]; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Link(NarrativeDialogueSO source, NarrativeNodeSO target)
        {
            using var data = new SerializedObject(source);
            data.FindProperty("_next").objectReferenceValue = target; data.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/Game/Module/Narrative/Tests/Interaction_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Game/Module/Narrative/Tests", System.IO.Path.GetFileName(_folder));
            _chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>();
            AssetDatabase.CreateAsset(_chapter, _folder + "/Chapter.asset");
            _layoutPath = NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(_folder + "/Chapter.asset") + ".asset";
        }
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_folder); AssetDatabase.DeleteAsset(_layoutPath);
            foreach (var asset in _temporary) if (asset) Object.DestroyImmediate(asset);
            _temporary.Clear();
        }
        [Test]
        public void ReadChecksUseCacheAndTypedLinks_ExplicitInvalidationRefreshes()
        {
            var a = MemoryNode("a"); var b = MemoryNode("b"); Register(a, b); Link(a, b);
            Assert.IsTrue(NarrativeGraphModel.IsTemplateActive(true)); int reads = NarrativeGraphModel.TemplateReadCount;
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(NarrativeGraphModel.CanEdit(_chapter)); Assert.AreEqual(b, NarrativeGraphModel.Target(a, "_next"));
            }
            Assert.AreEqual(reads, NarrativeGraphModel.TemplateReadCount);
            NarrativeGraphModel.InvalidateTemplateCache(); Assert.IsTrue(NarrativeGraphModel.IsTemplateActive());
            Assert.AreEqual(reads + 1, NarrativeGraphModel.TemplateReadCount);
        }
        [Test]
        public void AutoLayoutHandlesCyclesDisconnectedNodesAndUndoWithoutChangingStory()
        {
            var a = MemoryNode("a"); var b = MemoryNode("b"); var c = MemoryNode("c"); var disconnected = MemoryNode("other");
            Register(a, b, c, disconnected); Link(a, b); Link(b, c); Link(c, b);
            var story = _chapter.Nodes.ToDictionary(n => n, EditorJsonUtility.ToJson);
            var positions = NarrativeAutoLayout.Calculate(_chapter);
            Assert.AreEqual(4, positions.Count); Assert.AreEqual(4, positions.Values.Distinct().Count());
            Assert.Greater(positions[b].x, positions[a].x); Assert.AreEqual(positions[b].x, positions[c].x);
            CollectionAssert.AreEquivalent(positions, NarrativeAutoLayout.Calculate(_chapter));
            var baseline = _chapter.Nodes.ToDictionary(n => n, n => new Vector2(40, 80));
            NarrativeGraphModel.MoveMany(_chapter, baseline); Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            NarrativeGraphModel.MoveMany(_chapter, positions); Undo.FlushUndoRecordObjects();
            var layout = NarrativeGraphModel.GetLayout(_chapter, false);
            Assert.AreEqual(positions[b], layout.GetPosition("b", 0));
            Undo.PerformUndo(); Assert.AreEqual(new Vector2(40, 80), layout.GetPosition("b", 0));
            foreach (var pair in story) Assert.AreEqual(pair.Value, EditorJsonUtility.ToJson(pair.Key));
            Assert.AreEqual(new Vector2(40, -20), NarrativeAutoLayout.Snap(new Vector2(37, -27)));
        }
        [Test]
        public void AutoLayoutPutsMergeAfterBothBranches()
        {
            var choice = (NarrativeChoiceSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Choice);
            var a = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Dialogue);
            var b = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Dialogue);
            var merge = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Ending);
            NarrativeGraphModel.AddItem(choice, "_options"); NarrativeGraphModel.AddItem(choice, "_options");
            var ports = NarrativeGraphModel.Ports(choice);
            NarrativeGraphModel.Connect(_chapter, choice, ports[0], a); NarrativeGraphModel.Connect(_chapter, choice, ports[1], b);
            NarrativeGraphModel.Connect(_chapter, a, "_next", merge); NarrativeGraphModel.Connect(_chapter, b, "_next", merge);
            var positions = NarrativeAutoLayout.Calculate(_chapter);
            Assert.AreEqual(positions[a].x, positions[b].x); Assert.Greater(positions[merge].x, positions[a].x);
            Assert.AreNotEqual(positions[a].y, positions[b].y);
        }
        [UnityTest]
        public IEnumerator LargeGraphNavigationAndSpaceUseRetainedViewWithoutAssetWrites()
        {
            var nodes = Enumerable.Range(0, 200).Select(i => MemoryNode("n" + i)).ToArray(); Register(nodes);
            for (int i = 1; i < nodes.Length; i++) Link(nodes[i - 1], nodes[i]);
            string before = EditorJsonUtility.ToJson(_chapter);
            var host = ScriptableObject.CreateInstance<EditorWindow>(); host.position = new Rect(100, 100, 1100, 700);
            int searches = 0;
            var graph = new NarrativeFlowGraphView(_ => { }, action => action(), (_, _) => searches++);
            try
            {
                host.rootVisualElement.Add(graph); host.Show(); graph.Rebuild(_chapter, null);
                yield return null;
                int builds = graph.BuildCount; NarrativeGraphModel.IsTemplateActive(true); int reads = NarrativeGraphModel.TemplateReadCount;
                for (int i = 0; i < 200; i++) graph.UpdateViewTransform(new Vector3(i, i, 0), Vector3.one * (1 + i * .001f));
                Assert.AreEqual(builds, graph.BuildCount); Assert.AreEqual(reads, NarrativeGraphModel.TemplateReadCount);
                Assert.AreEqual(200, graph.NodeCount); Assert.AreEqual(199, graph.edges.Count());
                var map = graph.Q<NarrativeMiniMap>(); Assert.IsNotNull(map);
                Rect area = new(8, 8, 184, 116);
                foreach (float zoom in new[] { .1f, .25f, .87f, 1f, 3f })
                {
                    graph.UpdateViewTransform(new Vector3(-550, 230, 0), Vector3.one * zoom);
                    var mapping = map.GetMapping(area);
                    foreach (var node in graph.nodes)
                    {
                        Rect projected = mapping.Project(node.GetPosition());
                        Assert.GreaterOrEqual(projected.xMin, area.xMin - .01f); Assert.LessOrEqual(projected.xMax, area.xMax + .01f);
                        Assert.GreaterOrEqual(projected.yMin, area.yMin - .01f); Assert.LessOrEqual(projected.yMax, area.yMax + .01f);
                        Assert.Less(Vector2.Distance(node.GetPosition().center, mapping.Unproject(projected.center)), .1f);
                    }
                    Assert.AreEqual(200f, map.resolvedStyle.width, .1f); Assert.AreEqual(144f, map.resolvedStyle.height, .1f);
                }
                using (var e = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space }))
                { e.target = graph; graph.SendEvent(e); }
                Assert.AreEqual(1, searches, "空格只打开一次搜索菜单");
                float scale = graph.contentViewContainer.resolvedStyle.scale.value.x;
                using (var e = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, mousePosition = new Vector2(300, 250), delta = new Vector2(0, 2) }))
                { e.target = graph; graph.SendEvent(e); }
                Assert.AreNotEqual(scale, graph.contentViewContainer.resolvedStyle.scale.value.x, "真实滚轮事件必须缩放画布");
                Assert.AreEqual(builds, graph.BuildCount); Assert.AreEqual(before, EditorJsonUtility.ToJson(_chapter));
                Assert.IsNull(NarrativeGraphModel.GetLayout(_chapter, false), "导航不得创建布局资产");
            }
            finally { host.Close(); }
        }
        #endregion
    }
}
