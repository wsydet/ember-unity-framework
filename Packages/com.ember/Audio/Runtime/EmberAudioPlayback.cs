using System;
using UnityEngine;

namespace Ember.Audio
{
    /// <summary>由调用方持有的音效播放；释放只停止本次播放，不影响其他音效。</summary>
    public sealed class EmberAudioPlayback : IDisposable
    {
        #region 内部参数
        private AudioSource _source;
        private bool _paused;
        private bool _completed;
        public event Action Completed;
        public bool IsFinished => !_source || (!_paused && !_source.isPlaying);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberAudioPlayback(AudioSource source) { _source = source; }
        /// <summary>拥有者每帧调用；仅自然完成通知一次，暂停与主动停止不发送通知。</summary>
        public void Tick()
        {
            if (_completed || !_source || !IsFinished) return;
            _completed = true; Completed?.Invoke();
        }
        public void SetVolume(float volume) { if (_source) _source.volume = Mathf.Clamp01(volume); }
        public void SetPaused(bool paused)
        {
            if (!_source || _paused == paused) return;
            _paused = paused;
            if (paused) _source.Pause(); else _source.UnPause();
        }
        public void Dispose()
        {
            Completed = null;
            if (!_source) return;
            _source.Stop(); _source.clip = null;
            UnityEngine.Object.Destroy(_source); _source = null;
        }
        #endregion
    }
}
