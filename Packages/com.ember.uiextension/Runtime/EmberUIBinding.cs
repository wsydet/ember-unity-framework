//using System;
//using UnityEngine;
//
//namespace Ember.UIExtension
//{
//    /// <summary>
//    /// UI 控件绑定组件 —— 挂载到 UI 预制体上，记录每个控件的名称、类型和 GameObject 引用。
//    ///
//    /// 设计参考了 burner 的 <c>GameUIBinding</c>。
//    /// 编辑器工具读取此组件中的绑定配置，自动生成 C# 控件访问代码，
//    /// 避免手写 transform.Find + GetComponent。
//    ///
//    /// 工作流程：
//    /// 1. 在预制体上挂载此组件
//    /// 2. 在 Inspector 中拖入控件 GameObject 并命名
//    /// 3. 运行代码生成工具 → 自动生成 partial class
//    /// 4. 手写业务逻辑代码中使用生成的控件引用
//    ///
//    /// 生成的代码示例：
//    /// <code>
//    /// public partial class UIMainMenu : MonoBehaviour, IUIView
//    /// {
//    ///     private Button BtnStart;   // 自动生成
//    ///     private Text TxtTitle;     // 自动生成
//    ///
//    ///     partial void OnBind()      // 自动生成
//    ///     {
//    ///         BtnStart = GetBinding&lt;Button&gt;("BtnStart");
//    ///         TxtTitle = GetBinding&lt;Text&gt;("TxtTitle");
//    ///     }
//    /// }
//    /// </code>
//    /// </summary>
//    [DisallowMultipleComponent]
//    public class EmberUIBinding : MonoBehaviour
//    {
//        /// <summary>
//        /// 控件类型枚举。用于标识绑定的控件是什么类型。
//        /// </summary>
//        public enum WidgetType
//        {
//            Component   = 0,
//            Text        = 1,
//            Image       = 2,
//            RawImage    = 3,
//            Button      = 4,
//            Toggle      = 5,
//            ToggleGroup = 6,
//            InputField  = 7,
//            ScrollRect  = 8,
//            Slider      = 9,
//            Dropdown    = 10,
//            // 新类型加在此行上方
//        }
//
//        /// <summary>
//        /// 一个控件绑定条目。
//        /// </summary>
//        [Serializable]
//        public struct BindingEntry
//        {
//            /// <summary>控件名称（生成代码中的变量名）</summary>
//            public string Name;
//
//            /// <summary>控件的 GameObject 引用</summary>
//            public GameObject Target;
//
//            /// <summary>控件类型</summary>
//            public WidgetType Type;
//        }
//
//        #region 参数
//
//        /// <summary>所属页面名称（对应 PageDef 中的路径）</summary>
//        [SerializeField] private string _pageName;
//
//        /// <summary>是否为页面根节点（true = Page，false = 子组件）</summary>
//        [SerializeField] private bool _isPage;
//
//        /// <summary>生成的 C# 类名</summary>
//        [SerializeField] private string _className;
//
//        /// <summary>控件绑定列表</summary>
//        [SerializeField] private BindingEntry[] _bindings;
//
//        #endregion
//
//        // ============================================================
//
//        #region 外部方法
//
//        /// <summary>所属页面名称</summary>
//        public string PageName => _pageName;
//
//        /// <summary>是否为页面根节点</summary>
//        public bool IsPage => _isPage;
//
//        /// <summary>生成的类名</summary>
//        public string ClassName => _className;
//
//        /// <summary>控件绑定列表</summary>
//        public BindingEntry[] Bindings => _bindings;
//
//        #endregion
//    }
//}
