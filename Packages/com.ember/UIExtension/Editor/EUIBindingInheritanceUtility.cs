// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ember.Basic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding 继承管理工具（基类 Prefab 选择、信息展示、缺失检测、冲突校验）。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingInheritanceUtility
    {
        #region 生命周期（初始化）

        static EUIBindingInheritanceUtility()
        {
            EUIBinding.OnGetBasePrefabObject = HandleGetBasePrefabObject;
            EUIBinding.OnSetBasePrefabObject = HandleSetBasePrefabObject;
            EUIBinding.OnGetBaseInfoSummary = HandleGetBaseInfoSummary;
            EUIBinding.OnAutoFixMissingBindings = HandleAutoFixMissingBindings;
            EUIBinding.OnGetMissingFieldCount = HandleGetMissingFieldCount;
            EUIBinding.OnHasInheritanceConflict = HandleHasInheritanceConflict;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 基类 Prefab 选择

        private static GameObject HandleGetBasePrefabObject(EUIBinding binding)
        {
            if (!binding) return null;
            string guid;
            using (var so = new SerializedObject(binding))
                guid = so.FindProperty("baseBindingUUID").stringValue;
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void HandleSetBasePrefabObject(EUIBinding binding, GameObject prefab)
        {
            if (!binding) return;
            string guid = string.Empty;

            if (prefab)
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(path)) return;

                // 禁止自身继承
                if (IsSamePrefab(binding, path))
                {
                    EditorUtility.DisplayDialog("选择基类 Prefab",
                        "不能选择自身的 Prefab 作为基类。", "确定");
                    return;
                }

                var baseBinding = prefab.GetComponent<EUIBinding>();
                if (!baseBinding)
                {
                    EditorUtility.DisplayDialog("选择基类 Prefab",
                        $"Prefab \"{prefab.name}\" 上没有 EUIBinding 组件。", "确定");
                    return;
                }

                guid = AssetDatabase.AssetPathToGUID(path);
            }

            using (var so = new SerializedObject(binding))
            {
                so.FindProperty("baseBindingUUID").stringValue = guid;
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(binding);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 基类信息展示

        private static string HandleGetBaseInfoSummary(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return "（Prefab 已删除或 GUID 无效）";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) return "（Prefab 加载失败）";

            var baseBinding = prefab.GetComponent<EUIBinding>();
            if (!baseBinding) return "（Prefab 上无 EUIBinding 组件）";

            var sb = new StringBuilder();
            sb.Append("页面: ");
            sb.AppendLine(baseBinding.IsPage ? "是" : "否");
            if (baseBinding.IsPage)
                sb.AppendLine($"页面名: {baseBinding.PageName}");
            sb.Append($"类路径: {baseBinding.ClassPath}/{baseBinding.ClassName}");
            sb.AppendLine();
            sb.Append($"绑定数: {baseBinding.Bindings?.Length ?? 0}");
            return sb.ToString();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 继承冲突校验

        private static bool HandleHasInheritanceConflict(EUIBinding binding)
        {
            if (!binding) return false;

            var baseBinding = GetBaseBinding(binding);
            if (baseBinding == null) return false;

            return baseBinding.IsPage != binding.IsPage;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 缺失字段检测与修复

        private static int HandleGetMissingFieldCount(EUIBinding binding)
        {
            if (!binding) return 0;

            var baseBinding = GetBaseBinding(binding);
            if (baseBinding == null || baseBinding.Bindings == null) return 0;

            var currentNames = new HashSet<string>();
            if (binding.Bindings != null)
            {
                foreach (var b in binding.Bindings)
                {
                    if (!string.IsNullOrEmpty(b.Name))
                        currentNames.Add(b.Name);
                }
            }

            return baseBinding.Bindings.Count(b =>
                !string.IsNullOrEmpty(b.Name) && !currentNames.Contains(b.Name));
        }

        private static void HandleAutoFixMissingBindings(EUIBinding binding)
        {
            if (!binding) return;

            var baseBinding = GetBaseBinding(binding);
            if (baseBinding == null || baseBinding.Bindings == null) return;

            using (var so = new SerializedObject(binding))
            {
                var bindingsProp = so.FindProperty("bindings");

                // 收集当前已存在的绑定名
                var currentNames = new HashSet<string>();
                for (int i = 0; i < bindingsProp.arraySize; i++)
                {
                    var name = bindingsProp.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("Name").stringValue;
                    if (!string.IsNullOrEmpty(name))
                        currentNames.Add(name);
                }

                // 添加缺失的绑定条目
                int added = 0;
                foreach (var b in baseBinding.Bindings)
                {
                    if (string.IsNullOrEmpty(b.Name) || currentNames.Contains(b.Name))
                        continue;

                    int idx = bindingsProp.arraySize;
                    bindingsProp.InsertArrayElementAtIndex(idx);
                    var elem = bindingsProp.GetArrayElementAtIndex(idx);

                    elem.FindPropertyRelative("Name").stringValue = b.Name;
                    elem.FindPropertyRelative("Type").enumValueIndex =
                        b.Type > EUIBinding.WidgetTypes.End
                            ? (int)EUIBinding.WidgetTypes.End + 1
                            : (int)b.Type;
                    elem.FindPropertyRelative("ClassName").stringValue =
                        b.ClassName ?? string.Empty;

                    // 通过路径恢复 GameObject 引用
                    var goPath = GetRelativePath(baseBinding, b.GameObject);
                    if (!string.IsNullOrEmpty(goPath))
                    {
                        var target = binding.transform.Find(goPath);
                        elem.FindPropertyRelative("GameObject").objectReferenceValue =
                            target != null ? target.gameObject : null;
                    }

                    currentNames.Add(b.Name);
                    added++;
                }

                so.ApplyModifiedProperties();

                if (added > 0)
                {
                    EditorUtility.SetDirty(binding);
                    EmberDebug.Log("EmberUI", $"已自动添加 {added} 个缺失绑定");
                }
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 辅助

        /// <summary>从 binding 的 baseBindingUUID 加载基类 EUIBinding</summary>
        private static EUIBinding GetBaseBinding(EUIBinding binding)
        {
            if (!binding) return null;

            string guid;
            using (var so = new SerializedObject(binding))
                guid = so.FindProperty("baseBindingUUID").stringValue;

            if (string.IsNullOrEmpty(guid)) return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? prefab.GetComponent<EUIBinding>() : null;
        }

        /// <summary>获取 GameObject 相对于 binding 根的路径</summary>
        private static string GetRelativePath(EUIBinding binding, GameObject target)
        {
            if (!target || !binding) return null;

            var root = binding.transform;
            var cur = target.transform;
            string path = null;

            while (cur && cur != root)
            {
                path = path == null ? cur.name : cur.name + "/" + path;
                cur = cur.parent;
            }

            return cur == root ? path : null;
        }

        /// <summary>判断 binding 所在 Prefab 是否与给定路径相同</summary>
        private static bool IsSamePrefab(EUIBinding binding, string assetPath)
        {
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
            if (string.IsNullOrEmpty(prefabPath))
                prefabPath = AssetDatabase.GetAssetPath(binding.gameObject);

            return !string.IsNullOrEmpty(prefabPath)
                && prefabPath.Replace('\\', '/') == assetPath.Replace('\\', '/');
        }

        #endregion
    }
}
