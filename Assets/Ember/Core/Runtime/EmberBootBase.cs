using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 启动器抽象基类 —— 强制所有启动器显式声明全部生命周期意图。
    ///
    /// 设计参考了 burner 项目的 <c>BaseBoot</c>。核心思想：
    /// - MonoBehaviour 的生命周期方法是"可选覆盖"的——写了就调，不写不调
    /// - 在多人协作和长期维护中，这会造成信息丢失——读代码的人需要猜测"没写是忘了还是不需要"
    /// - 本类把所有生命周期标为 abstract，子类必须显式写出，哪怕是空方法
    ///
    /// 空方法也是信息 —— 比如 <c>OnBootFixedUpdate() { }</c> 告诉读代码的人：
    /// "这个启动器不依赖物理帧"，不需要去猜有没有遗漏。
    ///
    /// <b>继承体系：</b>
    /// <code>
    /// MonoBehaviour
    ///   └── EmberMonoSingleton&lt;T&gt;          ← Instance, singleton 注册/销毁逻辑
    ///         └── EmberBootBase&lt;T&gt;          ← 统一 9 个 OnBoot* abstract 钩子
    ///               └── GameLauncher         ← 显式实现全部生命周期
    /// </code>
    ///
    /// Awake / OnDestroy 由 <see cref="EmberMonoSingleton{T}"/> 内部处理单例注册，
    /// 再通过 virtual 钩子 → 本类 override → 转发为 <see cref="OnBootAwake"/> / <see cref="OnBootDestroy"/>。
    /// 子类只需要关心 9 个以 <c>OnBoot</c> 开头的 abstract 方法。
    /// </summary>
    /// <typeparam name="T">启动器自身类型（递归泛型约束，与 EmberMonoSingleton 一致）</typeparam>
    public abstract class EmberBootBase<T> : EmberMonoSingleton<T> where T : EmberMonoSingleton<T>
    {
        // ============================================================
        // Awake / OnDestroy（来自 EmberMonoSingleton 的 virtual 钩子）
        // ============================================================

        /// <summary>EmberMonoSingleton 在 Awake 中完成单例注册后调用此钩子。</summary>
        protected override void OnSingletonAwake()
        {
            OnBootAwake();
        }

        /// <summary>EmberMonoSingleton 在 OnDestroy 中调用此钩子（覆盖编辑器退出 Play Mode 和应用退出）。</summary>
        protected override void OnSingletonDestroy()
        {
            OnBootDestroy();
        }

        // ============================================================
        // Unity 消息 → 委托给 abstract 钩子（private 确保子类不会直接 override 消息）
        // ============================================================

        private void Start()
        {
            OnBootStart();
        }

        private void Update()
        {
            OnBootUpdate();
        }

        private void LateUpdate()
        {
            OnBootLateUpdate();
        }

        private void FixedUpdate()
        {
            OnBootFixedUpdate();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            OnBootApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnBootApplicationPause(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            OnBootApplicationQuit();
        }

        // ============================================================
        // 子类必须显式实现的 9 个生命周期钩子
        // ============================================================

        /// <summary>Awake：单例注册完成后调用。初始化日志、状态机等。</summary>
        protected abstract void OnBootAwake();

        /// <summary>Start：首帧 Update 之前调用一次。启动状态机。</summary>
        protected abstract void OnBootStart();

        /// <summary>Update：每帧调用，驱动框架帧更新。</summary>
        protected abstract void OnBootUpdate();

        /// <summary>LateUpdate：每帧 Update 之后调用。</summary>
        protected abstract void OnBootLateUpdate();

        /// <summary>FixedUpdate：物理帧更新。不需要时留空即可。</summary>
        protected abstract void OnBootFixedUpdate();

        /// <summary>OnDestroy：对象销毁时调用。清理框架资源、刷写日志。覆盖编辑器退出 Play Mode 和应用退出。</summary>
        protected abstract void OnBootDestroy();

        /// <summary>应用获得/失去焦点时调用。不需要时留空即可。</summary>
        protected abstract void OnBootApplicationFocus(bool hasFocus);

        /// <summary>应用暂停/恢复时调用。不需要时留空即可。</summary>
        protected abstract void OnBootApplicationPause(bool pauseStatus);

        /// <summary>应用退出时调用。通常与 OnBootDestroy 共用清理逻辑。</summary>
        protected abstract void OnBootApplicationQuit();
    }
}
