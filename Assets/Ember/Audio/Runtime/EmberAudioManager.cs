using System.Collections.Generic;
using Ember.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Ember.Audio
{
    /// <summary>
    /// 音频管理器 —— BGM / SFX 播放与音量控制。
    ///
    /// 参考 burner 的 <c>AudioMgr</c>，核心设计：
    /// - BGM 和 SFX 分离为两个独立 AudioSource
    /// - 通过 Unity AudioMixer 实现音量分组控制
    /// - 广播生命周期事件（AudioReady / AudioShutdown）
    ///
    /// 使用方式：
    /// <code>
    /// EmberAudioManager.Instance.PlayBGM("bgm/title", loop: true);
    /// EmberAudioManager.Instance.PlaySFX("sfx/click");
    /// EmberAudioManager.Instance.SetBGMVolume(0.8f);
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Audio)]
    public class EmberAudioManager : EmberSingleton<EmberAudioManager>, IEmberManager
    {
        private const string TAG = LogTags.AudioManager;
        #region 参数

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _bgmMixerParam = "BGMVolume";
        [SerializeField] private string _sfxMixerParam = "SFXVolume";

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private readonly Queue<AudioClip> _sfxQueue = new();
        private bool _initialized;

        private const float MinVolume = 0.0001f;
        private const float MaxVolume = 1f;
        private float _bgmVolume = 0.8f;
        private float _sfxVolume = 1f;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 初始化 ========

        /// <summary>
        /// 初始化音频管理器，创建 AudioSource 并应用 Mixer。
        /// 调用后广播 AudioReady 事件。
        ///
        /// 如果已通过 <see cref="IEmberManager.Init"/> 完成默认初始化（无 Mixer），
        /// 传入 mixer 参数仍可重新配置 Mixer。
        /// </summary>
        public void Init(AudioMixer mixer = null, string bgmParam = "BGMVolume", string sfxParam = "SFXVolume")
        {
            // 已通过 IEmberManager.Init() 完成默认初始化 + 有 mixer 需要配置
            if (_initialized && mixer != null)
            {
                _mixer = mixer;
                _bgmMixerParam = bgmParam;
                _sfxMixerParam = sfxParam;
                if (_bgmSource != null)
                    _bgmSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
                if (_sfxSource != null)
                    _sfxSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
                ApplyVolume();
                EmberDebug.LogInit(TAG, "EmberAudioManager mixer configured after default init.");
                return;
            }

            if (_initialized) return;

            var host = GameLauncher.Instance.AudioHost;
            if (host == null)
            {
                EmberDebug.LogError(TAG, "GameBoot 下缺少 AudioHost 子节点，AudioManager 无法初始化。");
                return;
            }

            _mixer = mixer;
            _bgmMixerParam = bgmParam;
            _sfxMixerParam = sfxParam;

            _bgmSource = host.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = host.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            if (_mixer != null)
            {
                _bgmSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
                _sfxSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
            }

            _initialized = true;
            ApplyVolume();
            EmberEventBus.OnNext(EmberBroadcastEvent.AudioReady);
        }

        // ======== BGM ========

        /// <summary>
        /// 播放背景音乐。如果 clip 与当前 BGM 相同则忽略。
        /// </summary>
        public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0f)
        {
            if (!_initialized || clip == null) return;
            if (_bgmSource.clip == clip) return;

            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.Play();
        }

        /// <summary>
        /// 停止 BGM。
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        /// <summary>
        /// 设置 BGM 音量（0.0 ~ 1.0）。
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp(volume, 0f, MaxVolume);
            ApplyVolume();
        }

        // ======== SFX ========

        /// <summary>
        /// 播放音效。支持多个 SFX 同时播放（每个 SFX 独立的临时 AudioSource）。
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (!_initialized || clip == null) return;

            var temp = GameLauncher.Instance.AudioHost.AddComponent<TempAudioSource>();
            temp.Play(clip, _sfxVolume * volumeScale);
        }

        /// <summary>
        /// 设置 SFX 音量（0.0 ~ 1.0）。
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp(volume, 0f, MaxVolume);
            ApplyVolume();
        }

        // ======== IEmberManager ========

        /// <summary>
        /// 由 ManagerCollector 自动调用的无参初始化。
        /// 创建默认 AudioSource（不使用 Mixer）。
        /// 完整初始化（含 Mixer 配置）请调用 <see cref="Init(AudioMixer, string, string)"/>。
        /// </summary>
        void IEmberManager.Init()
        {
            if (_initialized) return;

            var host = GameLauncher.Instance.AudioHost;
            if (host == null)
            {
                EmberDebug.LogError(TAG, "GameBoot 下缺少 AudioHost 子节点，AudioManager 无法初始化。");
                return;
            }

            _bgmSource = host.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = host.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            _initialized = true;
            ApplyVolume();
            EmberEventBus.OnNext(EmberBroadcastEvent.AudioReady);
            EmberDebug.LogInit(TAG, "EmberAudioManager initialized.");
        }

        /// <summary>
        /// 由 ManagerCollector 逆序调用的销毁逻辑。
        /// </summary>
        void IEmberManager.Destroy()
        {
            DestroyInternal();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// EmberSingleton 销毁钩子。
        /// </summary>
        protected override void OnDestroy()
        {
            DestroyInternal();
        }

        /// <summary>
        /// 共享清理逻辑：广播 AudioShutdown、停止 BGM、销毁 AudioSource 组件、重置状态。
        /// AudioHost 由 GameBoot 预置，不在此销毁。
        /// </summary>
        private void DestroyInternal()
        {
            EmberEventBus.OnNext(EmberBroadcastEvent.AudioShutdown);
            StopBGM();
            if (_bgmSource != null) { UnityEngine.Object.Destroy(_bgmSource); _bgmSource = null; }
            if (_sfxSource != null) { UnityEngine.Object.Destroy(_sfxSource); _sfxSource = null; }
            _initialized = false;
        }

        private void ApplyVolume()
        {
            if (_mixer != null)
            {
                _mixer.SetFloat(_bgmMixerParam, VolumeToDB(_bgmVolume));
                _mixer.SetFloat(_sfxMixerParam, VolumeToDB(_sfxVolume));
            }
        }

        private static float VolumeToDB(float volume)
        {
            return volume <= MinVolume ? -80f : 20f * Mathf.Log10(volume);
        }

        /// <summary>
        /// 临时 AudioSource 组件，用于播放单个 SFX，播完后自动销毁。
        /// </summary>
        private class TempAudioSource : MonoBehaviour
        {
            private AudioSource _source;

            public void Play(AudioClip clip, float volume)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.clip = clip;
                _source.volume = volume;
                _source.loop = false;
                _source.Play();
                Destroy(this, clip.length + 0.1f);
            }

            private void OnDestroy()
            {
                if (_source != null)
                    Destroy(_source);
            }
        }

        #endregion
    }
}
