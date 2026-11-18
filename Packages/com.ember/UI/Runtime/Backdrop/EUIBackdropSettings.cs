using Ember.Basic;
using UnityEngine;

namespace Ember.UI
{
    [CreateAssetMenu(menuName = "Ember/UI/Backdrop Settings")]
    public sealed class EUIBackdropSettings : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField, Range(0, 40)] private float _blurRadiusDU = 16;
        [SerializeField, Range(1, 8)] private int _downsample = 4;
        [SerializeField, Range(1, 3)] private int _iterations = 2;
        [SerializeField] private EUIBackdropUpdateMode _updateMode = EUIBackdropUpdateMode.Throttled;
        [SerializeField, Range(1, 120)] private int _updatesPerSecond = 30;
        [SerializeField] private Color _fallbackColor = new Color(.12f, .15f, .23f, .92f);
        #endregion

        #region 外部方法
        public float BlurRadiusDU => float.IsNaN(_blurRadiusDU) || float.IsInfinity(_blurRadiusDU) ? 16 : Mathf.Clamp(_blurRadiusDU, 0, 40);
        public int Downsample => _downsample <= 2 ? 2 : _downsample <= 4 ? 4 : 8;
        public int Iterations => Mathf.Clamp(_iterations, 1, 3);
        public EUIBackdropUpdateMode UpdateMode => _updateMode;
        public int UpdatesPerSecond => Mathf.Clamp(_updatesPerSecond, 1, 120);
        public Color FallbackColor => _fallbackColor;
        #endregion
    }

}
