using System;
namespace Game.UI
{
    public partial class EUISoftGlassSwitchItem
    {
        #region 内部参数
        public event Action<bool> Changed;
        public bool IsOn => Toggle.isOn;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Toggle.onValueChanged.AddListener(OnChanged); }
        public override void OnDispose() { Toggle.onValueChanged.RemoveListener(OnChanged); Changed = null; base.OnDispose(); }
        #endregion
        #region 内部方法
        private void OnChanged(bool value) => Changed?.Invoke(value);
        #endregion
        #region 外部方法
        public void SetValue(bool value) { Toggle.SetIsOnWithoutNotify(value); Toggle.Refresh(); }
        public void SetTitle(string title) => Label.text = title ?? string.Empty;
        #endregion
    }
}