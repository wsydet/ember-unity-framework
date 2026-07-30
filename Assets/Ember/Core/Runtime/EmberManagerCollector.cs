using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 管理器收集器 —— 反射扫描所有 <see cref="IEmberManager"/> 实现，
    /// 按 <see cref="EmberInitOrderAttribute"/> 排序后自动初始化。
    ///
    /// 参考 burner 的 <c>GameMgrCollector</c>。
    ///
    /// 核心流程：
    /// 1. 扫描所有已加载程序集中实现 IEmberManager 的类
    /// 2. 跳过抽象类、未标注 EmberInitOrder 的类
    /// 3. 通过静态 Instance 属性获取单例实例
    /// 4. 按 InitOrder 升序排序，依次调用 Init()
    /// 5. 销毁时逆序调用 Destroy()
    ///
    /// 使用方式（在游戏入口处调用一次即可）：
    /// <code>
    /// EmberManagerCollector.Instance.InitializeAll();
    /// // ... 游戏运行 ...
    /// EmberManagerCollector.Instance.DestroyAll();
    /// </code>
    /// </summary>
    public class EmberManagerCollector : EmberSingleton<EmberManagerCollector>
    {
        #region 参数

        private readonly List<IEmberManager> _managers = new();
        private bool _initialized;

        /// <summary>已发现的管理器数量</summary>
        public int ManagerCount => _managers.Count;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 扫描并初始化所有管理器。可安全重复调用（已初始化则跳过）。
        /// </summary>
        public void InitializeAll()
        {
            if (_initialized)
            {
                Debug.LogWarning("[Ember] EmberManagerCollector is already initialized.");
                return;
            }

            ScanAndCollect();
            _initialized = true;
        }

        /// <summary>
        /// 按 InitOrder 逆序销毁所有管理器。
        /// </summary>
        public void DestroyAll()
        {
            // 逆序销毁（先初始化的后销毁）
            for (int i = _managers.Count - 1; i >= 0; i--)
            {
                try
                {
                    _managers[i].Destroy();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ember] Error destroying manager {_managers[i].GetType().Name}: {ex.Message}");
                }
            }

            _managers.Clear();
            _initialized = false;
        }

        #endregion

        // ============================================================

        #region 内部方法

        private void ScanAndCollect()
        {
            var candidates = new List<(IEmberManager instance, int order)>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 跳过系统/Unity 内部程序集
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
                    if (!typeof(IEmberManager).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsInterface) continue;

                    var instance = GetSingletonInstance(type);
                    if (instance == null) continue;

                    int order = EmberInitOrderAttribute.Default;
                    if (type.GetCustomAttribute<EmberInitOrderAttribute>() is { } attr)
                        order = attr.Order;

                    candidates.Add((instance, order));
                }
            }

            // 按 InitOrder 排序
            candidates.Sort((a, b) => a.order - b.order);

            foreach (var (instance, _) in candidates)
            {
                _managers.Add(instance);
            }

            // 依次初始化
            foreach (var mgr in _managers)
            {
                try
                {
                    Debug.Log($"[Ember] Initializing manager: {mgr.GetType().Name}");
                    mgr.Init();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ember] Error initializing manager {mgr.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 通过反射获取类型的单例 Instance。
        /// 支持 <see cref="EmberSingleton{T}"/> 和 <see cref="EmberMonoSingleton{T}"/>。
        /// </summary>
        private static IEmberManager GetSingletonInstance(Type type)
        {
            // 查找静态属性 "Instance"
            var prop = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (prop == null)
            {
                Debug.LogWarning($"[Ember] IEmberManager type '{type.Name}' has no static Instance property. Skipping.");
                return null;
            }

            try
            {
                return prop.GetValue(null) as IEmberManager;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Ember] Failed to get Instance of '{type.Name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 跳过系统/Unity 内部程序集，只扫描项目代码。
        /// </summary>
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

        #endregion
    }
}
