using Ember.Core;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Unity 启动时自动检测并创建 EmberSceneMapping.asset（如果不存在）。
    /// 创建时自动扫描所有 EmberGameState 子类并匹配同名场景。
    /// </summary>
    [InitializeOnLoad]
    public class EmberSceneMappingCreator
    {
        private const string TAG = LogTags.CoreEditor;
        private const string Path = "Assets/Ember/Editor/Resources/EmberSceneMapping.asset";

        static EmberSceneMappingCreator()
        {
            EditorApplication.delayCall += EnsureExists;
        }

        private static void EnsureExists()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(Path);
            if (existing != null)
            {
                existing.SyncNewStates();
                return;
            }

            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir);
                var folderName = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            var mapping = ScriptableObject.CreateInstance<EmberSceneMapping>();
            mapping.PopulateFromStates();
            AssetDatabase.CreateAsset(mapping, Path);
            AssetDatabase.SaveAssets();

            EmberDebug.LogInit(TAG, $"EmberSceneMapping.asset auto-created at: {Path} " +
                $"({mapping.entries.Count} state entries populated).");
        }
    }
}
