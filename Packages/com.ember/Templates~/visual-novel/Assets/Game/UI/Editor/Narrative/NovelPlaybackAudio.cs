using System;
using System.Collections.Generic;
using Game.Narrative;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Game.UI.Editor
{
    /// <summary>Independent editor audio outputs. Muting changes gain, never the voice completion clock.</summary>
    public sealed class NovelPlaybackAudio : INovelAudio, INovelLoopAudio, INovelBgmChannel
    {
        #region 内部参数
        private sealed class Channel : IDisposable
        {
            internal GameObject Root;
            internal AudioSource Source;
            internal PlayableGraph Graph;
            internal AudioClipPlayable Clip;
            internal NovelCommandKind Kind;
            internal float Elapsed, Duration;
            internal bool Loop;
            public void Dispose()
            {
                if (Graph.IsValid()) Graph.Destroy();
                if (Root) UnityEngine.Object.DestroyImmediate(Root);
                Root = null;
            }
        }
        /// <summary>
        /// 编辑器试播的分段 BGM 输出：每换一段重建一条 manual 循环音，
        /// 播放时钟自己累计（与运行期一致），静音状态每帧重新套用，与试播的静音开关联动。
        /// </summary>
        private sealed class BgmOutput : INovelBgmOutput
        {
            private readonly Transform _host;
            private readonly Func<bool> _muted;
            private NovelLoopPlayback _loop;
            private AudioClip _clip;
            private float _elapsed, _volume;
            public BgmOutput(Transform host, Func<bool> muted) { _host = host; _muted = muted; }
            public float Position => _elapsed;
            public float Length => _clip ? _clip.length : 0;
            public bool IsPlaying => _loop != null && !_loop.IsDisposed;
            public void Tick(float delta) { _elapsed += Mathf.Max(0, delta); _loop?.Tick(delta); Apply(); }
            public void Play(AudioClip clip, bool loop)
            {
                if (!clip) throw new InvalidOperationException("试播的分段 BGM 缺少音频片段");
                _loop?.Dispose();
                _clip = clip; _elapsed = 0;
                _loop = new NovelLoopPlayback(_host, clip, true);
                Apply();
            }
            public void SetVolume(float volume) { _volume = Mathf.Clamp01(volume); Apply(); }
            public void Stop() { _loop?.Dispose(); _loop = null; _clip = null; _elapsed = 0; }
            public void Dispose() => Stop();
            private void Apply()
            {
                if (_loop == null) return;
                _loop.SetVolume(_volume);
                _loop.SetMuted(_muted());
            }
        }
        private readonly Transform _host;
        private readonly List<Channel> _channels = new();
        private readonly List<NovelLoopPlayback> _loops = new();
        private readonly NovelBgmDirector _director;
        private bool _paused, _muted, _disposed;
        private float _bgm = 1, _sfx = 1, _voice = 1;
        public event Action VoiceCompleted;
        public bool VoicePlaying => _channels.Exists(c => c.Kind == NovelCommandKind.Voice);
        public int ActiveCount => _channels.Count + _loops.FindAll(l => !l.IsDisposed).Count;
        public NovelBgmStatus Status { get; private set; }
        public string Error { get; private set; }
        public string TrackId => _director.TrackId;
        public NovelBgmSegment Segment => _director.Segment;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Remove(NovelCommandKind kind)
        {
            for (int i = _channels.Count - 1; i >= 0; i--)
                if (_channels[i].Kind == kind) { _channels[i].Dispose(); _channels.RemoveAt(i); }
        }
        private void RefreshVolumes()
        {
            foreach (var channel in _channels)
                channel.Source.volume = _muted ? 0 : channel.Kind == NovelCommandKind.BGM ? _bgm :
                    channel.Kind == NovelCommandKind.Voice ? _voice : _sfx;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelPlaybackAudio(Transform host)
        {
            _host = host;
            _director = new NovelBgmDirector(() => new BgmOutput(host, () => _muted));
        }
        /// <summary>
        /// 试播的分段 BGM：按曲目键同步加载四段音频（试播不异步），交给与运行期同一个调度器，
        /// 所以试播里能听到与正式一致的前奏→循环→高潮→尾段流转。
        /// </summary>
        public bool Play(NovelBgmTrack track, float volume = 1, float fade = 0, bool restart = false)
        {
            if (_disposed || track == null) return false;
            var clips = new NovelBgmClips
            {
                Intro = NovelPlaybackStory.LoadAsset<AudioClip>(track.IntroPath),
                Loop = NovelPlaybackStory.LoadAsset<AudioClip>(track.LoopPath),
                Climax = NovelPlaybackStory.LoadAsset<AudioClip>(track.ClimaxPath),
                Outro = NovelPlaybackStory.LoadAsset<AudioClip>(track.OutroPath)
            };
            if (!clips.Loop)
            {
                Error = "试播缺少循环段音频：" + track.LoopPath; Status = NovelBgmStatus.Failed; return false;
            }
            if (!_director.Play(track, clips, volume, fade, restart))
            {
                Error = "试播无法开始曲目：" + track.Id; Status = NovelBgmStatus.Failed; return false;
            }
            Error = null; Status = NovelBgmStatus.Playing; return true;
        }
        public bool Climax(float fade = NovelBgmRules.CLIMAX_FADE) => _director.Climax(fade);
        public bool FinishWithOutro(float fade = 0) => _director.FinishWithOutro(fade);
        public void Stop(float fade = 0)
        {
            _director.Stop(fade);
            Error = null; Status = NovelBgmStatus.Idle;
        }
        /// <summary>
        /// 编辑器试播的循环音<b>故意不接 Mixer</b>：试播要能独立于项目混音设置发声与静音
        /// （见 <see cref="SetMuted"/> / <see cref="SetVolumes"/> 直接写音源音量），
        /// 接上分组会引入第二次玩家音量衰减。正式运行期走 <c>NovelAudio</c>，不经过这里。
        /// </summary>
        public INovelLoopPlayback CreateLoop(AudioClip clip, bool bgm)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NovelPlaybackAudio));
            _loops.RemoveAll(l => l.IsDisposed);
            var loop = new NovelLoopPlayback(_host, clip, true); loop.SetMuted(_muted); _loops.Add(loop); return loop;
        }
        public void Play(NovelCommandKind kind, AudioClip clip)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NovelPlaybackAudio));
            if (!clip) throw new InvalidOperationException("试播音频为空。");
            if (kind == NovelCommandKind.BGM || kind == NovelCommandKind.Voice) Remove(kind);
            var channel = new Channel { Kind = kind, Duration = clip.length, Loop = kind == NovelCommandKind.BGM };
            try
            {
                channel.Root = new GameObject("Novel preview " + kind) { hideFlags = HideFlags.HideAndDontSave };
                channel.Root.transform.SetParent(_host, false);
                channel.Source = channel.Root.AddComponent<AudioSource>();
                channel.Source.playOnAwake = false; channel.Source.spatialBlend = 0;
                channel.Source.ignoreListenerPause = true; channel.Source.ignoreListenerVolume = true;
                channel.Graph = PlayableGraph.Create("Novel preview " + kind);
                channel.Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                channel.Clip = AudioClipPlayable.Create(channel.Graph, clip, channel.Loop);
                var output = AudioPlayableOutput.Create(channel.Graph, "Audio", channel.Source);
                output.SetSourcePlayable(channel.Clip); output.SetEvaluateOnSeek(true);
                _channels.Add(channel); RefreshVolumes();
                if (!_paused)
                {
                    channel.Graph.Play(); channel.Clip.Seek(0, 0, channel.Loop ? 0 : channel.Duration);
                    channel.Graph.Evaluate(0);
                }
            }
            catch { _channels.Remove(channel); channel.Dispose(); throw; }
        }
        public void AdvanceTime(float delta)
        {
            if (_paused || _disposed) return;
            // 试播的分段 BGM 也由这条时钟推进：段落流转不依赖会话 Tick，与运行期一致。
            _director.Tick(Mathf.Max(0, delta));
            foreach (var channel in _channels)
            {
                channel.Elapsed += Mathf.Max(0, delta);
                channel.Graph.Evaluate(Mathf.Max(0, delta));
            }
            Tick();
        }
        public void Tick()
        {
            if (_paused || _disposed) return;
            bool voiceCompleted = false;
            for (int i = _channels.Count - 1; i >= 0; i--)
            {
                var channel = _channels[i];
                if (channel.Loop || channel.Elapsed < channel.Duration) continue;
                voiceCompleted |= channel.Kind == NovelCommandKind.Voice;
                channel.Dispose(); _channels.RemoveAt(i);
            }
            if (voiceCompleted) VoiceCompleted?.Invoke();
        }
        public void StopVoice() => Remove(NovelCommandKind.Voice);
        public void SetPaused(bool paused)
        {
            if (_paused == paused || _disposed) return;
            _paused = paused;
            foreach (var channel in _channels)
            {
                if (paused) { channel.Clip.Pause(); channel.Graph.Evaluate(0); channel.Graph.Stop(); channel.Source.Pause(); }
                else
                {
                    double offset = channel.Loop && channel.Duration > 0 ? channel.Elapsed % channel.Duration : channel.Elapsed;
                    channel.Graph.Play(); channel.Source.UnPause();
                    channel.Clip.Seek(offset, 0, channel.Loop ? 0 : Math.Max(.001, channel.Duration - offset));
                    channel.Graph.Evaluate(0);
                }
            }
        }
        public void SetMuted(bool muted) { _muted = muted; RefreshVolumes(); foreach (var loop in _loops) loop.SetMuted(muted); }
        public void SetVolumes(float bgm, float sfx, float voice)
        { _bgm = Mathf.Clamp01(bgm); _sfx = Mathf.Clamp01(sfx); _voice = Mathf.Clamp01(voice); RefreshVolumes(); }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var channel in _channels) channel.Dispose();
            _channels.Clear(); foreach (var loop in _loops) loop.Dispose(); _loops.Clear();
            _director.Dispose(); VoiceCompleted = null; Status = NovelBgmStatus.Idle;
        }
        #endregion
    }
}
