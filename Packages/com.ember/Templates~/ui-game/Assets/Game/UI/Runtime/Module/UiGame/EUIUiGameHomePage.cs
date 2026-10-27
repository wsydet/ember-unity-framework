using Ember.Core;
namespace Game.UI
{
    public partial class EUIUiGameHomePage
    {
        #region 内部参数
        private bool _open;
        private bool CanNavigate => _open && GameLauncher.Instance.Fsm.Is<MainState>() && !GameLauncher.Instance.Fsm.IsTransitioning;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Start.onClick.AddListener(StartSession); Settings.onClick.AddListener(OpenSettings); }
        public override void OnOpen(object param) { base.OnOpen(param); _open = true; }
        public override void OnClose() { _open = false; base.OnClose(); }
        public override void OnDispose()
        {
            _open = false;
            Start.onClick.RemoveListener(StartSession);
            Settings.onClick.RemoveListener(OpenSettings);
            base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void StartSession() { if (CanNavigate) GameLauncher.Instance.Fsm.RequestTransitionTo<GameplayState>(); }
        private void OpenSettings() { if (CanNavigate) GameLauncher.Instance.Fsm.RequestPush<SettingsState>(SettingsContext.Main); }
        #endregion
    }
}
