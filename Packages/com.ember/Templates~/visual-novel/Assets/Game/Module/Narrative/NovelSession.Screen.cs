using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private Color _coverColor = Color.clear;
        private bool _coverWholeReader;
        private NovelEffectPreference _shakePreference, _flashPreference;
        private NovelCommand _crossFadeLoading;
        private INovelAssetLease<Sprite> _crossFadeSprite;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool PrepareCrossFade(NovelCommand command, NarrativeSnapshot snapshot)
        {
            if (_crossFadeLoading != command)
            {
                string id = command.TargetKind == NovelTargetKind.Background ? "background" : command.InstanceId;
                if (Target(command.TargetKind, id) == null) throw new InvalidOperationException("交叉淡化目标为空，请先显示：" + id);
                if (command.Kind == NovelCommandKind.Wipe && _view is not INovelWipeView) throw new InvalidOperationException("页面未提供擦除转场适配");
                if (_view is not INovelScreenView) throw new InvalidOperationException("页面未提供交叉淡化适配");
                if (!_catalog.TryResolve(NovelScreenRules.ResourceKind(command), command.ResourceKey, out string path))
                    throw new InvalidOperationException("交叉淡化图片键不存在：" + command.ResourceKey);
                _crossFadeLoading = command; _crossFadeSprite = Load<Sprite>(path);
                _runner.SetPresentationWait(snapshot.SessionGeneration, snapshot.PositionVersion, NarrativeWait.Resource);
                return false;
            }
            if (!_crossFadeSprite.IsDone) return false;
            if (!_crossFadeSprite.Asset) throw new InvalidOperationException("交叉淡化图片加载失败：" + command.ResourceKey + " · " + _crossFadeSprite.Error);
            return true;
        }
        private NovelActionHandle StartScreenAction(NovelCommand c, NarrativeSnapshot snapshot)
        {
            if (_view is not INovelScreenView view) throw new InvalidOperationException("页面未提供画面演出适配：" + c.Kind);
            bool mask = c.Kind == NovelCommandKind.Cover || c.Kind == NovelCommandKind.Flash;
            var kind = mask ? NovelTargetKind.Mask : c.TargetKind;
            string id = mask ? "cover" : kind == NovelTargetKind.Stage ? "stage" : kind == NovelTargetKind.Background ? "background" : c.InstanceId;
            var target = mask || kind == NovelTargetKind.Stage ? null : Target(kind, id);
            if (!mask && kind != NovelTargetKind.Stage && target == null) throw new InvalidOperationException("画面动作目标不存在：" + id);
            string property = mask ? "Cover" : c.Kind == NovelCommandKind.Wipe ? "CrossFade" : c.Kind.ToString();
            // Cancellation settles a previous blend before its replacement reads the new baseline.
            CancelTarget(kind, id, property);
            var action = new NovelActionHandle { Generation = snapshot.SessionGeneration, Sequence = ++_actionSequence,
                Id = c.ActionId, Kind = c.Kind, Property = property, TargetKind = kind, TargetId = id, Slot = target?.Slot ?? default,
                Duration = c.Duration + (mask ? c.Hold : 0), FadeDuration = c.Duration, Hold = c.Hold, Delay = c.Delay, Ease = c.Ease,
                WipeDirection = c.WipeDirection, Strength = c.Strength, Frequency = c.Frequency, Decay = c.Decay, VectorTo = c.Direction.normalized,
                ColorFrom = _coverColor, ColorTo = new Color(c.Color.r, c.Color.g, c.Color.b, c.Color.a * c.Opacity),
                WholeReader = c.WholeReader, PreviousWholeReader = _coverWholeReader };
            if (c.Kind == NovelCommandKind.CrossFade || c.Kind == NovelCommandKind.Wipe)
            {
                action.OldKey = target.Key; action.NewKey = c.ResourceKey;
                view.BeginCrossFade(kind, target.Slot, _crossFadeSprite.Asset);
                if (c.Kind == NovelCommandKind.Wipe) ((INovelWipeView)view).SetWipe(0, c.WipeDirection);
                target.Key = c.ResourceKey;
                if (kind == NovelTargetKind.Character)
                    target.CharacterId = _catalog.Portraits.FirstOrDefault(p => p.Id == c.ResourceKey)?.CharacterId;
                _crossFadeLoading = null; _crossFadeSprite = null;
            }
            _actions[c.ActionId] = action; _runningActions.Add(action);
            if (c.Kind == NovelCommandKind.CrossFade || c.Kind == NovelCommandKind.Wipe) RefreshEmphasis();
            return action;
        }
        private void SetCover(Color color, bool wholeReader)
        {
            _coverColor = color; _coverWholeReader = wholeReader;
            (_view as INovelScreenView)?.SetCover(color, wholeReader);
        }
        private void WriteScreenAction(NovelActionHandle a, float progress)
        {
            if (_disposed || _view is not INovelScreenView view) return;
            if (a.TargetKind == NovelTargetKind.Character && Target(a.TargetKind, a.TargetId) == null)
            { CancelScreenAction(a); a.Status = NovelActionStatus.Cancelled; return; }
            float elapsed = Mathf.Max(0, a.Elapsed - a.Delay);
            switch (a.Kind)
            {
                case NovelCommandKind.Shake:
                    float pulse = progress >= 1 ? 0 : Mathf.Sin(elapsed * a.Frequency * Mathf.PI * 2) *
                        (a.Decay ? 1 - progress : 1) * .02f * a.Strength * NovelScreenRules.PreferenceScale(_shakePreference);
                    view.SetShake(a.TargetKind, a.Slot, a.VectorTo * pulse); break;
                case NovelCommandKind.Cover:
                    float p = progress >= 1 || a.FadeDuration <= 0 ? 1 : Mathf.Clamp01(elapsed / a.FadeDuration);
                    SetCover(Color.Lerp(a.ColorFrom, a.ColorTo, NovelActorRules.Ease(a.Ease, p)), a.WholeReader); break;
                case NovelCommandKind.Flash:
                    if (progress >= 1) { SetCover(a.ColorFrom, a.PreviousWholeReader); break; }
                    float half = a.FadeDuration * .5f;
                    float flash = half <= 0 ? 1 : elapsed < half ? elapsed / half :
                        elapsed <= half + a.Hold ? 1 : 1 - (elapsed - half - a.Hold) / half;
                    flash = NovelActorRules.Ease(a.Ease, Mathf.Clamp01(flash)) * NovelScreenRules.PreferenceScale(_flashPreference);
                    // Disabled flashes still consume the same duration, with the previous stable cover intact.
                    view.SetCover(Color.Lerp(a.ColorFrom, a.ColorTo, flash), flash <= 0 ? a.PreviousWholeReader : a.WholeReader); break;
                case NovelCommandKind.Wipe:
                    ((INovelWipeView)view).SetWipe(NovelActorRules.Ease(a.Ease, progress), a.WipeDirection);
                    if (progress >= 1) view.EndCrossFade(a.TargetKind, a.Slot); break;
                case NovelCommandKind.CrossFade:
                    view.SetCrossFade(a.TargetKind, a.Slot, NovelActorRules.Ease(a.Ease, progress));
                    if (progress >= 1) view.EndCrossFade(a.TargetKind, a.Slot); break;
            }
        }
        private void CancelScreenAction(NovelActionHandle a)
        {
            if (_view is not INovelScreenView view) return;
            if (a.Kind == NovelCommandKind.Shake) view.SetShake(a.TargetKind, a.Slot, Vector2.zero);
            else if (a.Kind == NovelCommandKind.Flash) SetCover(a.ColorFrom, a.PreviousWholeReader);
            else if ((a.Kind == NovelCommandKind.CrossFade || a.Kind == NovelCommandKind.Wipe)) view.EndCrossFade(a.TargetKind, a.Slot);
        }
        private void PruneVisualResources()
        {
            if (_disposed || _restoring) return;
            var paths = new HashSet<string>(StringComparer.Ordinal);
            void Keep(NovelCommandKind kind, string key)
            { if (!string.IsNullOrEmpty(key) && _catalog != null && _catalog.TryResolve(kind, key, out string path)) paths.Add(path); }
            foreach (var v in _visualStates) Keep(v.Kind, v.Key);
            if (_pendingOldVisual != null) Keep(_pendingOldVisual.Kind, _pendingOldVisual.Key);
            foreach (var a in _runningActions)
                if (!a.IsFinished && (a.Kind == NovelCommandKind.CrossFade || a.Kind == NovelCommandKind.Wipe))
                { var kind = a.TargetKind == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character;
                    Keep(kind, a.OldKey); Keep(kind, a.NewKey); }
            if (_crossFadeLoading != null) Keep(NovelScreenRules.ResourceKind(_crossFadeLoading), _crossFadeLoading.ResourceKey);
            if (_presenting != null && _presenting.VisualAction != NovelVisualAction.Hide &&
                (_presenting.Kind == NovelCommandKind.Background || _presenting.Kind == NovelCommandKind.Character))
                Keep(_presenting.Kind, _presenting.ResourceKey);
            foreach (var key in _resourceCache.Keys.Where(k => k.Item1 == typeof(Sprite) && !paths.Contains(k.Item2)).ToArray())
            { var lease = _resourceCache[key]; _resourceCache.Remove(key); _leases.Remove(lease); lease.Dispose(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void ConfigureScreenEffects(NovelEffectPreference shake, NovelEffectPreference flash)
        {
            _shakePreference = Enum.IsDefined(typeof(NovelEffectPreference), shake) ? shake : NovelEffectPreference.Normal;
            _flashPreference = Enum.IsDefined(typeof(NovelEffectPreference), flash) ? flash : NovelEffectPreference.Normal;
            if (_disposed) return;
            foreach (var a in _runningActions)
                if (!a.IsFinished && a.Elapsed >= a.Delay && (a.Kind == NovelCommandKind.Shake || a.Kind == NovelCommandKind.Flash))
                    WriteScreenAction(a, a.Progress);
        }
        #endregion
    }
}
