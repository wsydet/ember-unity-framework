using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 服务定位器 —— 轻量级的服务注册与查找机制。
    ///
    /// 设计意图：
    /// burner 项目没有使用 DI 容器，而是通过 <c>Singleton&lt;T&gt;.Instance</c> +
    /// 反射扫描 <c>IManager</c> 的方式管理依赖。EmberServiceLocator 提供一种
    /// 介于"纯静态单例"和"完整 DI 容器"之间的中间方案：
    ///
    /// - 框架层模块通过 ServiceLocator 暴露服务接口
    /// - 业务层通过 ServiceLocator 获取所需服务
    /// - 避免硬编码的 .Instance 循环依赖
    ///
    /// 特性：
    /// - 接口 → 实现映射，面向接口编程
    /// - 支持立即注册和延迟工厂注册
    /// - 线程不安全（仅主线程使用，符合 Unity 规范）
    /// - 支持 Clear 重置（用于测试或热重启）
    ///
    /// 用法：
    /// <code>
    /// // 注册（框架初始化时）
    /// EmberServiceLocator.Register&lt;IResourceProvider&gt;(new AddressablesProvider());
    /// // 延迟注册
    /// EmberServiceLocator.RegisterLazy&lt;IAudioManager&gt;(() => new EmberAudioManager());
    /// // 解析（业务层）
    /// var resProvider = EmberServiceLocator.Resolve&lt;IResourceProvider&gt;();
    /// </code>
    /// </summary>
    public static class EmberServiceLocator
    {
        private const string TAG = LogTags.CoreServiceLocator;

        #region 参数

        /// <summary>
        /// 已注册的服务实例字典：Type → 实例。
        /// </summary>
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// 延迟工厂字典：Type → 工厂函数。首次 Resolve 时调用。
        /// </summary>
        private static readonly Dictionary<Type, Func<object>> _lazyFactories = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// 获取当前已注册服务的数量（用于诊断）。
        /// </summary>
        public static int RegisteredCount => _services.Count + _lazyFactories.Count;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 注册一个服务实例。同一接口只能注册一次。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <param name="instance">实现实例</param>
        /// <exception cref="ArgumentNullException">instance 为 null</exception>
        /// <exception cref="InvalidOperationException">该接口已注册</exception>
        public static void Register<TService>(TService instance) where TService : class
        {
            EmberDebug.LogInit(TAG, $"Register: {typeof(TService).Name}");
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = typeof(TService);

            if (_services.ContainsKey(type))
                throw new InvalidOperationException(
                    $"Service of type '{type.Name}' is already registered. " +
                    $"Use Unregister() first if you intend to replace it.");

            _services[type] = instance;
        }

        /// <summary>
        /// 注册一个延迟初始化的服务工厂。Factory 在首次 <see cref="Resolve{TService}"/> 时调用，
        /// 且只调用一次，结果会被缓存。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <param name="factory">创建服务实例的工厂函数</param>
        /// <exception cref="ArgumentNullException">factory 为 null</exception>
        /// <exception cref="InvalidOperationException">该接口已注册（无论是立即还是延迟）</exception>
        public static void RegisterLazy<TService>(Func<TService> factory) where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            Type type = typeof(TService);

            if (_services.ContainsKey(type))
                throw new InvalidOperationException(
                    $"Service of type '{type.Name}' is already registered.");
            if (_lazyFactories.ContainsKey(type))
                throw new InvalidOperationException(
                    $"A lazy factory for type '{type.Name}' is already registered.");

            _lazyFactories[type] = () => factory();
        }

        /// <summary>
        /// 尝试注册。如果已存在则返回 false 不抛异常。
        /// </summary>
        public static bool TryRegister<TService>(TService instance) where TService : class
        {
            if (instance == null) return false;

            Type type = typeof(TService);
            if (_services.ContainsKey(type)) return false;

            _services[type] = instance;
            return true;
        }

        /// <summary>
        /// 解析指定接口的服务实例。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">服务未注册</exception>
        public static TService Resolve<TService>() where TService : class
        {
            Type type = typeof(TService);

            // 先查已注册实例
            if (_services.TryGetValue(type, out object instance))
                return (TService)instance;

            // 再查延迟工厂
            if (_lazyFactories.TryGetValue(type, out Func<object> factory))
            {
                instance = factory();
                _services[type] = instance;
                _lazyFactories.Remove(type);
                return (TService)instance;
            }

            throw new InvalidOperationException(
                $"Service of type '{type.Name}' is not registered. " +
                $"Call Register<T>() or RegisterLazy<T>() first.");
        }

        /// <summary>
        /// 尝试解析服务，如果未注册返回默认值。
        /// </summary>
        public static TService TryResolve<TService>() where TService : class
        {
            Type type = typeof(TService);

            if (_services.TryGetValue(type, out object instance))
                return (TService)instance;

            if (_lazyFactories.TryGetValue(type, out Func<object> factory))
            {
                instance = factory();
                _services[type] = instance;
                _lazyFactories.Remove(type);
                return (TService)instance;
            }

            return null;
        }

        /// <summary>
        /// 检查服务是否已注册（包括延迟注册）。
        /// </summary>
        public static bool IsRegistered<TService>() where TService : class
        {
            Type type = typeof(TService);
            return _services.ContainsKey(type) || _lazyFactories.ContainsKey(type);
        }

        /// <summary>
        /// 注销指定接口的服务。
        /// </summary>
        /// <returns>是否成功注销</returns>
        public static bool Unregister<TService>() where TService : class
        {
            Type type = typeof(TService);
            bool removed = _services.Remove(type);
            removed |= _lazyFactories.Remove(type);
            return removed;
        }

        /// <summary>
        /// 清除所有已注册的服务和延迟工厂。仅在彻底重置时使用。
        /// </summary>
        public static void ClearAll()
        {
            // 按 IDisposable 顺序清理
            foreach (var kvp in _services)
            {
                if (kvp.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _services.Clear();
            _lazyFactories.Clear();
        }

        #endregion
    }
}
