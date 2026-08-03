using Ember.Core;

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
        /// TransitionTo：加载新场景 → Proceed → 卸载旧场景。
        /// </summary>
        private void HandleTransitionTo(SceneTransitionContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.ToScene))
            {
                // 目标无场景 → 直接执行生命周期，然后卸载旧场景
                ctx.Proceed();
                UnloadIfDifferent(ctx.FromScene, ctx.ToScene);
                return;
            }

            // 加载新场景 → Proceed → 卸载旧场景
            EmberSceneManager.Instance.LoadSceneAsync(ctx.ToScene, () =>
            {
                ctx.Proceed();
                UnloadIfDifferent(ctx.FromScene, ctx.ToScene);
            });
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
