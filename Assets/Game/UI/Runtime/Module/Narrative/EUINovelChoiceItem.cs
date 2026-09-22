using System;
namespace Game.UI
{
    public partial class EUINovelChoiceItem
    {
        #region 内部参数
        private Action _selected;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void OnInit() { base.OnInit(); Select.onClick.AddListener(Choose); }
        public void Configure(string label, Action selected) { Label.text = label; _selected = selected; }
        public void SetInteractable(bool value) { Select.interactable = value; }
        public override void OnClose() { _selected = null; base.OnClose(); }
        public override void OnDispose() { _selected = null; Select.onClick.RemoveListener(Choose); base.OnDispose(); }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Choose() => _selected?.Invoke();
        #endregion
    }
}
