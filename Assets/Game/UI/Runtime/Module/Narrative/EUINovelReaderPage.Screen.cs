using Ember.UI;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelReaderPage : INovelScreenView
    {
        #region 内部参数
        private readonly EUIItem[] _blendItems = new EUIItem[4];
        private readonly bool[] _blending = new bool[4];
        private readonly float[] _blendProgress = new float[4];
        private EUIItem _coverItem;
        private Vector2 _stageShake, _backgroundMin, _backgroundMax;
        private readonly Vector2[] _actorShake = new Vector2[3], _actorDisplacement = new Vector2[3];
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void InitScreenEffects()
        {
            _backgroundMin = Background.anchorMin; _backgroundMax = Background.anchorMax;
            _cameraBackgroundPosition = Background.anchoredPosition; _cameraBackgroundScale = Background.localScale;
            // Runtime instances of the existing development-center-authored Items, like ChoiceTemplate.
            // No new prefab skeleton, generated binding or parallel registration is introduced.
            _coverItem = CloneScreenItem(Background, Background.parent, "Cover");
            var roots = new[] { Background, Left, Center, Right };
            for (int i = 0; i < roots.Length; i++)
                _blendItems[i] = CloneScreenItem(roots[i], roots[i], "CrossFade");
            ClearScreenEffects();
        }
        private EUIItem CloneScreenItem(RectTransform source, Transform parent, string name)
        {
            var root = Object.Instantiate(source.gameObject, parent);
            root.name = name;
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            return CreateItem(root);
        }
        private void ClearBlend(int index)
        {
            if (index == 0) ResetWipe();
            _blending[index] = false; _blendProgress[index] = 0;
            if (_blendItems[index] == null) return;
            if (index == 0) ((EUINovelBackgroundItem)_blendItems[index].Logic).Clear();
            else ((EUINovelPortraitItem)_blendItems[index].Logic).Clear();
        }
        private void ClearScreenEffects()
        {
            _stageShake = Vector2.zero;
            _viewCameraZoom = 1; _viewCameraOffset = Vector2.zero;
            if (Background) ApplyCameraBackground();
            for (int i = 0; i < 3; i++) { _actorShake[i] = Vector2.zero; ApplyActorDisplacement(i); }
            for (int i = 0; i < 4; i++) ClearBlend(i);
            if (_coverItem != null) ((EUINovelBackgroundItem)_coverItem.Logic).Clear();
        }
        private void DisposeScreenEffects()
        {
            foreach (var effect in _particleInstances.ToArray()) effect.Dispose();
            ClearScreenEffects();
            for (int i = 0; i < _blendItems.Length; i++)
            {
                var item = _blendItems[i]; _blendItems[i] = null;
                if (item == null) continue;
                var root = item.GameObject; item.Dispose(); DestroyScreenRoot(root);
            }
            if (_coverItem != null)
            { var root = _coverItem.GameObject; _coverItem.Dispose(); _coverItem = null; DestroyScreenRoot(root); }
        }
        private void DestroyScreenRoot(GameObject root)
        { if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root); }
        private void ApplyActorDisplacement(int i)
        {
            if (_actorRects == null) return;
            var offset = CameraShift(_actorRects[i], _actorMin[i], _actorMax[i], _actorDisplacement[i] + _actorShake[i]);
            _actorRects[i].anchorMin = _actorMin[i] + offset;
            _actorRects[i].anchorMax = _actorMax[i] + offset;
            _actorRects[i].anchoredPosition = _actorPosition[i] * _viewCameraZoom;
            _actorRects[i].localScale = Vector3.Scale(_actorPoseScale[i], new Vector3(_viewCameraZoom, _viewCameraZoom, 1));
        }
        private void ApplyBlendOpacity(int index, float alpha)
        {
            float p = _blending[index] ? _blendProgress[index] : 0;
            if (index == 0)
            {
                ((EUINovelBackgroundItem)_visuals[index].Logic).SetOpacity(alpha * (_backgroundWiping ? 1 : 1 - p));
                if (_blending[index]) ((EUINovelBackgroundItem)_blendItems[index].Logic).SetOpacity(alpha * (_backgroundWiping ? 1 : p));
            }
            else
            {
                ((EUINovelPortraitItem)_visuals[index].Logic).SetOpacity(alpha * (1 - p));
                if (_blending[index]) ((EUINovelPortraitItem)_blendItems[index].Logic).SetOpacity(alpha * p);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void SetShake(NovelTargetKind kind, NovelPortraitSlot slot, Vector2 offset)
        {
            if (kind == NovelTargetKind.Stage)
            {
                _stageShake = offset; ApplyCameraBackground();
                for (int i = 0; i < 3; i++) ApplyActorDisplacement(i);
            }
            else { _actorShake[(int)slot] = offset; ApplyActorDisplacement((int)slot); }
        }
        public void SetCover(Color color, bool wholeReader)
        {
            if (_coverItem == null) return;
            var rect = _coverItem.GameObject.transform;
            if (wholeReader) rect.SetAsLastSibling();
            else
            {
                // Remove from the ordering before computing the insertion position.
                rect.SetAsLastSibling();
                int top = Mathf.Max(Left.GetSiblingIndex(), Mathf.Max(Center.GetSiblingIndex(), Right.GetSiblingIndex()));
                foreach (var effect in _particleInstances) if (effect.Root && effect.ActorIndex < 0) top = Mathf.Max(top, effect.Root.transform.GetSiblingIndex());
                rect.SetSiblingIndex(top + 1);
            }
            ((EUINovelBackgroundItem)_coverItem.Logic).SetSolidColor(color);
        }
        public void BeginCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, Sprite next)
        {
            int i = kind == NovelTargetKind.Background ? 0 : (int)slot + 1;
            EndCrossFade(kind, slot);
            var command = new NovelCommand("blend", kind == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character);
            if (i == 0) ((EUINovelBackgroundItem)_blendItems[i].Logic).Apply(command, next, 1);
            else
            {
                var item = (EUINovelPortraitItem)_blendItems[i].Logic;
                item.SetBrightness(((EUINovelPortraitItem)_visuals[i].Logic).Brightness);
                item.Apply(command, next, 1);
            }
            _blending[i] = true; _blendProgress[i] = 0; ApplyOpacity(i);
        }
        public void SetCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, float progress)
        {
            int i = kind == NovelTargetKind.Background ? 0 : (int)slot + 1;
            if (!_blending[i]) return;
            _blendProgress[i] = Mathf.Clamp01(progress); ApplyOpacity(i);
        }
        public void EndCrossFade(NovelTargetKind kind, NovelPortraitSlot slot)
        {
            int i = kind == NovelTargetKind.Background ? 0 : (int)slot + 1;
            if (!_blending[i]) return;
            var command = new NovelCommand("blend-end", kind == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character);
            if (i == 0) ((EUINovelBackgroundItem)_visuals[i].Logic).Apply(command, ((EUINovelBackgroundItem)_blendItems[i].Logic).CurrentSprite, 1);
            else ((EUINovelPortraitItem)_visuals[i].Logic).Apply(command, ((EUINovelPortraitItem)_blendItems[i].Logic).CurrentSprite, 1);
            ClearBlend(i); ApplyOpacity(i);
        }
        #endregion
    }
}
