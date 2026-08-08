/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : Settings
 * page name    : Settings
 * create date  : 2026/8/7 20:44:02
==============================================================*/
using Ember.Basic;
using Ember.UI;

namespace Game.UI
{
    public partial class Settings
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            _BtnClose.onClick.AddListener(() =>
                EmberUIPageRouter.Instance.ClosePage(Page));

            _BtnLogout.onClick.AddListener(() =>
                EmberDebug.Log(LogTags.EmberUI, "退出登录"));
        }

        public override void OnDispose()
        {
            _BtnClose.onClick.RemoveAllListeners();
            _BtnLogout.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
