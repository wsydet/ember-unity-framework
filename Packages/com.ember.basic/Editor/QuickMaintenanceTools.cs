// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 快捷维护工具 —— 独立菜单项，不需要打开窗口。
    /// </summary>
    public static class QuickMaintenanceTools
    {
        [MenuItem("Ember/Tool/清空本地缓存 (PlayerPrefs + PersistentData)", false, 360)]
        public static void ClearLocalCache()
        {
            var lang = EmberEditorWindow.GlobalLang;
            if (!EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Confirm Cleanup", "确认清理"),
                EditorToolUtility.L10n(lang,
                    "This will delete all PlayerPrefs records and files under persistentDataPath.\nThis action cannot be undone!",
                    "将删除所有 PlayerPrefs 记录和 persistentDataPath 下的文件（存档等）。\n此操作不可撤销！"),
                EditorToolUtility.L10n(lang, "Confirm", "确认清理"),
                EditorToolUtility.L10n(lang, "Cancel", "取消")))
                return;

            PlayerPrefs.DeleteAll();
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
            EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Done", "完成"),
                EditorToolUtility.L10n(lang, "Local cache cleared.", "本地缓存已清空。"),
                "OK");
        }

        [MenuItem("Ember/Tool/删除项目空文件夹", false, 370)]
        public static void RemoveEmptyFolders()
        {
            var lang = EmberEditorWindow.GlobalLang;
            if (!EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Confirm", "确认操作"),
                EditorToolUtility.L10n(lang,
                    "Scan the Assets directory and delete all empty sub-folders (including .meta).\nThis action cannot be undone!",
                    "将扫描 Assets 目录，删除所有空的子文件夹（含 .meta）。\n此操作不可撤销！"),
                EditorToolUtility.L10n(lang, "Delete", "确认删除"),
                EditorToolUtility.L10n(lang, "Cancel", "取消")))
                return;

            int count = 0;
            var dirs = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);
            for (int i = dirs.Length - 1; i >= 0; i--)
            {
                if (Directory.GetFiles(dirs[i]).Length == 0 && Directory.GetDirectories(dirs[i]).Length == 0)
                {
                    string meta = dirs[i] + ".meta";
                    Directory.Delete(dirs[i], true);
                    if (File.Exists(meta)) File.Delete(meta);
                    count++;
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Done", "完成"),
                EditorToolUtility.L10n(lang, $"Deleted {count} empty folders.", $"已删除 {count} 个空文件夹。"),
                "OK");
        }
    }
}
#endif
