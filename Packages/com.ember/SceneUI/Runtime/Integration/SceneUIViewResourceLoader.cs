// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.Resource;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>SceneUI 对单次 Prefab 资源加载的最小句柄契约。</summary>
    public interface ISceneUIViewResourceHandle : IDisposable
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        GameObject Asset { get; }
        string Error { get; }
        event Action Completed;
        void Cancel();
    }

    /// <summary>为 ResourceSceneUIViewHost 提供后端无关的 Prefab 加载句柄。</summary>
    public interface ISceneUIViewResourceLoader
    {
        ISceneUIViewResourceHandle LoadPrefab(string assetPath);
    }

    /// <summary>通过 EmberResourceManager 和 EmberAssetHandle 加载 SceneUI Prefab。</summary>
    public sealed class EmberSceneUIViewResourceLoader : ISceneUIViewResourceLoader
    {
        public ISceneUIViewResourceHandle LoadPrefab(string assetPath)
        {
            EmberAssetHandle<GameObject> handle =
                EmberResourceManager.Instance.LoadAssetHandle<GameObject>(assetPath);
            return handle != null ? new HandleAdapter(handle) : null;
        }

        private sealed class HandleAdapter : ISceneUIViewResourceHandle
        {
            private EmberAssetHandle<GameObject> _handle;
            private event Action CompletionCallbacks;

            public bool IsDone => _handle == null || _handle.IsDone;
            public bool Succeeded => _handle != null && _handle.Succeeded;
            public GameObject Asset => _handle != null ? _handle.Asset : null;
            public string Error => _handle?.Error;

            public event Action Completed
            {
                add
                {
                    if (IsDone)
                        value?.Invoke();
                    else
                        CompletionCallbacks += value;
                }
                remove => CompletionCallbacks -= value;
            }

            public HandleAdapter(EmberAssetHandle<GameObject> handle)
            {
                _handle = handle;
                _handle.Completed += OnHandleCompleted;
            }

            public void Cancel()
            {
                _handle?.Cancel();
            }

            public void Dispose()
            {
                if (_handle == null)
                    return;

                EmberAssetHandle<GameObject> handle = _handle;
                _handle = null;
                CompletionCallbacks = null;
                handle.Completed -= OnHandleCompleted;
                handle.Dispose();
            }

            private void OnHandleCompleted(GameObject _)
            {
                Action callbacks = CompletionCallbacks;
                CompletionCallbacks = null;
                callbacks?.Invoke();
            }
        }
    }
}
