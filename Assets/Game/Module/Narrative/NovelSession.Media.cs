using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private sealed class Effect : IDisposable
        {
            internal NovelEffectState State;
            internal INovelAssetLease<GameObject> Lease;
            internal INovelEffectInstance Instance;
            public void Dispose() { try { Instance?.Dispose(); } finally { Lease?.Dispose(); } }
        }
        private sealed class Loop : IDisposable
        {
            internal NovelLoopState State;
            internal INovelAssetLease<AudioClip> Lease;
            internal INovelLoopPlayback Playback;
            internal float Gain;
            internal bool Stopping;
            public void Dispose() { try { Playback?.Dispose(); } finally { Lease?.Dispose(); } }
        }
        private readonly Dictionary<string, Effect> _effects = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Loop> _loops = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Loop> _outgoing = new(StringComparer.Ordinal);
        private NovelCommand _mediaLoading;
        private INovelAssetLease<GameObject> _effectLoading;
        private INovelAssetLease<AudioClip> _loopLoading;
        private NovelActionHandle _mediaWait;
        private float _playerBgm = 1, _playerSfx = 1;
        private string _mediaChapter;
        private readonly List<Effect> _restoreEffects = new();
        private readonly List<Loop> _restoreLoops = new();
        public int ActiveEffectCount => _effects.Count;
        public int ActiveLoopCount => _loops.Count + _outgoing.Count;
        public int MediaResourceCount => _effects.Count + ActiveLoopCount + (_effectLoading == null ? 0 : 1) + (_loopLoading == null ? 0 : 1) + _restoreEffects.Count + _restoreLoops.Count;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private string LoopId(NovelCommand c) => c.Kind == NovelCommandKind.BGM || c.Kind == NovelCommandKind.BGMStop ? "bgm" : "ambient:" + c.InstanceId;
        private float PlayerGain(Loop loop) => loop.State.Bgm ? _playerBgm : _playerSfx;
        private void RefreshLoopVolumes()
        {
            foreach (var loop in _loops.Values.Concat(_outgoing.Values)) loop.Playback?.SetVolume(loop.Gain * PlayerGain(loop));
        }
        private void PauseMedia(bool paused)
        {
            foreach (var loop in _loops.Values.Concat(_outgoing.Values)) loop.Playback?.SetPaused(paused);
            if (!paused) foreach (var a in _runningActions)
                if (a.Property == "Audio" && !a.IsFinished && a.Elapsed < a.Delay &&
                    (a.Kind == NovelCommandKind.BGM || a.Kind == NovelCommandKind.AmbientPlay) &&
                    _loops.TryGetValue(a.TargetId, out var pending)) pending.Playback.SetPaused(true);
        }
        private void StopEffect(string id)
        { if (_effects.Remove(id, out var effect)) effect.Dispose(); }
        private void StopBoundEffects(string actorId)
        {
            foreach (var id in _effects.Where(p => p.Value.State.BindingId == actorId).Select(p => p.Key).ToArray()) StopEffect(id);
        }
        private void RemoveLoop(string id)
        {
            if (_loops.Remove(id, out var loop)) loop.Dispose();
            if (_outgoing.Remove(id, out var old)) old.Dispose();
        }
        private void CancelMediaAction(string id)
        {
            foreach (var a in _runningActions)
                if (a.Property == "Audio" && a.TargetId == id) a.Status = NovelActionStatus.Cancelled;
            _runningActions.RemoveAll(a => a.IsFinished);
        }
        private void SceneChangeMedia()
        {
            foreach (var id in _effects.Where(p => !p.Value.State.KeepOnSceneChange || !p.Value.State.Persistent).Select(p => p.Key).ToArray()) StopEffect(id);
            foreach (var id in _loops.Where(p => !p.Value.State.Bgm && !p.Value.State.KeepOnSceneChange).Select(p => p.Key).ToArray())
            { CancelMediaAction(id); RemoveLoop(id); }
        }
        private void TickMedia(float delta)
        {
            foreach (var pair in _effects.ToArray())
            {
                var effect = pair.Value;
                if (!string.IsNullOrEmpty(effect.State.BindingId) && Target(NovelTargetKind.Character, effect.State.BindingId) == null)
                { StopEffect(pair.Key); continue; }
                if (_readMode == NarrativeReadMode.Skip && !effect.State.Persistent) { StopEffect(pair.Key); continue; }
                effect.Instance.Tick(delta); // Environment runs at normal speed, never at reading multiplier.
                if (!effect.State.Persistent && !effect.Instance.IsAlive) StopEffect(pair.Key);
            }
            foreach (var loop in _loops.Values.Concat(_outgoing.Values)) loop.Playback.Tick(delta);
        }
        private void ClearMedia()
        {
            void Release(IDisposable item)
            {
                try { item?.Dispose(); }
                catch (Exception ex) { Ember.Basic.EmberDebug.LogWarning("Game.Narrative", "E3 实例释放：" + ex.Message); }
            }
            foreach (var effect in _effects.Values) Release(effect);
            foreach (var loop in _loops.Values.Concat(_outgoing.Values)) Release(loop);
            foreach (var effect in _restoreEffects) Release(effect);
            foreach (var loop in _restoreLoops) Release(loop);
            _effects.Clear(); _loops.Clear(); _outgoing.Clear(); _restoreEffects.Clear(); _restoreLoops.Clear();
            Release(_effectLoading); Release(_loopLoading); _effectLoading = null; _loopLoading = null;
            _mediaLoading = null; _mediaWait = null; _bgmKey = null;
        }
        private void PresentMedia(NovelCommand c, NarrativeSnapshot snapshot)
        {
            if (_presenting != c)
            {
                if (NovelMediaRules.NeedsResource(c.Kind) && !(_readMode == NarrativeReadMode.Skip && c.Kind == NovelCommandKind.EffectPlay && !c.Persistent))
                {
                    if (_mediaLoading != c)
                    {
                        _mediaLoading = c;
                        if (!_catalog.TryResolve(c.Kind, c.ResourceKey, out var path)) throw new InvalidOperationException("E3 资源键无法解析：" + c.ResourceKey);
                        if (c.Kind == NovelCommandKind.EffectPlay) _effectLoading = _resources.Load<GameObject>(path);
                        else _loopLoading = _resources.Load<AudioClip>(path);
                        _runner.SetPresentationWait(snapshot.SessionGeneration, snapshot.PositionVersion, NarrativeWait.Resource); return;
                    }
                    if (_effectLoading != null && !_effectLoading.IsDone || _loopLoading != null && !_loopLoading.IsDone) return;
                    if (_effectLoading != null && !_effectLoading.Asset || _loopLoading != null && !_loopLoading.Asset)
                        throw new InvalidOperationException("E3 资源加载失败 [" + c.ResourceKey + "]：" + (_effectLoading?.Error ?? _loopLoading?.Error));
                }
                _presenting = c; _presentationVersion = snapshot.PositionVersion; _mediaWait = null;
                if (c.Kind == NovelCommandKind.EffectStop) StopEffect(c.InstanceId);
                else if (c.Kind == NovelCommandKind.EffectPlay)
                {
                    if (_readMode != NarrativeReadMode.Skip || c.Persistent)
                    {
                        if (!_effects.ContainsKey(c.InstanceId) && _effects.Count >= 64) throw new InvalidOperationException("效果实例超过 64 个");
                        var state = new NovelEffectState { Id = c.InstanceId, Key = c.ResourceKey, BindingId = c.BindingId,
                            Persistent = c.Persistent, KeepOnSceneChange = c.KeepOnSceneChange, Position = c.Position, Scale = c.Scale, Layer = c.Layer };
                        var instance = CreateEffect(_effectLoading.Asset, state);
                        try { StopEffect(c.InstanceId); }
                        catch { instance.Dispose(); throw; }
                        _effects.Add(c.InstanceId, new Effect { State = state, Instance = instance, Lease = _effectLoading }); _effectLoading = null;
                    }
                    else { _effectLoading?.Dispose(); _effectLoading = null; }
                }
                else _mediaWait = StartAudioAction(c, snapshot);
                _mediaLoading = null;
                if (_mediaWait != null) WriteAudioAction(_mediaWait, _readMode == NarrativeReadMode.Skip || c.Duration + c.Delay == 0 ? 1 : 0);
            }
            if (_mediaWait == null || c.Parallel || _mediaWait.IsFinished)
            { _presenting = null; _mediaWait = null; _runner.CompletePresentation(snapshot.SessionGeneration, _presentationVersion); }
            else _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Actions);
        }
        private INovelEffectInstance CreateEffect(GameObject prefab, NovelEffectState state)
        {
            if (_view is not INovelEffectView effects) throw new InvalidOperationException("页面缺少粒子效果适配");
            var actor = string.IsNullOrEmpty(state.BindingId) ? null : Target(NovelTargetKind.Character, state.BindingId);
            if (!string.IsNullOrEmpty(state.BindingId) && actor == null) throw new InvalidOperationException("效果绑定人物不存在：" + state.BindingId);
            return effects.CreateEffect(prefab, state.Copy(), actor?.Slot ?? default);
        }
        private NovelActionHandle StartAudioAction(NovelCommand c, NarrativeSnapshot snapshot)
        {
            if (_audio is not INovelLoopAudio audio) throw new InvalidOperationException("音频适配不支持独立循环音");
            string id = LoopId(c), actionId = NovelMediaRules.ActionId(c);
            if (_actions.TryGetValue(actionId, out var prior) && !prior.IsFinished) throw new InvalidOperationException("动作 ID 正在使用：" + actionId);
            if (!_actions.ContainsKey(actionId) && _actions.Count >= 4096) throw new InvalidOperationException("动作 ID 超过会话上限");
            if (c.Kind == NovelCommandKind.AmbientVolume && !_loops.ContainsKey(id)) throw new InvalidOperationException("环境音实例不存在：" + c.InstanceId);
            CancelMediaAction(id);
            if (_outgoing.Remove(id, out var outgoing)) outgoing.Dispose();
            bool play = c.Kind == NovelCommandKind.BGM || c.Kind == NovelCommandKind.AmbientPlay;
            bool stop = c.Kind == NovelCommandKind.BGMStop || c.Kind == NovelCommandKind.AmbientStop;
            _loops.TryGetValue(id, out var current);
            if (play)
            {
                if (current == null && _loops.Count >= 64) throw new InvalidOperationException("循环音实例超过 64 个");
                var playback = audio.CreateLoop(_loopLoading.Asset);
                try { playback.SetVolume(0); playback.SetPaused(_pauses.Count > 0 || c.Delay > 0); }
                catch { playback.Dispose(); throw; }
                if (current != null) { _outgoing[id] = current; _loops.Remove(id); }
                current = new Loop { State = new NovelLoopState { Id = c.InstanceId, Key = c.ResourceKey, Bgm = c.Kind == NovelCommandKind.BGM,
                    Volume = c.Volume, KeepOnSceneChange = c.KeepOnSceneChange }, Lease = _loopLoading, Playback = playback };
                _loops[id] = current; _loopLoading = null;
            }
            else if (current != null) { current.State.Volume = stop ? 0 : c.Volume; current.Stopping = stop; }
            if (id == "bgm") _bgmKey = stop ? null : c.ResourceKey;
            var a = new NovelActionHandle { Id = actionId, Generation = snapshot.SessionGeneration, Sequence = ++_actionSequence,
                Kind = c.Kind, Property = "Audio", TargetKind = NovelTargetKind.Stage, TargetId = id, Duration = c.Duration, Delay = c.Delay,
                Ease = c.Ease, From = current?.Gain ?? 0, To = stop ? 0 : c.Volume, Strength = _outgoing.TryGetValue(id, out var old) ? old.Gain : 0 };
            _actions[actionId] = a; _runningActions.Add(a); return a;
        }
        private void WriteAudioAction(NovelActionHandle a, float progress)
        {
            float p = NovelActorRules.Ease(a.Ease, progress);
            if (_loops.TryGetValue(a.TargetId, out var loop))
            {
                loop.Gain = Mathf.Lerp(a.From, a.To, p); loop.Playback.SetVolume(loop.Gain * PlayerGain(loop));
                loop.Playback.SetPaused(_pauses.Count > 0 || a.Elapsed < a.Delay && progress < 1 &&
                    (a.Kind == NovelCommandKind.BGM || a.Kind == NovelCommandKind.AmbientPlay));
            }
            if (_outgoing.TryGetValue(a.TargetId, out var old))
            { old.Gain = a.Strength * (1 - p); old.Playback.SetVolume(old.Gain * PlayerGain(old)); }
            if (progress < 1) return;
            if (_outgoing.Remove(a.TargetId, out old)) old.Dispose();
            if (loop?.Stopping == true) RemoveLoop(a.TargetId);
            a.Progress = 1; a.Status = NovelActionStatus.Completed;
        }
        private void CaptureMedia(NovelCheckpoint checkpoint)
        {
            checkpoint.PersistentEffects = _effects.Values.Where(e => e.State.Persistent &&
                (string.IsNullOrEmpty(e.State.BindingId) || checkpoint.Visuals.Any(v => v.InstanceId == e.State.BindingId)))
                .Select(e => e.State.Copy()).ToList();
            checkpoint.Loops = _loops.Values.Where(l => !l.Stopping).Select(l => l.State.Copy()).ToList();
        }
        private void PrepareMediaRestore()
        {
            if (_restore.SchemaVersion < 5) { _restore.PersistentEffects = new(); _restore.Loops = new(); }
            if (_restore.Loops != null && _restore.Loops.Count == 0 && !string.IsNullOrEmpty(_restore.BgmKey) && _audio is INovelLoopAudio)
                _restore.Loops.Add(new NovelLoopState { Bgm = true, Key = _restore.BgmKey, Volume = 1 });
            if (_restore.PersistentEffects == null || _restore.Loops == null || _restore.PersistentEffects.Count > 64 || _restore.Loops.Count > 64)
                throw new InvalidOperationException("存档持续实例列表无效");
            var ids = new HashSet<string>();
            foreach (var state in _restore.PersistentEffects)
            {
                if (state == null || !state.Persistent || !ids.Add(state.Id ?? "") ||
                    NovelMediaRules.Validate(new NovelCommand("restore", NovelCommandKind.EffectPlay, instanceId: state.Id, resourceKey: state.Key,
                        position: state.Position, scale: state.Scale, layer: state.Layer)) != null ||
                    !string.IsNullOrEmpty(state.BindingId) && !_restore.Visuals.Any(v => v.Kind == NovelCommandKind.Character && v.InstanceId == state.BindingId))
                    throw new InvalidOperationException("存档持续效果参数或人物绑定无效");
                _restoreEffects.Add(new Effect { State = state.Copy(), Lease = _resources.Load<GameObject>(NovelMediaRules.EffectPath(state.Key)) });
            }
            ids.Clear();
            foreach (var state in _restore.Loops)
            {
                if (state == null || !ids.Add(state.Bgm ? "bgm" : "ambient:" + state.Id) || !state.Bgm && string.IsNullOrWhiteSpace(state.Id) ||
                    !NovelActionHandle.ValidTime(state.Volume) || state.Volume > 1 ||
                    !_catalog.TryResolve(state.Bgm ? NovelCommandKind.BGM : NovelCommandKind.AmbientPlay, state.Key, out var path))
                    throw new InvalidOperationException("存档循环音参数或资源键无效");
                _restoreLoops.Add(new Loop { State = state.Copy(), Gain = state.Volume, Lease = _resources.Load<AudioClip>(path) });
            }
        }
        private bool MediaRestoreReady()
        {
            if (_restoreEffects.Any(e => !e.Lease.IsDone) || _restoreLoops.Any(l => !l.Lease.IsDone)) return false;
            foreach (var e in _restoreEffects)
            {
                string error = NovelMediaRules.ValidatePrefab(e.Lease.Asset);
                if (error != null) throw new InvalidOperationException("恢复效果失败：" + e.State.Key + "：" + error);
            }
            foreach (var l in _restoreLoops) if (!l.Lease.Asset) throw new InvalidOperationException("恢复循环音失败：" + l.State.Key);
            return true;
        }
        private void CommitMediaRestore()
        {
            foreach (var e in _restoreEffects) e.Instance = CreateEffect(e.Lease.Asset, e.State);
            foreach (var l in _restoreLoops)
            {
                if (_audio is not INovelLoopAudio audio) throw new InvalidOperationException("恢复页面不支持循环音");
                l.Playback = audio.CreateLoop(l.Lease.Asset); l.Playback.SetVolume(l.Gain * PlayerGain(l)); l.Playback.SetPaused(_pauses.Count > 0);
            }
            foreach (var e in _restoreEffects) _effects.Add(e.State.Id, e);
            foreach (var l in _restoreLoops) _loops.Add(l.State.Bgm ? "bgm" : "ambient:" + l.State.Id, l);
            _restoreEffects.Clear(); _restoreLoops.Clear(); _mediaChapter = _runner.Snapshot.ChapterId;
        }
        #endregion
    }
}
