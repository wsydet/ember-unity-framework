// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
namespace Game.UI
{
    public enum EUIDialogResult { Dismissed, Accepted, Cancelled }
    /// <summary>默认弹窗的调用参数；回调在页面关闭时触发一次。</summary>
    public sealed class EUIDialogRequest
    {
        public string Title = "Information";
        public string Message = "Reusable dialog content.";
        public string AcceptText = "OK", CancelText = "Cancel";
        public System.Action<EUIDialogResult> Completed;
    }
}
