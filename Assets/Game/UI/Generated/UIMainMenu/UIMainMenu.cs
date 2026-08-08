/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : MainMenu
 * page name    : MainMenu
 * create date  : 2026/8/7 20:43:52
==============================================================*/
using Ember.Basic;
using Ember.UI;

namespace Game.UI
{
    public partial class UIMainMenu
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            _BtnSettings.onClick.AddListener(() =>
                EmberUIPageRouter.Instance.ShowPopup(GamePages.Settings));

            _BtnStart.onClick.AddListener(() =>
                EmberDebug.Log(LogTags.EmberUI, "开始游戏"));
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
            _BtnSettings.onClick.RemoveAllListeners();
            _BtnStart.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
