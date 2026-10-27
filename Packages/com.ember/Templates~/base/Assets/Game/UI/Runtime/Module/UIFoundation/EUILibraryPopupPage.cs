// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using Ember.UI;
namespace Game.UI
{
    public partial class EUILibraryPopupPage
    {
        #region 生命周期
        public override void OnInit() { base.OnInit(); Close.onClick.AddListener(ClosePopup); }
        public override void OnDispose() { Close.onClick.RemoveListener(ClosePopup); base.OnDispose(); }
        #endregion
        #region 内部方法
        private void ClosePopup() => EUIManager.Instance.ClosePage(Page);
        #endregion
    }
}

