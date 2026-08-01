using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// 自动同步 Build Settings 场景列表。
    /// - FrameworkScene 始终在首位（启动场景）
    /// - Assets/Game/Scenes/ 下所有 .unity 文件自动加入 Build Settings
    /// - 触发时机：编译完成 / 域重载 / Scenes 文件夹文件变更
    /// </summary>
    [InitializeOnLoad]
    public static class FrameworkSceneBootstrapper
    {
        private const string ScenesFolder = "Assets/Game/Scenes";
        private const string FrameworkSceneName = "FrameworkScene";

        static FrameworkSceneBootstrapper()
        {
            EditorApplication.delayCall += SyncBuildScenes;
        }

        /// <summary>
        /// 当 Scenes 文件夹下有 .unity 文件变动时触发同步。
        /// </summary>
        private class ScenesAssetPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] deleted, string[] movedFrom, string[] movedTo)
            {
                foreach (var path in imported)
                    if (IsInScenesFolder(path)) { SyncBuildScenes(); return; }

                foreach (var path in deleted)
                    if (IsInScenesFolder(path)) { SyncBuildScenes(); return; }

                foreach (var path in movedTo)
                    if (IsInScenesFolder(path)) { SyncBuildScenes(); return; }
            }

            private static bool IsInScenesFolder(string path) =>
                path.StartsWith(ScenesFolder) && path.EndsWith(".unity");
        }

        /// <summary>
        /// 扫描 Scenes 文件夹，同步到 EditorBuildSettings。
        /// FrameworkScene 强制首位，其余按文件名排序。
        /// </summary>
        private static void SyncBuildScenes()
        {
            // 扫描文件夹下所有场景
            var allScenes = new List<string>();
            if (Directory.Exists(ScenesFolder))
            {
                foreach (var file in Directory.GetFiles(ScenesFolder, "*.unity", SearchOption.AllDirectories))
                {
                    allScenes.Add(file.Replace("\\", "/"));
                }
            }

            if (allScenes.Count == 0)
            {
                Debug.LogWarning($"[Ember] No scenes found in {ScenesFolder}. Build Settings unchanged.");
                return;
            }

            // 找到 FrameworkScene
            string frameworkPath = null;
            foreach (var s in allScenes)
            {
                if (Path.GetFileNameWithoutExtension(s) == FrameworkSceneName)
                {
                    frameworkPath = s;
                    break;
                }
            }

            // 构建列表：FrameworkScene 首位，其余按名排序
            allScenes.Sort();
            var result = new List<EditorBuildSettingsScene>();

            if (frameworkPath != null)
            {
                result.Add(new EditorBuildSettingsScene(frameworkPath, true));
                foreach (var s in allScenes)
                    if (s != frameworkPath)
                        result.Add(new EditorBuildSettingsScene(s, true));
            }
            else
            {
                Debug.LogWarning($"[Ember] {FrameworkSceneName}.unity not found in {ScenesFolder}. All scenes added alphabetically.");
                foreach (var s in allScenes)
                    result.Add(new EditorBuildSettingsScene(s, true));
            }

            // 仅在列表变化时才写入，避免脏标记
            var current = EditorBuildSettings.scenes;
            if (ScenesEqual(current, result))
                return;

            EditorBuildSettings.scenes = result.ToArray();
            Debug.Log($"[Ember] Build Settings synced: {result.Count} scene(s) from {ScenesFolder}");
        }

        private static bool ScenesEqual(EditorBuildSettingsScene[] a, List<EditorBuildSettingsScene> b)
        {
            if (a.Length != b.Count) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].path != b[i].path || a[i].enabled != b[i].enabled)
                    return false;
            return true;
        }
    }
}
