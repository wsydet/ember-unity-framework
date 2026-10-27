// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUIGMEnhancedToggleItem
    {
        public event System.Action<bool> Changed;
        public override void OnInit()
        { base.OnInit(); Toggle.onValueChanged.AddListener(OnChanged); ((Ember.UIExtension.TMPEx)Toggle.Label).SetSource("增强开关"); }
        public override void OnDispose()
        { Toggle.onValueChanged.RemoveListener(OnChanged); Changed = null; base.OnDispose(); }
        private void OnChanged(bool value)
        { ((Ember.UIExtension.TMPEx)Toggle.Label).SetSource(value ? "已开启" : "已关闭"); Changed?.Invoke(value); }
    }
}

