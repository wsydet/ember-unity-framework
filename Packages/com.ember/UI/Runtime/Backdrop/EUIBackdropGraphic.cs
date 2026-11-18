using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>屏幕空间背景表面；纹理由所属边界租约借入，不拥有 RT。</summary>
    [AddComponentMenu("Ember/UI/Backdrop Graphic"), RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    public class EUIBackdropGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        #region 编辑器面板参数
        [SerializeField, Min(0)] private float _radius;
        [SerializeField, Range(0, 3)] private float _edgeWidth = 1;
        [SerializeField, Range(0, 1)] private float _edgeLight = .18f;
        [SerializeField, Range(0, 1)] private float _gloss;
        [SerializeField, Range(0, 1)] private float _frost;
        [SerializeField] private bool _squareTop;
        #endregion

        #region 内部参数
        private Material _surfaceMaterial;
        private static Material _sharedMaterial;
        private static int _materialUsers;
        private bool _wasReady;
        private IEUIBackdropLease _lease;
        public float Radius { get => _radius; set { _radius = float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value); SetVerticesDirty(); } }
        public override Texture mainTexture => _lease != null && _lease.IsReady && _lease.Texture ? _lease.Texture : Texture2D.whiteTexture;
        public IEUIBackdropLease Lease => _lease;
        public float EdgeLight { get => _edgeLight; set { _edgeLight = Mathf.Clamp01(value); SetVerticesDirty(); } }
        public float Gloss { get => _gloss; set { _gloss = Mathf.Clamp01(value); SetVerticesDirty(); } }
        public float Frost { get => _frost; set { _frost = Mathf.Clamp01(value); SetVerticesDirty(); } }
        public bool SquareTop { get => _squareTop; set { _squareTop = value; SetVerticesDirty(); } }
        #endregion

        #region 生命周期
        protected override void OnEnable()
        {
            base.OnEnable();
            var shader = Shader.Find("Ember/UI/Backdrop");
            if (shader)
            {
                if (!_sharedMaterial) _sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _surfaceMaterial = _sharedMaterial; _materialUsers++; material = _surfaceMaterial;
            }
        }

        protected override void OnDisable()
        {
            SetLease(null);
            material = null;
            if (_surfaceMaterial && --_materialUsers == 0)
            { if (Application.isPlaying) Destroy(_sharedMaterial); else DestroyImmediate(_sharedMaterial); _sharedMaterial = null; }
            _surfaceMaterial = null;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (_surfaceMaterial)
            {
                bool ready = _lease != null && _lease.IsReady;
                if (_wasReady != ready) { _wasReady = ready; SetVerticesDirty(); }
                // CanvasRenderer's main texture binding follows the persistent RT across frames.
                canvasRenderer.SetTexture(mainTexture);
            }
        }
        #endregion

        #region 外部方法
        public void SetLease(IEUIBackdropLease lease) { _lease = lease; SetMaterialDirty(); }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var p)) return false;
            var r = rectTransform.rect;
            var radius = _squareTop && p.y >= r.center.y ? 0 : Mathf.Clamp(_radius, 0, Mathf.Min(r.width, r.height) * .5f);
            var q = new Vector2(Mathf.Abs(p.x - r.center.x), Mathf.Abs(p.y - r.center.y)) - r.size * .5f + Vector2.one * radius;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) <= radius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            var radius = Mathf.Clamp(_radius, 0, Mathf.Min(r.width, r.height) * .5f);
            var tint = color;
            for (int i = 0; i < 4; i++)
            {
                float x = i == 1 || i == 2 ? 1 : 0, y = i >= 2 ? 1 : 0;
                var v = UIVertex.simpleVert;
                v.position = new Vector3(r.xMin + x * r.width, r.yMin + y * r.height);
                v.color = new Color(tint.r, tint.g, tint.b, 1);
                v.uv0 = new Vector4(x, y, r.width, r.height);
                v.uv1 = new Vector4(radius, tint.a, _edgeWidth, _edgeLight);
                v.uv2 = new Vector4(_lease != null && _lease.IsReady ? 1 : 0, _gloss, _frost, _squareTop ? 1 : 0);
                vh.AddVert(v);
            }
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0);
            if (canvas) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        }
        #endregion
    }
}
