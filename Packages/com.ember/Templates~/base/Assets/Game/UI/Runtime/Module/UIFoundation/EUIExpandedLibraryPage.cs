// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using Ember.UI;
namespace Game.UI
{
    public partial class EUIExpandedLibraryPage
    {
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit(); Tabs.ToggleGroupValueChange.AddListener(SelectTab);
            OpenConfirm.onClick.AddListener(Confirm); OpenAlert.onClick.AddListener(Alert); Notify.onClick.AddListener(ShowNotice);
        }
        public override void OnOpen(object param) { base.OnOpen(param); Tabs.SetToggleOn(0); SelectTab(0,-1); }
        public override void OnHide() { Notice.Hide(); base.OnHide(); }
        public override void OnDispose()
        {
            Tabs.ToggleGroupValueChange.RemoveListener(SelectTab);
            OpenConfirm.onClick.RemoveListener(Confirm); OpenAlert.onClick.RemoveListener(Alert); Notify.onClick.RemoveListener(ShowNotice); base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void SelectTab(int selected,int previous)
        { Controls.gameObject.SetActive(selected==0); Forms.gameObject.SetActive(selected==1); Feedback.gameObject.SetActive(selected==2); Layout.gameObject.SetActive(selected==3); }
        private void Confirm() => EUIManager.Instance.ShowPopup(GamePages.EUIConfirmPage,new EUIDialogRequest {Title="Apply changes?",Message="This dialog accepts dynamic text. Confirm, cancel, close or click the mask to return a result.",AcceptText="Apply",Completed=OnResult});
        private void Alert() => EUIManager.Instance.ShowPopup(GamePages.EUIAlertPage,new EUIDialogRequest {Title="Information",Message="The component library is ready to use. Content, icons and labels are instance overrides.",Completed=OnResult});
        private void OnResult(EUIDialogResult result) => Notice.Show("Dialog result: "+result);
        private void ShowNotice() => Notice.Show("Changes saved");
        #endregion
    }
}
