using System;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 分段 BGM 的段落调度器：只做段落推进与音量斜坡，不碰任何音频后端、存档与暂停语义。
    ///
    /// 段落流转：Idle →(Play) Intro →(播完) Loop ⇄(Climax) Climax →(播完) Loop →(FinishWithOutro) Outro →(播完) Idle。
    /// 段落之间的衔接不淡变（前奏尾接循环头、高潮尾接循环头都按无缝处理）；只有进入曲目、
    /// 高潮切入、换曲与停止才走淡入淡出，因此听起来不会在段落接点出现音量凹陷。
    ///
    /// 该对象<b>不感知暂停</b>：调用方决定何时 Tick，UI 弹窗与读档事务都不应该停止它。
    /// </summary>
    public sealed class NovelBgmDirector : IDisposable
    {
        #region 内部参数
        private readonly Func<INovelBgmOutput> _create;
        private INovelBgmOutput _current, _outgoing;
        private NovelBgmTrack _track;
        private NovelBgmClips _clips;
        private float _volume = 1;
        private float _gainFrom, _gainElapsed, _gainDuration, _currentGain;
        private float _outFrom, _outElapsed, _outDuration;
        private bool _disposed;
        /// <summary>当前曲目所在的段落；None 表示没有曲目。</summary>
        public NovelBgmSegment Segment { get; private set; }
        public string TrackId => _track?.Id;
        public bool IsPlaying => Segment != NovelBgmSegment.None;
        /// <summary>剧情音量目标（不含玩家音量，玩家音量由 Mixer 分组承担）。</summary>
        public float TargetVolume => _volume;
        /// <summary>当前通道正在写出的实际音量，用于交叉淡出时接手续接。</summary>
        public float CurrentVolume => _currentGain;
        public bool IsFading => _gainDuration > 0 && _gainElapsed < _gainDuration || _outgoing != null;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void WriteGain()
        {
            float p = _gainDuration <= 0 ? 1 : Mathf.Clamp01(_gainElapsed / _gainDuration);
            _currentGain = Mathf.Lerp(_gainFrom, _volume, p);
            _current?.SetVolume(_currentGain);
        }
        /// <summary>从 from 淡到剧情音量；duration 小于等于 0 时立即落到目标。</summary>
        private void StartGain(float from, float duration)
        {
            _gainFrom = Mathf.Clamp01(from); _gainElapsed = 0; _gainDuration = Mathf.Max(0, duration);
            WriteGain();
        }
        private void ReleaseOutgoing()
        {
            if (_outgoing == null) return;
            _outgoing.Stop();
            _outgoing.Dispose();
            _outgoing = null;
        }
        /// <summary>
        /// 把当前通道移交给淡出槽并清空当前段落。duration 为 0 表示硬停。
        /// 连续换曲且间隔短于淡出时长时，上一首的残留会被硬停，不排队二次淡出。
        /// </summary>
        private void FadeOutCurrent(float fade)
        {
            ReleaseOutgoing();
            if (_current == null) { Segment = NovelBgmSegment.None; return; }
            _outgoing = _current; _outFrom = _currentGain;
            _outElapsed = 0; _outDuration = Mathf.Max(0, fade);
            _current = null; _currentGain = 0; Segment = NovelBgmSegment.None;
            if (_outDuration <= 0) ReleaseOutgoing();
        }
        private void TickGain(float delta)
        {
            if (_current == null) return;
            if (_gainDuration > 0 && _gainElapsed < _gainDuration)
                _gainElapsed = Mathf.Min(_gainDuration, _gainElapsed + delta);
            WriteGain();
        }
        private void TickOutgoing(float delta)
        {
            if (_outgoing == null) return;
            _outElapsed = Mathf.Min(_outDuration, _outElapsed + delta);
            float p = _outDuration <= 0 ? 1 : Mathf.Clamp01(_outElapsed / _outDuration);
            _outgoing.SetVolume(_outFrom * (1 - p));
            if (p >= 1) ReleaseOutgoing();
        }
        private bool SegmentFinished()
        {
            if (_current == null) return true;
            float length = _current.Length;
            if (length <= 0) return !_current.IsPlaying;
            return _current.Position >= length - NovelBgmRules.END_EPSILON;
        }
        /// <summary>
        /// 曲目内取段的唯一入口。段位永远从<b>当前曲目</b>的分段表取，
        /// 因此一首曲目的前奏必然只接它自己的循环，不会串到别的曲目。
        /// </summary>
        private AudioClip ClipFor(NovelBgmSegment segment) =>
            _track == null || _clips == null ? null : _clips.For(segment);
        /// <summary>在当前通道内换到另一段；未配该段时保持原段落并返回 false。</summary>
        private bool SwitchTo(NovelBgmSegment segment, bool loop, float fade)
        {
            var clip = ClipFor(segment);
            if (!clip) return false;
            _current.Play(clip, loop);
            Segment = segment;
            // 段落接点不淡变：起始音量就是剧情音量，避免接缝处出现音量凹陷。
            StartGain(fade > 0 ? 0 : _volume, fade);
            return true;
        }
        private void TickSegment()
        {
            if (_current == null || !IsPlaying) return;
            switch (Segment)
            {
                case NovelBgmSegment.Intro:
                case NovelBgmSegment.Climax:
                    if (SegmentFinished()) SwitchTo(NovelBgmSegment.Loop, true, 0);
                    break;
                case NovelBgmSegment.Outro:
                    if (SegmentFinished()) { FadeOutCurrent(0); _track = null; _clips = null; }
                    break;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>工厂在每次需要一条新通道时调用；返回的对象由本调度器负责 Dispose。</summary>
        public NovelBgmDirector(Func<INovelBgmOutput> create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }
        /// <summary>
        /// 推进分段与音量。delta 建议传未缩放时间；本方法不做暂停判断。
        /// 播放时钟由这里推给输出：输出不读 AudioSource.time，Editor 静音时段落照样能走完。
        /// </summary>
        public void Tick(float delta)
        {
            if (_disposed) return;
            delta = Mathf.Max(0, delta);
            _current?.Tick(delta);
            _outgoing?.Tick(delta);
            TickGain(delta);
            TickOutgoing(delta);
            TickSegment();
        }
        /// <summary>
        /// 进入一首曲目：先播前奏（未配前奏则直接进循环），前奏播完自动接循环。
        /// 同一曲目已在播时幂等返回 true，不重播前奏；restart 为 true 时强制从头重播。
        ///
        /// <b>曲目永不串段：</b>段落只在同一首曲目的分段表内推进（<paramref name="clips"/> 与
        /// <paramref name="track"/> 成对进出），换曲时旧曲的分段上下文会被整体丢弃，
        /// 因此一首曲目的前奏只会接它自己的循环，不会接到别的曲目上。
        /// </summary>
        public bool Play(NovelBgmTrack track, NovelBgmClips clips, float volume = 1, float fade = 0, bool restart = false)
        {
            if (_disposed || track == null || clips == null) return false;
            if (!restart && IsPlaying && _track != null && string.Equals(_track.Id, track.Id, StringComparison.Ordinal)) return true;
            // 曲目与它的分段表成对进出：换曲时旧曲的段落上下文整体作废，不存在跨曲目接段。
            _track = track; _clips = clips;
            var segment = track.OpeningSegment;
            if (ClipFor(segment) == null) segment = NovelBgmSegment.Loop;
            var clip = ClipFor(segment);
            if (!clip) { _track = null; _clips = null; return false; }
            FadeOutCurrent(fade);
            _volume = Mathf.Clamp01(volume);
            _current = _create();
            try { _current.Play(clip, segment == NovelBgmSegment.Loop); }
            catch { _current.Dispose(); _current = null; _track = null; _clips = null; throw; }
            Segment = segment;
            StartGain(0, fade);
            return true;
        }
        /// <summary>
        /// 立即切入高潮段，播完自动回循环。没有曲目、已在高潮/尾段、或该曲目未配高潮段时返回 false 且不改变现状。
        /// </summary>
        public bool Climax(float fade = NovelBgmRules.CLIMAX_FADE)
        {
            if (_disposed || _current == null || !IsPlaying) return false;
            if (Segment == NovelBgmSegment.Climax || Segment == NovelBgmSegment.Outro) return false;
            return SwitchTo(NovelBgmSegment.Climax, false, fade);
        }
        /// <summary>
        /// 收尾：切到尾段，尾段播完自动停止并释放。未配尾段时退化为淡出停止。
        /// 返回是否真的播到了尾段。
        /// </summary>
        public bool FinishWithOutro(float fade = 0)
        {
            if (_disposed || _current == null || !IsPlaying) return false;
            if (ClipFor(NovelBgmSegment.Outro) == null) { Stop(fade); return false; }
            return SwitchTo(NovelBgmSegment.Outro, false, fade);
        }
        /// <summary>停止曲目；fade 为 0 时立刻停止。</summary>
        public void Stop(float fade = 0)
        {
            if (_disposed) return;
            FadeOutCurrent(fade);
            _track = null; _clips = null;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseOutgoing();
            if (_current != null) { _current.Stop(); _current.Dispose(); _current = null; }
            _track = null; _clips = null; _currentGain = 0; Segment = NovelBgmSegment.None;
        }
        #endregion
    }
}
