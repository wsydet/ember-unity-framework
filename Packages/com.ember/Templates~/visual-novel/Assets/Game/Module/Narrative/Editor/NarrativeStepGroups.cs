using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>Colored authoring groups over ordinary runtime commands.</summary>
    public static class NarrativeStepGroups
    {
        #region 内部参数
        private static readonly HashSet<string> Collapsed = new();
        [Serializable] private sealed class Stamp
        {
            public string _commandId, _lineId, _actionId, _stepGroupId, _stepGroupName;
            public Color _stepGroupColor;
            public List<string> _waitActions;
        }
        [Serializable] private sealed class Batch { public List<NovelCommand> _commands; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static IReadOnlyList<NovelCommand> Wrap(IReadOnlyList<NovelCommand> source, string label, Color color, bool freshIds = true)
        {
            if (source == null || source.Count == 0 || source.Any(c => c == null)) throw new ArgumentException("二级步骤不能为空");
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("填写二级步骤名称");
            string group = Guid.NewGuid().ToString("N");
            var commands = new Dictionary<string, string>(StringComparer.Ordinal);
            var actions = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var c in source)
            {
                if (string.IsNullOrEmpty(c.CommandId) || commands.ContainsKey(c.CommandId)) throw new ArgumentException("步骤 ID 为空或重复");
                commands[c.CommandId] = freshIds ? Guid.NewGuid().ToString("N") : c.CommandId;
                if (!string.IsNullOrEmpty(c.ActionId))
                {
                    if (actions.ContainsKey(c.ActionId)) throw new ArgumentException("组合内动作 ID 重复：" + c.ActionId);
                    actions[c.ActionId] = freshIds ? Guid.NewGuid().ToString("N") : c.ActionId;
                }
            }
            var result = new List<NovelCommand>();
            foreach (var c in source)
            {
                var waits = new List<string>();
                foreach (string wait in c.WaitActions)
                {
                    if (actions.TryGetValue(wait, out var mapped) || commands.TryGetValue(wait, out mapped)) waits.Add(mapped);
                    else if (!freshIds) waits.Add(wait);
                    else throw new ArgumentException("等待引用组合外动作：" + wait + "。请把启动该动作的步骤一起选入。");
                }
                var copy = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(c));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Stamp
                {
                    _commandId = commands[c.CommandId], _lineId = freshIds ? Guid.NewGuid().ToString("N") : c.LineId,
                    _actionId = string.IsNullOrEmpty(c.ActionId) ? c.ActionId : actions[c.ActionId], _waitActions = waits,
                    _stepGroupId = group, _stepGroupName = label.Trim(), _stepGroupColor = color
                }), copy);
                result.Add(copy);
            }
            return result.AsReadOnly();
        }
        public static void Insert(NarrativeDialogueSO node, int index, IReadOnlyList<NovelCommand> commands)
        {
            if (!NarrativeGraphModel.CanEdit(node)) throw new InvalidOperationException("当前不能编辑剧情");
            if (index < 0 || index > node.Commands.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var list = node.Commands.ToList(); list.InsertRange(index, commands); Write(node, list, "插入二级步骤");
        }
        public static void GroupRange(NarrativeDialogueSO node, int start, int count, string label, Color color)
        {
            if (start < 0 || count <= 0 || start + count > node.Commands.Count) throw new ArgumentOutOfRangeException(nameof(count));
            var list = node.Commands.ToList(); var grouped = Wrap(list.GetRange(start, count), label, color, false);
            list.RemoveRange(start, count); list.InsertRange(start, grouped); Write(node, list, "包装二级步骤");
        }
        public static Color BasicColor(NovelCommandKind kind) => kind switch
        {
            NovelCommandKind.Say => new Color(.8f, .9f, 1),
            NovelCommandKind.SetVariable or NovelCommandKind.Wait or NovelCommandKind.WaitActions => new Color(1, .85f, .55f),
            NovelCommandKind.HideAllCharacters => new Color(.65f, .85f, .6f),
            _ => new Color(.8f, .75f, .95f)
        };
        public static bool DrawHeader(SerializedObject data, NarrativeDialogueSO node, SerializedProperty list, ref int index, ref Action pending)
        {
            var item = list.GetArrayElementAtIndex(index);
            string id = item.FindPropertyRelative("_stepGroupId").stringValue;
            if (string.IsNullOrEmpty(id)) return false;
            bool first = index == 0 || list.GetArrayElementAtIndex(index - 1).FindPropertyRelative("_stepGroupId").stringValue != id;
            if (!first) return false;
            int start = index, end = index + 1;
            while (end < list.arraySize && list.GetArrayElementAtIndex(end).FindPropertyRelative("_stepGroupId").stringValue == id) end++;
            int count = end - start;
            string label = item.FindPropertyRelative("_stepGroupName").stringValue;
            Color color = item.FindPropertyRelative("_stepGroupColor").colorValue;
            Color old = GUI.backgroundColor; GUI.backgroundColor = item.FindPropertyRelative("_stepGroupColor").colorValue;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool expanded = EditorGUILayout.Foldout(!Collapsed.Contains(id), "二级步骤 · " + item.FindPropertyRelative("_stepGroupName").stringValue + "（" + count + " 条基础步骤）", true);
                    if (expanded) Collapsed.Remove(id); else Collapsed.Add(id);
                    if (GUILayout.Button("复制组", GUILayout.Width(55))) pending = () => { data.ApplyModifiedProperties(); try { Insert(node, end, Wrap(node.Commands.Skip(start).Take(count).ToArray(), label, color)); }
                        catch (ArgumentException error) { EditorUtility.DisplayDialog("无法复制二级步骤", error.Message, "知道了"); } data.Update(); };
                    if (GUILayout.Button("解包", GUILayout.Width(40))) pending = () => { for (int i = start; i < end; i++) { list.GetArrayElementAtIndex(i).FindPropertyRelative("_stepGroupId").stringValue = ""; list.GetArrayElementAtIndex(i).FindPropertyRelative("_stepGroupName").stringValue = ""; } };
                    if (GUILayout.Button("删除组", GUILayout.Width(55))) pending = () => { for (int i = end - 1; i >= start; i--) list.DeleteArrayElementAtIndex(i); };
                }
            }
            GUI.backgroundColor = old;
            if (Collapsed.Contains(id)) { index = end - 1; return true; }
            return false;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void Write(NarrativeDialogueSO node, List<NovelCommand> commands, string undo)
        {
            if (!NarrativeGraphModel.CanEdit(node)) throw new InvalidOperationException("当前不能编辑剧情");
            Undo.RecordObject(node, undo); JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Batch { _commands = commands }), node);
            using var data = new SerializedObject(node); data.FindProperty("_contentRevision").intValue++;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(node);
        }
        #endregion
    }
}
