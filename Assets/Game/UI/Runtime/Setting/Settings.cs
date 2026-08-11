/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : Settings
 * page name    : Settings
 * create date  : 2026/8/11 15:42:54
==============================================================*/
using Ember.Basic;
using Ember.Core;
using Ember.UI;

namespace Game.UI
{
    public partial class Settings
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            Btn_Close.onClick.AddListener(() =>
                GameLauncher.Instance.Fsm.Pop());
        }

        public override void OnOpen(object param)
        {
            base.OnOpen(param);

            var context = param is SettingsContext ctx ? ctx : SettingsContext.Main;
            Txt_NowScene.text = context switch
            {
                SettingsContext.Main     => "当前场景：主界面",
                SettingsContext.Gameplay => "当前场景：玩法中",
                _                        => "当前场景：未知",
            };
        }

        public override void OnDispose()
        {
            EmberDebug.Log(LogTags.EmberUI, "Settings 清理");
            Btn_Close.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
