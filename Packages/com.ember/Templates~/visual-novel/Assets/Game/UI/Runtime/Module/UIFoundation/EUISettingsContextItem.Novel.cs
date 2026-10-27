// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
namespace Game.UI
{
    public partial class EUISettingsContextItem
    {
        #region 外部方法

        /// <summary>保留视觉小说当前语言的设置上下文提示。</summary>
        public void SetNovelContext(Ember.Core.SettingsContext context)
        {
            SetContext(context);
            Label.SetSource(Game.Narrative.NovelLocalization.Runtime(
                context == Ember.Core.SettingsContext.Gameplay
                    ? "ui.setting.CurrentScene.Gameplay" : "ui.setting.CurrentScene.Main",
                Label.text));
        }

        #endregion
    }
}
