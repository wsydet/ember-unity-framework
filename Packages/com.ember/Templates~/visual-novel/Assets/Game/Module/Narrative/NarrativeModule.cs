using Ember.Basic;
using Ember.Core;
using System;
using UnityEngine;
using Ember.Input;
using UnityEngine.InputSystem;
using Ember.UI;

namespace Game.Narrative
{
    /// <summary>
    /// 小说 Gameplay 会话所有者，驱动正式阅读页面、资源等待及输入门控。
    /// 禁用时编辑器入口也随脚本重载隐藏，不创建模块单例来判断启用状态。
    /// </summary>
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class NarrativeModule : EmberSingleton<NarrativeModule>, IEmberModule, IEmberUpdate
    {
        #region 内部参数
        private IDisposable _observation;
        private bool _active;
        private string _previousMap;
        public NovelSession Session { get; private set; }
        public bool PreparingEntryUnderCover { get; set; }
        public event Action MenuRequested;
        public event Action HistoryRequested;
        public event Action QuickSaveRequested;
        public event Action QuickLoadRequested;
        public event Action<NovelSession> SessionChanged;
        public event Action<string> FatalRestoreError;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void OnInit() { _active = true; }
        public void Begin(NovelNewGameRequest request, Func<NarrativeTableCatalog> catalog)
        {
            if (!_active) throw new InvalidOperationException("Narrative Gameplay 模块未激活");
            EndSession();
            Session = new NovelSession(request, catalog, new NovelResources());
            ConfigureSession();
        }
        public void BeginPrepared(NovelSession candidate)
        {
            if (!_active || candidate == null || !candidate.RestoreReady) throw new InvalidOperationException("候选会话尚未准备完成");
            EndSession(); Session = candidate; ConfigureSession();
        }
        private void ConfigureSession()
        {
            RefreshObservation();
            var input = EmberInputManager.Instance;
            if (!input.IsInitialized)
            {
                var actions = Resources.Load<InputActionAsset>("Config/Narrative/NovelInput");
                input.Init(actions, "UI");
            }
            _previousMap = input.CurrentMap;
            input.SwitchMap("Novel");
        }
        public void RefreshObservation()
        {
            _observation?.Dispose(); _observation = Session == null ? null : NarrativeObservation.Register(Session);
        }
        public void CommitCandidate(NovelSession candidate)
        {
            var view = Session?.View;
            if (view == null || candidate?.RestoreReady != true) throw new InvalidOperationException("阅读页面不可用，尚未提交");
            var old = Session; Session = candidate;
            try
            {
                old.Dispose(); // explicit irreversible commit boundary
                RefreshObservation(); SessionChanged?.Invoke(candidate); candidate.CommitRestore(view);
            }
            catch
            { EndSession(); MenuRequested?.Invoke(); throw; }
        }
        public void Update()
        {
            if (Session == null) return;
            bool loading = false;
            foreach (var page in EUIViewEngine.Instance.ActivePages)
                if (page.EUIPageDef == EUIManager.DefaultLoadingPageDef && page.GameObject && page.GameObject.activeInHierarchy)
                { loading = true; break; }
            if (loading && !PreparingEntryUnderCover) Session.Pause("SceneLoading"); else Session.Resume("SceneLoading");
            var input = EmberInputManager.Instance;
            if (!loading)
            {
            if (input.IsPressed("Novel/Advance")) Session.Advance(Time.frameCount);
            if (input.IsPressed("Novel/HideUI")) Session.SetDialogueHidden(!Session.DialogueHidden);
            if (Session.Snapshot.PauseReasons.Count == 0)
            {
                if (input.IsPressed("Novel/AutoToggle")) Session.SetReadMode(Session.ReadMode == NarrativeReadMode.Auto ? NarrativeReadMode.Manual : NarrativeReadMode.Auto);
                if (input.IsPressed("Novel/SkipHeld")) Session.SetReadMode(NarrativeReadMode.Skip);
                if (input.IsPressed("Novel/History")) HistoryRequested?.Invoke();
                if (input.IsPressed("Novel/QuickSave")) QuickSaveRequested?.Invoke();
                if (input.IsPressed("Novel/QuickLoad")) QuickLoadRequested?.Invoke();
            }
            if (input.GetAction("Novel/SkipHeld")?.WasReleasedThisFrame() == true && Session.ReadMode == NarrativeReadMode.Skip)
                Session.SetReadMode(NarrativeReadMode.Manual);
            if (input.IsPressed("Novel/Menu") && Session.Snapshot.PauseReasons.Count == 0) MenuRequested?.Invoke();
            }
            Session?.Tick(Time.unscaledDeltaTime, Time.frameCount);
            if (Session?.RestoreReady == true && Session.Snapshot.State == NarrativeState.Faulted)
            {
                string message = Session.Snapshot.Error?.ToString();
                EndSession(); FatalRestoreError?.Invoke(message); MenuRequested?.Invoke();
            }
        }
        public void EndSession()
        {
            PreparingEntryUnderCover = false;
            var previous = Session; Session = null;
            previous?.Dispose(); _observation?.Dispose(); _observation = null;
            if (previous != null && EmberInputManager.TryGetInstance(out var input)) input.SwitchMap(_previousMap ?? "UI");
        }
        void IEmberModule.OnDestroy() { _active = false; EndSession(); MenuRequested = null; SessionChanged = null; FatalRestoreError = null; HistoryRequested = null; QuickSaveRequested = null; QuickLoadRequested = null; }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
        #endregion
    }
}
