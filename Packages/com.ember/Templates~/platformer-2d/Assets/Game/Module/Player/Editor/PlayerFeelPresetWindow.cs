using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>实际配置的比较、独立另存和显式参考恢复；不操作运行实例或进度。</summary>
    public sealed class PlayerFeelPresetWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerFeelProfile _current;
        [SerializeField] private PlayerFeelProfile _reference;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.HelpBox("预设工作流已整合到完整玩家方案；原预设选择保留。", MessageType.Info);
            if (GUILayout.Button("打开玩家配置页")) { Open(_current); Close(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static UnityEngine.Object[] Groups(PlayerFeelProfile p)
        {
            if (p == null || p.Movement == null || p.Jump == null || p.Input == null) throw new InvalidOperationException("预设缺少分组配置。");
            return new UnityEngine.Object[] { p.Movement, p.Jump, p.Input };
        }
        private static string Value(SerializedProperty p) => p.propertyType switch
        {
            SerializedPropertyType.Float => p.floatValue.ToString("G9", System.Globalization.CultureInfo.InvariantCulture),
            SerializedPropertyType.Boolean => p.boolValue ? "开启" : "关闭",
            SerializedPropertyType.Enum => p.enumDisplayNames[p.enumValueIndex],
            SerializedPropertyType.Integer or SerializedPropertyType.LayerMask => p.intValue.ToString(),
            _ => p.propertyType.ToString()
        };
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void ShowWindow() => Open(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)?.Feel);
        public static void Open(PlayerFeelProfile current)
        {
            PlayerSchemeWindow.OpenTab(1);
            GetWindow<PlayerSchemeWindow>().PreserveScaleDraft(current, true);
        }
        public static List<string> Compare(PlayerFeelProfile current, PlayerFeelProfile reference)
        {
            var a = Groups(current); var b = Groups(reference); var result = new List<string>();
            string[] names = { "移动", "跳跃", "输入" };
            for (int i = 0; i < a.Length; i++)
            {
                using var left = new SerializedObject(a[i]); using var right = new SerializedObject(b[i]);
                var p = left.GetIterator();
                while (p.Next(true))
                {
                    if (!p.propertyPath.StartsWith("_", StringComparison.Ordinal) || p.propertyType == SerializedPropertyType.Generic) continue;
                    var q = right.FindProperty(p.propertyPath);
                    if (q == null || !SerializedProperty.DataEquals(p, q))
                        result.Add($"{names[i]} / {p.displayName} ({p.propertyPath})\n{Value(p)} → {(q == null ? "缺失" : Value(q))}");
                }
            }
            return result;
        }
        public static void Restore(PlayerFeelProfile current, PlayerFeelProfile reference)
        {
            PlayerEditorAccess.Require();
            var destination = Groups(current); var source = Groups(reference); reference.Capture(Physics2D.gravity);
            PlayerSchemeOwnership.RequireWritable(current);
            foreach (var asset in destination) PlayerSchemeOwnership.RequireWritable(asset);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("恢复预设参考参数");
            Undo.RegisterCompleteObjectUndo(destination, "恢复预设参考参数");
            for (int i = 0; i < destination.Length; i++)
            {
                if (destination[i] == source[i]) continue;
                string name = destination[i].name; EditorUtility.CopySerialized(source[i], destination[i]);
                destination[i].name = name; destination[i].hideFlags &= ~HideFlags.NotEditable; EditorUtility.SetDirty(destination[i]);
            }
            Undo.CollapseUndoOperations(group);
        }
        public static PlayerFeelProfile SaveCopy(PlayerFeelProfile source, string path)
        {
            PlayerEditorAccess.Require();
            source.Capture(Physics2D.gravity);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("请选择 Assets 内的新 .asset 路径。");
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) throw new ArgumentException("目标目录不存在。");
            string stem = path.Substring(0, path.Length - 6);
            string[] paths = { path, stem + "Movement.asset", stem + "Jump.asset", stem + "Input.asset" };
            foreach (var item in paths) if (File.Exists(item) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(item))) throw new InvalidOperationException("目标已存在，不覆盖：" + item);
            var copy = source.CreateIndependentCopy();
            var assets = new UnityEngine.Object[] { copy, copy.Movement, copy.Jump, copy.Input };
            foreach (var asset in assets) asset.hideFlags &= ~HideFlags.NotEditable;
            int created = 0;
            try
            {
                for (int i = 0; i < assets.Length; i++) { AssetDatabase.CreateAsset(assets[i], paths[i]); created++; }
                PlayerFsmParameterEditor.Save(copy); return copy;
            }
            catch
            {
                for (int i = 0; i < created; i++) AssetDatabase.DeleteAsset(paths[i]);
                for (int i = created; i < assets.Length; i++) if (assets[i] != null) DestroyImmediate(assets[i]);
                throw;
            }
        }
        #endregion
    }
}
