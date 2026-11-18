using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>每次借用独立所有权；回池不保留 RT、材质或页面回调。</summary>
    internal sealed class EUIBackdropMaskSession : MonoBehaviour
    {
        #region 内部参数
        private IEUIBackdropLease _lease;
        private EUIBackdropGraphic _surface;
        private int _generation;
        public IEUIBackdropLease Lease => _lease;
        #endregion

        #region 生命周期
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
        #endregion

        #region 外部方法
        public void Configure(Canvas foreground, int boundary, EUIMaskEffect effect, EUIBackdropSettings settings, Color tint)
        {
            Release();
            var image = GetComponent<Image>(); image.color = tint;
            var group = foreground ? foreground.GetComponent<EUIBackdropGroup>() : null;
            if (effect != EUIMaskEffect.Blur || (settings && settings.UpdateMode == EUIBackdropUpdateMode.Off))
            { group?.UseOwnLease(); return; }
            _lease = EUIBackdrop.Provider?.Acquire(new EUIBackdropRequest(foreground, boundary, settings, ++_generation));
            if (_lease == null) { image.color = settings ? settings.FallbackColor : new Color(.08f, .1f, .16f, .92f); group?.SetExternalLease(null); return; }
            if (!_surface)
            {
                var go = new GameObject("Blur Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(EUIBackdropGraphic));
                go.layer = gameObject.layer; go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                _surface = go.GetComponent<EUIBackdropGraphic>(); _surface.raycastTarget = false; _surface.EdgeLight = 0;
            }
            _surface.gameObject.SetActive(true); _surface.color = tint; _surface.SetLease(_lease);
            image.color = Color.clear;
            group?.SetExternalLease(_lease);
            EUIBackdrop.MarkOrderDirty();
        }

        public void Release()
        {
            _generation++;
            if (_surface) { _surface.SetLease(null); _surface.gameObject.SetActive(false); }
            _lease?.Dispose(); _lease = null;
        }
        #endregion
    }
}
