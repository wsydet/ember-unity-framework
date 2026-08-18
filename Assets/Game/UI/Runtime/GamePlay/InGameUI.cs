/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : InGameUI
 * page name    : InGameUI
 * create date  : 2026/8/11 16:13:58
==============================================================*/
using Ember.Basic;
using Ember.Core;
using Ember.UI;

namespace Game.UI
{
    public partial class InGameUI
    {
        private const string TAG = LogTags.Game + "." + nameof(InGameUI);

        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            Btn_Back.onClick.AddListener(() =>
                GameLauncher.Instance.Fsm.TransitionTo<MainState>());
        }

        public override void OnDispose()
        {
            EmberDebug.Log(TAG, "InGameUI 清理");
            Btn_Back.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
