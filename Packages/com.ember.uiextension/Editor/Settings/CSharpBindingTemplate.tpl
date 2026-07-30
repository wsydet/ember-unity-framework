/*=============================================================
 * author       : {author_name} 
 * prefab name  : {prefab_name} 

 {if isPage = 1:
{" * page name    : "}{page_name}
 }

 * update time  : {create_date} 
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Burner.UIExtension;

namespace {namespace_name}

\{
    public partial class {class_name} : {base_class_name}{"
    "}
    \{{"
"}
{for f in fields:
{"        /// <summary>
        /// "}{f.comment}{"
        /// </summary>
        "}{f.type} {f.name};{"
"}
}

        public override void OnBind()
        \{
            base.OnBind();{"
"}
{for f in fields:

{"            "}{f.name} = controlMap["{f.name}"] as {f.type};{"
"}
}

        \}
    \}
\}
