using System.Collections.Generic;
using Ember.Core;
using Ember.UI;
using Ember.UIExtension;
using Game.UiGame;
using UnityEngine;

namespace Game.UI
{
    public partial class EUIUiGamePlayPage
    {
        #region 内部参数
        private UiGameSession _session;
        private EUIItemList<EUIOptionRowData> _rows;
        private readonly List<EUIOptionRowData> _data = new();
        private int _lastInputFrame = -1;
        private bool _open;
        public EUIItemList<EUIOptionRowData> Rows => _rows;
        public UiGameSession Session => _session;
        private bool CanNavigate => _open && UiGameModule.Instance.IsCurrent(_session)
            && !_session.IsPaused && !_session.IsNavigating
            && GameLauncher.Instance.Fsm.Is<GameplayState>() && !GameLauncher.Instance.Fsm.IsTransitioning;
        #endregion
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();
            Advance.onClick.AddListener(AdvanceStep);
            Settings.onClick.AddListener(OpenSettings);
            Home.onClick.AddListener(ReturnHome);
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param);
            Release();
            _session = param as UiGameSession;
            if (!UiGameModule.Instance.IsCurrent(_session)) return;
            _open = true;
            _lastInputFrame = -1;
            _rows = Steps.Create<EUIOptionRowData>(data => data.Id,
                lease => ((EUIDynamicOptionRowItem)lease.Item.Logic).Bind(lease, null));
            _session.Changed += Refresh;
            Refresh();
        }
        public override void OnHide() { _rows?.Hide(); base.OnHide(); }
        public override void OnShow() { base.OnShow(); _rows?.Show(); }
        public override void OnClose() { Release(); base.OnClose(); }
        public override void OnDispose()
        {
            Release();
            Advance.onClick.RemoveListener(AdvanceStep);
            Settings.onClick.RemoveListener(OpenSettings);
            Home.onClick.RemoveListener(ReturnHome);
            base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void AdvanceStep()
        {
            if (!CanNavigate || _lastInputFrame == Time.frameCount) return;
            _lastInputFrame = Time.frameCount;
            var session = _session;
            if (!session.TryAdvance(session.CompletedSteps) || !session.IsComplete) return;
            _open = false;
            EUIManager.Instance.ClosePageByDef(GamePages.EUIUiGamePlayPage);
            EUIManager.Instance.ShowMainPage(GamePages.EUIUiGameResultPage, session,
                isCurrent: () => UiGameModule.Instance.IsCurrent(session) && session.IsComplete && !session.IsNavigating);
        }
        private void OpenSettings() { if (CanNavigate) GameLauncher.Instance.Fsm.RequestPush<SettingsState>(SettingsContext.Gameplay); }
        private void ReturnHome() { if (CanNavigate) UiGameNavigation.ReturnHome(_session); }
        private void Release()
        {
            _open = false;
            if (_session != null) _session.Changed -= Refresh;
            _rows?.Dispose();
            _data.Clear();
            _session = null;
        }
        private void Refresh()
        {
            if (!_open || !UiGameModule.Instance.IsCurrent(_session)) return;
            Status.SetSource((_session.IsPaused ? "Paused" : _session.IsNavigating ? "Leaving" : "Progress") + " · " + _session.CompletedSteps + " / 3");
            Advance.interactable = _session.CanAdvance;
            _data.Clear();
            var icon = Steps.ItemTemplate.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite;
            for (var i = 0; i < UiGameSession.STEP_COUNT; i++)
            {
                var done = i < _session.CompletedSteps;
                _data.Add(new EUIOptionRowData { Id = "step-" + i, Title = "Step " + (i + 1) + (done ? " · Complete" : " · Ready"),
                    Icon = icon, Selected = done, Interactable = false, Progress = done ? 1 : 0 });
            }
            _rows.Replace(_data);
        }
        #endregion
    }
}
