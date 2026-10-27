// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUISettingsContextItem
    {
        public void SetContext(Ember.Core.SettingsContext context)
        {
            Label.SetSource(context switch {
                Ember.Core.SettingsContext.Main => "当前场景：主界面",
                Ember.Core.SettingsContext.Gameplay => "当前场景：玩法中",
                _ => "当前场景：未知"
            });
        }
    }
}

