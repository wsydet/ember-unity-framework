/*=============================================================
 * author       : {author_name} 
 * prefab name  : {prefab_name} 

 {if isPage = 1:
{" * page name    : "}{page_name}
 }

 * create date  : {create_date} 
==============================================================*/
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
    \{
        public override void OnInit()
        \{
            //千万不能删除下面这行
            base.OnInit();{"
"}
{for f in fields:
    {if f.type = "GameButton":

{"            "}{f.name}.OnClick += OnClick{f.name};{"
"}
    }
}

        \}

        public override void OnOpen(object param)
        \{
            base.OnOpen(param);
        \}

        public void RefreshPage()
        \{
            
        \}{"
"}
{for f in fields:
    {if f.type = "GameButton":
{"        "}void OnClick{f.name}(GameUIComponent sender)
        \{

        \}{"
"}

    }
}

    \}
\}
