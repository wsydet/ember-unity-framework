using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 统一 Update 循环管理器 —— 驱动所有模块的帧更新。
    ///
    /// 参考 burner 的 <c>GameUpdateManager</c>，核心设计：
    /// - 通过反射自动发现所有实现 <see cref="IEmberUpdate"/> / <see cref="IEmberLateUpdate"/> / <see cref="IEmberFixedUpdate"/> 的单例
    /// - 每帧统一调用，避免几十个 MonoBehaviour 各自 Update
    /// - 按模块阶段分组，当前阶段之前的模块才会被 Tick
    /// - 纯 C# 类，不继承 MonoBehaviour，由 <see cref="GameLauncher"/> 驱动
    ///
    /// 使用方式：无需手动操作，由 GameLauncher 自动创建并驱动。
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Core)]
    public class EmberUpdateManager : EmberSingleton<EmberUpdateManager>, IEmberManager
    {
        private const string TAG = LogTags.CoreUpdateManager;
        #region 参数

        /// <summary>Update 接收者列表，按优先级分组</summary>
        private readonly Dictionary<int, List<IEmberUpdate>> _updaters = new();

        /// <summary>LateUpdate 接收者列表</summary>
        private readonly Dictionary<int, List<IEmberLateUpdate>> _lateUpdaters = new();

        /// <summary>FixedUpdate 接收者列表</summary>
        private readonly Dictionary<int, List<IEmberFixedUpdate>> _fixedUpdaters = new();

        /// <summary>当前激活的模块阶段。只 Tick 此阶段及之前的接收者。</summary>
        public int CurrentPhase { get; set; } = int.MaxValue;

        #endregion

        // ============================================================

        #region 外部方法

        void IEmberManager.Init()
        {
            CollectAll();
            EmberDebug.LogInit(TAG, "EmberUpdateManager initialized.");
        }

        void IEmberManager.Destroy()
        {
            CleanupInternal();
        }

        // ======== 帧驱动（由 GameLauncher 调用） ========

        /// <summary>
        /// 驱动所有 <see cref="IEmberUpdate"/> 的 Update。
        /// 由 <see cref="GameLauncher"/> 每帧调用。
        /// </summary>
        public void DoUpdate()
        {
            foreach (var kvp in _updaters)
            {
                if (kvp.Key > CurrentPhase) continue;

                foreach (var updater in kvp.Value)
                {
                    try
                    {
                        updater.Update();
                    }
                    catch (Exception ex)
                    {
                        EmberDebug.LogError(TAG,
                            $"Error in {updater.GetType().Name}.Update(): {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 驱动所有 <see cref="IEmberLateUpdate"/> 的 LateUpdate。
        /// 由 <see cref="GameLauncher"/> 每帧调用。
        /// </summary>
        public void DoLateUpdate()
        {
            foreach (var kvp in _lateUpdaters)
            {
                if (kvp.Key > CurrentPhase) continue;

                foreach (var updater in kvp.Value)
                {
                    try
                    {
                        updater.LateUpdate();
                    }
                    catch (Exception ex)
                    {
                        EmberDebug.LogError(TAG,
                            $"Error in {updater.GetType().Name}.LateUpdate(): {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 驱动所有 <see cref="IEmberFixedUpdate"/> 的 FixedUpdate。
        /// 由 <see cref="GameLauncher"/> FixedUpdate 调用。
        /// </summary>
        public void DoFixedUpdate()
        {
            foreach (var kvp in _fixedUpdaters)
            {
                if (kvp.Key > CurrentPhase) continue;

                foreach (var updater in kvp.Value)
                {
                    try
                    {
                        updater.FixedUpdate();
                    }
                    catch (Exception ex)
                    {
                        EmberDebug.LogError(TAG,
                            $"Error in {updater.GetType().Name}.FixedUpdate(): {ex.Message}");
                    }
                }
            }
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// 共享清理逻辑：清空所有更新接收者列表。
        /// 同时被 <see cref="IEmberManager.Destroy"/> 和 <see cref="OnDestroy"/> 调用。
        /// </summary>
        private void CleanupInternal()
        {
            _updaters.Clear();
            _lateUpdaters.Clear();
            _fixedUpdaters.Clear();
        }

        /// <summary>
        /// 反射扫描所有已加载程序集中实现 IEmberUpdate 等接口的单例。
        /// </summary>
        private void CollectAll()
        {
            _updaters.Clear();
            _lateUpdaters.Clear();
            _fixedUpdaters.Clear();

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

                    TryCollect<IEmberUpdate>(type, _updaters);
                    TryCollect<IEmberLateUpdate>(type, _lateUpdaters);
                    TryCollect<IEmberFixedUpdate>(type, _fixedUpdaters);
                }
            }
        }

        /// <summary>
        /// 尝试从类型获取单例并加入对应列表。
        /// 阶段分组：优先读取 [EmberInitOrder] 的值，无则归入 int.MaxValue（始终 Tick）。
        /// </summary>
        private static void TryCollect<T>(Type type, Dictionary<int, List<T>> target) where T : class
        {
            if (!typeof(T).IsAssignableFrom(type)) return;

            var prop = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (prop == null) return;

            try
            {
                if (prop.GetValue(null) is not T instance) return;

                int phase = int.MaxValue;
                if (type.GetCustomAttribute<EmberInitOrderAttribute>() is { } attr)
                    phase = attr.Order;

                if (!target.TryGetValue(phase, out var list))
                {
                    list = new List<T>();
                    target[phase] = list;
                }

                list.Add(instance);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, 
                    $"[Ember] EmberUpdateManager: failed to collect {type.Name}: {ex.Message}");
            }
        }

        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) return true;

            // 保留 Ember 和 Game 程序集，跳过其他所有已知的系统/第三方程序集
            if (name.StartsWith("Ember") || name.StartsWith("Game")) return false;

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
                || name.Contains(".");
        }

        /// <summary>
        /// EmberSingleton 销毁钩子：确保通过 EmberSingleton.Destroy() 直接销毁时也能清理。
        /// 正常情况下由 <see cref="EmberManagerCollector.DestroyAll"/> → <see cref="IEmberManager.Destroy"/> 驱动。
        /// </summary>
        protected override void OnDestroy()
        {
            CleanupInternal();
        }

        #endregion
    }
}
