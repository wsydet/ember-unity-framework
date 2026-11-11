// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using Ember.UI;
using Ember.UIExtension;

namespace Game.UI
{
    /// <summary>两种嵌套所有权的最小示例；不进入 base 的正常导航。</summary>
    public partial class EUIOwnershipExamplePage
    {
        private EUIItem _dynamic;
        public EUIOwnershipLeafItem StaticLogic => StaticItem;
        public EUIOwnershipLeafItem DynamicLogic => _dynamic?.Logic as EUIOwnershipLeafItem;

        public override void OnInit()
        {
            base.OnInit();
            // StaticItem 由 Bridge 创建；DynamicItem 仅绑定 RectTransform，由宿主包装。
            if (!EUIItemFactory.TryCreate(DynamicItem.gameObject, out _dynamic, out var error))
                throw new InvalidOperationException(error);
            TrackDisposable(_dynamic);
            StaticItem.SetLabel("Parent-owned UILogic");
            DynamicLogic.SetLabel("Host-owned EUIItem");
        }
        public override void OnOpen(object param) { base.OnOpen(param); _dynamic.BeginUse(param); }
        public override void OnShow() { base.OnShow(); _dynamic.Show(); }
        public override void OnHide() { _dynamic.Hide(); base.OnHide(); }
        public override void OnClose() { _dynamic.ResetForPool(); base.OnClose(); }
        public override void OnDispose() { _dynamic = null; base.OnDispose(); }
    }
}
