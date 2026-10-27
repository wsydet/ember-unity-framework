using Ember.Core;

namespace Game.UiGame
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class UiGameModule : EmberSingleton<UiGameModule>, IEmberModule
    {
        #region 内部参数
        private long _generation;
        private bool _active;
        public UiGameSession Session { get; private set; }
        public int CreatedSessions { get; private set; }
        public int ReleasedSessions { get; private set; }
        #endregion

        #region 内部方法
        private void Release()
        {
            if (Session == null) return;
            Session.Dispose();
            Session = null;
            ReleasedSessions++;
        }

        private void Create()
        {
            Session = new UiGameSession(++_generation);
            CreatedSessions++;
        }
        protected override void OnDestroy() { _active = false; Release(); base.OnDestroy(); }
        #endregion

        #region 外部方法
        public void OnInit() { Release(); _active = true; Create(); }
        void IEmberModule.OnDestroy() => OnDestroy();
        public void ResetModuleData() { Release(); if (_active) Create(); }
        public bool IsCurrent(UiGameSession session) => _active && ReferenceEquals(Session, session) && session != null && !session.IsDisposed;
        public bool TryRestart(UiGameSession expected)
        {
            if (!IsCurrent(expected) || !expected.IsComplete || !expected.TryBeginNavigation()) return false;
            Release();
            Create();
            return true;
        }
        public void SetPaused(bool paused) { if (_active) Session?.SetPaused(paused); }
        #endregion
    }
}
