// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using Ember.UI;
namespace Game.UI
{
    public partial class EUILibraryPreviewPage
    {
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();
            Primary.onClick.AddListener(OpenPopup);
            Choice.onValueChanged.AddListener(SetProgress);
        }
        public override void OnOpen(object param) { base.OnOpen(param); SetProgress(Choice.isOn); }
        public override void OnDispose()
        {
            Primary.onClick.RemoveListener(OpenPopup);
            Choice.onValueChanged.RemoveListener(SetProgress);
            base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void OpenPopup() => EUIManager.Instance.ShowPopup(GamePages.EUILibraryPopupPage);
        private void SetProgress(bool selected)
        { Progress.SetProgress(selected ? 1 : 0); }
        #endregion
    }
}
