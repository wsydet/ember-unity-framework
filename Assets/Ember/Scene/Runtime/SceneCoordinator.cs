using System;

using Cysharp.Threading.Tasks;

using Ember.Core;
using Ember.Basic;

namespace Ember.Scene
{
    /// <summary>
    /// 状态机 ↔ 场景管理器桥接器。
    ///
    /// 因为 <c>Ember.Core.Runtime</c> 不能引用 <c>Ember.Scene.Runtime</c>（依赖方向限制），
    /// 所以把桥接代码放在 Scene 程序集中（它已经引用了 Core）。
    ///
    /// 实现 <see cref="IEmberManager"/>，在 Init 阶段自动注入
    /// <see cref="EmberStateMachine.OnSceneTransition"/> 钩子。
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Scene)]
    public class SceneCoordinator : EmberSingleton<SceneCoordinator>, IEmberManager
    {
        private const string TAG = LogTags.SceneManager;

        #region IEmberManager

        void IEmberManager.Init()
        {
            var fsm = GameLauncher.Instance.Fsm;
            fsm.OnSceneTransition = HandleSceneTransition;
            fsm.LoadSceneAsync = (sceneName, onComplete) =>
                EmberSceneManager.Instance.LoadSceneAsync(sceneName, onComplete);
            EmberDebug.LogInit(TAG, "SceneCoordinator: hooked onto FSM.");
        }

        void IEmberManager.Destroy()
        {
            if (GameLauncher.Instance != null)
            {
                GameLauncher.Instance.Fsm.OnSceneTransition = null;
                GameLauncher.Instance.Fsm.LoadSceneAsync = null;
            }
        }

        #endregion

        // ============================================================

        #region 内部方法

        private void HandleSceneTransition(SceneTransitionContext ctx)
        {
            switch (ctx.Type)
            {
                case TransitionType.TransitionTo:
                    HandleTransitionTo(ctx);
                    break;
                case TransitionType.Push:
                    HandlePush(ctx);
                    break;
                case TransitionType.Pop:
                    HandlePop(ctx);
                    break;
            }
        }

        /// <summary>
        /// TransitionTo 场景加载的拦截器。
        /// 参数：(sceneName, fromScene, onLoaded)。返回 true 表示已拦截，不再走默认加载。
        /// onLoaded 为「进入目标状态前的准备 + Proceed」的异步委托（Func《UniTask》），UI 层应 await 它，
        /// 保证 Loading 页渐出前目标状态的底层内容（如背景页）已就绪。
        /// UI 层在此注入 Loading 页面逻辑。
        /// </summary>
        public static Func<string, string, Func<UniTask>, bool> InterceptSceneLoad;

        /// TransitionTo：加载新场景 → Proceed → 卸载旧场景。
        /// </summary>
        private void HandleTransitionTo(SceneTransitionContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.ToScene))
            {
                ProceedWithPrepareAsync(ctx).Forget();
                return;
            }

            // 检查 UI 层是否拦截（带 Loading 页面）
            if (InterceptSceneLoad != null && InterceptSceneLoad(ctx.ToScene, ctx.FromScene,
                () => ProceedWithPrepareAsync(ctx)))
            {
                return;
            }

            // 默认：直接加载
            EmberSceneManager.Instance.LoadSceneAsync(ctx.ToScene,
                () => ProceedWithPrepareAsync(ctx).Forget());
        }

        /// <summary>
        /// 进入目标状态前先 await 其 <see cref="EmberGameState.PrepareEnterAsync"/>（如 MainState 加载背景页），
        /// 再 Proceed（OnEnter）并卸载旧场景。
        /// </summary>
        private async UniTask ProceedWithPrepareAsync(SceneTransitionContext ctx)
        {
            await ctx.ToState.PrepareEnterAsync();
            ctx.Proceed();
            UnloadIfDifferent(ctx.FromScene, ctx.ToScene);
        }

        /// <summary>
        /// Push：加载覆盖场景（如有）→ Proceed。底层场景保留。
        /// </summary>
        private void HandlePush(SceneTransitionContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.ToScene))
            {
                ctx.Proceed();
                return;
            }

            EmberSceneManager.Instance.LoadSceneAsync(ctx.ToScene, ctx.Proceed);
        }

        /// <summary>
        /// Pop：Proceed（恢复底层状态）→ 卸载覆盖场景。
        /// </summary>
        private void HandlePop(SceneTransitionContext ctx)
        {
            ctx.Proceed();
            UnloadIfDifferent(ctx.FromScene, ctx.ToScene);
        }

        /// <summary>如果两个场景路径不同且旧场景非空，则卸载旧场景。</summary>
        private static void UnloadIfDifferent(string fromScene, string toScene)
        {
            if (!string.IsNullOrEmpty(fromScene) && fromScene != toScene)
            {
                EmberSceneManager.Instance.UnloadSceneAsync(fromScene);
            }
        }

        #endregion
    }
}
