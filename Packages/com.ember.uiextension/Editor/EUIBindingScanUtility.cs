// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;
using System.Text;

using Ember.Basic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// 扫描子组件按钮的编辑器实现。
    /// 递归遍历 binding 的所有子节点，报告未被绑定且未被 EUIBindingExclude 标记的节点。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingScanUtility
    {
        static EUIBindingScanUtility()
        {
            EUIBinding.OnScanUnboundChildren = HandleScanUnboundChildren;
        }

        private static void HandleScanUnboundChildren(EUIBinding binding)
        {
            if (!binding)
            {
                EUIBinding.ScanUnboundResult = null;
                return;
            }

            // 收集已绑定的 GameObject
            var boundObjects = new HashSet<GameObject>();
            if (binding.Bindings != null)
            {
                foreach (var entry in binding.Bindings)
                {
                    if (entry.GameObject)
                        boundObjects.Add(entry.GameObject);
                }
            }

            // 扫描未绑定子节点
            var unbound = new List<string>();
            ScanRecursive(binding.transform, binding.transform, boundObjects, unbound);

            if (unbound.Count == 0)
            {
                EUIBinding.ScanUnboundResult = null;
                EmberDebug.Log("EmberUI", "扫描完成：所有子组件已绑定 ✓");
                EditorUtility.DisplayDialog("扫描子组件",
                    "所有子组件都已绑定，未发现遗漏。", "确定");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"发现 {unbound.Count} 个未绑定的子组件：");
            foreach (var path in unbound)
                sb.AppendLine($"  · {path}");

            EUIBinding.ScanUnboundResult = sb.ToString();
            EmberDebug.LogWarning("EmberUI", sb.ToString());
        }

        private static void ScanRecursive(
            Transform root,
            Transform current,
            HashSet<GameObject> boundObjects,
            List<string> unbound)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                var childGO = child.gameObject;

                // 跳过被标记排除的节点及其子树
                if (childGO.GetComponent<EUIBindingExclude>())
                    continue;

                // 跳过本身就是 EUIBinding 根节点的（子页面）
                var subBinding = childGO.GetComponent<EUIBinding>();
                if (subBinding && subBinding != root.GetComponent<EUIBinding>())
                    continue;

                // 检查是否已绑定
                if (!boundObjects.Contains(childGO))
                {
                    var path = GetRelativePath(root, child);
                    unbound.Add(path ?? childGO.name);
                }

                // 递归子节点
                ScanRecursive(root, child, boundObjects, unbound);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (!root || !target) return null;
            if (root == target) return "/";

            var parts = new List<string>();
            var current = target;
            while (current && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root) return null;

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
