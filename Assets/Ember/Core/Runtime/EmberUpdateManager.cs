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
    /// - 同时驱动 <see cref="EmberManagerCollector"/> 中收集的 Timer（后续接入）
    ///
    /// 使用方式：挂到场景中的 GameObject 上，或由 ManagerCollector 自动创建。
    /// </summary>
    public class EmberUpdateManager : EmberMonoSingleton<EmberUpdateManager>, IEmberManager
    {
        #region 参数

        /// <summary>Update 接收者列表，按优先级分组</summary>
        private readonly Dictionary<int, List<IEmberUpdate>> _updaters = new();

        /// <summary>LateUpdate 接收者列表</summary>
        private readonly Dictionary<int, List<IEmberLateUpdate>> _lateUpdaters = new();

        /// <summary>FixedUpdate 接收者列表</summary>
        private readonly Dictionary<int, List<IEmberFixedUpdate>> _fixedUpdaters = new();

        /// <summary>当前激活的模块阶段。只 Tick 此阶段及之前的接收者。</summary>
        public int CurrentPhase { get; set; } = int.MaxValue;

        /// <summary>防止同帧重复执行</summary>
        private long _lastFrameCount = -1;

        #endregion

        // ============================================================

        #region 外部方法

        void IEmberManager.Init()
        {
            CollectAll();
        }

        void IEmberManager.Destroy()
        {
            _updaters.Clear();
            _lateUpdaters.Clear();
            _fixedUpdaters.Clear();
        }

        #endregion

        // ============================================================

        #region 生命周期

        private void Update()
        {
            // 防止同帧重复调用（某些 Unity 版本或 Pause 场景可能触发多次）
            if (Time.frameCount == _lastFrameCount) return;
            _lastFrameCount = Time.frameCount;

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
                        Debug.LogError(
                            $"[Ember] Error in {updater.GetType().Name}.Update(): {ex.Message}");
                    }
                }
            }
        }

        private void LateUpdate()
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
                        Debug.LogError(
                            $"[Ember] Error in {updater.GetType().Name}.LateUpdate(): {ex.Message}");
                    }
                }
            }
        }

        private void FixedUpdate()
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
                        Debug.LogError(
                            $"[Ember] Error in {updater.GetType().Name}.FixedUpdate(): {ex.Message}");
                    }
                }
            }
        }

        #endregion

        // ============================================================

        #region 内部方法

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
                Debug.LogWarning(
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

        #endregion
    }
}
