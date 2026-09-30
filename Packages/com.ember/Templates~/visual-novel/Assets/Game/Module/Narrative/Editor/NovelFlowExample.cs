using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>无私有资源的日程循环示例；通过正式图模型创建独立剧情资产。</summary>
    public static class NovelFlowExample
    {
        #region 内部参数
        public const string ROOT = "Assets/GameResource/Resources/Config/Narrative/FlowExample";
        [Serializable] private sealed class Commands { public List<NovelCommand> _commands; }
        [Serializable] private sealed class Variables { public List<NovelVariable> _variables; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void Reference(UnityEngine.Object asset, string field, UnityEngine.Object value)
        {
            using var data = new SerializedObject(asset); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedProperties();
        }
        private static void WriteCommands(NarrativeNodeSO node, params NovelCommand[] commands)
        { JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands = commands.ToList() }), node); EditorUtility.SetDirty(node); }
        private static NovelCommand Say(string id, string text) => new(id, NovelCommandKind.Say, text, id + "-line");
        private static void Route(NarrativeNodeSO node, string list, NarrativeNodeSO target, string label, NovelCondition condition = null)
        {
            NarrativeGraphModel.AddItem(node, list);
            using var data = new SerializedObject(node);
            var item = data.FindProperty(list).GetArrayElementAtIndex(data.FindProperty(list).arraySize - 1);
            item.FindPropertyRelative("_text").stringValue = label; item.FindPropertyRelative("_target").objectReferenceValue = target;
            data.ApplyModifiedProperties();
            if (condition != null)
            {
                // The shared serialized condition is a plain serializable value.
                var routes = node is NarrativeChoiceSO c ? c.Options : ((NarrativeBranchSO)node).Branches;
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(condition), routes[routes.Count - 1].Condition); EditorUtility.SetDirty(node);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [MenuItem("Ember/Novel/创建日程与子流程示例")]
        public static void Create() => CreateAt(ROOT);

        public static NarrativeStorySO CreateAt(string folder)
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(folder + "/Story.asset"))
                return AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(folder + "/Story.asset");
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
            }
            var chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>(); AssetDatabase.CreateAsset(chapter, folder + "/Chapter.asset");
            using (var data = new SerializedObject(chapter))
            { data.FindProperty("_displayName").stringValue = "日程循环示例"; data.FindProperty("_assetPrefix").stringValue = "Schedule"; data.ApplyModifiedProperties(); }
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = new() { new("month", new NovelValue(1)), new("step", new NovelValue(0)) } }), chapter);
            NarrativeNodeSO Node(NovelNodeKind kind, string name)
            {
                var n = NarrativeGraphModel.CreateNode(chapter, kind);
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(n), name); return n;
            }
            var daily = (NarrativeReceiverSO)Node(NovelNodeKind.Receiver, "每日分流");
            var month = Node(NovelNodeKind.Branch, "月份检查");
            var step = Node(NovelNodeKind.Branch, "当月步数检查");
            var chooseReceiver = (NarrativeReceiverSO)Node(NovelNodeKind.Receiver, "行动选择入口");
            var choice = Node(NovelNodeKind.Choice, "上午或下午行动");
            var a = (NarrativeFlowCallSO)Node(NovelNodeKind.FlowCall, "上午清扫");
            var b = (NarrativeFlowCallSO)Node(NovelNodeKind.FlowCall, "下午清扫");
            var advance = Node(NovelNodeKind.Dialogue, "完成后统一推进步数");
            var again = (NarrativeJumpSO)Node(NovelNodeKind.Jump, "返回每日分流");
            var cancelled = (NarrativeJumpSO)Node(NovelNodeKind.Jump, "取消返回选择");
            var settlement = Node(NovelNodeKind.Dialogue, "月末结算与换月");
            var monthAgain = (NarrativeJumpSO)Node(NovelNodeKind.Jump, "换月返回每日分流");
            var end = Node(NovelNodeKind.Ending, "示例完成");
            var flow = (NarrativeFlowStartSO)Node(NovelNodeKind.FlowStart, "A清扫流程开始");
            var action = Node(NovelNodeKind.Choice, "确认行动");
            var work = Node(NovelNodeKind.Dialogue, "行动与等待");
            var complete = Node(NovelNodeKind.FlowReturn, "清扫完成");
            var cancel = Node(NovelNodeKind.FlowReturn, "清扫取消");
            foreach (var n in new[] { action, work, complete, cancel }) NarrativeGraphModel.AssignFlow(chapter, n, flow);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = new() { new("attempt", new NovelValue(0)) } }), flow);
            using (var data = new SerializedObject(cancel)) { data.FindProperty("_result").stringValue = "取消"; data.ApplyModifiedProperties(); }
            foreach (var call in new[] { a, b })
            {
                Reference(call, "_callee", flow);
                using var data = new SerializedObject(call); var results = data.FindProperty("_results"); results.arraySize = 2;
                results.GetArrayElementAtIndex(0).FindPropertyRelative("_name").stringValue = "完成";
                results.GetArrayElementAtIndex(0).FindPropertyRelative("_target").objectReferenceValue = advance;
                results.GetArrayElementAtIndex(1).FindPropertyRelative("_name").stringValue = "取消";
                results.GetArrayElementAtIndex(1).FindPropertyRelative("_target").objectReferenceValue = cancelled; data.ApplyModifiedProperties();
            }
            Reference(daily, "_next", month); Reference(month, "_fallback", step);
            Route(month, "_branches", end, "两个月后结束", new NovelCondition(predicates: new[] { new NovelPredicate("month", NovelComparison.Greater, new NovelValue(2)) }));
            Reference(step, "_fallback", choice);
            Route(step, "_branches", settlement, "每月三步", new NovelCondition(predicates: new[] { new NovelPredicate("step", NovelComparison.GreaterOrEqual, new NovelValue(3)) }));
            Reference(chooseReceiver, "_next", choice);
            Route(choice, "_options", a, "上午清扫"); Route(choice, "_options", b, "下午清扫");
            Reference(flow, "_next", action); Route(action, "_options", work, "完成行动"); Route(action, "_options", cancel, "取消，不推进时间");
            WriteCommands(work, new NovelCommand("attempt", NovelCommandKind.RandomVariable, variableId: "attempt", scope: NovelVariableScope.Flow, randomMin: 1, randomMax: 6),
                Say("action-line", "这是独立行动流程。可以在本句、选择或接下来的等待中保存。"), new NovelCommand("action-wait", NovelCommandKind.Wait, duration: 2));
            Reference(work, "_next", complete);
            WriteCommands(advance, new NovelCommand("step-add", NovelCommandKind.CalculateVariable, variableId: "step", integerOperation: NovelIntegerOperation.Add, integerOperand: 1));
            Reference(advance, "_next", again);
            WriteCommands(settlement, Say("month-line", "月末结算，进入下个月。"),
                new NovelCommand("month-add", NovelCommandKind.CalculateVariable, variableId: "month", integerOperation: NovelIntegerOperation.Add, integerOperand: 1),
                new NovelCommand("step-reset", NovelCommandKind.SetVariable, variableId: "step", value: new NovelValue(0)));
            Reference(settlement, "_next", monthAgain);
            NarrativeGraphModel.SetReceiver(chapter, again, daily); NarrativeGraphModel.SetReceiver(chapter, monthAgain, daily); NarrativeGraphModel.SetReceiver(chapter, cancelled, chooseReceiver);
            NarrativeGraphModel.SetEntry(chapter, daily);
            var story = ScriptableObject.CreateInstance<NarrativeStorySO>(); AssetDatabase.CreateAsset(story, folder + "/Story.asset");
            using (var data = new SerializedObject(story))
            {
                data.FindProperty("_displayName").stringValue = "日程循环与复合流程";
                data.FindProperty("_entry").objectReferenceValue = chapter;
                var chapters = data.FindProperty("_chapters"); chapters.arraySize = 1; chapters.GetArrayElementAtIndex(0).objectReferenceValue = chapter; data.ApplyModifiedProperties();
            }
            NarrativeGraphModel.MoveMany(chapter, NarrativeAutoLayout.Calculate(chapter));
            NarrativeGraphModel.Save(chapter); EditorUtility.SetDirty(story); AssetDatabase.SaveAssets();
            Selection.activeObject = story; return story;
        }
        #endregion
    }
}
