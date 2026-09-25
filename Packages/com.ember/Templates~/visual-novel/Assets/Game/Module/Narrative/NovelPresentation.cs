using System;
using System.Collections.Generic;
using Ember.Audio;
using Ember.Resource;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NovelPreparedGameRequest
    {
        public NovelSession Session { get; }
        public NovelPreparedGameRequest(NovelSession session) { Session = session; }
    }
    public sealed class NovelNewGameRequest
    {
        public string StoryPath { get; }
        public string StoryId { get; }
        public string ChapterId { get; }
        public string NodeId { get; }
        public NovelNewGameRequest(string storyPath = NarrativeLibrarySO.CURRENT_STORY, string chapterId = null, string nodeId = null, string storyId = null)
        {
            // 目录迁移前的存档仍保存旧 Resources 路径；加载时归一化，后续存档写入新路径。
            StoryPath = storyPath != null && storyPath.StartsWith("VisualNovel/", StringComparison.Ordinal)
                ? "Config/Narrative/" + storyPath.Substring("VisualNovel/".Length)
                : storyPath;
            ChapterId = chapterId; NodeId = nodeId; StoryId = storyId;
        }
    }

    /// <summary>正式 EUI 只显示数据与收集意图；不持有剧情连接。</summary>
    public interface INovelView
    {
        int TextLength { get; }
        void Render(NarrativeSnapshot snapshot, NovelCommand command, string speaker, int visibleCharacters, string status);
        void Visual(NovelCommand command, Sprite sprite, float progress);
        void ClearVisuals();
    }

    public interface INovelAssetLease<T> : IDisposable where T : UnityEngine.Object
    {
        bool IsDone { get; }
        T Asset { get; }
        string Error { get; }
    }

    public interface INovelResources
    {
        INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object;
    }

    public interface INovelStoryResources
    {
        INovelAssetLease<NarrativeStorySO> LoadStory(string path, string storyId);
    }

    public sealed class NovelResources : INovelResources, INovelStoryResources
    {
        private sealed class Lease<T> : INovelAssetLease<T> where T : UnityEngine.Object
        {
            private EmberAssetHandle<T> _handle;
            public bool IsDone => _handle?.IsDone == true;
            public T Asset => _handle?.Asset;
            public string Error => _handle?.Error;
            public Lease(string path) { _handle = EmberResourceManager.Instance.LoadAssetHandle<T>(path); }
            public void Dispose() { _handle?.Dispose(); _handle = null; }
        }
        private sealed class StoryLease : INovelAssetLease<NarrativeStorySO>
        {
            private NarrativeLibrarySO _library;
            public bool IsDone => true;
            public NarrativeStorySO Asset { get; private set; }
            public string Error => Asset ? null : "小说入口或存档对应的小说未登记，请打开 Ember/视觉小说/当前小说。";
            public StoryLease(NarrativeLibrarySO library, NarrativeStorySO story) { _library = library; Asset = story; }
            public void Dispose() { Asset = null; _library = null; }
        }
        public INovelAssetLease<NarrativeStorySO> LoadStory(string path, string storyId)
        {
            var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
            var story = library && !string.IsNullOrEmpty(storyId) ? library.Find(storyId) : null;
            if (story) return new StoryLease(library, story);
            if (path == NarrativeLibrarySO.CURRENT_STORY)
                return new StoryLease(library, library && string.IsNullOrEmpty(storyId) ? library.Current : null);
            return new Lease<NarrativeStorySO>(path);
        }
        public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(NarrativeStorySO))
                return (INovelAssetLease<T>)(object)LoadStory(path, null);
            return new Lease<T>(path);
        }
    }

    public interface INovelAudio : IDisposable
    {
        bool VoicePlaying { get; }
        event Action VoiceCompleted;
        void Play(NovelCommandKind kind, AudioClip clip);
        void Tick();
        void StopVoice();
        void SetPaused(bool paused);
        void SetVolumes(float bgm, float sfx, float voice);
    }

    /// <summary>会话内音频适配；BGM 归当前阅读会话，SFX 与单路 Voice 通过独立句柄持有。</summary>
    public sealed class NovelAudio : INovelAudio, INovelLoopAudio
    {
        #region 内部参数
        private readonly List<EmberAudioPlayback> _effects = new();
        private bool _ownsBgm;
        private EmberAudioPlayback _voice;
        private float _voiceVolume = 1f;
        public bool VoicePlaying => _voice != null && !_voice.IsFinished;
        public event Action VoiceCompleted;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public INovelLoopPlayback CreateLoop(AudioClip clip)
        {
            if (!EmberAudioManager.Instance.IsInitialized) throw new InvalidOperationException("音频 Manager 尚未就绪");
            return new NovelLoopPlayback(Ember.Core.GameLauncher.Instance.AudioHost.transform, clip, false);
        }
        public void Play(NovelCommandKind kind, AudioClip clip)
        {
            var audio = EmberAudioManager.Instance;
            if (!audio.IsInitialized) throw new InvalidOperationException("音频 Manager 尚未就绪");
            if (kind == NovelCommandKind.BGM) { audio.PlayBGM(clip); _ownsBgm = true; }
            else if (kind == NovelCommandKind.Voice)
            {
                StopVoice(); _voice = audio.PlayOwnedVoice(clip, _voiceVolume);
                _voice.Completed += OnVoiceCompleted;
            }
            else _effects.Add(audio.PlayOwnedSFX(clip));
        }
        private void OnVoiceCompleted() { VoiceCompleted?.Invoke(); }
        public void Tick()
        {
            _voice?.Tick();
            for (int i = _effects.Count - 1; i >= 0; i--)
                if (_effects[i].IsFinished) { _effects[i].Dispose(); _effects.RemoveAt(i); }
        }
        public void StopVoice() { _voice?.Dispose(); _voice = null; }
        public void SetVolumes(float bgm, float sfx, float voice)
        {
            _voiceVolume = voice; _voice?.SetVolume(voice);
            foreach (var effect in _effects) effect.SetVolume(sfx);
            if (EmberAudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
            { audio.SetBGMVolume(bgm); audio.SetSFXVolume(sfx); }
        }
        public void SetPaused(bool paused)
        {
            _voice?.SetPaused(paused);
            if (_ownsBgm) EmberAudioManager.Instance.SetBGMPaused(paused);
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].IsFinished) { _effects[i].Dispose(); _effects.RemoveAt(i); }
                else _effects[i].SetPaused(paused);
            }
        }
        public void Dispose()
        {
            StopVoice(); VoiceCompleted = null;
            foreach (var effect in _effects) effect.Dispose();
            _effects.Clear();
            if (_ownsBgm && EmberAudioManager.TryGetInstance(out var audio)) audio.StopBGM();
            _ownsBgm = false;
        }
        #endregion
    }
}
