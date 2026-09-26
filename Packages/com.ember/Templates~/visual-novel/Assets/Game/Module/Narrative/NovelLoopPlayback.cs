using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Game.Narrative
{
    /// <summary>Owned loop output on the existing audio host. Shares the preview's public Playables backend.</summary>
    public sealed class NovelLoopPlayback : INovelLoopPlayback
    {
        #region 内部参数
        private GameObject _root;
        private AudioSource _source;
        private PlayableGraph _graph;
        private AudioClipPlayable _clip;
        private readonly float _duration;
        private readonly bool _manual;
        private double _elapsed;
        private float _volume;
        private bool _paused, _muted;
        public bool IsDisposed => !_root;
        /// <summary>
        /// 是否已经接到 Mixer 分组输出。
        /// 接上以后玩家音量由分组承担（与 BGM 的既有约定一致），调用方不能再在
        /// <see cref="SetVolume"/> 里叠乘一次玩家音量，否则会双重衰减。
        /// </summary>
        public bool MixerRouted => _source && _source.outputAudioMixerGroup;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <param name="group">
        /// 这条循环音要接的 Mixer 分组；BGM 循环传 BGM 分组、环境音循环传 SFX 分组。
        /// 传 null 表示不接 Mixer（例如编辑器试播，它自己管理静音与音量）。
        /// </param>
        public NovelLoopPlayback(Transform host, AudioClip clip, bool manual, AudioMixerGroup group = null)
        {
            if (!host || !clip || clip.length <= 0) throw new InvalidOperationException("循环音缺少 Host 或有效 AudioClip");
            _manual = manual; _duration = clip.length;
            try
            {
                _root = new GameObject("Novel owned loop") { hideFlags = HideFlags.HideAndDontSave };
                _root.transform.SetParent(host, false);
                _source = _root.AddComponent<AudioSource>(); _source.playOnAwake = false;
                _source.spatialBlend = 0; _source.volume = 0;
                // 与 EmberAudioManager 的 BGM / SFX 走同一条混音路径，否则玩家音量、分组静音
                // 和总线效果都作用不到循环音上。
                if (group) _source.outputAudioMixerGroup = group;
                _source.ignoreListenerPause = manual; _source.ignoreListenerVolume = manual;
                _graph = PlayableGraph.Create("Novel owned loop");
                _graph.SetTimeUpdateMode(manual ? DirectorUpdateMode.Manual : DirectorUpdateMode.UnscaledGameTime);
                _clip = AudioClipPlayable.Create(_graph, clip, true);
                var output = AudioPlayableOutput.Create(_graph, "Audio", _source);
                output.SetSourcePlayable(_clip); output.SetEvaluateOnSeek(true);
                _graph.Play(); _clip.Seek(0, 0, 0); _graph.Evaluate(0);
            }
            catch { Dispose(); throw; }
        }
        public void Tick(float delta)
        {
            if (IsDisposed || _paused) return;
            _elapsed += Mathf.Max(0, delta);
            if (_manual) _graph.Evaluate(Mathf.Max(0, delta));
        }
        public void SetVolume(float volume) { _volume = Mathf.Clamp01(volume); if (_source) _source.volume = _muted ? 0 : _volume; }
        public void SetMuted(bool muted) { _muted = muted; SetVolume(_volume); }
        public void SetPaused(bool paused)
        {
            if (IsDisposed || _paused == paused) return;
            _paused = paused;
            if (paused) { _elapsed = _clip.GetTime(); _clip.Pause(); _graph.Evaluate(0); _graph.Stop(); _source.Pause(); }
            else { _graph.Play(); _source.UnPause(); _clip.Seek(_elapsed % _duration, 0, 0); _graph.Evaluate(0); }
        }
        public void Dispose()
        {
            if (_graph.IsValid()) _graph.Destroy();
            if (_source) { _source.Stop(); _source.clip = null; }
            if (_root) { if (Application.isPlaying) UnityEngine.Object.Destroy(_root); else UnityEngine.Object.DestroyImmediate(_root); }
            _root = null; _source = null;
        }
        #endregion
    }
}
