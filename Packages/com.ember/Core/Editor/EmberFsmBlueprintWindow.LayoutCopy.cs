using System;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 外部方法
        /// <summary>按稳定节点 ID 复制已保存的本机布局。源窗口及未确认预览不变；目标必须没有已保存布局。</summary>
        public static void CopySavedLayout(EmberFsmGraph source, EmberFsmGraph destination)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string targetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(destination));
            if (source == null || destination == null || string.IsNullOrEmpty(sourceGuid) ||
                string.IsNullOrEmpty(targetGuid) || sourceGuid == targetGuid)
                throw new ArgumentException("布局复制需要两个不同的已保存图资产。");
            string root = "Ember.Fsm.Layout." + Application.dataPath + ".";
            string from = root + sourceGuid + ".", to = root + targetGuid + ".";
            var versions = new[] { "", "v2.canvas.", "v3.nested.", "v4.exterior." };
            foreach (string version in versions)
            {
                if (EditorPrefs.HasKey(to + version + "saved")) throw new InvalidOperationException("目标图已有布局，不覆盖。");
                foreach (var node in destination.Nodes)
                    if (node != null && EditorPrefs.HasKey(to + version + node.Id + ".x"))
                        throw new InvalidOperationException("目标图已有节点坐标，不覆盖。");
            }
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var node in source.Nodes) if (node != null) ids.Add(node.Id);
            foreach (string version in versions)
            {
                foreach (string key in new[] { "saved", "redirects" })
                    if (EditorPrefs.HasKey(from + version + key)) EditorPrefs.SetBool(to + version + key, EditorPrefs.GetBool(from + version + key));
                foreach (string key in new[] { "panX", "panY", "zoom" })
                    if (EditorPrefs.HasKey(from + version + key)) EditorPrefs.SetFloat(to + version + key, EditorPrefs.GetFloat(from + version + key));
                foreach (var node in destination.Nodes)
                {
                    if (node == null || !ids.Contains(node.Id)) continue;
                    string a = from + version + node.Id, b = to + version + node.Id;
                    foreach (string suffix in new[] { ".x", ".y" })
                        if (EditorPrefs.HasKey(a + suffix)) EditorPrefs.SetFloat(b + suffix, EditorPrefs.GetFloat(a + suffix));
                    foreach (string suffix in new[] { ".fold", ".redirect", ".pinned" })
                        if (EditorPrefs.HasKey(a + suffix)) EditorPrefs.SetBool(b + suffix, EditorPrefs.GetBool(a + suffix));
                }
            }
        }
        #endregion
    }
}
