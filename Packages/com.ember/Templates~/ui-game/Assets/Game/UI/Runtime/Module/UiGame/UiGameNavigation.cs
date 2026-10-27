using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.UiGame;
namespace Game.UI
{
    internal static class UiGameNavigation
    {
        #region 内部方法
        private static async UniTask ReturnAsync(UiGameSession session)
        {
            var result = await GameLauncher.Instance.Fsm.RequestTransitionTo<MainState>().Completion;
            await UniTask.SwitchToMainThread();
            if (result.Status == EmberTransitionStatus.Completed) return;
            if (UiGameModule.Instance.IsCurrent(session)) session.CancelNavigation();
            EmberDebug.LogWarning(LogTags.Game, "UI game return: " + result.Status + " " + result.Error);
        }
        #endregion
        #region 外部方法
        internal static void ReturnHome(UiGameSession session)
        {
            if (!UiGameModule.Instance.IsCurrent(session) || !session.TryBeginNavigation()) return;
            ReturnAsync(session).Forget();
        }
        #endregion
    }
}
