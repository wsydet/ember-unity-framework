/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EUIBackgroundPage
 * page name    : EUIBackgroundPage
 * create date  : 2026/8/11 20:30:52
==============================================================*/
using System.Collections;

namespace Ember.UI
{
    public partial class EUIBackground
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            // 在此处初始化业务数据和事件绑定
            base.OnInit();
        }

        public override void OnOpen(object param)
        {
            // 页面被打开，处理传入参数
            base.OnOpen(param);
        }

        public override void OnShow()
        {
            // 页面变为可见
            base.OnShow();
        }

        public override void OnHide()
        {
            // 页面被隐藏
            base.OnHide();
        }

        public override void OnClose()
        {
            // 页面被关闭
            base.OnClose();
        }

        public override void OnDispose()
        {
            // 清理：注销事件、释放引用
            base.OnDispose();
        }
    }
}
