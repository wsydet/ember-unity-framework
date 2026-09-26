using System;
using Ember.Basic;
using Ember.Core;
using Ember.Resource;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 分段 BGM 的门面：持有常驻播放器并按曲目加载四段音源。
    ///
    /// 声明为 Global 模块，因此在退出 Gameplay、回到主界面时它依然存活 ——
    /// 这正是尾段能跨状态播完的前提（阅读会话此时已经销毁）。
    ///
    /// 句柄所有权在本模块而非阅读会话：会话销毁会释放它自己的资源引用，
    /// 若 BGM 片段挂在会话上，尾段随时可能因为资源被卸载而断掉。
    /// </summary>
    [EmberModule(ModulePhase.Global, Enabled = true)]
    public sealed class NovelBgmModule : EmberSingleton<NovelBgmModule>, IEmberModule, IEmberUpdate, INovelBgmChannel
    {
        #region 内部参数
        private const string TAG = "Game.Narrative";
        private NovelBgmRunner _runner;
        private EmberAssetHandle<AudioClip> _intro, _loop, _climax, _outro;
        private NovelBgmTrack _loading;
        private float _loadingVolume = 1, _loadingFade, _playerVolume = 1;
        private bool _loadingRestart;
        private string _error;
        public NovelBgmStatus Status { get; private set; }
        public string Error => _error;
        public string TrackId => Director?.TrackId;
        public NovelBgmSegment Segment => Director?.Segment ?? NovelBgmSegment.None;
        public bool IsPlaying => Director?.IsPlaying == true;
        public NovelBgmDirector Director => _runner ? _runner.Director : null;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private NovelBgmRunner EnsureRunner()
        {
            if (_runner) return _runner;
            var launcher = GameLauncher.IsValid ? GameLauncher.Instance : null;
            var host = launcher ? launcher.AudioHost : null;
            if (!host)
            {
                _error = "AudioHost 缺失，分段 BGM 无法播放";
                Status = NovelBgmStatus.Failed;
                return null;
            }
            _runner = host.GetComponent<NovelBgmRunner>() ?? host.AddComponent<NovelBgmRunner>();
            _runner.PlayerVolume = _playerVolume;
            return _runner;
        }
        private void ReleaseHandles()
        {
            _intro?.Dispose(); _loop?.Dispose(); _climax?.Dispose(); _outro?.Dispose();
            _intro = null; _loop = null; _climax = null; _outro = null;
            _loading = null;
        }
        private static bool Settled(EmberAssetHandle<AudioClip> handle) => handle == null || handle.IsDone;
        private static AudioClip ClipOf(EmberAssetHandle<AudioClip> handle) => handle != null && handle.Succeeded ? handle.Asset : null;
        private void CommitLoading()
        {
            var clips = new NovelBgmClips { Intro = ClipOf(_intro), Loop = ClipOf(_loop), Climax = ClipOf(_climax), Outro = ClipOf(_outro) };
            if (!clips.Loop)
            {
                _error = "分段 BGM 循环段加载失败：" + (_loop?.Error ?? _loading?.LoopPath);
                _loading = null; Status = NovelBgmStatus.Failed; return;
            }
            var runner = EnsureRunner();
            if (runner?.Director == null) { _loading = null; return; }
            var track = _loading;
            if (!runner.Director.Play(track, clips, _loadingVolume, _loadingFade, _loadingRestart))
            {
                _error = "分段 BGM 无法开始播放：" + track.Id;
                _loading = null; Status = NovelBgmStatus.Failed; return;
            }
            if (track.ClimaxPath != null && clips.Climax == null)
                EmberDebug.LogWarning(TAG, "曲目 " + track.Id + " 的高潮段加载失败，高潮指令会被忽略。");
            if (track.OutroPath != null && clips.Outro == null)
                EmberDebug.LogWarning(TAG, "曲目 " + track.Id + " 的尾段加载失败，收尾会退化为淡出停止。");
            _loading = null; _error = null; Status = NovelBgmStatus.Playing;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>
        /// 取当前已装配的分段 BGM 通道，供会话构造处显式注入。
        /// 用装配状态而不是静态单例：单例在 Play Mode 退出与热重启后会残留成半死对象，
        /// 隐式取用会让会话行为依赖静态状态。模块未装配时返回 null，调用方退回单文件循环。
        /// </summary>
        public static INovelBgmChannel Active =>
            EmberModuleCollector.TryGetInstance(out var collector) && collector.TryGetModule(out NovelBgmModule module)
                ? module : null;
        public void OnInit() { Status = NovelBgmStatus.Idle; _error = null; }
        /// <summary>
        /// 请求播放一首曲目：先播前奏（未配前奏则直接进循环），前奏播完自动接循环。
        /// 同曲目已在播时幂等（不重播前奏），restart 为 true 时强制从头重播。
        /// </summary>
        public bool Play(NovelBgmTrack track, float volume = 1, float fade = 0, bool restart = false)
        {
            if (track == null) { _error = "分段 BGM 曲目为空"; Status = NovelBgmStatus.Failed; return false; }
            if (!restart && Status == NovelBgmStatus.Playing && Director != null &&
                string.Equals(Director.TrackId, track.Id, StringComparison.Ordinal)) return true;
            ReleaseHandles();
            _loading = track; _loadingVolume = Mathf.Clamp01(volume); _loadingFade = Mathf.Max(0, fade); _loadingRestart = restart;
            _error = null; Status = NovelBgmStatus.Loading;
            var manager = EmberResourceManager.Instance;
            if (track.HasIntro) _intro = manager.LoadAssetHandle<AudioClip>(track.IntroPath);
            _loop = manager.LoadAssetHandle<AudioClip>(track.LoopPath);
            if (track.HasClimax) _climax = manager.LoadAssetHandle<AudioClip>(track.ClimaxPath);
            if (track.HasOutro) _outro = manager.LoadAssetHandle<AudioClip>(track.OutroPath);
            return true;
        }
        /// <summary>立即切入高潮段，播完自动回循环；没有曲目或未配高潮段时安全忽略。</summary>
        public bool Climax(float fade = NovelBgmRules.CLIMAX_FADE) => Director?.Climax(fade) == true;
        /// <summary>收尾：切到尾段并让它在播完后停下；未配尾段时退化为淡出停止。</summary>
        public bool FinishWithOutro(float fade = 0)
        {
            var playing = Director?.FinishWithOutro(fade) == true;
            if (!playing) ReleaseHandles();
            return playing;
        }
        public void Stop(float fade = 0)
        {
            ReleaseHandles(); _error = null; Status = NovelBgmStatus.Idle;
            Director?.Stop(fade);
        }
        /// <summary>同步玩家 BGM 音量；接了 Mixer 分组时由分组承担，这里只作为无分组时的回退。</summary>
        public void SetPlayerVolume(float volume)
        {
            _playerVolume = Mathf.Clamp01(volume);
            if (_runner) _runner.PlayerVolume = _playerVolume;
        }
        public void Update()
        {
            if (Status == NovelBgmStatus.Loading)
            {
                if (!Settled(_intro) || !Settled(_loop) || !Settled(_climax) || !Settled(_outro)) return;
                CommitLoading();
                return;
            }
            // 尾段播完会自己收摊；这里把对外状态同步回 Idle，避免门面与播放器说法不一致。
            if (Status == NovelBgmStatus.Playing && Director?.IsPlaying != true) Status = NovelBgmStatus.Idle;
        }
        void IEmberModule.OnDestroy() { ReleaseHandles(); _error = null; Status = NovelBgmStatus.Idle; }
        /// <summary>
        /// 热重启只清请求状态：正在播的曲目（尤其是正在收尾的尾段）不能被一次模块重置切断。
        /// </summary>
        public void ResetModuleData()
        {
            if (Status == NovelBgmStatus.Loading) { ReleaseHandles(); Status = NovelBgmStatus.Idle; }
            _error = null;
        }
        #endregion
    }
}
