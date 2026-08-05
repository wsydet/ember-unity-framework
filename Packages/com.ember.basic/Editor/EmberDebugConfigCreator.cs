using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Unity 启动时自动检测并创建 EmberDebugConfig.asset（如果不存在）。
    /// </summary>
    [InitializeOnLoad]
    public class EmberDebugConfigCreator
    {
        private const string TAG = LogTags.CoreEditor;
        private const string Path = "Assets/Ember/Core/Runtime/Resources/EmberDebugConfig.asset";
        static EmberDebugConfigCreator()
        {
            EditorApplication.delayCall += EnsureConfigExists;
        }

        private static void EnsureConfigExists()
        {
            // 已存在则跳过
            if (AssetDatabase.LoadAssetAtPath<EmberDebugConfigSO>(Path) != null)
                return;

            // 确保 Resources 目录存在
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir);
                var folderName = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            // 创建 SO 并预填框架标签
            var config = ScriptableObject.CreateInstance<EmberDebugConfigSO>();
            config.PopulateFrameworkEntries();
            AssetDatabase.CreateAsset(config, Path);
            AssetDatabase.SaveAssets();

            EmberDebug.Log(TAG, $"EmberDebugConfig.asset auto-created at: {Path} " +
                $"({config.frameworkEntries.Count} framework tags pre-populated).");
    
        }
    }
}
