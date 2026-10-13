using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Basic;

namespace Ember.Scene
{
    /// <summary>可等待的流程/场景桥接；UI 只包裹遮罩，场景所有权始终由 Runner 管理。</summary>
    [EmberInitOrder(EmberInitOrderAttribute.Scene)]
    public class SceneCoordinator : EmberSingleton<SceneCoordinator>, IEmberManager
    {
        #region 内部参数
        private EmberSceneTransitionRunner _runner;
        private EmberStateMachine _fsm;
        /// <summary>UI 包裹整个操作，必须等待 operation；不得自行加载或卸载场景。</summary>
        public static Func<SceneTransitionContext, Func<UniTask>, UniTask> WrapTransitionAsync;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private UniTask HandleSceneTransitionAsync(SceneTransitionContext request)
        {
            // Even a quick/skip/overlay request may share an outer curtain.
            if (WrapTransitionAsync != null)
                return WrapTransitionAsync(request, () => _runner.RunAsync(request));
            return _runner.RunAsync(request);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        void IEmberManager.Init()
        {
            _fsm = GameLauncher.Instance.Fsm;
            _runner = new EmberSceneTransitionRunner(EmberSceneManager.Instance);
            _fsm.OnSceneTransitionAsync = HandleSceneTransitionAsync;
            _fsm.LoadSceneAsync = (sceneName, onComplete) =>
                EmberSceneManager.Instance.LoadSceneAsync(sceneName, onComplete);
        }

        void IEmberManager.Destroy()
        {
            if (_fsm != null)
            {
                _fsm.StopTransitions();
                _fsm.OnSceneTransitionAsync = null;
                _fsm.LoadSceneAsync = null;
            }
            _fsm = null;
        }
        #endregion
    }
}
