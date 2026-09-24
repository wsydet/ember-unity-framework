using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private readonly NovelTextClock _textClock = new();
        private NovelCommand _textCommand;
        private int _pageStart, _pageEnd;
        private long _textAdvanceFrame = -1;
        private float _textEnterElapsed, _textExitElapsed;
        private bool _textExiting;
        private bool TextEntering => _textCommand?.TextReveal == NovelTextReveal.Fade && _textEnterElapsed < _textCommand.TextFadeDuration;
        public bool TextTransitionActive => _textExiting || TextEntering;
        public bool StoryDialogueVisible { get; private set; } = true;
        public int TextPageStart => _pageStart;
        public int TextPageEnd => _pageEnd;
        public bool TextPageComplete => _visible >= _pageEnd && !TextEntering;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ResetText(NovelCommand command)
        {
            _textCommand = command; _pageStart = _pageEnd = 0; _textClock.Reset(command.TextBeats);
            _visible = 0; _autoElapsed = 0; _textEnterElapsed = _textExitElapsed = 0; _textExiting = false; StoryDialogueVisible = true;
        }
        private void PrepareText(NovelCommand command, bool restored)
        {
            if (command?.Kind != NovelCommandKind.Say) return;
            if (_textCommand != command)
            {
                ResetText(command);
                if (restored) _textEnterElapsed = command.TextFadeDuration;
                // Stable saves represent the fully read sentence. Restore its final page without replaying beats/voice.
                if (restored && _view is INovelTextView savedView)
                {
                    int length = NovelTextRules.Length(command.Text);
                    int end = savedView.PrepareText(command, 0);
                    while (end < length && end > _pageStart)
                    { _pageStart = end; end = savedView.PrepareText(command, _pageStart); }
                }
            }
            _pageEnd = _view is INovelTextView textView ? textView.PrepareText(command, _pageStart) : _view.TextLength;
            _pageEnd = Mathf.Max(_pageStart + 1, _pageEnd);
        }
        private int FullTextLength => _view is INovelTextView ? NovelTextRules.Length(_runner.CurrentCommand.Text) : _view.TextLength;
        private void TickText(float delta, long frame)
        {
            var s = _runner.Snapshot;
            PrepareText(_runner.CurrentCommand, false);
            if (_readMode == NarrativeReadMode.Skip)
            {
                _textEnterElapsed = _textCommand.TextFadeDuration; _textClock.Complete(FullTextLength); _visible = FullTextLength;
                if (_view is INovelTextView)
                    while (_pageEnd < FullTextLength) { _pageStart = _pageEnd; PrepareText(_runner.CurrentCommand, false); }
                _runner.CompleteReveal(s.SessionGeneration, s.PositionVersion); return;
            }
            bool wasComplete = TextPageComplete;
            if (_textCommand.TextReveal == NovelTextReveal.Typewriter)
                _visible = _textClock.Tick(delta * ReadingMultiplier, _textSpeed * _textCommand.TextSpeedMultiplier, _pageEnd);
            else
            {
                _textEnterElapsed += delta * ReadingMultiplier;
                _visible = _pageEnd;
            }
            if (!TextPageComplete) return;
            if (_pageEnd >= FullTextLength)
            { _autoElapsed = 0; _runner.CompleteReveal(s.SessionGeneration, s.PositionVersion); return; }
            if (!wasComplete) { Notify(); return; }
            // Intermediate pages don't wait for the full-line voice; the final page retains the existing voice gate.
            if (wasComplete && _readMode == NarrativeReadMode.Auto)
            {
                _autoElapsed += delta;
                if (_autoElapsed >= (Mathf.Max(.5f, (_pageEnd - _pageStart) / 20f) + _autoInterval) / ReadingMultiplier)
                    AdvanceTextPage(frame);
            }
        }
        private void RenderTextEffects()
        {
            if (_view is not INovelTextEffectsView effects) return;
            float enter = TextEntering ? NovelActorRules.Ease(_textCommand.TextEase, Mathf.Clamp01(_textEnterElapsed / _textCommand.TextFadeDuration)) : 1;
            float exit = _textExiting ? 1 - NovelActorRules.Ease(_textCommand.TextEase, Mathf.Clamp01(_textExitElapsed / _textCommand.TitleExitDuration)) : 1;
            effects.SetTextEffects(enter, exit);
        }
        private bool BeginTextExit()
        {
            if (_runner.Snapshot.State != NarrativeState.AwaitingAdvance || _textCommand?.TextMode != NovelTextMode.Title ||
                _textCommand.TitleExitDuration <= 0 || _readMode == NarrativeReadMode.Skip) return false;
            _textExiting = true; _textExitElapsed = 0; Render(); return true;
        }
        private bool TickTextExit(float delta, long frame)
        {
            if (!_textExiting) return false;
            _textExitElapsed += delta * ReadingMultiplier;
            if (_textExitElapsed >= _textCommand.TitleExitDuration || _readMode == NarrativeReadMode.Skip)
            { _textExiting = false; AdvanceRunnerCore(frame); }
            else Render();
            return true;
        }
        private bool AdvanceTextPage(long frame)
        {
            var s = _runner.Snapshot;
            if (s.State != NarrativeState.Revealing && s.State != NarrativeState.AwaitingAdvance) return false;
            if (frame <= _textAdvanceFrame) return true;
            _textAdvanceFrame = frame;
            PrepareText(_runner.CurrentCommand, s.State == NarrativeState.AwaitingAdvance);
            if (s.State == NarrativeState.Revealing && !TextPageComplete)
            {
                _textEnterElapsed = _textCommand.TextFadeDuration; _textClock.Complete(_pageEnd); _visible = _pageEnd; _autoElapsed = 0;
                if (_pageEnd >= FullTextLength) _runner.CompleteReveal(s.SessionGeneration, s.PositionVersion);
                return true;
            }
            if (_pageEnd < FullTextLength)
            { _pageStart = _pageEnd; _textEnterElapsed = 0; _autoElapsed = 0; PrepareText(_runner.CurrentCommand, false); Notify(); return true; }
            return false;
        }
        #endregion
    }
}
