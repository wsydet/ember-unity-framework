using System;
namespace Game.UI
{
    public partial class EUISoftGlassSecondaryButtonItem
    {
        #region 内部参数
        public event Action Clicked;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Button.onClick.AddListener(InvokeClick); }
        public override void OnDispose() { Button.onClick.RemoveListener(InvokeClick); Clicked = null; base.OnDispose(); }
        #endregion
        #region 内部方法
        private void InvokeClick() => Clicked?.Invoke();
        #endregion
        #region 外部方法
        public void SetTitle(string title) => Label.text = title ?? string.Empty;
        public void SetEnabled(bool enabled) => Button.SetEnabled(enabled);
        public void SetStyle(Ember.UIExtension.EUIGlassStyle style) { if(style) style.ApplyTo(Surface, Ember.UIExtension.EUIGlassRadius.Control); }
        #endregion
    }
}