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
        public void Configure(string label, Action selected)
        {
            // 选项文字是运行期写入的：控件上必须用 SetSource 接管（同时清掉 Key），
            // 否则下一次切语言 RefreshAll 会把它换回表里的静态文案（曾经会变成「选项」两个字）。
            if (Label is Ember.UIExtension.TMPEx localized) localized.SetSource(label);
            else Label.text = label;
            _selected = selected;
        }
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
