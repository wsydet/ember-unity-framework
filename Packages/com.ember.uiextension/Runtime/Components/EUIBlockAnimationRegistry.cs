// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;
using System.Collections.Generic;
using System.Reflection;

using Ember.Basic;
using Ember.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 方块动画注册表 —— 反射扫描所有 <see cref="EUIBlockAnimation"/> 子类，
    /// 按 <see cref="EUIBlockAnimation.Type"/> 建立枚举 → 实例的映射。
    ///
    /// 仿 <c>EmberManagerCollector</c> 的自动发现：新增动画只需继承基类并实现，
    /// 无需手动注册。
    /// </summary>
    public static class EUIBlockAnimationRegistry
    {
        private const string TAG = LogTags.EmberUI;

        private static readonly Dictionary<EUIBlockAnimationType, EUIBlockAnimation> _map = new();
        private static bool _scanned;

        /// <summary>获取指定动画类型的实例（懒扫描，首次调用时自动发现）</summary>
        public static EUIBlockAnimation Get(EUIBlockAnimationType type)
        {
            EnsureScanned();
            return _map.TryGetValue(type, out var anim) ? anim : null;
        }

        private static void EnsureScanned()
        {
            if (_scanned) return;
            _scanned = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsSystemAssembly(assembly)) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(EUIBlockAnimation).IsAssignableFrom(type)) continue;

                    try
                    {
                        var instance = (EUIBlockAnimation)Activator.CreateInstance(type);
                        _map[instance.Type] = instance;
                        EmberDebug.LogInit(TAG, $"注册方块动画：{instance.Type} → {type.Name}");
                    }
                    catch (Exception ex)
                    {
                        EmberDebug.LogError(TAG, $"注册方块动画失败 {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>跳过系统/第三方程序集，只扫描项目代码。</summary>
        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) return true;

            return name.StartsWith("System")
                || name.StartsWith("Unity")
                || name.StartsWith("UnityEngine")
                || name.StartsWith("UnityEditor")
                || name.StartsWith("mscorlib")
                || name.StartsWith("netstandard")
                || name.StartsWith("Mono.")
                || name.StartsWith("Sirenix")
                || name.StartsWith("UniTask")
                || name.StartsWith("Cysharp")
                || name.StartsWith("TMPro")
                || name.StartsWith("Autodesk")
                || name.StartsWith("Coffee")
                || name.StartsWith("Feel")
                || name.StartsWith("YooAsset")
                || name.StartsWith("HybridCLR");
        }
    }
}
