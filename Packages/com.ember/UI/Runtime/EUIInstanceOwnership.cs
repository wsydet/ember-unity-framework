// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Runtime.CompilerServices;
using Ember.Basic;
using UnityEngine;

namespace Ember.UI
{
    /// <summary>主线程上的实例逻辑所有权。令牌释放后可再次包装；不拥有或销毁 GameObject。</summary>
    public static class EUIInstanceOwnership
    {
        private static readonly ConditionalWeakTable<GameObject, Lease> Owners = new();

        private sealed class Lease : IDisposable
        {
            private GameObject _instance;
            internal Lease(GameObject instance) { _instance = instance; }
            public void Dispose()
            {
                if (ReferenceEquals(_instance, null)) return;
                Owners.Remove(_instance);
                _instance = null;
            }
        }

        /// <summary>检查实例是否已有 Item 或父级 UILogic 所有者。</summary>
        [NoGC]
        public static bool IsOwned(GameObject instance) => !ReferenceEquals(instance, null) && Owners.TryGetValue(instance, out _);

        /// <summary>在构造或绑定逻辑前占用实例，重复占用立即失败。</summary>
        [HasGC]
        public static IDisposable Acquire(GameObject instance)
        {
            if (!instance) throw new ArgumentNullException(nameof(instance));
            if (IsOwned(instance)) throw new InvalidOperationException($"EUI instance '{instance.name}' already has a logic owner.");
            var lease = new Lease(instance);
            Owners.Add(instance, lease);
            return lease;
        }
    }
}
