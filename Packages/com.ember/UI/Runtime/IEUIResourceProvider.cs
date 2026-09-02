// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 资源加载注入接口。
    /// 解耦 EUIViewEngine 与具体的资源加载实现。
    /// 默认实现走 <c>EmberResourceManager</c>，业务层可注入 Mock 实现用于测试。
    /// </summary>
    public interface IEUIResourceProvider
    {
        /// <summary>
        /// 异步加载 UI 预制体。
        /// </summary>
        /// <param name="prefabPath">预制体资源路径</param>
        /// <param name="onLoaded">加载完成回调</param>
        void LoadPrefabAsync(string prefabPath, Action<GameObject> onLoaded);

        /// <summary>
        /// 释放资源句柄（可选实现，用于引用计数场景）。
        /// </summary>
        void Release(string prefabPath);
    }

    /// <summary>
    /// IEUIResourceProvider 的默认实现 —— 委托给 EmberResourceManager。
    /// </summary>
    /// <para><b>路径形态：</b>支持两种输入——
    /// <list type="bullet">
    ///   <item>完整 Assets 路径（如 Assets/GameResource/Resources/UI/Common/Prefabs/X.prefab）：自动剥离到最近一个 "Resources/" 之后并去掉扩展名；</item>
    ///   <item>已是 Resources 相对路径：统一斜杠并去掉扩展名后透传。</item>
    /// </list>
    /// Editor 下业务层通常注入 EditorUIResourceProvider 走 AssetDatabase 直载，本类服务于打包运行时。</para>
    public class DefaultUIResourceProvider : IEUIResourceProvider
    {
        public void LoadPrefabAsync(string prefabPath, Action<GameObject> onLoaded)
        {
            Ember.Resource.EmberResourceManager.Instance.LoadAssetAsync<GameObject>(ToResourcesPath(prefabPath), onLoaded);
        }

        public void Release(string prefabPath)
        {
            // EmberResourceManager 目前由 Provider 自行管理生命周期
        }

        /// <summary>完整 Assets 路径 → Resources.Load 可用的无扩展名相对路径。</summary>
        internal static string ToResourcesPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var normalized = path.Replace('\\', '/');
            int resourcesIndex = normalized.LastIndexOf("/Resources/", System.StringComparison.Ordinal);
            if (resourcesIndex >= 0)
                normalized = normalized.Substring(resourcesIndex + "/Resources/".Length);

            int slashIndex = normalized.LastIndexOf('/');
            int extensionIndex = normalized.LastIndexOf('.');
            if (extensionIndex > slashIndex)
                normalized = normalized.Substring(0, extensionIndex);

            return normalized;
        }
    }
}
