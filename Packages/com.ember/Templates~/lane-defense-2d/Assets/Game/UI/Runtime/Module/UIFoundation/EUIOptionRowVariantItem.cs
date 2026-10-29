/*=============================================================
 * author       : Bingo
 * prefab name  : EUIOptionRowVariantItem
 * ui role      : Item
 * create date  : 2026/10/26 19:13:09
==============================================================*/
using System.Collections;

namespace Game.UI
{
    public partial class EUIOptionRowVariantItem
    {
        // ── UI 配置 ──



        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            // 在此处初始化业务数据和事件绑定
            base.OnInit();
        }

        public override void OnOpen(object param)
        {
            // UI 开始一次使用，处理传入参数
            base.OnOpen(param);
        }

        public override void OnShow()
        {
            // UI 变为可见
            base.OnShow();
        }

        public override void OnHide()
        {
            // UI 被隐藏
            base.OnHide();
        }

        public override void OnClose()
        {
            // UI 结束本次使用
            base.OnClose();
        }

        public override void OnDispose()
        {
            // 清理：注销事件、释放引用
            base.OnDispose();
        }
    }
}
