/*=============================================================
 * 页面定义文件 —— 本文件为自动生成，请勿修改
 * create date  : {create_date}
==============================================================*/
using Ember.UI;

namespace {namespace_name}
\{
    public static class PageDef
    \{{"
"}
{for p in pages:
    public const string {p.name} = "{p.info}";{"
"}
}

    \}
\}
