using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor
{
    /// <summary>结构修改前显式创建独立方案、图与 Controller；失败只清理本次创建的资产。</summary>
    public static class PlayerSchemeStructureWorkflow
    {
        #region 内部方法
        private static void RequireNew(string path, string extension)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") ||
                Path.GetExtension(path) != extension || File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) ||
                !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("结构复制需要 Assets 内有效且不存在的目标：" + path);
        }

        private static void Reference(Object target, string property, Object value)
        {
            using var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static Object CopyDependency(Object source, Dictionary<Object, Object> copies, List<Object> temporary)
        {
            if (source == null || source is not ScriptableObject || source is MonoScript) return source;
            if (copies.TryGetValue(source, out var existing)) return existing;
            if (source is EmberFsmGraph || source is PlayerScheme || source is PlayerConfig)
                throw new InvalidOperationException("节点配置反向引用其他图/完整玩家装配，不能隐式复制：" + source.name);
            var result = Object.Instantiate(source);
            temporary.Add(result); copies.Add(source, result);
            result.name = source.name; result.hideFlags = HideFlags.None;
            using var serialized = new SerializedObject(result);
            var property = serialized.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    property.objectReferenceValue = CopyDependency(property.objectReferenceValue, copies, temporary);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void RequireCompatible(PlayerScheme target, PlayerScheme source)
        {
            if (target.Graph == null || source.Graph == null || target.Graph.EntryId != source.Graph.EntryId ||
                target.Graph.Nodes.Count != source.Graph.Nodes.Count)
                throw new InvalidOperationException("参数恢复要求结构兼容：图入口或节点数量不同。");
            var nodes = target.Graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            foreach (var node in source.Graph.Nodes)
            {
                if (!nodes.TryGetValue(node.Id, out var other) || node.Definition == null || other.Definition == null ||
                    node.Definition.GetType() != other.Definition.GetType() || node.ParentId != other.ParentId ||
                    node.DefaultChildId != other.DefaultChildId ||
                    !node.Bindings.Select(b => b.Slot + "\n" + b.TargetId).OrderBy(x => x, StringComparer.Ordinal)
                        .SequenceEqual(other.Bindings.Select(b => b.Slot + "\n" + b.TargetId).OrderBy(x => x, StringComparer.Ordinal)))
                    throw new InvalidOperationException("参数恢复要求结构兼容：节点或绑定不同：" + node.Id);
            }
            if (target.Animation == null || source.Animation == null || target.Animation.Slots.Count != source.Animation.Slots.Count)
                throw new InvalidOperationException("参数恢复要求结构兼容：动画目标数量不同。");
            var slots = target.Animation.Slots.ToDictionary(s => s.NodeId, StringComparer.Ordinal);
            foreach (var slot in source.Animation.Slots)
                if (!slots.TryGetValue(slot.NodeId, out var other) || slot.State != other.State || other.Key == null)
                    throw new InvalidOperationException("参数恢复要求结构兼容：动画目标不同：" + slot.NodeId);
        }

        public static PlayerScheme CopyIndependent(PlayerScheme source, string schemePath, string graphPath, string controllerPath)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(source);
            RequireNew(schemePath, ".asset"); RequireNew(graphPath, ".asset"); RequireNew(controllerPath, ".controller");
            if (schemePath == graphPath) throw new ArgumentException("方案和图必须使用不同路径。");
            string controllerSource = AssetDatabase.GetAssetPath(source.Animation.Controller);
            if (string.IsNullOrEmpty(controllerSource) || AssetDatabase.LoadAllAssetsAtPath(controllerSource).Any(EditorUtility.IsDirty))
                throw new InvalidOperationException("Controller 尚未保存或含内存修改；请先保存该素材后执行结构复制，不会丢弃草稿。");
            var created = new List<string>(); var temporary = new List<Object>();
            try
            {
                var graph = Object.Instantiate(source.Graph); temporary.Add(graph);
                graph.name = Path.GetFileNameWithoutExtension(graphPath); graph.hideFlags = HideFlags.None;
                var closure = new Dictionary<Object, Object> { [source.Graph] = graph };
                var definitions = new Dictionary<EmberFsmStateDefinition, EmberFsmStateDefinition>();
                foreach (var node in source.Graph.Nodes)
                    if (!definitions.ContainsKey(node.Definition))
                    {
                        var copy = (EmberFsmStateDefinition)CopyDependency(node.Definition, closure, temporary);
                        definitions.Add(node.Definition, copy);
                    }
                graph.Configure(source.Graph.EntryId, source.Graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                    definitions[n.Definition], n.ParentId, n.DefaultChildId,
                    n.Bindings.Select(b => new EmberFsmTargetBinding(b.Slot, b.TargetId)).ToArray())).ToArray());
                foreach (var definition in definitions.Values)
                {
                    using var serialized = new SerializedObject(definition);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (property.objectReferenceValue == source.Graph) property.objectReferenceValue = graph;
                            else if (property.objectReferenceValue is EmberFsmStateDefinition original && definitions.TryGetValue(original, out var replacement))
                                property.objectReferenceValue = replacement;
                        }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(graph, graphPath); created.Add(graphPath);
                foreach (var dependency in closure.Values) if (dependency != graph) AssetDatabase.AddObjectToAsset(dependency, graph);
                AssetDatabase.SaveAssetIfDirty(graph);
                if (!AssetDatabase.CopyAsset(controllerSource, controllerPath)) throw new InvalidOperationException("Controller 复制失败。");
                created.Add(controllerPath);
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(controllerPath)) if (item != null) item.hideFlags &= ~HideFlags.NotEditable;
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                var result = PlayerSchemeWorkflow.Copy(source, schemePath); created.Add(schemePath);
                Reference(result, "_graph", graph); Reference(result.Animation, "_controller", controller);
                // CopyAsset 会重映射 Controller 内嵌键；方案映射仍指向源键，按稳定 local file ID 对齐。
                var embedded = new Dictionary<long, Object>();
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out string _, out long id)) embedded.Add(id, item);
                using (var serialized = new SerializedObject(result.Animation))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var original = property.objectReferenceValue;
                        if (original == null || AssetDatabase.GetAssetPath(original) != controllerSource) continue;
                        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string _, out long id) ||
                            !embedded.TryGetValue(id, out var replacement) || replacement.GetType() != original.GetType())
                            throw new InvalidOperationException("Controller 内嵌动画引用复制失败：" + original.name);
                        property.objectReferenceValue = replacement;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PlayerSchemeWorkflow.Validate(result);
                PlayerSchemeWorkflow.Save(result);
                AssetDatabase.SaveAssetIfDirty(controller);
                EmberFsmBlueprintWindow.CopySavedLayout(source.Graph, graph);
                return result;
            }
            catch
            {
                for (int i = created.Count - 1; i >= 0; i--) AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
            finally
            {
                foreach (var item in temporary) if (item != null && !AssetDatabase.Contains(item)) Object.DestroyImmediate(item);
            }
        }
        #endregion
    }
}
