using System;
using System.Collections.Generic;
using Ember.Core;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelReaderPage : INovelView, INovelOpacityView, INovelActorView
    {
        #region 内部参数
        private NovelSession _session;
        private NarrativeModule _module;
        private readonly List<EUIItem> _visuals = new();
        private readonly List<EUIItem> _choices = new();
        private long _choiceGeneration = long.MinValue, _choicePosition = -1;
        private string _text;
        private float _stageAlpha = 1;
        private readonly float[] _visualAlpha = { 1, 1, 1, 1 };
        private float _bodyFontSize;
        private IDisposable _languageSubscription;
        public int TextLength => Body.textInfo.characterCount;

        /// <summary>阅读页自带的运行期文案 Key：这些字不挂在 TMPEx 上，由代码按当前语言取。</summary>
        private const string ENDING_KEY = "ui.reader.Ending.Text";
        private const string CHOICE_PROMPT_KEY = "ui.reader.Choice.Prompt";
        private const string AUTO_ON_KEY = "ui.reader.Auto.On";
        private const string AUTO_OFF_KEY = "ui.reader.Auto.Off";
        private const string SPEED_KEY = "ui.reader.Speed.Label";
        private const string SKIP_STOP_KEY = "ui.reader.Skip.Stop";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>界面运行期文案：命中当前语言就用表项，未装配多语言时用传入的源语言原文。</summary>
        private static string Localized(string key, string fallback) => NovelLocalization.Runtime(key, fallback);

        /// <summary>
        /// 写按钮文案。挂了多语言 Key 的控件必须用 <c>SetSource</c> 接管（同时清掉 Key），
        /// 否则下一次切语言的 RefreshAll 会把它换回表里的静态文案。
        /// </summary>
        private static void ApplyLabel(UnityEngine.UI.Button button, string text)
        {
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (!label || label.text == text) return;
            if (label is TMPEx localized) localized.SetSource(text);
            else label.text = text;
        }

        /// <summary>
        /// 语言变更：选项文字是读定义时的快照，必须整批重建（清掉缓存代次，下一次渲染重建）；
        /// 当前句由会话按语言缓存键重新解析，这里推动它立刻重渲染一次，
        /// 所以设置弹窗盖着阅读页时也能当场看到新语言。
        /// </summary>
        private void OnLanguageChanged(string language)
        {
            _choiceGeneration = long.MinValue; _choicePosition = -1;
            _session?.RefreshLocalization();
        }
        private EUIItem CreateItem(GameObject root)
        {
            if (!EUIItemFactory.TryCreate(root, out var item, out string error)) throw new InvalidOperationException(error);
            item.Show(); return item;
        }
        private void Continue() => _session?.Advance(Time.frameCount);
        private void OpenSaves() => NovelSaveUI.Open();
        private void SaveQuick() => NovelSaveUI.Module?.Save(6);
        private void LoadQuick() => NovelSaveUI.Load(6, _ => NovelSaveUI.Open());
        private void ToggleAuto() => _session?.SetReadMode(_session.ReadMode == NarrativeReadMode.Auto ? NarrativeReadMode.Manual : NarrativeReadMode.Auto);
        private void ToggleSkip() => _session?.SetReadMode(_session.ReadMode == NarrativeReadMode.Skip ? NarrativeReadMode.Manual : NarrativeReadMode.Skip);
        private void ToggleDialogue() => _session?.SetDialogueHidden(!_session.DialogueHidden);
        private void OpenHistory()
        {
            if (_session == null || _session.IsDisposed || _session.Snapshot.PauseReasons.Count > 0) return;
            var session = _session; var pending = session.AcquirePause("HistoryOpening");
            EUIManager.Instance.ShowPopup(GamePages.EUINovelHistoryPage, session, page =>
            {
                pending.Dispose();
                if (session.IsDisposed && page != null) EUIManager.Instance.ClosePage(page);
            });
        }
        private void ReplaceSession(NovelSession session)
        {
            _session = session; _choiceGeneration = long.MinValue; _text = null;
            session.AttachView(this);
        }
        private void ReturnMenu() => NovelReadingUI.Open(GamePages.EUINovelReadingMenuPage, _session);
        private void OpenSettings() => NovelReadingUI.Open(GamePages.EUINovelFontPage, _session);
        private void RestoreHiddenUI() => _session?.SetDialogueHidden(false);
        private void CycleSpeed()
        {
            var save = NovelSaveUI.Module;
            if (_session == null || save?.Account == null) return;
            save.SaveReadingDisplay(Mathf.Clamp(save.Account.FontSize, 0, 2), _session.ReadingMultiplier % 3 + 1);
        }
        private void ClearChoices()
        {
            foreach (var item in _choices) { var go = item.GameObject; item.Dispose(); UnityEngine.Object.Destroy(go); }
            _choices.Clear(); _choicePosition = -1;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void OnInit()
        {
            base.OnInit(); CacheActorLayout(); _bodyFontSize = Body.fontSize; CacheTextLayout();
            Speed.onClick.AddListener(CycleSpeed); RestoreUI.onClick.AddListener(RestoreHiddenUI);
            _visuals.Add(CreateItem(Background.gameObject));
            _visuals.Add(CreateItem(Left.gameObject)); _visuals.Add(CreateItem(Center.gameObject)); _visuals.Add(CreateItem(Right.gameObject));
            InitScreenEffects();
            Advance.onClick.AddListener(Continue); Menu.onClick.AddListener(ReturnMenu); Settings.onClick.AddListener(OpenSettings);
            Saves.onClick.AddListener(OpenSaves); QuickSave.onClick.AddListener(SaveQuick); QuickLoad.onClick.AddListener(LoadQuick);
            Auto.onClick.AddListener(ToggleAuto); Skip.onClick.AddListener(ToggleSkip); History.onClick.AddListener(OpenHistory); HideDialogue.onClick.AddListener(ToggleDialogue);
            NovelSkin.Apply(this, "EUINovelReaderPage");
            // 正文、选项、状态行都不挂在 TMPEx 的 Key 上，所以这里自己订阅全局语言变更。
            _languageSubscription = EmberEventBus.Subscribe<string>(EmberBroadcastEvent.LanguageChanged, OnLanguageChanged);
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param);
            _session = param as NovelSession;
            if (_session == null || _session.IsDisposed) return;
            if (EmberModuleCollector.Instance.TryGetModule(out _module))
            {
                _module.SessionChanged += ReplaceSession; _module.HistoryRequested += OpenHistory;
                _module.QuickSaveRequested += SaveQuick; _module.QuickLoadRequested += LoadQuick;
            }
            _session.AttachView(this);
        }
        public override void OnPause() { base.OnPause(); _session?.Pause("ReaderModal"); }
        public override void OnResume() { base.OnResume(); _session?.Resume("ReaderModal"); }
        public override void OnHide() { _session?.Pause("ReaderHidden"); base.OnHide(); }
        public override void OnShow() { base.OnShow(); _session?.Resume("ReaderHidden"); }
        public override void OnClose()
        {
            if (_module != null)
            {
                _module.SessionChanged -= ReplaceSession; _module.HistoryRequested -= OpenHistory;
                _module.QuickSaveRequested -= SaveQuick; _module.QuickLoadRequested -= LoadQuick;
            }
            _module = null;
            _session?.DetachView(this); _session = null; ClearChoices(); base.OnClose();
        }
        public override void OnDispose()
        {
            OnClose();
            _languageSubscription?.Dispose(); _languageSubscription = null;
            Speed.onClick.RemoveListener(CycleSpeed); RestoreUI.onClick.RemoveListener(RestoreHiddenUI);
            Advance.onClick.RemoveListener(Continue); Menu.onClick.RemoveListener(ReturnMenu); Settings.onClick.RemoveListener(OpenSettings);
            Saves.onClick.RemoveListener(OpenSaves); QuickSave.onClick.RemoveListener(SaveQuick); QuickLoad.onClick.RemoveListener(LoadQuick);
            Auto.onClick.RemoveListener(ToggleAuto); Skip.onClick.RemoveListener(ToggleSkip); History.onClick.RemoveListener(OpenHistory); HideDialogue.onClick.RemoveListener(ToggleDialogue);
            DisposeScreenEffects();
            foreach (var item in _visuals) item.Dispose(); _visuals.Clear();
            base.OnDispose();
        }
        public void Render(NarrativeSnapshot snapshot, NovelCommand command, string speaker, int visibleCharacters, string status)
        {
            bool say = command?.Kind == NovelCommandKind.Say &&
                (snapshot.State == NarrativeState.Revealing || snapshot.State == NarrativeState.AwaitingAdvance);
            string text = say ? command.Text
                : snapshot.State == NarrativeState.Ended ? Localized(ENDING_KEY, "故事暂告一段落。感谢阅读。")
                : snapshot.State == NarrativeState.AwaitingChoice ? Localized(CHOICE_PROMPT_KEY, "你会如何选择？")
                : status;
            if (say)
            {
                PrepareText(command, _textPresentationCommand == command ? _renderTextStart : 0);
                ShowText(speaker, visibleCharacters);
            }
            else
            {
                bool visible = _storyDialogueVisible; ResetTextLayout(); _storyDialogueVisible = visible; Body.fontSize = _bodyFontSize * NovelReadingUI.FontScale;
                if (_text != text) { _text = text; Body.text = text; Body.maxVisibleCharacters = int.MaxValue; Body.ForceMeshUpdate(); }
                Speaker.text = "";
            }
            Status.text = string.IsNullOrEmpty(status) ? NovelSaveUI.Module?.Message ?? "" : status;
            bool unblocked = snapshot.PauseReasons.Count == 0 && snapshot.State != NarrativeState.Cancelled;
            bool hidden = _session?.DialogueHidden == true;
            bool historyOpen = false;
            foreach (var reason in snapshot.PauseReasons)
                if (reason.StartsWith("History#", StringComparison.Ordinal)) { historyOpen = true; break; }
            Dialogue.gameObject.SetActive(_storyDialogueVisible && !hidden && !historyOpen); Choices.gameObject.SetActive(!hidden && !historyOpen);
            ReadingShading.gameObject.SetActive(!hidden);
            ReadingControls.gameObject.SetActive(!hidden && !historyOpen && _activeTextMode != NovelTextMode.Title); HideDialogue.gameObject.SetActive(!hidden && !historyOpen);
            RestoreUI.gameObject.SetActive(hidden);
            Saves.gameObject.SetActive(false); QuickSave.gameObject.SetActive(false); QuickLoad.gameObject.SetActive(false);
            Skip.gameObject.SetActive(!hidden && snapshot.ReadMode == NarrativeReadMode.Skip);
            Auto.interactable = Skip.interactable = unblocked && snapshot.HasActiveSession && snapshot.State != NarrativeState.AwaitingChoice;
            Speed.interactable = unblocked; History.interactable = unblocked; HideDialogue.interactable = unblocked;
            bool auto = snapshot.ReadMode == NarrativeReadMode.Auto;
            ApplyLabel(Auto, Localized(auto ? AUTO_ON_KEY : AUTO_OFF_KEY, auto ? "自动 ON" : "自动 OFF"));
            ApplyLabel(Speed, Localized(SPEED_KEY, "速度") + " " + (_session?.ReadingMultiplier ?? 1) + "X");
            ApplyLabel(Skip, Localized(SKIP_STOP_KEY, "停止快进"));
            // 「隐藏」按钮的文案由 Prefab 上的多语言 Key（ui.reader.HideDialogue.Label）驱动，
            // 这里不再每帧写死中文，否则切到其它语言它不会跟着变。
            Advance.interactable = say && unblocked;
            Menu.interactable = unblocked; Settings.interactable = unblocked;
            Saves.interactable = unblocked; QuickLoad.interactable = unblocked && NovelSaveUI.Module?.IsBusy == false;
            QuickSave.interactable = unblocked && (snapshot.State == NarrativeState.AwaitingAdvance || snapshot.State == NarrativeState.AwaitingChoice) && NovelSaveUI.Module?.IsBusy == false;
            if (snapshot.State != NarrativeState.AwaitingChoice) { if (_choices.Count > 0) ClearChoices(); return; }
            if (_choiceGeneration != snapshot.SessionGeneration || _choicePosition != snapshot.PositionVersion)
            {
                ClearChoices(); _choiceGeneration = snapshot.SessionGeneration; _choicePosition = snapshot.PositionVersion;
                var owner = _session;
                foreach (var option in snapshot.Options)
                {
                    var root = UnityEngine.Object.Instantiate(ChoiceTemplate.gameObject, Choices);
                    var item = CreateItem(root); _choices.Add(item);
                    // 选项文字在读定义时就被烤成了当时的语言，显示时按 Key 重新解析，
                    // 语言切换后由 OnLanguageChanged 清掉缓存代次、这里整批重建即可跟上。
                    ((EUINovelChoiceItem)item.Logic).Configure(NovelLocalization.RouteText(option),
                        () => owner?.Choose(snapshot.SessionGeneration, snapshot.PositionVersion, option.Id, Time.frameCount));
                }
            }
            foreach (var item in _choices) ((EUINovelChoiceItem)item.Logic).SetInteractable(unblocked);
        }
        public void Visual(NovelCommand command, Sprite sprite, float progress)
        {
            int index = command.Kind == NovelCommandKind.Background ? 0 : 1 + (int)command.Slot;
            _visualAlpha[index] = command.VisualAction == NovelVisualAction.Hide ? 1 - progress : progress;
            ClearBlend(index);
            if (command.Kind == NovelCommandKind.Background) ((EUINovelBackgroundItem)_visuals[0].Logic).Apply(command, sprite, progress);
            else ((EUINovelPortraitItem)_visuals[1 + (int)command.Slot].Logic).Apply(command, sprite, progress);
            ApplyOpacity(index);
        }
        private void ApplyOpacity(int index)
        {
            float alpha = _stageAlpha * _visualAlpha[index];
            ApplyBlendOpacity(index, alpha);
        }
        public void SetOpacity(NovelTargetKind kind, NovelPortraitSlot slot, float opacity)
        {
            if (kind == NovelTargetKind.Stage) _stageAlpha = Mathf.Clamp01(opacity);
            else _visualAlpha[kind == NovelTargetKind.Background ? 0 : 1 + (int)slot] = Mathf.Clamp01(opacity);
            for (int i = 0; i < _visuals.Count; i++) ApplyOpacity(i);
        }
        public void ClearVisuals()
        {
            ResetTextLayout();
            foreach (var effect in _particleInstances.ToArray()) effect.Dispose();
            if (_visuals.Count == 0) return;
            ClearScreenEffects();
            _stageAlpha = 1; for (int i = 0; i < _visualAlpha.Length; i++) _visualAlpha[i] = 1;
            for (int i = 0; i < 3; i++) ResetActor((NovelPortraitSlot)i);
            ((EUINovelBackgroundItem)_visuals[0].Logic).Clear();
            for (int i = 1; i < _visuals.Count; i++) ((EUINovelPortraitItem)_visuals[i].Logic).Clear();
            ClearChoices();
        }
        #endregion
    }
}
