using System;
using Ember.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Narrative
{
    /// <summary>
    /// 分段 BGM 的发声与推进者：挂在 AudioHost 下，由自己 Update 推进段落。
    ///
    /// <b>为什么不挂在阅读会话上：</b>会话在菜单、历史、设置、存档页、读档事务与场景加载时
    /// 都会被暂停（<c>NovelSession.Pause</c> 会连带 PauseMedia），而音乐必须在这些情况下继续；
    /// 尾段更要在会话销毁、回到主界面之后继续播完，因此音源生命周期必须长于会话。
    ///
    /// 音量约定与既有循环音一致：接了 BGM Mixer 分组时玩家音量由分组承担，
    /// 本类只写剧情音量，不叠乘玩家音量。
    /// </summary>
    public sealed class NovelBgmRunner : MonoBehaviour
    {
        #region 内部参数
        private NovelBgmDirector _director;
        private float _playerVolume = 1;
        public NovelBgmDirector Director => _director;
        /// <summary>没有配置 Mixer 分组时的玩家 BGM 音量回退；接了分组时该值不影响输出。</summary>
        public float PlayerVolume { get => _playerVolume; set => _playerVolume = Mathf.Clamp01(value); }
        private AudioMixerGroup MixerGroup =>
            EmberAudioManager.TryGetInstance(out var audio) && audio.IsInitialized ? audio.BgmMixerGroup : null;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake() { _director = new NovelBgmDirector(() => new Source(this)); }
        private void Update() { Tick(Time.unscaledDeltaTime); }
        private void OnDestroy() { _director?.Dispose(); _director = null; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>
        /// 显式推进一个时间片。正常运行由 <c>Update</c> 用未缩放时间调用；
        /// 运行期用例在 Editor 帧率不可控时也走这条入口，避免用等待真实时间来假定段落已经推进。
        /// </summary>
        public void Tick(float delta) { _director?.Tick(delta); }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>一条分段音源；每次换曲新建一条，旧的那条由 Director 淡出后释放。</summary>
        private sealed class Source : INovelBgmOutput
        {
            private readonly NovelBgmRunner _owner;
            private GameObject _root;
            private AudioSource _source;
            private float _elapsed;
            public Source(NovelBgmRunner owner)
            {
                _owner = owner;
                _root = new GameObject("Novel BGM segment") { hideFlags = HideFlags.HideAndDontSave };
                _root.transform.SetParent(owner.transform, false);
                _source = _root.AddComponent<AudioSource>();
                _source.playOnAwake = false; _source.spatialBlend = 0; _source.loop = false; _source.volume = 0;
                // 分段 BGM 不随任何 UI 暂停：与「菜单不暂停 BGM」的约定一致。
                _source.ignoreListenerPause = true;
                var group = _owner.MixerGroup;
                if (group) _source.outputAudioMixerGroup = group;
            }
            /// <summary>
            /// 已播放时长由调度器推进的未缩放时间累计，不读 AudioSource.time：
            /// Editor 静音或无音频设备时真实播放位置会停住，前奏就永远接不到循环。
            /// </summary>
            public float Position => _elapsed;
            public float Length => _source && _source.clip ? _source.clip.length : 0;
            public bool IsPlaying => _source && _source.isPlaying;
            public void Tick(float delta) { if (_source) _elapsed += Mathf.Max(0, delta); }
            public void Play(AudioClip clip, bool loop)
            {
                if (!_source) throw new InvalidOperationException("分段 BGM 音源已释放");
                if (!clip) throw new InvalidOperationException("分段 BGM 缺少音频片段");
                _elapsed = 0;
                _source.clip = clip; _source.loop = loop; _source.time = 0; _source.Play();
            }
            public void SetVolume(float volume)
            {
                if (!_source) return;
                float gain = Mathf.Clamp01(volume);
                _source.volume = _source.outputAudioMixerGroup ? gain : gain * _owner.PlayerVolume;
            }
            public void Stop() { if (_source) { _source.Stop(); _source.clip = null; } }
            public void Dispose()
            {
                if (_source) { _source.Stop(); _source.clip = null; }
                if (_root) { if (Application.isPlaying) UnityEngine.Object.Destroy(_root); else UnityEngine.Object.DestroyImmediate(_root); }
                _root = null; _source = null;
            }
        }
        #endregion
    }
}
