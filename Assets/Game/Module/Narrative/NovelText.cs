using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public enum NovelTextReveal { Default, Typewriter, Fade, Instant }

    public interface INovelTextEffectsView
    {
        void SetTextEffects(float textOpacity, float cardOpacity);
    }

    public enum NovelTextMode { Dialogue, Title, FullScreen }

    /// <summary>Offsets count Unicode scalars in plain story text, including punctuation and newlines.</summary>
    [Serializable]
    public sealed class NovelTextBeat
    {
        #region 编辑器面板参数
        [SerializeField] private int _at;
        [SerializeField] private float _pause;
        [SerializeField] private float _speed = 1;
        [SerializeField] private int _instant;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public int At => _at;
        public float Pause => _pause;
        public float Speed => _speed;
        public int Instant => _instant;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelTextBeat(int at, float pause = 0, float speed = 1, int instant = 0)
        { _at = at; _pause = pause; _speed = speed; _instant = instant; }
        #endregion
    }

    /// <summary>Optional paginated text surface. Preparation must not invoke the game UI manager.</summary>
    public interface INovelTextView
    {
        int PrepareText(NovelCommand command, int start);
        void SetStoryDialogueVisible(bool visible);
        void ShowText(string speaker, int visibleCharacters);
    }

    public static class NovelTextRules
    {
        #region 外部方法
        [NoGC]
        public static int Length(string text, int utf16End = int.MaxValue)
        {
            int count = 0;
            for (int i = 0; text != null && i < text.Length && i < utf16End; i++, count++)
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
            return count;
        }
        [NoGC]
        public static int Utf16Index(string text, int start)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int i = 0;
            for (int count = 0; count < start && i < text.Length; count++, i++)
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
            return i;
        }
        [HasGC]
        public static string Slice(string text, int start) => string.IsNullOrEmpty(text) ? string.Empty : text.Substring(Utf16Index(text, start));
        [HasGC]
        public static string Validate(NovelCommand command)
        {
            if (command.Kind != NovelCommandKind.Say) return null;
            if (!Enum.IsDefined(typeof(NovelTextMode), command.TextMode)) return "正文显示模式无效";
            if (!Enum.IsDefined(typeof(NovelTextReveal), command.TextReveal) || !Enum.IsDefined(typeof(NovelEase), command.TextEase) ||
                !NovelActionHandle.ValidTime(command.TextFadeDuration) || command.TextFadeDuration > 30 ||
                !NovelActionHandle.ValidTime(command.TitleExitDuration) || command.TitleExitDuration > 30 ||
                !NovelActionHandle.ValidTime(command.TextSpeedMultiplier) || command.TextSpeedMultiplier < .05f || command.TextSpeedMultiplier > 20)
                return "文字效果：渐变时长 0–30 秒，打字速度倍率 0.05–20，效果与缓动须有效";
            int length = Length(command.Text), previous = -1;
            foreach (var beat in command.TextBeats)
            {
                if (beat == null || beat.At <= previous || beat.At < 0 || beat.At >= length ||
                    !NovelActionHandle.ValidTime(beat.Pause) || beat.Pause > 60 ||
                    !NovelActionHandle.ValidTime(beat.Speed) || beat.Speed < .05f || beat.Speed > 20 ||
                    beat.Instant < 0 || beat.Instant > length - beat.At)
                    return "文字节奏须按 At 严格递增：标量索引在正文内，Pause 0–60 秒，Speed 0.05–20，Instant 不超出正文";
                previous = beat.At;
            }
            return null;
        }
        #endregion
    }

    /// <summary>Consumes time at exact beat boundaries; never embeds control tags in displayed/history text.</summary>
    public sealed class NovelTextClock
    {
        #region 内部参数
        private IReadOnlyList<NovelTextBeat> _beats = Array.Empty<NovelTextBeat>();
        private int _next, _instantEnd;
        private float _pause, _speed = 1;
        public float Visible { get; private set; }
        public bool Pausing => _pause > 0;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Reset(IReadOnlyList<NovelTextBeat> beats)
        { _beats = beats ?? Array.Empty<NovelTextBeat>(); _next = _instantEnd = 0; _pause = Visible = 0; _speed = 1; }
        public void Complete(int end)
        {
            Visible = end; _pause = 0;
            while (_next < _beats.Count && _beats[_next].At < end)
            { var beat = _beats[_next++]; _speed = beat.Speed; _instantEnd = Math.Max(_instantEnd, beat.At + beat.Instant); }
        }
        public float Tick(float delta, float charactersPerSecond, int end)
        {
            delta = Mathf.Max(0, delta);
            while (Visible < end)
            {
                if (_next < _beats.Count && Visible >= _beats[_next].At)
                {
                    var beat = _beats[_next++]; _pause = beat.Pause; _speed = beat.Speed;
                    _instantEnd = Math.Max(_instantEnd, beat.At + beat.Instant);
                }
                if (_pause > 0)
                { float used = Mathf.Min(delta, _pause); _pause -= used; delta -= used; if (_pause > 0) break; }
                int boundary = _next < _beats.Count ? Math.Min(end, _beats[_next].At) : end;
                if (_instantEnd > Visible) { Visible = Math.Min(_instantEnd, boundary); continue; }
                if (delta <= 0) break;
                float rate = Mathf.Max(.001f, charactersPerSecond * _speed);
                float step = Mathf.Min(boundary - Visible, delta * rate);
                Visible += step; delta = Mathf.Max(0, delta - step / rate);
            }
            return Visible;
        }
        #endregion
    }
}
