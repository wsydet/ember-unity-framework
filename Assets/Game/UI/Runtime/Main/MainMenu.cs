/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : MainMenu
 * page name    : MainMenu
 * create date  : 2026/8/11 15:10:00
==============================================================*/
using Ember.Basic;
using Ember.Core;
using Ember.UI;

namespace Game.UI
{
    public partial class MainMenu
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            Btn_Start.onClick.AddListener(() =>
                GameLauncher.Instance.Fsm.TransitionTo<GameplayState>());

            Btn_Settings.onClick.AddListener(() =>
                GameLauncher.Instance.Fsm.Push<SettingsState>(SettingsContext.Main));
        }

        public override void OnPause()
        {
            base.OnPause();
            EmberDebug.Log(LogTags.EmberUI, "MainMenu 被遮挡");
        }

        public override void OnResume()
        {
            base.OnResume();
            EmberDebug.Log(LogTags.EmberUI, "MainMenu 恢复可见");
        }

        public override void OnDispose()
        {
            Btn_Start.onClick.RemoveAllListeners();
            Btn_Settings.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
