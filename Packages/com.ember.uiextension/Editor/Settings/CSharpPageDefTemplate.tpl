/*=============================================================
 * 页面定义文件，本文件为自动生成文件，请勿修改
 * create date  : {create_date} 
==============================================================
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Burner.UIExtension;

namespace {namespace_name}

\{
    public static class PageDef
    \{{"
"}
        
{for p in pages:
{"        "}public const string {p.name} = "{p.info}";{"
"}
}

    \}
\}
