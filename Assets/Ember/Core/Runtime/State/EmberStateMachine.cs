using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core
{
    // ============================================================
    // GameState 基类
    // ============================================================

    /// <summary>
    /// 游戏状态基类 —— 所有游戏状态的抽象。
    ///
    /// 每个状态有四个生命周期钩子：
    /// - <see cref="OnEnter"/>：进入状态时调用，接收上一个状态传来的参数
    /// - <see cref="OnExit"/>：离开状态时调用
    /// - <see cref="OnUpdate"/>：每帧调用（如果状态机启用了 Update 驱动）
    /// - <see cref="OnPause"/> / <see cref="OnResume"/>：被其他状态覆盖/重新可见
    ///
    /// 子类只需要 override 关心的钩子，其余保持空实现。
    ///
    /// 用法：
    /// <code>
    /// public class BattleState : EmberGameState
    /// {
    ///     public override void OnEnter(object args) { /* 加载战斗场景 */ }
    ///     public override void OnExit() { /* 清理 */ }
    ///     public override void OnUpdate() { /* 每帧逻辑 */ }
    /// }
    /// </code>
    /// </summary>
    public abstract class EmberGameState
    {
        /// <summary>状态名称（自动从类名推断，可 override）</summary>
        public virtual string Name => GetType().Name;

        /// <summary>状态描述（图形化编辑器中展示）</summary>
        public virtual string Description => string.Empty;

        /// <summary>是否为系统必需状态（不可删除）</summary>
        public virtual bool IsRequired => false;

        /// <summary>是否允许主动切换到自身</summary>
        public virtual bool AllowReEnter => false;

        // ---- 生命周期 ----

        /// <summary>进入状态。args 为 TransitionTo 传入的参数。</summary>
        public virtual void OnEnter(object args) { }

        /// <summary>离开状态。</summary>
        public virtual void OnExit() { }

        /// <summary>每帧调用（需在 StateMachine 中启用）。</summary>
        public virtual void OnUpdate() { }

        /// <summary>被其他状态覆盖时调用。</summary>
        public virtual void OnPause() { }

        /// <summary>覆盖它的状态退出后，此状态重新可见时调用。</summary>
        public virtual void OnResume() { }
    }

    // ============================================================
    // 状态机
    // ============================================================

    /// <summary>
    /// 游戏状态机 —— 管理全局游戏状态的切换。
    ///
    /// 参考 burner 的 <c>GameStateManager</c>。
    ///
    /// 特性：
    /// - 泛型 + 类型安全（TransitionTo&lt;T&gt;()）
    /// - 必需状态保护（IsRequired = true 的状态不可移除）
    /// - 栈式覆盖（Push / Pop）：弹窗式状态切换，不销毁底层状态
    /// - 切换事件广播（EmberBroadcastEvent.GameStateChanged）
    /// - 为图形化编辑器预留：反射枚举所有状态、动态注册/注销
    ///
    /// 用法：
    /// <code>
    /// var fsm = new EmberStateMachine();
    /// fsm.Register(new InitState());
    /// fsm.Register(new LoginState());
    /// fsm.Register(new MainMenuState());
    /// fsm.Register(new BattleState());
    ///
    /// fsm.Start&lt;InitState&gt;();
    ///
    /// // 登录完成后切换
    /// fsm.TransitionTo&lt;MainMenuState&gt;(args: null);
    ///
    /// // 弹窗式覆盖（暂停 MainMenu，进入 Settings，退出 Settings 后恢复 MainMenu）
    /// fsm.Push&lt;SettingsState&gt;(args: null);
    /// fsm.Pop();
    /// </code>
    /// </summary>
    public class EmberStateMachine
    {
        private const string TAG = LogTags.CoreStateMachine;

        #region 参数

        private readonly Dictionary<Type, EmberGameState> _states = new();
        private EmberGameState _current;
        private readonly Stack<EmberGameState> _overlayStack = new();

        /// <summary>当前活跃状态</summary>
        public EmberGameState Current => _current;

        /// <summary>上一个状态</summary>
        public EmberGameState Previous { get; private set; }

        /// <summary>状态变更事件（oldState, newState）</summary>
        public event Action<EmberGameState, EmberGameState> OnStateChanged;

        /// <summary>所有已注册的状态</summary>
        public IReadOnlyCollection<EmberGameState> RegisteredStates => _states.Values;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 注册 & 初始化 ========

        /// <summary>
        /// 注册一个状态。如果已经注册同类型状态则忽略。
        /// </summary>
        public void Register(EmberGameState state)
        {
            if (state == null) return;

            var type = state.GetType();
            if (_states.ContainsKey(type))
            {
                EmberDebug.LogWarning(TAG, $"StateMachine: state '{type.Name}' is already registered.");
                return;
            }

            _states[type] = state;
        }

        /// <summary>
        /// 注销一个状态。如果状态是必需状态（IsRequired = true）则拒绝。
        /// </summary>
        /// <returns>是否成功注销</returns>
        public bool Unregister<T>() where T : EmberGameState
        {
            var type = typeof(T);

            if (_states.TryGetValue(type, out var state) && state.IsRequired)
            {
                EmberDebug.LogError(TAG, 
                    $"StateMachine: cannot unregister required state '{type.Name}'.");
                return false;
            }

            if (_current != null && _current.GetType() == type)
            {
                EmberDebug.LogError(TAG, 
                    $"StateMachine: cannot unregister the current active state '{type.Name}'.");
                return false;
            }

            return _states.Remove(type);
        }

        /// <summary>
        /// 启动状态机，直接进入指定状态（不走 TransitionTo 的 Exit/Enter 流程）。
        /// 通常在游戏启动时调用一次。
        /// </summary>
        public void Start<T>(object args = null) where T : EmberGameState
        {
            var type = typeof(T);
            if (!_states.TryGetValue(type, out var state))
            {
                EmberDebug.LogError(TAG, $"StateMachine: state '{type.Name}' is not registered.");
                return;
            }

            _current = state;
            _current.OnEnter(args);
            EmberEventBus.OnNext(EmberBroadcastEvent.GameStateChanged);
            OnStateChanged?.Invoke(null, _current);
        }

        // ======== 切换 ========

        /// <summary>
        /// 切换到目标状态：Exit 当前状态 → Enter 目标状态。
        /// 如果目标状态的 <see cref="EmberGameState.AllowReEnter"/> 为 false 且
        /// 当前已是该状态，则忽略。
        /// </summary>
        public void TransitionTo<T>(object args = null) where T : EmberGameState
        {
            var type = typeof(T);
            if (!_states.TryGetValue(type, out var next))
            {
                EmberDebug.LogError(TAG, $"StateMachine: state '{type.Name}' is not registered.");
                return;
            }

            if (_current != null)
            {
                if (_current.GetType() == type && !_current.AllowReEnter)
                {
                    EmberDebug.LogWarning(TAG, 
                        $"StateMachine: already in state '{type.Name}' and AllowReEnter is false.");
                    return;
                }

                _current.OnExit();
            }

            Previous = _current;
            _current = next;
            _current.OnEnter(args);

            EmberEventBus.OnNext(EmberBroadcastEvent.GameStateChanged);
            OnStateChanged?.Invoke(Previous, _current);
        }

        // ======== 栈式覆盖（Push / Pop） ========

        /// <summary>
        /// 将当前状态暂停（OnPause），在其上方覆盖一个新状态。
        /// 适用于弹窗式场景：设置面板、背包界面等不改变游戏主循环但需要暂时接管输入的界面。
        /// </summary>
        public void Push<T>(object args = null) where T : EmberGameState
        {
            var type = typeof(T);
            if (!_states.TryGetValue(type, out var overlay))
            {
                EmberDebug.LogError(TAG, $"StateMachine: state '{type.Name}' is not registered.");
                return;
            }

            _current?.OnPause();
            _overlayStack.Push(_current);

            _current = overlay;
            _current.OnEnter(args);

            EmberEventBus.OnNext(EmberBroadcastEvent.GameStateChanged);
            OnStateChanged?.Invoke(Previous, _current);
        }

        /// <summary>
        /// 弹出最上层的覆盖状态，恢复到之前被暂停的状态。
        /// </summary>
        public void Pop()
        {
            if (_overlayStack.Count == 0)
            {
                EmberDebug.LogWarning(TAG, "StateMachine: no overlay state to pop.");
                return;
            }

            _current?.OnExit();
            _current = _overlayStack.Pop();
            _current?.OnResume();

            EmberEventBus.OnNext(EmberBroadcastEvent.GameStateChanged);
            OnStateChanged?.Invoke(Previous, _current);
        }

        // ======== 查询 ========

        /// <summary>
        /// 当前是否处于指定状态。
        /// </summary>
        public bool Is<T>() where T : EmberGameState
        {
            return _current != null && _current.GetType() == typeof(T);
        }

        /// <summary>
        /// 获取已注册的状态实例。
        /// </summary>
        public T GetState<T>() where T : EmberGameState
        {
            return _states.TryGetValue(typeof(T), out var state) ? state as T : null;
        }

        /// <summary>
        /// 检查所有必需状态（IsRequired = true）是否都已注册。
        /// </summary>
        public bool ValidateRequiredStates()
        {
            foreach (var kvp in _states)
            {
                if (kvp.Value.IsRequired) return true;
            }

            EmberDebug.LogError(TAG, "StateMachine: no required state registered. At minimum, register an InitState.");
            return false;
        }

        #endregion
    }
}
