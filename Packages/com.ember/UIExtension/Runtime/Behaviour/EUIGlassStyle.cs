using Ember.Basic;
using Ember.UI;
using UnityEngine;

namespace Ember.UIExtension
{
    public enum EUIGlassRadius { Small, Control, Card, Panel, Capsule }

    [CreateAssetMenu(menuName = "Ember/UI/Glass Style")]
    public sealed class EUIGlassStyle : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private Color _tint = new Color(.1f,.14f,.23f,.58f);
        [SerializeField] private Vector4 _radiusTokens = new Vector4(12,20,32,48);
        #endregion
        #region 外部方法
        public Color Tint => _tint;
        public void ApplyTo(EUIBackdropGraphic surface, EUIGlassRadius token)
        {
            if (!surface) return;
            surface.color = _tint;
            var size = surface.rectTransform.rect.size;
            surface.Radius = token == EUIGlassRadius.Capsule ? Mathf.Min(size.x,size.y)*.5f : _radiusTokens[Mathf.Clamp((int)token,0,3)];
        }
        #endregion
    }
}
