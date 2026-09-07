/*=============================================================
 * author       : Bingo
 * prefab name  : EUIBaseBubbleItem
 * ui role      : Item
 * create date  : 2026/9/4 17:28:59
==============================================================*/
using System.Collections;

namespace Game.UI
{
    public partial class EUIBaseBubbleItem
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
            // Item 从池中借出，处理本次使用参数
            base.OnOpen(param);
        }

        public override void OnShow()
        {
            // Item 变为可见
            base.OnShow();
        }

        public override void OnHide()
        {
            // Item 被临时隐藏
            base.OnHide();
        }

        public override void OnClose()
        {
            // Item 归还对象池
            base.OnClose();
        }

        public override void OnDispose()
        {
            // 清理：注销事件、释放引用
            base.OnDispose();
        }
    }
}
