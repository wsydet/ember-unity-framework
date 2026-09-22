using System;
using System.Collections.Generic;
using Game.Narrative;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Game.UI.Editor
{
    /// <summary>Independent editor audio outputs. Muting changes gain, never the voice completion clock.</summary>
    public sealed class NovelPlaybackAudio : INovelAudio, INovelLoopAudio
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
        private readonly Transform _host;
        private readonly List<Channel> _channels = new();
        private readonly List<NovelLoopPlayback> _loops = new();
        private bool _paused, _muted, _disposed;
        private float _bgm = 1, _sfx = 1, _voice = 1;
        public event Action VoiceCompleted;
        public bool VoicePlaying => _channels.Exists(c => c.Kind == NovelCommandKind.Voice);
        public int ActiveCount => _channels.Count + _loops.FindAll(l => !l.IsDisposed).Count;
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
        public NovelPlaybackAudio(Transform host) { _host = host; }
        public INovelLoopPlayback CreateLoop(AudioClip clip)
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
            _channels.Clear(); foreach (var loop in _loops) loop.Dispose(); _loops.Clear(); VoiceCompleted = null;
        }
        #endregion
    }
}
