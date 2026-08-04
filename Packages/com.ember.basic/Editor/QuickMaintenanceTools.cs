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
        [MenuItem("Tools/Ember/清空本地缓存 (PlayerPrefs + PersistentData)", false, 9000)]
        public static void ClearLocalCache()
        {
            if (!EditorUtility.DisplayDialog("确认清理",
                "将删除所有 PlayerPrefs 记录和 persistentDataPath 下的文件（存档等）。\n此操作不可撤销！",
                "确认清理", "取消"))
                return;

            PlayerPrefs.DeleteAll();
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
            EditorUtility.DisplayDialog("完成", "本地缓存已清空。", "OK");
        }

        [MenuItem("Tools/Ember/删除项目空文件夹", false, 9001)]
        public static void RemoveEmptyFolders()
        {
            if (!EditorUtility.DisplayDialog("确认操作",
                "将扫描 Assets 目录，删除所有空的子文件夹（含 .meta）。\n此操作不可撤销！",
                "确认删除", "取消"))
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
            EditorUtility.DisplayDialog("完成", $"已删除 {count} 个空文件夹。", "OK");
        }
    }
}
#endif
