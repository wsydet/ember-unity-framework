using System;
using UnityEngine;

namespace Ember.Core
{
    // ============================================================
    // 内部生命周期接口
    // ============================================================

    /// <summary>
    /// 内部接口，用于在不知道具体 T 类型的情况下调用单例销毁钩子。
    /// </summary>
    internal interface IEmberSingletonLifecycle
    {
        void InvokeOnDestroy();
    }

    // ============================================================
    // 非 MonoBehaviour 版本
    // ============================================================

    /// <summary>
    /// 非 MonoBehaviour 单例基类。
    ///
    /// 参考 burner 项目的 <c>Singleton&lt;T&gt;</c> 模式。
    /// 用于不需要挂在 GameObject 上的纯逻辑管理器。
    ///
    /// 特性：
    /// - 线程安全（双检锁 + volatile）
    /// - 懒初始化，首次访问 .Instance 时创建
    /// - 提供 Destroy 方法用于手动清理
    ///
    /// 用法：
    /// <code>
    /// public class MyManager : EmberSingleton&lt;MyManager&gt;
    /// {
    ///     public void Init() { ... }
    ///     protected override void OnDestroy() { /* 清理逻辑 */ }
    /// }
    /// // 使用
    /// MyManager.Instance.Init();
    /// </code>
    /// </summary>
    /// <typeparam name="T">单例类型（自身）</typeparam>
    public abstract class EmberSingleton<T> : IEmberSingletonLifecycle where T : class, new()
    {
        private const string TAG = LogTags.CoreSingleton;

        private static volatile T _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例。首次访问时自动创建。
        /// 线程安全。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 检查单例是否已创建（不会触发创建）。
        /// </summary>
        public static bool IsValid => _instance != null;

        /// <summary>
        /// 销毁单例实例。销毁前会调用 <see cref="OnDestroy"/> 钩子。
        /// </summary>
        public static void Destroy()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    if (_instance is IEmberSingletonLifecycle lifecycle)
                    {
                        lifecycle.InvokeOnDestroy();
                    }
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// 实例被销毁时的回调。子类可重写以做清理。
        /// </summary>
        protected virtual void OnDestroy() { }

        /// <summary>
        /// 显式接口实现，供 Destroy() 方法内部调用。
        /// </summary>
        void IEmberSingletonLifecycle.InvokeOnDestroy()
        {
            OnDestroy();
        }
    }

    // ============================================================
    // MonoBehaviour 版本（无 DontDestroyOnLoad）
    // ============================================================

    /// <summary>
    /// MonoBehaviour 单例基类 —— <b>不含 DontDestroyOnLoad</b>。
    ///
    /// 用于挂载到场景 GameObject 上的组件单例。持久化由场景本身保证
    /// （场景永不卸载即等价于 DDOL）。
    ///
    /// 特性：
    /// - 线程安全（双检锁）
    /// - 懒初始化：.Instance 首次访问时，找不到场景对象则自动创建
    /// - 自动检测并销毁重复实例
    ///
    /// 如需 DontDestroyOnLoad，使用 <see cref="EmberMonoSingletonDontDestroy{T}"/>。
    /// </summary>
    public abstract class EmberMonoSingleton<T> : MonoBehaviour where T : EmberMonoSingleton<T>
    {
        private const string TAG = LogTags.CoreSingleton;

        private static volatile T _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例。
        /// - 若已存在则直接返回
        /// - 若场景中有，自动找到并缓存
        /// - 若不存在，自动创建新 GameObject（不挂 DontDestroyOnLoad）
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindAnyObjectByType<T>();

                            if (_instance == null)
                            {
                                GameObject go = new GameObject($"[Ember] {typeof(T).Name}");
                                _instance = go.AddComponent<T>();
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 检查单例是否有效（不会触发创建）。
        /// </summary>
        public static bool IsValid => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                EmberDebug.LogWarning(TAG,
                    $"[Ember] Duplicate instance of {typeof(T).Name} detected. " +
                    $"Destroying the duplicate on '{gameObject.name}'.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnSingletonDestroy();
                _instance = null;
            }
        }

        /// <summary>
        /// 替代 Awake 的单例初始化钩子。仅在首次创建时调用一次。
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 替代 OnDestroy 的单例清理钩子。
        /// </summary>
        protected virtual void OnSingletonDestroy() { }
    }

    // ============================================================
    // MonoBehaviour 版本（含 DontDestroyOnLoad）
    // ============================================================

    /// <summary>
    /// MonoBehaviour 单例基类 —— <b>含 DontDestroyOnLoad</b>。
    ///
    /// 继承自 <see cref="EmberMonoSingleton{T}"/>，在 Awake 时自动标记
    /// DontDestroyOnLoad。用于需要在场景切换时保留的对象。
    ///
    /// ⚠ Unity 6 已知问题：DDOL 对象树在 Editor 退出 Play Mode 时被递归销毁，
    /// 可能触发 Hierarchy 窗口竞态（GameObjectTreeViewDataSource 索引越界）。
    /// 优先使用多场景叠加（永不卸载的场景）替代 DDOL。
    /// </summary>
    public abstract class EmberMonoSingletonDontDestroy<T> : EmberMonoSingleton<T>
        where T : EmberMonoSingletonDontDestroy<T>
    {
        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }
    }
}
