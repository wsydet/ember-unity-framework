using System;
using System.IO;
using Ember.Basic;
using Ember.UI;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    /// <summary>
    /// 编辑器专用 UI 资源加载器。
    /// EUIPageDef 中的 PrefabPath 即完整 Asset 路径（Assets/ 开头），
    /// 直接走 AssetDatabase.LoadAssetAtPath，无需映射、无需 Resources 目录。
    ///
    /// 运行时由 <see cref="EditorUIResourceProviderSetup"/> 自动注入到
    /// <see cref="EUIViewEngine.ResourceProvider"/>。
    /// </summary>
    public class EditorUIResourceProvider : IEUIResourceProvider
    {
        private const string TAG = LogTags.Game + "." + nameof(EditorUIResourceProvider);

        public void LoadPrefabAsync(string prefabPath, Action<GameObject> onLoaded)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                onLoaded?.Invoke(null);
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                onLoaded?.Invoke(prefab);
            }
            else
            {
                EmberDebug.LogError(TAG,
                    $"预制体加载失败: {prefabPath}，请确认文件存在且路径正确。");
                onLoaded?.Invoke(null);
            }
        }

        public void Release(string prefabPath) { }
    }

    /// <summary>
    /// 编辑器进入 Play Mode 时自动注入 <see cref="EditorUIResourceProvider"/>。
    /// </summary>
    public static class EditorUIResourceProviderSetup
    {
        private const string TAG = LogTags.Game + "." + nameof(EditorUIResourceProviderSetup);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inject()
        {
            try
            {
                EUIViewEngine.Instance.ResourceProvider = new EditorUIResourceProvider();
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"注入失败: {ex.Message}");
            }
        }
    }
}
