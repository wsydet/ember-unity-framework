using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>有限条件树编辑器；只修改调用方交付的序列化属性，Undo由SerializedObject提交。</summary>
    public static class EmberConditionEditor
    {
        #region 内部方法
        private static int Find(SerializedProperty nodes, string id)
        { for (int i = 0; i < nodes.arraySize; i++) if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue == id) return i; return -1; }
        private static void Remove(SerializedProperty nodes, string id, HashSet<string> seen)
        {
            if (!seen.Add(id)) return;
            int index = Find(nodes, id); if (index < 0) return;
            var children = nodes.GetArrayElementAtIndex(index).FindPropertyRelative("_children");
            var ids = new List<string>(); for (int i = 0; i < children.arraySize; i++) ids.Add(children.GetArrayElementAtIndex(i).stringValue);
            foreach (var child in ids) Remove(nodes, child, seen);
            index = Find(nodes, id); if (index >= 0) nodes.DeleteArrayElementAtIndex(index);
        }
        private static string Add<T>(SerializedProperty nodes, string kind, EmberConditionCatalog<T> catalog) where T : class
        {
            int index = nodes.arraySize++; var node = nodes.GetArrayElementAtIndex(index);
            string id = Guid.NewGuid().ToString("N"); node.FindPropertyRelative("_id").stringValue = id;
            node.FindPropertyRelative("_kind").stringValue = kind;
            node.FindPropertyRelative("_children").ClearArray();
            var parameters = node.FindPropertyRelative("_parameters"); parameters.ClearArray();
            var leaf = catalog.Leaves.FirstOrDefault(l => l.Id == kind);
            if (leaf != null)
                foreach (var spec in leaf.Parameters)
                {
                    int p = parameters.arraySize++; var item = parameters.GetArrayElementAtIndex(p);
                    item.FindPropertyRelative("_name").stringValue = spec.Name;
                    item.FindPropertyRelative("_type").enumValueIndex = (int)spec.Type;
                    item.FindPropertyRelative("_number").floatValue = 0;
                    item.FindPropertyRelative("_boolean").boolValue = false;
                    item.FindPropertyRelative("_text").stringValue = "";
                    item.FindPropertyRelative("_choice").intValue = 0;
                }
            return id;
        }
        private static void Wrap<T>(SerializedProperty nodes, string id, string kind, EmberConditionCatalog<T> catalog) where T : class
        {
            int i = Find(nodes, id); if (i < 0) return;
            nodes.InsertArrayElementAtIndex(i);
            string child = Guid.NewGuid().ToString("N");
            nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue = child;
            string extra = kind == "not" ? null : Add(nodes, "true", catalog);
            var parent = nodes.GetArrayElementAtIndex(Find(nodes, id));
            parent.FindPropertyRelative("_kind").stringValue = kind;
            parent.FindPropertyRelative("_parameters").ClearArray();
            var children = parent.FindPropertyRelative("_children"); children.arraySize = kind == "not" ? 1 : 2;
            children.GetArrayElementAtIndex(0).stringValue = child;
            if (extra != null) children.GetArrayElementAtIndex(1).stringValue = extra;
        }
        private static void DrawNode<T>(SerializedProperty nodes, string id, EmberConditionCatalog<T> catalog,
            int depth, HashSet<string> visited, ref Action edit) where T : class
        {
            int index = Find(nodes, id);
            if (index < 0 || depth > 32 || !visited.Add(id)) { EditorGUILayout.HelpBox("节点缺失、重复或循环：" + id, MessageType.Error); return; }
            var node = nodes.GetArrayElementAtIndex(index); string kind = node.FindPropertyRelative("_kind").stringValue;
            var leaf = catalog.Leaves.FirstOrDefault(l => l.Id == kind);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string label = kind switch { "and" => "全部满足 AND", "or" => "任一满足 OR", "not" => "不满足 NOT", "true" => "无额外限制", _ => leaf?.Label ?? "未知积木：" + kind };
                EditorGUILayout.LabelField(new GUIContent(label, "节点 " + id), EditorStyles.boldLabel);
                var children = node.FindPropertyRelative("_children");
                if (leaf != null)
                {
                    var parameters = node.FindPropertyRelative("_parameters");
                    foreach (var spec in leaf.Parameters)
                    {
                        SerializedProperty value = null;
                        for (int i = 0; i < parameters.arraySize; i++)
                        { var p = parameters.GetArrayElementAtIndex(i); if (p.FindPropertyRelative("_name").stringValue == spec.Name) value = p; }
                        if (value == null) { EditorGUILayout.HelpBox("缺少参数：" + spec.Label, MessageType.Error); continue; }
                        if (spec.Type == EmberConditionValueKind.Choice)
                        {
                            var p = value.FindPropertyRelative("_choice");
                            EditorGUI.BeginChangeCheck(); int next = EditorGUILayout.Popup(spec.Label, p.intValue, spec.Choices.ToArray());
                            if (EditorGUI.EndChangeCheck()) p.intValue = next;
                        }
                        else EditorGUILayout.PropertyField(value.FindPropertyRelative(spec.Type switch
                        { EmberConditionValueKind.Boolean => "_boolean", EmberConditionValueKind.Number => "_number", _ => "_text" }), new GUIContent(spec.Label));
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("包入 AND")) edit = () => Wrap(nodes, id, "and", catalog);
                    if (GUILayout.Button("包入 OR")) edit = () => Wrap(nodes, id, "or", catalog);
                    if (GUILayout.Button("包入 NOT")) edit = () => Wrap(nodes, id, "not", catalog);
                }
                string[] kinds = new[] { "true" }.Concat(catalog.Leaves.Select(l => l.Id)).ToArray();
                string[] labels = new[] { "无额外限制" }.Concat(catalog.Leaves.Select(l => l.Label)).ToArray();
                int selected = Array.IndexOf(kinds, kind);
                EditorGUI.BeginChangeCheck(); int replacement = EditorGUILayout.Popup("替换为查询", selected, labels);
                if (EditorGUI.EndChangeCheck() && replacement >= 0)
                    edit = () =>
                    {
                        Remove(nodes, id, new HashSet<string>());
                        string added = Add(nodes, kinds[replacement], catalog);
                        nodes.GetArrayElementAtIndex(Find(nodes, added)).FindPropertyRelative("_id").stringValue = id;
                    };
                for (int c = 0; c < children.arraySize; c++)
                {
                    int ci = c; string child = children.GetArrayElementAtIndex(c).stringValue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("子项 " + (c + 1));
                        using (new EditorGUI.DisabledScope(c == 0)) if (GUILayout.Button("上移", GUILayout.Width(46))) edit = () => children.MoveArrayElement(ci, ci - 1);
                        using (new EditorGUI.DisabledScope(c == children.arraySize - 1)) if (GUILayout.Button("下移", GUILayout.Width(46))) edit = () => children.MoveArrayElement(ci, ci + 1);
                        if (GUILayout.Button("删除", GUILayout.Width(46))) edit = () => { children.DeleteArrayElementAtIndex(ci); Remove(nodes, child, new HashSet<string>()); };
                    }
                    DrawNode(nodes, child, catalog, depth + 1, visited, ref edit);
                }
                if (kind is "and" or "or" or "not")
                    if (GUILayout.Button("添加恒真子项")) edit = () =>
                    {
                        string added = Add(nodes, "true", catalog);
                        var target = nodes.GetArrayElementAtIndex(Find(nodes, id)).FindPropertyRelative("_children");
                        int i = target.arraySize++; target.GetArrayElementAtIndex(i).stringValue = added;
                    };
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Draw<T>(SerializedProperty expression, EmberConditionCatalog<T> catalog) where T : class
        {
            var nodes = expression.FindPropertyRelative("_nodes"); var root = expression.FindPropertyRelative("_root");
            Action edit = null;
            if (GUILayout.Button("重置为无额外限制")) edit = () => { nodes.ClearArray(); root.stringValue = Add(nodes, "true", catalog); };
            DrawNode(nodes, root.stringValue, catalog, 0, new HashSet<string>(), ref edit);
            edit?.Invoke();
        }
        #endregion
    }
}
