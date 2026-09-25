using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>所有图操作直接修改 SO。撤销创建/移除仅改变章节成员，不销毁独立资产文件。</summary>
    [InitializeOnLoad]
    public static class NarrativeGraphModel
    {
        #region 内部参数
        public const string LAYOUT_ROOT = "Assets/Game/Module/Narrative/Editor/Layouts";
        private static bool? _templateActive;
        public static int TemplateReadCount { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [Serializable] private sealed class CommandBatch { public List<NovelCommand> _commands; }
        static NarrativeGraphModel() => EditorApplication.projectChanged += InvalidateTemplateCache;
        private static void RequireEdit(UnityEngine.Object asset)
        {
            NarrativeEditorAvailability.RequireEnabled();
            // 写入时重新核对授权；绘制/导航只读缓存，不在每个 UI 事件读磁盘。
            IsTemplateActive(true);
            if (!CanEdit(asset)) throw new InvalidOperationException("仅可在正式编辑或部署 visual-novel（及派生模板）的项目 Assets 中修改剧情资产。");
        }
        private static void RequireMember(NarrativeChapterSO chapter, NarrativeNodeSO node)
        {
            RequireEdit(chapter);
            if (!node || node.ChapterId != chapter.ChapterId || !chapter.Nodes.Contains(node))
                throw new InvalidOperationException("节点不属于当前章节。");
            RequireEdit(node);
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private static void FreshIds(SerializedObject data)
        {
            data.FindProperty("_nodeId").stringValue = Guid.NewGuid().ToString("N");
            data.FindProperty("_contentRevision").intValue = 1;
            var commands = data.FindProperty("_commands");
            if (commands != null)
                for (int i = 0; i < commands.arraySize; i++) FreshCommandIds(commands.GetArrayElementAtIndex(i));
            var routes = data.FindProperty("_options") ?? data.FindProperty("_branches");
            if (routes != null)
                for (int i = 0; i < routes.arraySize; i++)
                    routes.GetArrayElementAtIndex(i).FindPropertyRelative("_optionId").stringValue = Guid.NewGuid().ToString("N");
            var ending = data.FindProperty("_endingId");
            if (ending != null) ending.stringValue = Guid.NewGuid().ToString("N");
        }
        private static void FreshCommandIds(SerializedProperty command)
        {
            command.FindPropertyRelative("_commandId").stringValue = Guid.NewGuid().ToString("N");
            command.FindPropertyRelative("_lineId").stringValue = Guid.NewGuid().ToString("N");
            command.FindPropertyRelative("_textRevision").intValue = 1;
            // 动作 ID 是可选的等待句柄别名，默认留空并回退到指令 ID。复制节点会连源数据的
            // _actionId 一起 Instantiate，所以这里必须显式清空，否则复制出的句柄会和源节点重名。
            command.FindPropertyRelative("_actionId").stringValue = string.Empty;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void InvalidateTemplateCache() => _templateActive = null;
        public static bool IsTemplateActive(bool refresh = false)
        {
            if (!refresh && _templateActive.HasValue) return _templateActive.Value;
            TemplateReadCount++;
            _templateActive = Ember.Core.Editor.EmberProjectSetup.IsTemplateActive("visual-novel");
            return _templateActive.Value;
        }
        public static bool CanEdit(UnityEngine.Object asset)
            => NarrativeEditorAvailability.Enabled && !EditorApplication.isPlayingOrWillChangePlaymode && IsTemplateActive() && asset &&
                AssetDatabase.GetAssetPath(asset).StartsWith("Assets/", StringComparison.Ordinal);

        public static NarrativeGraphLayout GetLayout(UnityEngine.Object chapter, bool create)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chapter));
            if (string.IsNullOrEmpty(guid)) return null;
            string path = LAYOUT_ROOT + "/" + guid + ".asset";
            var layout = AssetDatabase.LoadAssetAtPath<NarrativeGraphLayout>(path);
            if (!layout && create)
            {
                RequireEdit(chapter); EnsureFolder(LAYOUT_ROOT);
                layout = ScriptableObject.CreateInstance<NarrativeGraphLayout>();
                layout.Initialize(guid); AssetDatabase.CreateAsset(layout, path);
            }
            return layout;
        }
        public static void Move(NarrativeChapterSO chapter, NarrativeNodeSO node, Vector2 position)
        {
            RequireMember(chapter, node);
            var layout = GetLayout(chapter, true);
            Undo.RecordObject(layout, "移动剧情节点"); layout.SetPosition(node.NodeId, position); EditorUtility.SetDirty(layout);
        }
        public static NarrativeNodeSO CreateNode(NarrativeChapterSO chapter, NovelNodeKind kind, NarrativeNodeSO copy = null)
        {
            RequireEdit(chapter);
            if (copy) RequireMember(chapter, copy);
            Type type = kind switch { NovelNodeKind.Dialogue => typeof(NarrativeDialogueSO),
                NovelNodeKind.Choice => typeof(NarrativeChoiceSO), NovelNodeKind.Branch => typeof(NarrativeBranchSO),
                NovelNodeKind.ChapterExit => typeof(NarrativeChapterExitSO), _ => typeof(NarrativeEndingSO) };
            var node = copy ? UnityEngine.Object.Instantiate(copy) : (NarrativeNodeSO)ScriptableObject.CreateInstance(type);
            using (var data = new SerializedObject(node))
            {
                FreshIds(data); data.FindProperty("_chapterId").stringValue = chapter.ChapterId;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(node, NarrativeStoryModel.NewNodePath(chapter, node, copy ? copy.name + " Copy" : kind.ToString()));
            AddExisting(chapter, node);
            return node;
        }
        public static void AddExisting(NarrativeChapterSO chapter, NarrativeNodeSO node)
        {
            RequireEdit(chapter); RequireEdit(node);
            if (node.ChapterId != chapter.ChapterId || chapter.Nodes.Any(n => n && n.NodeId == node.NodeId))
                throw new InvalidOperationException("只能添加同章节且 ID 唯一的独立节点。");
            using var data = new SerializedObject(chapter);
            var nodes = data.FindProperty("_nodes"); nodes.arraySize++;
            nodes.GetArrayElementAtIndex(nodes.arraySize - 1).objectReferenceValue = node;
            if (!chapter.Entry) data.FindProperty("_entry").objectReferenceValue = node;
            data.ApplyModifiedProperties();
        }
        public static void SetEntry(NarrativeChapterSO chapter, NarrativeNodeSO node)
        {
            RequireMember(chapter, node);
            using var data = new SerializedObject(chapter);
            data.FindProperty("_entry").objectReferenceValue = node; data.ApplyModifiedProperties();
        }
        /// <summary>连接属性路径只来自当前 SO，不持久化第二份边列表。</summary>
        public static List<string> Ports(NarrativeNodeSO node)
        {
            var result = new List<string>();
            if (node is NarrativeDialogueSO) result.Add("_next");
            if (node is NarrativeBranchSO branch)
            {
                for (int i = 0; i < branch.Branches.Count; i++) result.Add("_branches.Array.data[" + i + "]._target");
                result.Add("_fallback");
            }
            if (node is NarrativeChoiceSO choice)
                for (int i = 0; i < choice.Options.Count; i++) result.Add("_options.Array.data[" + i + "]._target");
            return result;
        }
        public static NarrativeNodeSO Target(NarrativeNodeSO node, string port)
        {
            if (node is NarrativeDialogueSO dialogue && port == "_next") return dialogue.Next;
            if (node is NarrativeBranchSO fallback && port == "_fallback") return fallback.Fallback;
            int start = port.IndexOf('['), end = port.IndexOf(']');
            if (start < 0 || end <= start || !int.TryParse(port.Substring(start + 1, end - start - 1), out int index)) return null;
            var routes = node is NarrativeChoiceSO choice ? choice.Options : (node as NarrativeBranchSO)?.Branches;
            return routes != null && index >= 0 && index < routes.Count ? routes[index]?.Target : null;
        }
        public static void MoveMany(NarrativeChapterSO chapter, IReadOnlyDictionary<NarrativeNodeSO, Vector2> positions)
        {
            RequireEdit(chapter);
            var members = new HashSet<NarrativeNodeSO>(chapter.Nodes);
            foreach (var pair in positions)
                if (!pair.Key || !members.Contains(pair.Key) || pair.Key.ChapterId != chapter.ChapterId)
                    throw new InvalidOperationException("布局包含章外节点。");
            var layout = GetLayout(chapter, true);
            Undo.RecordObject(layout, "排列剧情节点");
            foreach (var pair in positions) layout.SetPosition(pair.Key.NodeId, pair.Value);
            EditorUtility.SetDirty(layout);
        }
        public static void Connect(NarrativeChapterSO chapter, NarrativeNodeSO node, string port, NarrativeNodeSO target)
        {
            RequireMember(chapter, node);
            if (target) RequireMember(chapter, target);
            if (!Ports(node).Contains(port)) throw new ArgumentException("输出端口已失效。");
            using var data = new SerializedObject(node);
            data.FindProperty(port).objectReferenceValue = target; data.ApplyModifiedProperties();
        }
        public static List<string> Incoming(NarrativeChapterSO chapter, NarrativeNodeSO node)
        {
            var result = new List<string>();
            if (chapter.Entry == node) result.Add("章节入口");
            foreach (var source in chapter.Nodes.Where(n => n))
                foreach (string port in Ports(source))
                    if (Target(source, port) == node) result.Add(source.name + " / " + port);
            return result;
        }
        public static void Remove(NarrativeChapterSO chapter, NarrativeNodeSO node, bool disconnectIncoming)
        {
            RequireMember(chapter, node);
            if (!disconnectIncoming && Incoming(chapter, node).Count != 0)
                throw new InvalidOperationException("存在入口或入链，必须先确认断开。");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("移除剧情节点并断开入链");
            foreach (var source in chapter.Nodes.Where(n => n).ToArray())
                foreach (string port in Ports(source))
                    if (Target(source, port) == node) Connect(chapter, source, port, null);
            using var data = new SerializedObject(chapter);
            if (chapter.Entry == node) data.FindProperty("_entry").objectReferenceValue = null;
            var nodes = data.FindProperty("_nodes");
            for (int i = nodes.arraySize - 1; i >= 0; i--)
                if (nodes.GetArrayElementAtIndex(i).objectReferenceValue == node)
                { nodes.GetArrayElementAtIndex(i).objectReferenceValue = null; nodes.DeleteArrayElementAtIndex(i); }
            data.ApplyModifiedProperties(); Undo.CollapseUndoOperations(group);
        }
        public static void AppendCommands(NarrativeDialogueSO node, IReadOnlyList<NovelCommand> commands)
        {
            RequireEdit(node);
            if (commands == null || commands.Count == 0) throw new ArgumentException("预设没有指令");
            var batch = new CommandBatch { _commands = node.Commands.ToList() };
            batch._commands.AddRange(commands);
            Undo.RecordObject(node, "插入演出预设");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(batch), node);
            using var data = new SerializedObject(node);
            data.FindProperty("_contentRevision").intValue++;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(node);
        }
        public static void AddItem(NarrativeNodeSO node, string listName, int copyIndex = -1)
        {
            RequireEdit(node);
            using var data = new SerializedObject(node);
            var list = data.FindProperty(listName);
            if (list == null || !list.isArray || (listName != "_commands" && listName != "_options" && listName != "_branches"))
                throw new ArgumentException("未知剧情列表。");
            if (copyIndex >= list.arraySize) throw new ArgumentOutOfRangeException(nameof(copyIndex));
            int index = copyIndex >= 0 ? copyIndex : list.arraySize;
            list.InsertArrayElementAtIndex(index);
            var item = list.GetArrayElementAtIndex(index);
            if (listName == "_commands")
            {
                if (copyIndex < 0)
                {
                    item.FindPropertyRelative("_kind").enumValueIndex = 0;
                    item.FindPropertyRelative("_scope").enumValueIndex = 0;
                    foreach (string field in new[] { "_text", "_characterId", "_resourceKey", "_variableId", "_speakerNameKey" }) item.FindPropertyRelative(field).stringValue = "";
                    item.FindPropertyRelative("_duration").floatValue = 0;
                    item.FindPropertyRelative("_volume").floatValue = 1;
                    item.FindPropertyRelative("_cameraZoom").floatValue = 1;
                    item.FindPropertyRelative("_wipeDirection").enumValueIndex = 0;
                    item.FindPropertyRelative("_textMode").enumValueIndex = 0;
                    item.FindPropertyRelative("_textEffectsVersion").intValue = 1;
                    item.FindPropertyRelative("_textReveal").enumValueIndex = 0;
                    item.FindPropertyRelative("_textFadeDuration").floatValue = .8f;
                    item.FindPropertyRelative("_titleExitDuration").floatValue = .65f;
                    item.FindPropertyRelative("_textSpeedMultiplier").floatValue = 1;
                    item.FindPropertyRelative("_textEase").enumValueIndex = (int)NovelEase.SmoothStep;
                    item.FindPropertyRelative("_stepGroupId").stringValue = "";
                    item.FindPropertyRelative("_stepGroupName").stringValue = "";
                    item.FindPropertyRelative("_textBeats").ClearArray();
                    item.FindPropertyRelative("_dialogueVisible").boolValue = true;
                    item.FindPropertyRelative("_persistent").boolValue = false;
                    item.FindPropertyRelative("_keepOnSceneChange").boolValue = false;
                    item.FindPropertyRelative("_bindingId").stringValue = "";
                }
                FreshCommandIds(item);
            }
            else
            {
                item.FindPropertyRelative("_optionId").stringValue = Guid.NewGuid().ToString("N");
                if (copyIndex < 0)
                {
                    item.FindPropertyRelative("_text").stringValue = "";
                    item.FindPropertyRelative("_target").objectReferenceValue = null;
                    item.FindPropertyRelative("_condition").FindPropertyRelative("_junction").enumValueIndex = 0;
                    item.FindPropertyRelative("_condition").FindPropertyRelative("_predicates").arraySize = 0;
                }
            }
            data.ApplyModifiedProperties();
        }
        public static void Save(NarrativeChapterSO chapter)
        {
            RequireEdit(chapter); AssetDatabase.SaveAssetIfDirty(chapter);
            foreach (var node in chapter.Nodes.Where(n => n)) AssetDatabase.SaveAssetIfDirty(node);
            var layout = GetLayout(chapter, false); if (layout) AssetDatabase.SaveAssetIfDirty(layout);
        }
        #endregion
    }
}
