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
        public int TextLength => Body.textInfo.characterCount;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
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
            string text = say ? command.Text : snapshot.State == NarrativeState.Ended ? "故事暂告一段落。感谢阅读。"
                : snapshot.State == NarrativeState.AwaitingChoice ? "你会如何选择？" : status;
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
            Auto.GetComponentInChildren<TMPro.TMP_Text>().text = snapshot.ReadMode == NarrativeReadMode.Auto ? "自动 ON" : "自动 OFF";
            Speed.GetComponentInChildren<TMPro.TMP_Text>().text = "速度 " + (_session?.ReadingMultiplier ?? 1) + "X";
            Skip.GetComponentInChildren<TMPro.TMP_Text>().text = "停止快进";
            HideDialogue.GetComponentInChildren<TMPro.TMP_Text>().text = "隐藏";
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
                    ((EUINovelChoiceItem)item.Logic).Configure(option.Text,
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
