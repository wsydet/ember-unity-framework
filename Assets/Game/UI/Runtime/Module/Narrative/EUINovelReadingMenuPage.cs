using System;
using Cysharp.Threading.Tasks;
using Ember.UI;
using Ember.Core;
using Game.Narrative;

namespace Game.UI
{
    public partial class EUINovelReadingMenuPage
    {
        private IDisposable _pause;
        private NovelSession _session;
        private Game.NovelSave.NovelSaveModule _save;
        private bool _leaving;
        private void ClosePage() => EUIManager.Instance.ClosePage(Page);
        private void Leave(Action action)
        {
            if (_leaving || _session == null || _session.IsDisposed) return;
            _leaving = true; NovelReadingUI.AfterClose(Page, _session, action).Forget();
        }
        private void OpenSaves() => Leave(NovelSaveUI.Open);
        private void SaveQuick() => _save?.Save(6);
        private void LoadQuick() => Leave(() => NovelSaveUI.Load(6, _ => NovelSaveUI.Open()));
        private void OpenSettings() => Leave(() => GameLauncher.Instance.Fsm.TransitionTo<SettingsState>(SettingsContext.Gameplay));
        private void ReturnMain() => Leave(() => GameLauncher.Instance.Fsm.TransitionTo<MainState>());
        private void StartSkip() { var session = _session; Leave(() => session.SetReadMode(session.ReadMode == NarrativeReadMode.Skip ? NarrativeReadMode.Manual : NarrativeReadMode.Skip)); }
        private void Refresh()
        {
            Feedback.text = _save?.Message ?? "";
            bool stable = _session != null && (_session.Snapshot.State == NarrativeState.AwaitingAdvance || _session.Snapshot.State == NarrativeState.AwaitingChoice);
            QuickSave.interactable = stable && _save?.IsBusy == false;
            QuickLoad.interactable = _save?.IsBusy == false;
            ReadSkip.interactable = _session != null && _session.Snapshot.HasActiveSession && _session.Snapshot.State != NarrativeState.AwaitingChoice;
        }
        public override void OnInit()
        {
            base.OnInit(); Close.onClick.AddListener(ClosePage); Saves.onClick.AddListener(OpenSaves);
            QuickSave.onClick.AddListener(SaveQuick); QuickLoad.onClick.AddListener(LoadQuick);
            Settings.onClick.AddListener(OpenSettings); ReadSkip.onClick.AddListener(StartSkip); ReturnMenu.onClick.AddListener(ReturnMain);
            NovelSkin.Apply(this, "EUINovelReadingMenuPage");
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param); _leaving = false; _session = param as NovelSession;
            if (_session == null || _session.IsDisposed) { ClosePage(); return; }
            _pause = _session.AcquirePause("ReadingMenu"); _save = NovelSaveUI.Module;
            if (_save != null) _save.Changed += Refresh; Refresh();
        }
        public override void OnClose()
        {
            if (_save != null) _save.Changed -= Refresh; _save = null; _session = null;
            _pause?.Dispose(); _pause = null; base.OnClose();
        }
        public override void OnDispose()
        {
            OnClose(); Close.onClick.RemoveListener(ClosePage); Saves.onClick.RemoveListener(OpenSaves);
            QuickSave.onClick.RemoveListener(SaveQuick); QuickLoad.onClick.RemoveListener(LoadQuick);
            Settings.onClick.RemoveListener(OpenSettings); ReadSkip.onClick.RemoveListener(StartSkip); ReturnMenu.onClick.RemoveListener(ReturnMain); base.OnDispose();
        }
    }
}
