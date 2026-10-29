// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUILibraryProgressItem
    {
        public void SetProgress(float value)
        { value = UnityEngine.Mathf.Clamp01(value); Progress.SetValueWithoutNotify(value); Value.SetSource(UnityEngine.Mathf.FloorToInt(value * 100f) + "%"); }
        public override void OnResetDefault() { base.OnResetDefault(); SetProgress(0); }
    }
}
