// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using Ember.UI;
namespace Game.UI
{
    public partial class EUIAlertPage
    {
        #region 内部参数
        private EUIDialogRequest _request;
        private EUIDialogResult _result;
        private bool _closing;
        #endregion
        #region 生命周期
        public override void OnInit()
        { base.OnInit(); Accept.onClick.AddListener(AcceptDialog); Cancel.onClick.AddListener(CancelDialog); Close.onClick.AddListener(DismissDialog); }
        public override void OnOpen(object param)
        {
            base.OnOpen(param); _request = param as EUIDialogRequest ?? new EUIDialogRequest(); _result = EUIDialogResult.Dismissed; _closing = false;
            Title.text = _request.Title; Body.text = _request.Message;
            Accept.Label.text = _request.AcceptText; Cancel.Label.text = _request.CancelText;
            Body.GetComponentInParent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 1;
        }
        public override void OnClose()
        { var callback = _request?.Completed; _request = null; base.OnClose(); callback?.Invoke(_result); }
        public override void OnDispose()
        { Accept.onClick.RemoveListener(AcceptDialog); Cancel.onClick.RemoveListener(CancelDialog); Close.onClick.RemoveListener(DismissDialog); _request = null; base.OnDispose(); }
        #endregion
        #region 内部方法
        private void AcceptDialog() => Finish(EUIDialogResult.Accepted);
        private void CancelDialog() => Finish(EUIDialogResult.Cancelled);
        private void DismissDialog() => Finish(EUIDialogResult.Dismissed);
        private void Finish(EUIDialogResult result)
        { if (_closing) return; _closing = true; _result = result; EUIManager.Instance.ClosePage(Page); }
        #endregion
    }
}
