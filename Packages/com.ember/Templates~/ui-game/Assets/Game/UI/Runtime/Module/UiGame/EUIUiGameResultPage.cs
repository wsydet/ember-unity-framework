using Ember.Core;
using Ember.UI;
using Game.UiGame;
namespace Game.UI
{
    public partial class EUIUiGameResultPage
    {
        #region 内部参数
        private UiGameSession _session;
        private bool _open;
        private bool CanNavigate => _open && UiGameModule.Instance.IsCurrent(_session)
            && GameLauncher.Instance.Fsm.Is<GameplayState>() && !GameLauncher.Instance.Fsm.IsTransitioning;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Restart.onClick.AddListener(RestartSession); Home.onClick.AddListener(ReturnHome); }
        public override void OnOpen(object param)
        {
            base.OnOpen(param);
            _session = param as UiGameSession;
            _open = UiGameModule.Instance.IsCurrent(_session) && _session.IsComplete;
            Summary.SetSource("All three steps are complete.\nReady for another session?");
        }
        public override void OnClose() { _open = false; _session = null; base.OnClose(); }
        public override void OnDispose()
        {
            _open = false; _session = null;
            Restart.onClick.RemoveListener(RestartSession);
            Home.onClick.RemoveListener(ReturnHome);
            base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void RestartSession()
        {
            if (!CanNavigate || !UiGameModule.Instance.TryRestart(_session)) return;
            _open = false;
            EUIManager.Instance.ClosePageByDef(GamePages.EUIUiGameResultPage);
            var session = UiGameModule.Instance.Session;
            EUIManager.Instance.ShowMainPage(GamePages.EUIUiGamePlayPage, session,
                isCurrent: () => UiGameModule.Instance.IsCurrent(session) && !session.IsNavigating);
        }
        private void ReturnHome() { if (CanNavigate) UiGameNavigation.ReturnHome(_session); }
        #endregion
    }
}
