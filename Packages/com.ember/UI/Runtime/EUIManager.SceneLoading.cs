using System;
using Cysharp.Threading.Tasks;
using Ember.Core;

namespace Ember.UI
{
    // 场景适配：业务阶段和展示凭证只在此关联，通用遮幕会话不依赖场景请求。
    public partial class EUIManager
    {
        #region 内部方法
        private async UniTask RunSceneTransitionWithLoadingAsync(SceneTransitionContext request, Func<UniTask> operation)
        {
            bool quick = EmberStateMachine.QuickSceneLoad;
            EmberStateMachine.QuickSceneLoad = false;
            if (_curtainSession != null)
            {
                if (_curtainSession.State != EUICurtainState.Covered || _curtainPage?.IsOpened != true)
                    throw new InvalidOperationException("Loading curtain is not accepting scene requests.");
                BindLoadingRequest(request);
                await operation();
                return;
            }
            if (DefaultLoadingPageDef == null || (request.Type != TransitionType.InPlace
                && (request.Type != TransitionType.TransitionTo || request.SkipSceneLoad)))
            {
                await operation();
                return;
            }
            await RunWithLoadingCoreAsync(DefaultLoadingPageDef, null, async () =>
            {
                BindLoadingRequest(request);
                var owner = _curtainSession;
                var logic = _curtainPage?.Logic;
                if (quick && logic != null) logic.SkipFakeProgress = true;
                await operation();
                if (logic != null)
                    await UniTask.WaitUntil(() => !_initialized || _curtainSession != owner || logic.IsTransitionReady,
                        cancellationToken: request.CommitStarted ? default : request.Cancellation);
            }, request.Cancellation, () => request.CommitStarted, new EUICurtainOptions { SkipFakeProgress = quick });
        }

        private void BindLoadingRequest(SceneTransitionContext request)
        {
            var presentation = request.HoldPresentation();
            _curtainSession.Released += succeeded =>
            {
                if (succeeded) presentation.Complete();
                else presentation.Fail("Scene presentation owner released before successful reveal.");
            };
            if (_curtainPage?.Logic != null)
                _curtainPage.Logic.TransitionOperation = request.Operation;
        }

        #endregion
    }
}
