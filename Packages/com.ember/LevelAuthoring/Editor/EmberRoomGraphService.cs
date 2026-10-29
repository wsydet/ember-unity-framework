using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    [InitializeOnLoad]
    public static class EmberRoomGraphService
    {
        #region 内部参数
        private sealed class Registration : IDisposable
        {
            internal IEmberRoomGraphAdapter Adapter;
            internal string Owner;
            internal Func<bool> Enabled;
            public void Dispose() { if (Adapters.TryGetValue(Adapter.Id, out var current) && ReferenceEquals(current, this)) Adapters.Remove(Adapter.Id); }
        }
        private static readonly Dictionary<string, Registration> Adapters = new(StringComparer.Ordinal);
        public const string MenuPath = "Ember/Level/房间地图组合";
        public static bool Available => EmberExtensionProject.IsEnabled(EmberExtensionProject.RoomGraph);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberRoomGraphService() => EmberConditionalMenu.Register(MenuPath, () => Available, EmberRoomGraphWindow.Open);
        private static IEmberRoomGraphAdapter Require(string id, UnityEngine.Object graph, bool writing = false)
        {
            EmberExtensionProject.Require(EmberExtensionProject.RoomGraph, writing);
            if (!Adapters.TryGetValue(id ?? "", out var entry) || !EmberLevelAccess.Allows(entry.Owner, entry.Enabled) || graph == null || !entry.Adapter.CanRead(graph))
                throw new InvalidOperationException("房间图适配器未登记、所属模板/模块关闭，或不支持此资产。");
            return entry.Adapter;
        }
        private static void Mutate(string id, UnityEngine.Object graph, string expectedRevision, string title, Action<IEmberRoomGraphAdapter> action)
        {
            var adapter = Require(id, graph, true);
            if (expectedRevision != Revision(id, graph)) throw new InvalidOperationException("原图或房间数据已变化。请刷新后检查草稿，再重新应用。");
            var targets = adapter.Targets(graph).Append(graph).Distinct().ToArray();
            if (targets.Any(t => t == null || !AssetDatabase.GetAssetPath(t).StartsWith("Assets/", StringComparison.Ordinal)))
                throw new InvalidOperationException("房间图写入仅允许项目 Assets 中的已保存资产；不得写 PackageCache。");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(title);
            Undo.RegisterCompleteObjectUndo(targets, title);
            try
            {
                action(adapter);
                var errors = EmberRoomGraphRules.Validate(adapter.Read(graph)).Concat(adapter.Validate(graph).Where(d => d.Severity == EmberLevelSeverity.Error).Select(d => d.Message)).ToArray();
                if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
                foreach (var target in targets) EditorUtility.SetDirty(target);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <param name="ownerTemplateId">所属模板；明确传空字符串表示独立消费项目接线，仍必须声明业务模块条件。</param>
        public static IDisposable Register(IEmberRoomGraphAdapter adapter, string ownerTemplateId, Func<bool> enabled)
        {
            if (adapter == null || string.IsNullOrWhiteSpace(adapter.Id)) throw new ArgumentException("适配器需要稳定 ID。");
            if (ownerTemplateId == null || enabled == null) throw new ArgumentException("必须明确声明项目/模板所有者与模块条件。");
            if (Adapters.ContainsKey(adapter.Id)) throw new InvalidOperationException("房间图适配器 ID 重复：" + adapter.Id);
            var entry = new Registration { Adapter = adapter, Owner = ownerTemplateId, Enabled = enabled }; Adapters.Add(adapter.Id, entry); return entry;
        }
        public static string[] FindAdapters(UnityEngine.Object graph) => Available ? Adapters.Values.Where(e => EmberLevelAccess.Allows(e.Owner, e.Enabled) && graph != null && e.Adapter.CanRead(graph)).Select(e => e.Adapter.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
        public static EmberRoomGraphSnapshot Read(string id, UnityEngine.Object graph) => Require(id, graph).Read(graph);
        public static string Revision(string id, UnityEngine.Object graph)
        {
            var adapter = Require(id, graph);
            return Hash128.Compute(string.Join("\n", adapter.Targets(graph).Append(graph).Distinct()
                .Select(t => GlobalObjectId.GetGlobalObjectIdSlow(t) + ":" + EditorJsonUtility.ToJson(t)).OrderBy(t => t, StringComparer.Ordinal))).ToString();
        }
        public static string[] Validate(string id, UnityEngine.Object graph)
        {
            var adapter = Require(id, graph);
            return EmberRoomGraphRules.Validate(adapter.Read(graph)).Concat(adapter.Validate(graph).Select(d => d.RuleId + "：" + d.Message)).ToArray();
        }
        public static void ApplyLayout(string id, UnityEngine.Object graph, IReadOnlyDictionary<string, Vector2> positions, string expectedRevision)
        {
            var snapshot = Read(id, graph);
            if (positions.Count != snapshot.Nodes.Count || snapshot.Nodes.Any(n => !positions.TryGetValue(n.Id, out var p) || !float.IsFinite(p.x) || !float.IsFinite(p.y)))
                throw new ArgumentException("排版草稿必须包含全部稳定房间 ID 和有限图坐标。");
            Mutate(id, graph, expectedRevision, "应用房间图排版", a => a.WriteLayout(graph, positions));
        }
        public static void Connect(string id, UnityEngine.Object graph, EmberRoomGraphConnection edge, string expectedRevision)
        {
            var snapshot = Read(id, graph);
            var errors = EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(snapshot.Nodes, snapshot.Connections.Append(edge), snapshot.RequireOpposedNormals, snapshot.RequireWorldAlignment, snapshot.WorldTolerance));
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Mutate(id, graph, expectedRevision, "连接房间端口", a => a.AddConnection(graph, edge));
        }
        public static void Disconnect(string id, UnityEngine.Object graph, EmberRoomGraphConnection edge, string expectedRevision)
            => Mutate(id, graph, expectedRevision, "移除房间连线", a => a.RemoveConnection(graph, edge));
        public static EmberLevelDescriptor[] Trials(string id, UnityEngine.Object graph, string room)
            => Require(id, graph) is IEmberRoomGraphTrialAdapter trials && EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelTrial) ? trials.Trials(graph, room).ToArray() : Array.Empty<EmberLevelDescriptor>();
        public static void StartTrial(string id, UnityEngine.Object graph, string room, EmberLevelDescriptor level)
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelTrial, true);
            var adapter = Require(id, graph, true) as IEmberRoomGraphTrialAdapter ?? throw new InvalidOperationException("适配器未提供正式试玩接线。");
            if (!Read(id, graph).Nodes.Any(n => n.Id == room) || !adapter.Trials(graph, room).Any(t => t.id == level?.id && t.configuration == level.configuration && t.adapterId == level.adapterId))
                throw new InvalidOperationException("所选试玩未登记到此房间。");
            var request = adapter.CreateTrial(graph, room, level) ?? throw new InvalidOperationException("缺少试玩初态。");
            EmberLevelTrialSession.Start(level, request.mode, request.position, request.checkpointId);
        }
        #endregion
    }
}
