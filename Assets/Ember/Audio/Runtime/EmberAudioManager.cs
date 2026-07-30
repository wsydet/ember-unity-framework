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
    public class EmberAudioManager : EmberMonoSingleton<EmberAudioManager>
    {
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
        /// </summary>
        public void Init(AudioMixer mixer = null, string bgmParam = "BGMVolume", string sfxParam = "SFXVolume")
        {
            if (_initialized) return;

            _mixer = mixer;
            _bgmMixerParam = bgmParam;
            _sfxMixerParam = sfxParam;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            if (_mixer != null)
            {
                _bgmSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
                _sfxSource.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
            }

            _initialized = true;
            ApplyVolume();
            EmberEventBus.Dispatch(EmberBroadcastEvent.AudioReady);
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

            var temp = gameObject.AddComponent<TempAudioSource>();
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

        #endregion

        // ============================================================

        #region 生命周期

        protected override void OnSingletonDestroy()
        {
            EmberEventBus.Dispatch(EmberBroadcastEvent.AudioShutdown);

            StopBGM();
            _initialized = false;
        }

        #endregion

        // ============================================================

        #region 内部方法

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
