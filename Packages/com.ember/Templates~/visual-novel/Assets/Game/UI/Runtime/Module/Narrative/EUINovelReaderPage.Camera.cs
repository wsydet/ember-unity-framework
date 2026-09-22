using Game.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUINovelReaderPage : INovelCameraView, INovelWipeView
    {
        #region 内部参数
        private Vector2 _viewCameraOffset, _cameraBackgroundPosition;
        private float _viewCameraZoom = 1;
        private Vector3 _cameraBackgroundScale;
        private readonly Vector3[] _actorPoseScale = new Vector3[3];
        private bool _backgroundWiping;
        private Image.Type _wipeOriginalType;
        private Image.FillMethod _wipeOriginalMethod;
        private int _wipeOriginalOrigin;
        private float _wipeOriginalAmount;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Vector2 CameraShift(RectTransform rect, Vector2 min, Vector2 max, Vector2 offset)
        {
            var pivot = min + Vector2.Scale(max - min, rect.pivot);
            return offset + (pivot + offset - Vector2.one * .5f) * (_viewCameraZoom - 1) + _viewCameraOffset + _stageShake;
        }
        private void ApplyCameraBackground()
        {
            Vector2 shift = CameraShift(Background, _backgroundMin, _backgroundMax, Vector2.zero);
            Background.anchorMin = _backgroundMin + shift; Background.anchorMax = _backgroundMax + shift;
            Background.anchoredPosition = _cameraBackgroundPosition * _viewCameraZoom;
            Background.localScale = Vector3.Scale(_cameraBackgroundScale, new Vector3(_viewCameraZoom, _viewCameraZoom, 1));
            foreach (var effect in _particleInstances) if (effect.Root && effect.ActorIndex < 0)
            {
                var rect = (RectTransform)effect.Root.transform;
                var effectShift = CameraShift(rect, Vector2.zero, Vector2.one, Vector2.zero);
                rect.anchorMin = effectShift; rect.anchorMax = Vector2.one + effectShift;
                rect.localScale = new Vector3(_viewCameraZoom, _viewCameraZoom, 1);
            }
        }
        private void ResetWipe()
        {
            if (!_backgroundWiping) return;
            _backgroundWiping = false;
            if (_blendItems[0] == null) return;
            var image = _blendItems[0].GameObject.GetComponentInChildren<Image>(true);
            image.type = _wipeOriginalType; image.fillMethod = _wipeOriginalMethod;
            image.fillOrigin = _wipeOriginalOrigin; image.fillAmount = _wipeOriginalAmount;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void SetCamera(Vector2 offset, float zoom)
        {
            if (!NovelCameraRules.Valid(offset, zoom)) throw new System.ArgumentOutOfRangeException(nameof(zoom), "舞台镜头参数无效");
            _viewCameraOffset = offset; _viewCameraZoom = zoom;
            ApplyCameraBackground();
            for (int i = 0; i < 3; i++) ApplyActorDisplacement(i);
        }
        public void SetWipe(float progress, NovelWipeDirection direction)
        {
            if (!_blending[0]) return;
            var image = _blendItems[0].GameObject.GetComponentInChildren<Image>(true);
            if (!_backgroundWiping)
            {
                _wipeOriginalType = image.type; _wipeOriginalMethod = image.fillMethod;
                _wipeOriginalOrigin = image.fillOrigin; _wipeOriginalAmount = image.fillAmount;
                _backgroundWiping = true;
            }
            image.type = Image.Type.Filled;
            image.fillMethod = direction == NovelWipeDirection.LeftToRight || direction == NovelWipeDirection.RightToLeft ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
            image.fillOrigin = direction == NovelWipeDirection.RightToLeft || direction == NovelWipeDirection.TopToBottom ? 1 : 0;
            image.fillAmount = Mathf.Clamp01(progress); ApplyOpacity(0);
        }
        #endregion
    }
}
