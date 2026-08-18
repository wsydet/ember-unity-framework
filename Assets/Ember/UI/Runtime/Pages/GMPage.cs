/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : GMPage
 * page name    : GMPage
 * create date  : 2026/8/18 18:27:57
==============================================================*/
using System.Collections;

namespace Ember.UI
{
    public partial class GMPage
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            // 按钮点击切换面板显示
            Btn_GM.onClick.AddListener(TogglePanel);

            //Slider_TimeScale.onValueChanged();
        }

        public override void OnReset()
        {
            // 页面默认关闭
            Panel_GM.gameObject.SetActive(false);
        }

        public override void OnDispose()
        {
            Btn_GM.onClick.RemoveAllListeners();
            base.OnDispose();
        }

        // ── 内部方法 ──

        private void TogglePanel()
        {
            Panel_GM.gameObject.SetActive(!Panel_GM.gameObject.activeSelf);
        }
    }
}
