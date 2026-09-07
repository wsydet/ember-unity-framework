using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 统一 Update 循环管理器 —— 驱动所有模块的帧更新。
    ///
    /// 参考 burner 的 <c>GameUpdateManager</c>，核心设计：
    /// - 业务模块只从 <see cref="EmberModuleCollector"/> 读取，绝不自行创建禁用模块
    /// - 非业务单例通过反射发现 <see cref="IEmberUpdate"/> / <see cref="IEmberLateUpdate"/> / <see cref="IEmberFixedUpdate"/>
    /// - 每帧统一调用，避免几十个 MonoBehaviour 各自 Update
    /// - 业务模块仅在对应 Phase 完成 OnInit、尚未 OnDestroy 时才会被 Tick
    /// - 非业务接收者保留按优先级阈值筛选的机制
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

        /// <summary>业务模块 Update 接收者；仅在模块完成 OnInit 后驱动。</summary>
        private readonly List<EmberModuleCollector.ModuleEntry> _moduleUpdaters = new();

        /// <summary>业务模块 LateUpdate 接收者；仅在模块完成 OnInit 后驱动。</summary>
        private readonly List<EmberModuleCollector.ModuleEntry> _moduleLateUpdaters = new();

        /// <summary>业务模块 FixedUpdate 接收者；仅在模块完成 OnInit 后驱动。</summary>
        private readonly List<EmberModuleCollector.ModuleEntry> _moduleFixedUpdaters = new();

        /// <summary>非业务更新接收者的优先级阈值。</summary>
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

            for (int i = 0; i < _moduleUpdaters.Count; i++)
            {
                EmberModuleCollector.ModuleEntry entry = _moduleUpdaters[i];
                if (!entry.IsActive || entry.Module is not IEmberUpdate updater)
                    continue;

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

            for (int i = 0; i < _moduleLateUpdaters.Count; i++)
            {
                EmberModuleCollector.ModuleEntry entry = _moduleLateUpdaters[i];
                if (!entry.IsActive || entry.Module is not IEmberLateUpdate updater)
                    continue;

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

            for (int i = 0; i < _moduleFixedUpdaters.Count; i++)
            {
                EmberModuleCollector.ModuleEntry entry = _moduleFixedUpdaters[i];
                if (!entry.IsActive || entry.Module is not IEmberFixedUpdate updater)
                    continue;

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
            _moduleUpdaters.Clear();
            _moduleLateUpdaters.Clear();
            _moduleFixedUpdaters.Clear();
        }

        /// <summary>
        /// 复用已发现业务模块，并反射扫描其他实现 IEmberUpdate 等接口的单例。
        /// </summary>
        private void CollectAll()
        {
            _updaters.Clear();
            _lateUpdaters.Clear();
            _fixedUpdaters.Clear();
            _moduleUpdaters.Clear();
            _moduleLateUpdaters.Clear();
            _moduleFixedUpdaters.Clear();

            CollectDiscoveredModules();

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
                    if (typeof(IEmberModule).IsAssignableFrom(type)) continue;

                    TryCollect<IEmberUpdate>(type, _updaters);
                    TryCollect<IEmberLateUpdate>(type, _lateUpdaters);
                    TryCollect<IEmberFixedUpdate>(type, _fixedUpdaters);
                }
            }
        }

        /// <summary>
        /// 只登记 ModuleCollector 已发现并创建的启用模块。
        /// 每帧通过 ModuleEntry.IsActive 保证 OnInit 前和 OnDestroy 后不驱动模块。
        /// </summary>
        private void CollectDiscoveredModules()
        {
            if (!EmberModuleCollector.TryGetInstance(out EmberModuleCollector collector)
                || !collector.IsDiscovered)
            {
                EmberDebug.LogError(
                    TAG,
                    "EmberModuleCollector must discover modules before EmberUpdateManager initializes.");
                return;
            }

            IReadOnlyList<EmberModuleCollector.ModuleEntry> modules = collector.DiscoveredModules;
            for (int i = 0; i < modules.Count; i++)
            {
                EmberModuleCollector.ModuleEntry entry = modules[i];
                if (entry.Module is IEmberUpdate)
                    _moduleUpdaters.Add(entry);
                if (entry.Module is IEmberLateUpdate)
                    _moduleLateUpdaters.Add(entry);
                if (entry.Module is IEmberFixedUpdate)
                    _moduleFixedUpdaters.Add(entry);
            }
        }

        /// <summary>
        /// 尝试从非业务模块类型获取单例并加入对应列表。
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
