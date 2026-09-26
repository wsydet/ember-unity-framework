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
        private float _playerBgm = 1, _playerSfx = 1, _bgmVolume = 1;
        /// <summary>恢复用的曲目与段落；由存档解析，提交时交给常驻通道。</summary>
        private NovelBgmTrack _restoreBgmTrack;
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
        /// <summary>
        /// 循环音音源应该写入的增益。
        /// 接到 Mixer 分组的循环音由分组承担玩家音量（与 BGM 的既有约定一致），
        /// 这里只写剧情增益，避免玩家音量被叠乘两次。
        /// </summary>
        private float LoopSourceGain(Loop loop) => loop.Gain * (loop.Playback != null && loop.Playback.MixerRouted ? 1 : PlayerGain(loop));
        private void RefreshLoopVolumes()
        {
            foreach (var loop in _loops.Values.Concat(_outgoing.Values)) loop.Playback?.SetVolume(LoopSourceGain(loop));
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
            _bgmVolume = 1; _restoreBgmTrack = null;
        }
        /// <summary>
        /// 解析曲目键：优先取分段表（novel_bgm）的四段拆分；
        /// 表里没有该键时退回旧的单文件 BGM 行，当作「只有循环段」的曲目，旧剧情因此零改动。
        /// </summary>
        private bool TryResolveBgmTrack(string key, out NovelBgmTrack track)
        {
            track = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (_catalog.TryGetBgm(key, out var row)) { track = NovelBgmRules.ToTrack(row); return track != null; }
            if (_catalog.TryResolve(NovelCommandKind.BGM, key, out var path))
            { track = new NovelBgmTrack(key, null, path, null, null); return true; }
            return false;
        }
        /// <summary>
        /// BGM 族指令：只发意图给常驻通道，不建立循环音实例，也不随会话暂停。
        /// 只有曲目资源仍在加载时才让演出等待；渐变本身不参与剧情等待（与既有循环音一致）。
        /// </summary>
        private void PresentBgm(NovelCommand c, NarrativeSnapshot snapshot)
        {
            if (_presenting != c)
            {
                _presenting = c; _presentationVersion = snapshot.PositionVersion;
                switch (c.Kind)
                {
                    case NovelCommandKind.BGM:
                        if (!TryResolveBgmTrack(c.ResourceKey, out var track))
                            throw new InvalidOperationException("E3 BGM 曲目键无法解析：" + c.ResourceKey);
                        _bgmKey = c.ResourceKey; _bgmVolume = Mathf.Clamp01(c.Volume);
                        _bgm.Play(track, c.Volume, c.Duration);
                        break;
                    case NovelCommandKind.BGMClimax:
                        // 没有曲目、没配高潮段、或已经处在高潮/尾段时安全忽略：编排不该因此中断剧情。
                        _bgm.Climax(c.Duration > 0 ? c.Duration : NovelBgmRules.CLIMAX_FADE);
                        break;
                    case NovelCommandKind.BGMStop:
                        _bgmKey = null;
                        _bgm.Stop(c.Duration);
                        break;
                }
            }
            if (_bgm.Status == NovelBgmStatus.Loading)
            { _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Resource); return; }
            if (_bgm.Status == NovelBgmStatus.Failed && c.Kind == NovelCommandKind.BGM)
                throw new InvalidOperationException("E3 BGM 加载失败 [" + c.ResourceKey + "]：" + (_bgm.Error ?? "未知原因"));
            _presenting = null;
            _runner.CompletePresentation(snapshot.SessionGeneration, _presentationVersion);
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
                var playback = audio.CreateLoop(_loopLoading.Asset, c.Kind == NovelCommandKind.BGM);
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
                loop.Gain = Mathf.Lerp(a.From, a.To, p); loop.Playback.SetVolume(LoopSourceGain(loop));
                loop.Playback.SetPaused(_pauses.Count > 0 || a.Elapsed < a.Delay && progress < 1 &&
                    (a.Kind == NovelCommandKind.BGM || a.Kind == NovelCommandKind.AmbientPlay));
            }
            if (_outgoing.TryGetValue(a.TargetId, out var old))
            { old.Gain = a.Strength * (1 - p); old.Playback.SetVolume(LoopSourceGain(old)); }
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
            // 只剩环境音：BGM 已经不在会话的循环音字典里。
            checkpoint.Loops = _loops.Values.Where(l => !l.Stopping).Select(l => l.State.Copy()).ToList();
            // BGM 走常驻通道时以通道为唯一真源（曲目键与所在段落都在通道上）；
            // 没有通道的旧路径继续用会话自己记的键。
            checkpoint.BgmKey = _bgm != null ? _bgm.TrackId : _bgmKey;
            checkpoint.BgmSegment = _bgm != null ? _bgm.Segment : string.IsNullOrEmpty(_bgmKey) ? NovelBgmSegment.None : NovelBgmSegment.Loop;
            checkpoint.BgmVolume = Mathf.Clamp01(_bgmVolume);
        }
        private void PrepareMediaRestore()
        {
            if (_restore.SchemaVersion < 5) { _restore.PersistentEffects = new(); _restore.Loops = new(); }
            // Schema 8 起 BGM 单独记录曲目与段落；旧档只有 BgmKey，而旧曲目都没有前奏，
            // 所以旧档一律迁移成「停在循环段」。
            if (_restore.SchemaVersion < NovelCheckpoint.CurrentSchemaVersion)
                _restore.BgmSegment = string.IsNullOrEmpty(_restore.BgmKey) ? NovelBgmSegment.None : NovelBgmSegment.Loop;
            if (!NovelActionHandle.ValidTime(_restore.BgmVolume) || _restore.BgmVolume > 1)
                throw new InvalidOperationException("存档 BGM 音量无效");
            // 只有没有常驻通道时（编辑器试播、单测）才把 BGM 合成为会话内循环音。
            if (_bgm == null && _restore.Loops != null && _restore.Loops.Count == 0 && !string.IsNullOrEmpty(_restore.BgmKey) && _audio is INovelLoopAudio)
                _restore.Loops.Add(new NovelLoopState { Bgm = true, Key = _restore.BgmKey, Volume = _restore.BgmVolume });
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
                    !NovelActionHandle.ValidTime(state.Volume) || state.Volume > 1)
                    throw new InvalidOperationException("存档循环音参数或资源键无效");
                if (state.Bgm && _bgm != null)
                {
                    // Schema 5–7 的 BGM 循环音项并入曲目状态，交给常驻通道重建，
                    // 这样读档后的 BGM 与正常播放同属一条通道，不会再被会话暂停带走。
                    if (string.IsNullOrEmpty(_restore.BgmKey)) _restore.BgmKey = state.Key;
                    if (_restore.BgmSegment == NovelBgmSegment.None) _restore.BgmSegment = NovelBgmSegment.Loop;
                    _restore.BgmVolume = state.Volume;
                    if (!TryResolveBgmTrack(_restore.BgmKey, out _restoreBgmTrack))
                        throw new InvalidOperationException("存档 BGM 键缺失：" + _restore.BgmKey);
                    continue;
                }
                if (!_catalog.TryResolve(state.Bgm ? NovelCommandKind.BGM : NovelCommandKind.AmbientPlay, state.Key, out var path))
                    throw new InvalidOperationException("存档循环音参数或资源键无效");
                _restoreLoops.Add(new Loop { State = state.Copy(), Gain = state.Volume, Lease = _resources.Load<AudioClip>(path) });
            }
            if (_bgm != null && _restoreBgmTrack == null && _restore.BgmSegment != NovelBgmSegment.None &&
                !TryResolveBgmTrack(_restore.BgmKey, out _restoreBgmTrack))
                throw new InvalidOperationException("存档 BGM 键缺失：" + _restore.BgmKey);
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
            var loops = new List<Loop>();
            foreach (var l in _restoreLoops)
            {
                if (_audio is not INovelLoopAudio audio) throw new InvalidOperationException("恢复页面不支持循环音");
                l.Playback = audio.CreateLoop(l.Lease.Asset, l.State.Bgm); l.Playback.SetVolume(LoopSourceGain(l)); l.Playback.SetPaused(_pauses.Count > 0);
                loops.Add(l);
            }
            foreach (var e in _restoreEffects) _effects.Add(e.State.Id, e);
            foreach (var l in loops) _loops.Add(l.State.Bgm ? "bgm" : "ambient:" + l.State.Id, l);
            _restoreEffects.Clear(); _restoreLoops.Clear(); _mediaChapter = _runner.Snapshot.ChapterId;
            CommitBgmRestore();
        }
        /// <summary>
        /// 恢复 BGM：同一首曲目且同一段落时<b>完全不动</b>，音乐零中断（通道常驻，读档不该打断正在播的曲子）；
        /// 曲目或段落不同才重新进入曲目，因此会从新曲目的前奏开始。
        /// </summary>
        private void CommitBgmRestore()
        {
            var track = _restoreBgmTrack;
            _restoreBgmTrack = null;
            if (track == null || _bgm == null) return;
            _bgmVolume = Mathf.Clamp01(_restore.BgmVolume);
            bool unchanged = _bgm.Status == NovelBgmStatus.Playing &&
                string.Equals(_bgm.TrackId, track.Id, StringComparison.Ordinal) && _bgm.Segment == _restore.BgmSegment;
            if (unchanged) return;
            _bgm.Play(track, _bgmVolume, 0);
        }
        #endregion
    }
}
