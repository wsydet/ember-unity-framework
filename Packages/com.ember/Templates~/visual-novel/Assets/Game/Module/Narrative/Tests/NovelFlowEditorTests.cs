using System;
using System.Collections.Generic;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Tests
{
    public sealed class NovelFlowEditorTests
    {
        private string _folder, _layout;
        private NarrativeChapterSO _chapter;
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => false;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = null; return false; }
        }
        [SetUp] public void Setup()
        {
            _folder = "Assets/Game/Module/Narrative/Tests/FlowTemp_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Game/Module/Narrative/Tests", System.IO.Path.GetFileName(_folder));
            _chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>(); AssetDatabase.CreateAsset(_chapter, _folder + "/Chapter.asset");
            _layout = NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(_folder + "/Chapter.asset") + ".asset";
        }
        [TearDown] public void Cleanup() { AssetDatabase.DeleteAsset(_layout); AssetDatabase.DeleteAsset(_folder); }
        [Test] public void SelectionHighlightsPairedNodesWithoutChangingSelectionOrAssets()
        {
            var receiver = (NarrativeReceiverSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Receiver);
            var other = (NarrativeReceiverSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Receiver);
            var a = (NarrativeJumpSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            var b = (NarrativeJumpSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            NarrativeGraphModel.SetReceiver(_chapter, a, receiver); NarrativeGraphModel.SetReceiver(_chapter, b, receiver);
            var before = _chapter.Nodes.Select(EditorJsonUtility.ToJson).ToArray();
            var graph = new NarrativeFlowGraphView(_ => { }, action => action(), (_, _) => { });
            graph.Rebuild(_chapter, null);
            Node View(NarrativeNodeSO model) => graph.nodes.Single(n => n.viewDataKey == model.NodeId);
            string[] Related() => graph.nodes.Where(n => n.ClassListContains("narrative-related")).Select(n => n.viewDataKey).ToArray();
            graph.AddToSelection(View(receiver));
            CollectionAssert.AreEquivalent(new[] { a.NodeId, b.NodeId }, Related());
            Assert.AreEqual(1, graph.selection.Count);
            graph.SelectModel(a, false);
            CollectionAssert.AreEqual(new[] { receiver.NodeId }, Related());
            graph.AddToSelection(View(receiver));
            CollectionAssert.AreEquivalent(new[] { receiver.NodeId, a.NodeId, b.NodeId }, Related());
            graph.RemoveFromSelection(View(a));
            CollectionAssert.AreEquivalent(new[] { a.NodeId, b.NodeId }, Related());
            graph.Rebuild(_chapter, null);
            CollectionAssert.AreEquivalent(new[] { a.NodeId, b.NodeId }, Related());
            graph.ClearSelection(); Assert.IsEmpty(Related());
            graph.SelectModel(other, false); Assert.IsEmpty(Related());
            CollectionAssert.AreEqual(before, _chapter.Nodes.Select(EditorJsonUtility.ToJson).ToArray());
            graph.SelectModel(a, false);
            // Isolate the user edit from fixture creation in this synchronous test.
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            NarrativeGraphModel.SetReceiver(_chapter, a, other); Undo.FlushUndoRecordObjects();
            graph.Rebuild(_chapter, null); CollectionAssert.AreEqual(new[] { other.NodeId }, Related());
            Undo.PerformUndo();
            Assert.AreEqual(receiver.NodeId, a.ReceiverId);
            graph.Rebuild(_chapter, null);
            CollectionAssert.AreEqual(new[] { receiver.NodeId }, Related());
            graph.Rebuild(null, null); Assert.IsEmpty(Related());
        }

        [Test] public void ReceiverRenameSaveReloadDeleteUndoKeepsStableAssociation()
        {
            var receiver = (NarrativeReceiverSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Receiver);
            var a = (NarrativeJumpSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            var b = (NarrativeJumpSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            NarrativeGraphModel.SetReceiver(_chapter, a, receiver); NarrativeGraphModel.SetReceiver(_chapter, b, receiver);
            var end = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Ending); NarrativeGraphModel.Connect(_chapter, receiver, "_next", end);
            string id = receiver.NodeId; AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(receiver), "改名后的每日分流");
            NarrativeGraphModel.Save(_chapter); AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(a), ImportAssetOptions.ForceUpdate);
            Assert.AreEqual(id, a.ReceiverId); Assert.AreEqual(id, b.ReceiverId);
            Assert.AreEqual(0, NarrativeGraphModel.Ports(a).Count);
            Assert.AreEqual(3, NarrativeGraphModel.Incoming(_chapter, receiver).Count);
            NarrativeGraphModel.Remove(_chapter, receiver, true); Assert.IsEmpty(a.ReceiverId);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.AreEqual(id, a.ReceiverId); Assert.Contains(receiver, _chapter.Nodes.ToList());
        }
        [Test] public void WholeFlowCopyRemapsScopeJumpCallAndResultThenUndoRestoresMembership()
        {
            var start = (NarrativeFlowStartSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowStart);
            var receiver = (NarrativeReceiverSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Receiver);
            var jump = (NarrativeJumpSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            var ret = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowReturn);
            foreach (var n in new NarrativeNodeSO[] { receiver, jump, ret }) NarrativeGraphModel.AssignFlow(_chapter, n, start);
            NarrativeGraphModel.Connect(_chapter, start, "_next", jump); NarrativeGraphModel.SetReceiver(_chapter, jump, receiver); NarrativeGraphModel.Connect(_chapter, receiver, "_next", ret);
            var graph = new NarrativeFlowGraphView(_ => { }, a => a(), (_, _) => { });
            graph.Rebuild(_chapter, null); graph.SelectModel(start, false); graph.DuplicateSelection();
            var copy = _chapter.Nodes.OfType<NarrativeFlowStartSO>().Single(n => n != start);
            var copiedJump = _chapter.Nodes.OfType<NarrativeJumpSO>().Single(n => n != jump);
            var copiedReceiver = _chapter.Nodes.OfType<NarrativeReceiverSO>().Single(n => n != receiver);
            Assert.AreNotEqual(start.NodeId, copy.NodeId); Assert.AreEqual(copy, copiedJump.Flow);
            Assert.AreEqual(copiedReceiver.NodeId, copiedJump.ReceiverId); Assert.AreEqual(copiedJump, copy.Next);
            Assert.AreEqual(copy, copiedReceiver.Next.Flow);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.AreEqual(4, _chapter.Nodes.Count);
        }
        [Test] public void FlowCollapseAndMovementAreUndoableAndIndependentOfRuntimeDefinition()
        {
            var start = (NarrativeFlowStartSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowStart);
            var ret = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowReturn); NarrativeGraphModel.AssignFlow(_chapter, ret, start);
            NarrativeGraphModel.Connect(_chapter, start, "_next", ret);
            NarrativeGraphModel.GetLayout(_chapter, true);
            NarrativeGraphModel.SetFlowCollapsed(_chapter, start, true); Undo.FlushUndoRecordObjects();
            Assert.Contains(start.NodeId, NarrativeGraphModel.GetLayout(_chapter, false).CollapsedFlows.ToList());
            Undo.PerformUndo(); Assert.IsEmpty(NarrativeGraphModel.GetLayout(_chapter, false).CollapsedFlows);
            var before = EditorJsonUtility.ToJson(start);
            NarrativeGraphModel.MoveMany(_chapter, new Dictionary<NarrativeNodeSO, Vector2> { [start] = new(100, 200), [ret] = new(400, 200) });
            Assert.AreEqual(before, EditorJsonUtility.ToJson(start));
            NarrativeGraphModel.Save(_chapter); AssetDatabase.ImportAsset(_layout, ImportAssetOptions.ForceUpdate);
            Assert.AreEqual(new Vector2(100, 200), NarrativeGraphModel.GetLayout(_chapter, false).GetPosition(start.NodeId, 0));
        }
        [Test] public void ExampleCancelsWithoutStepThenCompletesTwoMonths()
        {
            // Use a child directory so the fixture's own chapter remains intact.
            var story = NovelFlowExample.CreateAt(_folder + "/Example");
            var chapter = story.Chapters.Single();
            string layout = NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chapter)) + ".asset";
            try
            {
                Assert.IsTrue(story.TryReadDefinition(new Catalog(), out var definition, out var errors), string.Join("\n", errors));
                var r = new NarrativeRunner(); Assert.IsTrue(r.StartStory(definition, new Catalog()));
                long frame = 1;
                void Choose(int index) { var s = r.Snapshot; Assert.IsTrue(r.Choose(s.SessionGeneration, s.PositionVersion, s.Options[index].Id, frame++)); }
                Choose(0); Choose(1); Assert.AreEqual(0, r.Snapshot.Variables["step"].Int); Assert.IsEmpty(r.Snapshot.CallPath);
                for (int i = 0; i < 80 && r.Snapshot.State != NarrativeState.Ended; i++)
                {
                    var s = r.Snapshot;
                    if (s.State == NarrativeState.AwaitingChoice) Choose(0);
                    else if (s.State == NarrativeState.Revealing) r.CompleteReveal(s.SessionGeneration, s.PositionVersion);
                    else if (s.State == NarrativeState.AwaitingAdvance) r.Advance(s.SessionGeneration, s.PositionVersion, frame++);
                    else if (s.Wait == NarrativeWait.Timer) r.Tick(s.SessionGeneration, 3, frame++);
                    else Assert.Fail(s.Error?.ToString() ?? "Unexpected stop");
                }
                Assert.AreEqual(NarrativeState.Ended, r.Snapshot.State); Assert.AreEqual(3, r.Snapshot.Variables["month"].Int);
            }
            finally { AssetDatabase.DeleteAsset(layout); }
        }

        [Test] public void IsolatedViewAndFoldPreservePortsAndLocateRevealsHiddenNode()
        {
            var start = (NarrativeFlowStartSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowStart);
            var ret = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.FlowReturn);
            var jump = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Jump);
            var receiver = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Receiver);
            NarrativeGraphModel.AssignFlow(_chapter, ret, start);
            var graph = new NarrativeFlowGraphView(_ => { }, a => a(), (_, _) => { }); graph.Rebuild(_chapter, null);
            Assert.AreEqual(1, graph.nodes.Single(n => n.viewDataKey == jump.NodeId).inputContainer.Query<Port>().ToList().Count);
            Assert.AreEqual(0, graph.nodes.Single(n => n.viewDataKey == jump.NodeId).outputContainer.Query<Port>().ToList().Count);
            Assert.AreEqual(0, graph.nodes.Single(n => n.viewDataKey == receiver.NodeId).inputContainer.Query<Port>().ToList().Count);
            graph.ShowFlow(start);
            Assert.AreEqual(DisplayStyle.None, graph.nodes.Single(n => n.viewDataKey == jump.NodeId).style.display.value);
            graph.ShowFlow(null); NarrativeGraphModel.SetFlowCollapsed(_chapter, start, true); graph.Rebuild(_chapter, null);
            Assert.AreEqual(DisplayStyle.None, graph.nodes.Single(n => n.viewDataKey == ret.NodeId).style.display.value);
            graph.SelectModel(ret, false);
            Assert.AreEqual(start, graph.ViewFlow);
            Assert.AreEqual(DisplayStyle.Flex, graph.nodes.Single(n => n.viewDataKey == ret.NodeId).style.display.value);
            graph.Rebuild(null, null); graph.Rebuild(_chapter, null);
            Assert.IsNull(graph.ViewFlow);
            Assert.AreEqual(DisplayStyle.Flex, graph.nodes.Single(n => n.viewDataKey == jump.NodeId).style.display.value);
        }

        [Test] public void ContinuousPreviewClonesReferencesAndInternalPreviewReturnsWithoutChangingAssets()
        {
            var story = NovelFlowExample.CreateAt(_folder + "/Preview");
            var chapter = story.Chapters.Single();
            string layout = NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chapter)) + ".asset";
            var originals = chapter.Nodes.Cast<UnityEngine.Object>().Append(chapter).Append(story).ToArray();
            var json = originals.Select(EditorJsonUtility.ToJson).ToArray();
            try
            {
                using (var preview = Game.UI.Editor.NovelPlaybackStory.ForFlow(chapter.Entry, chapter, story))
                {
                    Assert.IsTrue(preview.Story.TryReadDefinition(new Catalog(), out var definition, out var errors), string.Join("\n", errors));
                    Assert.IsFalse(preview.Story.Chapters.Single().Nodes.Any(n => chapter.Nodes.Contains(n)));
                    var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(definition, new Catalog()));
                    Assert.AreEqual(NarrativeState.AwaitingChoice, runner.Snapshot.State);
                    var s = runner.Snapshot; runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[0].Id, 1);
                    Assert.AreEqual(1, runner.Snapshot.CallPath.Count);
                    s = runner.Snapshot; runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[1].Id, 2);
                    Assert.IsEmpty(runner.Snapshot.CallPath); Assert.AreEqual(0, runner.Snapshot.Variables["step"].Int);
                }
                var internalNode = chapter.Nodes.OfType<NarrativeFlowReturnSO>().First();
                using (var preview = Game.UI.Editor.NovelPlaybackStory.ForFlow(internalNode, chapter, story))
                {
                    Assert.IsTrue(preview.Story.TryReadDefinition(new Catalog(), out var definition, out var errors), string.Join("\n", errors));
                    var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(definition, new Catalog()));
                    Assert.AreEqual(NarrativeState.AwaitingChoice, runner.Snapshot.State);
                    var s = runner.Snapshot; runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[1].Id, 1);
                    Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State); Assert.IsEmpty(runner.Snapshot.CallPath);
                }
                CollectionAssert.AreEqual(json, originals.Select(EditorJsonUtility.ToJson).ToArray());
            }
            finally { AssetDatabase.DeleteAsset(layout); }
        }
    }
}
