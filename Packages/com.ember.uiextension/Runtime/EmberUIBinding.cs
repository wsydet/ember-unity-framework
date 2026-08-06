// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Sirenix.OdinInspector;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 控件绑定组件 —— 挂载到 UI 预制体根节点上，记录每个子控件的名称、类型和 GameObject 引用。
    /// 编辑器工具读取此组件中的绑定配置，自动生成 C# partial class 控件访问代码。
    ///
    /// <para>工作流程：</para>
    /// <list type="bullet">
    ///   <item>1. 在预制体根节点上挂载此组件</item>
    ///   <item>2. 在 Inspector 中拖入子控件 GameObject 并命名</item>
    ///   <item>3. 类型自动检测（也可手动调整）</item>
    ///   <item>4. 点击"生成代码" → 自动生成 .bindings.cs 文件</item>
    /// </list>
    ///
    /// <para>生成的代码示例：</para>
    /// <code>
    /// public partial class UIMainMenu : EmberPage
    /// {
    ///     private Button _btnStart;
    ///     private Text _txtTitle;
    ///
    ///     public override void OnBind()
    ///     {
    ///         base.OnBind();
    ///         _btnStart = GetBinding《Button》("btnStart");
    ///         _txtTitle = GetBinding《Text》("txtTitle");
    ///     }
    /// }
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ember/UI Binding")]
    public class EmberUIBinding : MonoBehaviour
    {
        #region 编辑器面板参数

        [FoldoutGroup("代码生成")]
        [SerializeField]
        [LabelText("命名空间")]
        [Tooltip("生成的代码所在的命名空间")]
        private string _namespaceName = "Game.UI";

        [FoldoutGroup("代码生成")]
        [SerializeField]
        [LabelText("类名")]
        [Tooltip("生成的 partial class 名称")]
        private string _className;

        [FoldoutGroup("代码生成")]
        [SerializeField]
        [LabelText("输出目录")]
        [Tooltip("代码文件输出目录，相对于 Assets/")]
        private string _outputDirectory = "Game/UI/Generated";

        [FoldoutGroup("代码生成")]
        [SerializeField]
        [LabelText("基类名")]
        [Tooltip("生成的类继承自哪个基类")]
        private string _baseClassName = "Ember.UI.EmberPage";

        [FoldoutGroup("绑定列表")]
        [SerializeField]
        [LabelText("控件绑定")]
        private BindingEntry[] _bindings = Array.Empty<BindingEntry>();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>生成的命名空间</summary>
        public string NamespaceName => _namespaceName;

        /// <summary>生成的类名</summary>
        public string ClassName => _className;

        /// <summary>输出目录（相对于 Assets/）</summary>
        public string OutputDirectory => _outputDirectory;

        /// <summary>基类名（含命名空间）</summary>
        public string BaseClassName => _baseClassName;

        /// <summary>控件绑定列表</summary>
        public BindingEntry[] Bindings => _bindings;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        /// <summary>
        /// 控件类型枚举。
        /// </summary>
        public enum WidgetType
        {
            Component   = 0,
            Text        = 1,
            Image       = 2,
            RawImage    = 3,
            Button      = 4,
            Toggle      = 5,
            ToggleGroup = 6,
            InputField  = 7,
            ScrollRect  = 8,
            Slider      = 9,
            Dropdown    = 10,
            /// <summary>自定义扩展类型（类名从 ClassName 字段读取）</summary>
            Extension   = 99,
        }

        /// <summary>
        /// 一个控件绑定条目。
        /// </summary>
        [Serializable]
        public struct BindingEntry
        {
            /// <summary>变量名（生成代码中的字段名）</summary>
            [LabelText("变量名")]
            public string Name;

            /// <summary>控件的 GameObject 引用</summary>
            [LabelText("节点")]
            public GameObject Target;

            /// <summary>控件类型</summary>
            [LabelText("类型")]
            public WidgetType Type;

            /// <summary>扩展类型的完整类名（Type == Extension 时有效）</summary>
            [LabelText("扩展类名")]
            [ShowIf("@Type == WidgetType.Extension")]
            public string ClassName;
        }

        #endregion
    }
}
