using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Game.Narrative.Editor
{
    public static partial class NarrativeGraphModel
    {
        #region 外部方法
        public static void SetFlowCollapsed(NarrativeChapterSO chapter, NarrativeFlowStartSO flow, bool collapsed)
        {
            RequireMember(chapter, flow);
            var layout = GetLayout(chapter, true);
            Undo.IncrementCurrentGroup(); Undo.RecordObject(layout, "折叠或展开流程");
            layout.SetFlowCollapsed(flow.NodeId, collapsed); EditorUtility.SetDirty(layout);
        }

        public static void AssignFlow(NarrativeChapterSO chapter, NarrativeNodeSO node, NarrativeFlowStartSO flow)
        {
            RequireMember(chapter, node);
            if (flow) RequireMember(chapter, flow);
            if (node is NarrativeFlowStartSO) throw new InvalidOperationException("流程开始始终属于自身作用域");
            using var data = new SerializedObject(node);
            data.FindProperty("_flow").objectReferenceValue = flow; data.ApplyModifiedProperties();
        }

        public static void SetReceiver(NarrativeChapterSO chapter, NarrativeJumpSO jump, NarrativeReceiverSO receiver)
        {
            RequireMember(chapter, jump);
            if (receiver)
            {
                RequireMember(chapter, receiver);
                if (jump.ScopeId != receiver.ScopeId) throw new InvalidOperationException("跳转不能逃离或进入其他流程");
            }
            using var data = new SerializedObject(jump);
            data.FindProperty("_receiverId").stringValue = receiver ? receiver.NodeId : ""; data.ApplyModifiedProperties();
        }

        public static void RemapFlowCopies(IReadOnlyDictionary<NarrativeNodeSO, NarrativeNodeSO> copies)
        {
            foreach (var pair in copies)
            {
                using var data = new SerializedObject(pair.Value);
                if (pair.Key.Flow && copies.TryGetValue(pair.Key.Flow, out var flow)) data.FindProperty("_flow").objectReferenceValue = flow;
                if (pair.Key is NarrativeFlowCallSO call && call.Callee && copies.TryGetValue(call.Callee, out var callee))
                    data.FindProperty("_callee").objectReferenceValue = callee;
                if (pair.Key is NarrativeJumpSO jump)
                {
                    var receiver = copies.Keys.FirstOrDefault(n => n.NodeId == jump.ReceiverId);
                    if (receiver) data.FindProperty("_receiverId").stringValue = copies[receiver].NodeId;
                }
                data.ApplyModifiedProperties();
            }
        }
        #endregion
    }
}
