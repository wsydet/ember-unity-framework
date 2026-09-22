using System.Linq;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelReaderPage
    {
        #region 内部参数
        private RectTransform[] _actorRects;
        private readonly Vector2[] _actorMin = new Vector2[3], _actorMax = new Vector2[3], _actorPosition = new Vector2[3];
        private readonly Vector3[] _actorScale = new Vector3[3];
        private readonly Quaternion[] _actorRotation = new Quaternion[3];
        private readonly int[] _actorOrder = new int[3], _actorLayer = new int[3];
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CacheActorLayout()
        {
            _actorRects = new[] { Left, Center, Right };
            for (int i = 0; i < 3; i++)
            {
                var rect = _actorRects[i]; _actorMin[i] = rect.anchorMin; _actorMax[i] = rect.anchorMax;
                _actorPosition[i] = rect.anchoredPosition; _actorScale[i] = rect.localScale; _actorPoseScale[i] = rect.localScale;
                _actorRotation[i] = rect.localRotation; _actorOrder[i] = rect.GetSiblingIndex();
            }
        }
        private void SortActors()
        {
            // Only permute the three existing actor slots. Background and reading UI retain their layers.
            var sorted = Enumerable.Range(0, 3).OrderBy(i => _actorLayer[i]).ThenBy(i => i).ToArray();
            var indices = _actorOrder.OrderBy(i => i).ToArray();
            for (int i = 0; i < 3; i++) _actorRects[sorted[i]].SetSiblingIndex(indices[i]);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public Vector2 NamedPosition(NovelPortraitSlot slot)
        {
            int i = (int)slot;
            return Vector2.Lerp(_actorMin[i], _actorMax[i], .5f);
        }
        public void ApplyActor(NovelVisualState state, Vector2 gestureOffset, float gestureRotation)
        {
            int i = (int)state.Slot; var rect = _actorRects[i];
            _actorPoseScale[i] = Vector3.Scale(_actorScale[i], new Vector3(state.Scale.x * (state.Mirror ? -1 : 1), state.Scale.y, 1));
            _actorDisplacement[i] = state.Offset + gestureOffset;
            ApplyActorDisplacement(i);
            rect.anchoredPosition = _actorPosition[i] * _viewCameraZoom;
            rect.localRotation = _actorRotation[i] * Quaternion.Euler(0, 0, state.Rotation + gestureRotation);
            if (_actorLayer[i] != state.Layer) { _actorLayer[i] = state.Layer; SortActors(); }
            ((EUINovelPortraitItem)_visuals[i + 1].Logic).SetBrightness(state.Brightness);
            if (_blendItems[i + 1] != null) ((EUINovelPortraitItem)_blendItems[i + 1].Logic).SetBrightness(state.Brightness);
        }
        public void ResetActor(NovelPortraitSlot slot)
        {
            if (_actorRects == null) return;
            int i = (int)slot; var rect = _actorRects[i];
            _actorPoseScale[i] = _actorScale[i];
            _actorDisplacement[i] = _actorShake[i] = Vector2.zero; ApplyActorDisplacement(i);
            rect.anchoredPosition = _actorPosition[i] * _viewCameraZoom; rect.localRotation = _actorRotation[i];
            _actorLayer[i] = 0; SortActors();
            if (_visuals.Count > i + 1) ((EUINovelPortraitItem)_visuals[i + 1].Logic).SetBrightness(1);
        }
        #endregion
    }
}
