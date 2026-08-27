using System.IO;
using Ember.Core;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Unity 启动时自动检测并创建 EmberSceneMapping.asset（如果不存在）。
    /// 创建时自动扫描所有 EmberGameState 子类并匹配同名场景。
    /// 供 EmberProjectSetup 显式调用（EnsureAndRescan），保证场景生成后映射立即刷新。
    /// </summary>
    [InitializeOnLoad]
    public class EmberSceneMappingCreator
    {
        private const string TAG = LogTags.CoreEditor;
        private const string Path = "Assets/Editor/Ember/EmberSceneMapping.asset";

        static EmberSceneMappingCreator()
        {
            EditorApplication.delayCall += EnsureExists;
        }

        /// <summary>确保映射 SO 存在（多层建目录，兼容全新项目无 Assets/Editor 的情况）。</summary>
        public static void EnsureExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(Path);
            if (existing != null)
            {
                existing.SyncNewStates();
                return;
            }

            EnsureFolderMultiLevel(Path);

            var mapping = ScriptableObject.CreateInstance<EmberSceneMapping>();
            mapping.PopulateFromStates();
            AssetDatabase.CreateAsset(mapping, Path);
            AssetDatabase.SaveAssets();

            EmberDebug.LogInit(TAG, $"EmberSceneMapping.asset auto-created at: {Path} " +
                $"({mapping.entries.Count} state entries populated).");
        }

        /// <summary>确保映射 SO 存在，并全量重新扫描状态↔场景匹配（Setup 向导在创建场景后调用）。</summary>
        public static void EnsureAndRescan()
        {
            EnsureExists();

            var mapping = AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(Path);
            if (mapping == null) return;

            mapping.PopulateFromStates();
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();
            EmberDebug.LogInit(TAG, $"EmberSceneMapping 已重新扫描（{mapping.entries.Count} 个状态条目）。");
        }

        /// <summary>按 assetPath 逐级创建目录（AssetDatabase.CreateFolder 只支持单层且要求父目录已存在）。</summary>
        private static void EnsureFolderMultiLevel(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (string.IsNullOrEmpty(dir)) return;

            var parts = dir.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var parent = current;
                current += "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                    AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }
}
