using System;
using System.Collections.Generic;
using System.Reflection;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 业务模块收集器 —— 反射扫描所有 <see cref="IEmberModule"/> 实现，按 Phase 分组管理生命周期。
    ///
    /// 与 <see cref="EmberManagerCollector"/>（框架管道，启动即初始化）不同，
    /// 本收集器管理的模块由顶层状态机按阶段驱动：
    ///
    /// <code>
    /// // 进入 Init 状态（InitState.OnEnter）
    /// EmberModuleCollector.Instance.InitPhase(ModulePhase.Global);
    ///
    /// // 进入 Main 状态（MainState.OnEnter）
    /// EmberModuleCollector.Instance.InitPhase(ModulePhase.Main);
    ///
    /// // 游戏退出（GameLauncher.ShutdownFramework）
    /// EmberModuleCollector.Instance.DestroyAll();
    /// </code>
    ///
    /// 生命周期：
    /// - InitPhase：首次进入 → OnInit；再次进入（热重启）→ ResetModuleData + OnInit
    /// - DestroyPhase：OnDestroy（对象保留，供热重启复用）
    /// - DestroyAll：销毁全部并清空登记表（游戏退出）
    ///
    /// 模块以单例形式存在（继承 <see cref="EmberSingleton{T}"/>），通过静态 Instance 属性访问。
    /// </summary>
    public class EmberModuleCollector : EmberSingleton<EmberModuleCollector>
    {
        private const string TAG = LogTags.CoreModuleCollector;

        #region 内部参数

        /// <summary>Phase → 模块条目列表</summary>
        private readonly Dictionary<int, List<ModuleEntry>> _phaseMap = new();

        /// <summary>所有模块条目（用于 DestroyAll + 计数）</summary>
        private readonly List<ModuleEntry> _all = new();

        private bool _scanned;

        /// <summary>已发现的模块总数</summary>
        public int ModuleCount => _all.Count;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 初始化指定阶段的所有模块。
        /// 首次进入调用 OnInit；再次进入（热重启）先 ResetModuleData 再 OnInit。可安全重复调用。
        /// </summary>
        public void InitPhase(int phase)
        {
            EnsureScanned();
            if (!_phaseMap.TryGetValue(phase, out var list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry.IsActive) continue;   // 已活跃，幂等跳过

                if (entry.EverInitialized)
                {
                    EmberDebug.LogEvent(TAG, $"Hot-restarting module: {entry.Name}");
                    try { entry.Module.ResetModuleData(); }
                    catch (Exception ex) { EmberDebug.LogError(TAG, $"Error resetting module {entry.Name}: {ex.Message}"); }
                }

                EmberDebug.LogInit(TAG, $"Initializing module: {entry.Name}");
                try { entry.Module.OnInit(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Error initializing module {entry.Name}: {ex.Message}"); }

                entry.IsActive = true;
                entry.EverInitialized = true;
            }
        }

        /// <summary>
        /// 销毁指定阶段的所有模块（对象保留，供热重启复用）。
        /// </summary>
        public void DestroyPhase(int phase)
        {
            EnsureScanned();
            if (!_phaseMap.TryGetValue(phase, out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var entry = list[i];
                if (!entry.IsActive) continue;

                EmberDebug.LogCleanup(TAG, $"Destroying module: {entry.Name}");
                try { entry.Module.OnDestroy(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Error destroying module {entry.Name}: {ex.Message}"); }

                entry.IsActive = false;
            }
        }

        /// <summary>
        /// 销毁所有阶段的所有模块并清空登记表（游戏退出时调用）。
        /// </summary>
        public void DestroyAll()
        {
            EnsureScanned();

            for (int i = _all.Count - 1; i >= 0; i--)
            {
                var entry = _all[i];
                if (!entry.IsActive) continue;

                EmberDebug.LogCleanup(TAG, $"Destroying module: {entry.Name}");
                try { entry.Module.OnDestroy(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Error destroying module {entry.Name}: {ex.Message}"); }

                entry.IsActive = false;
            }

            _phaseMap.Clear();
            _all.Clear();
            _scanned = false;
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>惰性扫描：反射所有 IEmberModule 实现，按 Phase 分组。不触发 OnInit。</summary>
        private void EnsureScanned()
        {
            if (_scanned) return;
            _scanned = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsSystemAssembly(assembly)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException) { continue; }

                foreach (var type in types)
                {
                    if (!typeof(IEmberModule).IsAssignableFrom(type)) continue;
                    if (type.IsAbstract || type.IsInterface) continue;

                    var module = GetSingletonInstance(type);
                    if (module == null) continue;
                    if (!module.Enabled) continue;   // 跳过未启用的模块

                    var entry = new ModuleEntry(module);
                    _all.Add(entry);

                    if (!_phaseMap.TryGetValue(module.Phase, out var list))
                    {
                        list = new List<ModuleEntry>();
                        _phaseMap[module.Phase] = list;
                    }
                    list.Add(entry);
                }
            }

            EmberDebug.LogInit(TAG, $"Scanned {_all.Count} business module(s) across {_phaseMap.Count} phase(s).");
        }

        /// <summary>通过反射获取模块单例 Instance（支持 EmberSingleton）。</summary>
        private static IEmberModule GetSingletonInstance(Type type)
        {
            var prop = type.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (prop == null)
            {
                EmberDebug.LogWarning(TAG, $"IEmberModule type '{type.Name}' has no static Instance property. Skipping.");
                return null;
            }

            try
            {
                return prop.GetValue(null) as IEmberModule;
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"Failed to get Instance of '{type.Name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>跳过系统/Unity 内部程序集，只扫描项目代码（含 Assembly-CSharp）。</summary>
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

        /// <summary>模块条目：模块实例 + 生命周期状态标记。</summary>
        private sealed class ModuleEntry
        {
            public readonly IEmberModule Module;
            public bool IsActive;          // OnInit 已调用且尚未 OnDestroy
            public bool EverInitialized;   // OnInit 至少调用过一次（决定热重启是否 Reset）

            public string Name => Module.GetType().Name;

            public ModuleEntry(IEmberModule module)
            {
                Module = module;
            }
        }

        #endregion
    }
}
