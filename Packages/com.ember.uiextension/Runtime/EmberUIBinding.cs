// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Sirenix.OdinInspector;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 页面标记。
    /// </summary>
    [Flags]
    public enum PageFlags
    {
        /// <summary>未设置（无效状态）</summary>
        None = 0,

        /// <summary>主页面：替换当前 MainPage，压入主栈</summary>
        MainPage = 1 << 0,

        /// <summary>弹窗：覆盖在主页面之上，自动创建 BG Mask</summary>
        Popup = 1 << 1,

        /// <summary>置顶弹窗：高于所有 Popup</summary>
        TopMost = 1 << 2,

        /// <summary>子页面：嵌入父页面指定区域</summary>
        SubPage = 1 << 3,

        /// <summary>自由页面：不受栈管理</summary>
        FreePage = 1 << 4,
    }

    /// <summary>
    /// UI 控件绑定组件 —— 挂载到 UI 预制体根节点上，记录每个子控件的名称、类型和 GameObject 引用。
    /// 编辑器工具读取此组件中的绑定配置，通过 LogicImplementationData 生成 partial class 代码。
    ///
    /// <para>工作流程：</para>
    /// <list type="bullet">
    ///   <item>1. 在预制体根节点上挂载此组件</item>
    ///   <item>2. 在 Inspector 中配置页面信息（Page/非Page、类名等）</item>
    ///   <item>3. 拖入子控件或点击"自动收集"绑定子节点</item>
    ///   <item>4. 类型自动检测（也可手动调整）</item>
    ///   <item>5. 点击"生成代码" → 通过 LogicImplementationData 生成代码文件</item>
    /// </list>
    ///
    /// <para>继承支持：可通过 baseBindingUUID 指定基类预制体，自动继承基类的绑定字段。</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Ember/UI Binding")]
    public class EmberUIBinding : MonoBehaviour
    {
        #region 嵌套类型

        /// <summary>
        /// 控件类型枚举。新加类型一定要写在 End 前面，以免影响序列化。
        /// </summary>
        public enum WidgetTypes
        {
            Component   = 0,
            Text        = 1,
            Toggle      = 2,
            Button      = 3,
            ProgressBar = 4,
            Image       = 5,
            UIContainer = 6,
            UILogic     = 7,
            InputField  = 8,
            ToggleGroup = 9,
            ScrollRect  = 10,
            RawImage    = 11,
            Canvas      = 12,
            TabLoader   = 13,

            // 新加类型一定要写在这行上面，以免影响序列化信息
            // 新加类型一定要写在这行上面，以免影响序列化信息
            End,

            /// <summary>自定义扩展类型（类名从 ClassName 字段读取）</summary>
            Extension = 65535,
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

            /// <summary>绑定的 GameObject</summary>
            [LabelText("节点")]
            public GameObject GameObject;

            /// <summary>控件类型</summary>
            [LabelText("类型")]
            public WidgetTypes Type;

            /// <summary>扩展类型的完整类名（Type == Extension 或 UILogic 时有效）</summary>
            [LabelText("类名")]
            public string ClassName;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private const string GROUP = "Ember UI Binding";

        #endregion

        // --------------------------------------------------------

        #region 编辑器面板参数

        // ── 页面配置 ──

        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP", Expanded = true)]
        [BoxGroup("$GROUP/页面配置", ShowLabel = false)]
        [Title("页面配置", "配置页面信息和子控件绑定，用于自动生成 UI 逻辑代码。")]
        [SerializeField, LabelText("是否为 Page")]
        [Tooltip("勾选后此 binding 为页面级，会生成 PageDef")]
        [ShowIf("@!noCodeGen")]
        private bool isPage;

        [PropertyOrder(-99)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("页面名称")]
        [Tooltip("PageDef 常量名，如 MainMenu、Settings")]
        [ShowIf("@isPage && !noCodeGen"), Required("请输入页面名称")]
        private string pageName;

        [PropertyOrder(-98)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("页面类型")]
        [Tooltip("MainPage / Popup / TopMost / SubPage / FreePage")]
        [ShowIf("@isPage && !noCodeGen")]
        private PageFlags pageFlags;

        [PropertyOrder(-97)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("启用预设渐入渐出")]
        [Tooltip("开启后使用 CanvasGroup alpha 做渐入渐出动画")]
        [ShowIf("@isPage && !noCodeGen")]
        private bool usePresetFade;

        [PropertyOrder(-96)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("渐入时间 (秒)")]
        [ShowIf("@isPage && usePresetFade && !noCodeGen")]
        private float fadeInTime = 0.3f;

        [PropertyOrder(-95)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("渐出时间 (秒)")]
        [ShowIf("@isPage && usePresetFade && !noCodeGen")]
        private float fadeOutTime = 0.2f;

        // ── 输出设置 ──

        [PropertyOrder(-80)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置", ShowLabel = false)]
        [Title("输出设置")]
        [SerializeField, LabelText("输出根目录")]
        [Tooltip("代码输出根目录（相对于 Assets/）。留空则使用全局设置")]
        private string codePath;

        [PropertyOrder(-79)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("输出子目录")]
        [Tooltip("相对于根目录的子目录，如 MainMenu 或 Battle/UI")]
        private string classPath;

        [PropertyOrder(-78)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("类名")]
        [Tooltip("生成的 partial class 名称，如 UIMainMenu")]
        [Required("请输入类名（如 UIMainMenu）")]
        private string className;

        [PropertyOrder(-77)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("不生成代码到文件")]
        [Tooltip("勾选后代码只复制到剪贴板，不写入文件")]
        private bool noCodeGen;

        // ── 自身控件 ──

        [PropertyOrder(-60)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/自身控件", ShowLabel = false)]
        [Title("自身控件")]
        [ValidateInput("ValidateSelfWidgetType", "自身控件类型不能为 UI Logic")]
        [SerializeField, LabelText("自身控件类型")]
        private WidgetTypes selfWidgetType;

        [PropertyOrder(-59)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/自身控件")]
        [SerializeField, LabelText("自身控件类名")]
        [ShowIf("@selfWidgetType == WidgetTypes.Extension")]
        private string selfWidgetClassName;

        // ── 继承 ──

        [PropertyOrder(-40)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/继承", ShowLabel = false)]
        [Title("继承")]
        [SerializeField, LabelText("基类 Prefab GUID")]
        [Tooltip("指向基类预制体的 GUID，用于继承绑定字段")]
        private string baseBindingUUID;

        // ── 控件绑定 ──

        [PropertyOrder(-20)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/绑定列表", ShowLabel = false)]
        [Title("控件绑定")]
        [ListDrawerSettings(
            ShowFoldout = false,
            ShowIndexLabels = false
        )]
        [SerializeField, LabelText("绑定列表")]
        private BindingEntry[] bindings = Array.Empty<BindingEntry>();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>绑定条目列表</summary>
        public BindingEntry[] Bindings => bindings;

        /// <summary>自身控件类型</summary>
        public WidgetTypes SelfWidgetType => selfWidgetType;

        /// <summary>自身控件对应的扩展类名</summary>
        public string SelfWidgetClassName => selfWidgetClassName;

        /// <summary>是否为页面级 Binding</summary>
        public bool IsPage => isPage;

        /// <summary>页面名称（PageDef 常量名）</summary>
        public string PageName => pageName;

        /// <summary>生成的类名</summary>
        public string ClassName => className;

        /// <summary>输出根目录（覆盖 SO 的 codePath，留空则使用 SO）</summary>
        public string CodePath => codePath;

        /// <summary>输出子目录</summary>
        public string ClassPath => classPath;

        /// <summary>是否不生成代码文件（仅复制到剪贴板）</summary>
        public bool NoCodeGeneration => noCodeGen;

        /// <summary>页面类型标记</summary>
        public PageFlags PageFlags => pageFlags;

        /// <summary>是否启用预设渐入渐出动画</summary>
        public bool UsePresetFade => usePresetFade;

        /// <summary>预设渐入持续时间（秒）</summary>
        public float FadeInTime => fadeInTime;

        /// <summary>预设渐出持续时间（秒）</summary>
        public float FadeOutTime => fadeOutTime;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>验证自身控件类型不是 UILogic</summary>
        private bool ValidateSelfWidgetType(WidgetTypes type)
        {
            return type != WidgetTypes.UILogic;
        }

        #endregion

        // --------------------------------------------------------

#if UNITY_EDITOR
        #region 编辑器模板操作

        /// <summary>保存为模板回调 —— 由 Editor 程序集中的 EmberUIBindingTemplateUtility 注册</summary>
        public static System.Action<EmberUIBinding> OnSaveAsTemplate;

        /// <summary>加载模板回调 —— 由 Editor 程序集中的 EmberUIBindingTemplateUtility 注册</summary>
        public static System.Action<EmberUIBinding> OnLoadTemplate;

        /// <summary>复制模板回调 —— 由 Editor 程序集中的 EmberUIBindingTemplateUtility 注册</summary>
        public static System.Action<EmberUIBinding> OnCopyTemplate;

        /// <summary>粘贴模板回调 —— 由 Editor 程序集中的 EmberUIBindingTemplateUtility 注册</summary>
        public static System.Action<EmberUIBinding> OnPasteTemplate;

        /// <summary>是否有已复制的模板数据（内存剪贴板）</summary>
        public static bool HasCopiedTemplate { get; set; }

        /// <summary>实例属性，供 Odin [EnableIf] 表达式引用静态状态</summary>
        private bool HasTemplateCopied => HasCopiedTemplate;

        [PropertyOrder(1000)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板", ShowLabel = false)]
        [Title("模板")]
        [Button("加载模板", ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.9f)]
        private void LoadTemplate()
        {
            OnLoadTemplate?.Invoke(this);
        }

        [PropertyOrder(1001)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [Button("保存为模板", ButtonSizes.Medium), GUIColor(0.3f, 0.7f, 0.3f)]
        private void SaveAsTemplate()
        {
            OnSaveAsTemplate?.Invoke(this);
        }

        [PropertyOrder(1002)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [Button("复制", ButtonSizes.Medium), GUIColor(0.5f, 0.5f, 0.8f)]
        private void CopyTemplate()
        {
            OnCopyTemplate?.Invoke(this);
        }

        [PropertyOrder(1003)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [Button("粘贴", ButtonSizes.Medium), GUIColor(0.5f, 0.8f, 0.5f)]
        [EnableIf("HasTemplateCopied")]
        private void PasteTemplate()
        {
            if (!HasCopiedTemplate) return;
            OnPasteTemplate?.Invoke(this);
        }

        #endregion
#endif
    }
}
