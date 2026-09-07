/*=============================================================
 * author       : {author_name}
 * prefab name  : {prefab_name}
 * ui role      : {ui_role}
 * create date  : {create_date}
==============================================================*/
using System.Collections;

namespace {namespace_name}
{{
    public partial class {class_name}
    {{
        // ── UI 配置 ──

{page_feature_members}

        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {{
            // 在此处初始化业务数据和事件绑定
            base.OnInit();
        }}

        public override void OnOpen(object param)
        {{
            // UI 开始一次使用，处理传入参数
            base.OnOpen(param);
        }}

        public override void OnShow()
        {{
            // UI 变为可见
            base.OnShow();
        }}

        public override void OnHide()
        {{
            // UI 被隐藏
            base.OnHide();
        }}

        public override void OnClose()
        {{
            // UI 结束本次使用
            base.OnClose();
        }}

        public override void OnDispose()
        {{
            // 清理：注销事件、释放引用
            base.OnDispose();
        }}
    }}
}}
