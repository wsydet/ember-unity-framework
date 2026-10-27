// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUIDynamicRowProgressItem
    {
        public float CurrentValue => Progress.value;
        public void SetProgress(float value) => Progress.SetValueWithoutNotify(UnityEngine.Mathf.Clamp01(value));
        public override void OnResetDefault() { base.OnResetDefault(); SetProgress(0); }
    }
}
