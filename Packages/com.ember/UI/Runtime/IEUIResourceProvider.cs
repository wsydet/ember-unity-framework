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
    public class DefaultUIResourceProvider : IEUIResourceProvider
    {
        public void LoadPrefabAsync(string prefabPath, Action<GameObject> onLoaded)
        {
            Ember.Resource.EmberResourceManager.Instance.LoadAssetAsync<GameObject>(prefabPath, onLoaded);
        }

        public void Release(string prefabPath)
        {
            // EmberResourceManager 目前由 Provider 自行管理生命周期
        }
    }
}
