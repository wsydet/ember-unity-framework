// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
namespace Game.UI
{
    /// <summary>实例计数用于检查一次绑定后的初始化、借用和释放；不在运行时搭建 UI。</summary>
    public partial class EUIOwnershipLeafItem
    {
        public int InitCount { get; private set; }
        public int OpenCount { get; private set; }
        public int DisposeCount { get; private set; }
        public void SetLabel(string value) { Txt_Label.text = value; }
        public override void OnInit() { base.OnInit(); InitCount++; }
        public override void OnOpen(object param) { base.OnOpen(param); OpenCount++; }
        public override void OnDispose() { DisposeCount++; base.OnDispose(); }
    }
}
