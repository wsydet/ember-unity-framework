using System.Collections.Generic;
using UnityEngine;

namespace Ember.UI
{
    [DisallowMultipleComponent]
    public sealed class EUIBackdropGroup : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private EUIBackdropSettings _settings;
        [SerializeField] private bool _autoAcquire = true;
        #endregion

        #region 内部参数
        private readonly List<EUIBackdropGraphic> _graphics = new();
        private IEUIBackdropLease _lease;
        private bool _owned;
        private bool _external;
        private int _generation;
        private int _width, _height;
        private Canvas _canvas;
        private CanvasGroup _visibility;
        public IEUIBackdropLease Lease => _lease;
        #endregion

        #region 生命周期
        private void OnEnable() { _canvas=GetComponent<Canvas>();_visibility=GetComponent<CanvasGroup>(); }
        private void LateUpdate()
        {
            if ((_canvas && (!_canvas.enabled || (_canvas.worldCamera && _canvas.planeDistance >= _canvas.worldCamera.farClipPlane))) ||
                (_visibility && _visibility.alpha<=0))
            { if(_owned)Release();return; }
            if (_graphics.Count == 0) RefreshSurfaces();
            bool visibleSurface = false;
            foreach (var graphic in _graphics) if (graphic && graphic.isActiveAndEnabled) { visibleSurface = true; break; }
            if (!visibleSurface) { if (_owned) Release(); return; }
            if (_lease == null && _autoAcquire && !_external && EUIBackdrop.Provider != null)
            {
                var canvas = GetComponent<Canvas>();
                if (!canvas || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return;
                var settings = _settings ? _settings : EUIBackdrop.DefaultSettings;
                if (settings && settings.UpdateMode == EUIBackdropUpdateMode.Off) return;
                _lease = EUIBackdrop.Provider.Acquire(new EUIBackdropRequest(canvas, canvas.sortingOrder, settings, ++_generation));
                _owned = _lease != null;
                RefreshSurfaces();
            }
            if (_width != Screen.width || _height != Screen.height)
            { _width = Screen.width; _height = Screen.height; _lease?.Invalidate(); }
        }

        private void OnDisable() { Release(); }
        private void OnTransformChildrenChanged() { RefreshSurfaces(); EUIBackdrop.MarkOrderDirty(); }
        #endregion

        #region 外部方法
        public void SetExternalLease(IEUIBackdropLease lease)
        {
            Release(); _external = true; _lease = lease; _owned = false; RefreshSurfaces();
        }

        public void UseOwnLease() { Release(); _external = false; }

        public void RefreshSurfaces()
        {
            _graphics.Clear(); GetComponentsInChildren(true, _graphics);
            for (int i = _graphics.Count - 1; i >= 0; i--)
            {
                var graphic = _graphics[i];
                if (graphic.GetComponentInParent<EUIBackdropGroup>(true) != this) { _graphics.RemoveAt(i); continue; }
                graphic.SetLease(_lease);
            }
        }

        public void Release()
        {
            foreach (var graphic in _graphics) if (graphic) graphic.SetLease(null);
            if (_owned) _lease?.Dispose();
            _lease = null; _owned = false; _external = false; _generation++;
        }
        #endregion
    }
}
