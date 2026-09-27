using System;
using System.Collections;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeStoryEditorTests
    {
        private string _folder;
        private NarrativeStorySO _story;
        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/Game/Module/Narrative/Tests/Story_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Game/Module/Narrative/Tests", System.IO.Path.GetFileName(_folder));
            _story = NarrativeStoryModel.CreateStory(_folder + "/Story.asset");
        }
        [TearDown]
        public void TearDown()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:NarrativeChapterSO", new[] { _folder }))
                AssetDatabase.DeleteAsset(NarrativeGraphModel.LAYOUT_ROOT + "/" + guid + ".asset");
            AssetDatabase.DeleteAsset(NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(_folder + "/Story.asset") + ".asset");
            AssetDatabase.DeleteAsset(_folder);
        }

        [UnityTest]
        public IEnumerator OverviewPortsDoubleClickAndReadOnlyNavigationUseSameAssets()
        {
            var a = NarrativeStoryModel.CreateChapter(_story, "CH01");
            var b = NarrativeStoryModel.CreateChapter(_story, "CH02");
            NarrativeGraphModel.CreateNode(a, NovelNodeKind.ChapterExit);
            NarrativeGraphModel.CreateNode(b, NovelNodeKind.Ending);
            NarrativeStoryModel.SyncExits(_story);
            var window = ScriptableObject.CreateInstance<NarrativeGraphWindow>(); window.Show(); window.ShowStory(_story);
            try
            {
                yield return null;
                var graph = window.StoryGraph; Assert.AreEqual(2, graph.ChapterCount); Assert.IsTrue(window.IsOverview);
                var output = graph.ports.First(p => p.direction == Direction.Output);
                var input = graph.ports.First(p => p.direction == Direction.Input && p.node.title.Contains("CH02"));
                var edge = new Edge { output = output, input = input };
                graph.graphViewChanged(new GraphViewChange { edgesToCreate = new System.Collections.Generic.List<Edge> { edge } });
                Assert.AreSame(b, _story.Exits[0].Fallback);
                Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
                NarrativeStoryModel.RemoveChapter(_story, b); Undo.FlushUndoRecordObjects();
                Assert.IsNull(_story.Exits[0].Fallback); Assert.IsFalse(_story.Chapters.Contains(b));
                Undo.PerformUndo(); Assert.AreSame(b, _story.Exits[0].Fallback);
                window.ShowStory(_story); yield return null;
                var card = window.StoryGraph.nodes.First(n => n.title.Contains("CH01"));
                using (var e = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, clickCount = 2 }))
                { e.target = card; card.SendEvent(e); }
                Assert.IsFalse(window.IsOverview); Assert.AreSame(a, window.Chapter);
                window.ShowStory(_story); Assert.IsTrue(window.IsOverview);
                Assert.AreEqual(1, _story.Exits.Count, "视图切换不能创建第二套路线");
            }
            finally { window.Close(); }
        }

        [Test]
        public void ErrorLocationFindsOtherChapterAndCommandWithoutWritingAssets()
        {
            var a = NarrativeStoryModel.CreateChapter(_story, "CH01");
            var b = NarrativeStoryModel.CreateChapter(_story, "CH02");
            var node = (NarrativeDialogueSO)NarrativeGraphModel.CreateNode(b, NovelNodeKind.Dialogue);
            NarrativeGraphModel.AddItem(node, "_commands");
            var assets = new UnityEngine.Object[] { _story, a, b, node };
            var before = assets.ToDictionary(x => x, JsonUtility.ToJson);
            var window = ScriptableObject.CreateInstance<NarrativeGraphWindow>(); window.Show(); window.ShowChapter(a);
            try
            {
                window.LocateError(new NarrativeError("test", "missing resource", b.ChapterId, node.NodeId, node.Commands[0].CommandId));
                Assert.AreSame(b, window.Chapter); Assert.AreSame(node, window.SelectedNode);
                using var data = new SerializedObject(node);
                Assert.IsTrue(data.FindProperty("_commands").GetArrayElementAtIndex(0).isExpanded);
                foreach (var pair in before) Assert.AreEqual(pair.Value, JsonUtility.ToJson(pair.Key));
            }
            finally { window.Close(); }
        }

        [Test]
        public void ObservationUsesStoryIdentityWhenBrowsingAnotherStoryWithMatchingIds()
        {
            var chapter = NarrativeStoryModel.CreateChapter(_story, "CH01");
            var node = NarrativeGraphModel.CreateNode(chapter, NovelNodeKind.Ending);
            var otherStory = NarrativeStoryModel.CreateStory(_folder + "/Other/Story.asset");
            var otherChapter = NarrativeStoryModel.CreateChapter(otherStory, "CH01");
            using (var data = new SerializedObject(otherChapter))
            {
                data.FindProperty("_chapterId").stringValue = chapter.ChapterId;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var otherNode = NarrativeGraphModel.CreateNode(otherChapter, NovelNodeKind.Ending);
            using (var data = new SerializedObject(otherNode))
            {
                data.FindProperty("_nodeId").stringValue = node.NodeId;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var assets = new UnityEngine.Object[] { _story, chapter, node, otherStory, otherChapter, otherNode };
            var before = assets.ToDictionary(x => x, JsonUtility.ToJson);
            Assert.IsTrue(_story.TryReadDefinition(new Catalog(), out var definition, out _));
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(definition, new Catalog()));
            var window = ScriptableObject.CreateInstance<NarrativeGraphWindow>(); window.Show();
            try
            {
                typeof(NarrativeGraphWindow).GetField("_snapshot", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(window, runner.Snapshot);
                window.ShowChapter(otherChapter);
                Assert.IsFalse(window.Graph.nodes.Any(n => n.ClassListContains("narrative-current")),
                    "关闭跟随浏览另一个剧情时不能误高亮同 ID 节点");
                window.LocateCurrent(true);
                Assert.AreSame(_story, window.Story);
                Assert.AreSame(chapter, window.Chapter);
                Assert.AreSame(node, window.SelectedNode);
                Assert.IsTrue(window.Graph.nodes.Any(n => n.ClassListContains("narrative-current")));
                foreach (var pair in before) Assert.AreEqual(pair.Value, JsonUtility.ToJson(pair.Key));
            }
            finally
            {
                window.Close();
                AssetDatabase.DeleteAsset(NarrativeGraphModel.LAYOUT_ROOT + "/" +
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(otherStory)) + ".asset");
            }
        }

        [Test]
        public void OrganizingAssetsPreservesGuidsIdsAndLinks()
        {
            var chapter = NarrativeStoryModel.CreateChapter(_story, "CH01");
            var a = (NarrativeDialogueSO)NarrativeGraphModel.CreateNode(chapter, NovelNodeKind.Dialogue);
            var b = NarrativeGraphModel.CreateNode(chapter, NovelNodeKind.Ending);
            NarrativeGraphModel.Connect(chapter, a, "_next", b); NarrativeStoryModel.Save(_story);
            var assets = new UnityEngine.Object[] { chapter, a, b };
            var guids = assets.ToDictionary(x => x, x => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x)));
            string id = a.NodeId;
            var moves = NarrativeStoryModel.PreviewOrganization(chapter, _folder + "/Chapters/CH01_Moved");
            Assert.AreEqual(3, moves.Count); NarrativeStoryModel.ApplyOrganization(moves);
            Assert.AreSame(b, a.Next); Assert.AreSame(chapter, _story.Entry); Assert.AreEqual(id, a.NodeId);
            foreach (var asset in assets) Assert.AreEqual(guids[asset], AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
            Assert.IsTrue(AssetDatabase.GetAssetPath(a).Contains("/Dialogue/CH01_"));
            Assert.IsTrue(AssetDatabase.GetAssetPath(b).Contains("/Endings/CH01_"));
        }
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        [Test]
        public void ForeignChapterCloneWithSameIdCannotMasqueradeAsRegisteredTarget()
        {
            var a = NarrativeStoryModel.CreateChapter(_story, "CH01");
            var b = NarrativeStoryModel.CreateChapter(_story, "CH02");
            NarrativeGraphModel.CreateNode(a, NovelNodeKind.ChapterExit); NarrativeGraphModel.CreateNode(b, NovelNodeKind.Ending);
            NarrativeStoryModel.SyncExits(_story); NarrativeStoryModel.Connect(_story, 0, -1, b);
            Assert.IsTrue(_story.TryReadDefinition(new Catalog(), out _, out _));
            var clone = UnityEngine.Object.Instantiate(b); AssetDatabase.CreateAsset(clone, _folder + "/Foreign.asset");
            using (var data = new SerializedObject(_story))
            {
                data.FindProperty("_exits").GetArrayElementAtIndex(0).FindPropertyRelative("_fallback").objectReferenceValue = clone;
                data.ApplyModifiedProperties();
            }
            Assert.AreEqual(b.ChapterId, clone.ChapterId);
            Assert.IsFalse(_story.TryReadDefinition(new Catalog(), out _, out var errors));
            Assert.IsTrue(errors.Any(e => e.Message.Contains("直接引用")));
        }
    }
}
