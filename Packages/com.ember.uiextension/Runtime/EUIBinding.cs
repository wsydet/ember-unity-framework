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
        None = 0,
        MainPage = 1 << 0,
        Popup = 1 << 1,
        TopMost = 1 << 2,
        SubPage = 1 << 3,
        FreePage = 1 << 4,
        Background = 1 << 5,
    }

    /// <summary>
    /// UI 控件绑定组件 —— 挂载到 UI 预制体根节点上，记录每个子控件的名称、类型和 GameObject 引用。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("EUI/EUIBinding")]
    public class EUIBinding : MonoBehaviour
    {
        #region 嵌套类型

        /// <summary>
        /// 代码生成路径模式：框架 (Framework) 或业务 (Business)。
        /// </summary>
        public enum CodePathMode
        {
            Framework = 0,
            Business = 1,
        }

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
            CanvasGroup = 14,
            // 新加类型一定要写在这行上面
            End,
            Extension = 65535,
        }

        [Serializable]
        public struct BindingEntry
        {
            [LabelText("变量名")]
            public string Name;

            [LabelText("节点")]
            public GameObject GameObject;

            [LabelText("类型")]
            public WidgetTypes Type;

            [LabelText("类名")]
            public string ClassName;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private const string GROUP = "EUI Binding";

        #endregion

        // --------------------------------------------------------

        #region 编辑器面板参数

        // ═══════════════════════════════════════
        // P1: 模板
        // ═══════════════════════════════════════

        // (editor-only, see #if UNITY_EDITOR below)

        // ═══════════════════════════════════════
        // P2: 继承
        // ═══════════════════════════════════════

        [PropertyOrder(-90)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/继承")]
        [SerializeField, HideInInspector]
        private string baseBindingUUID;

        // ═══════════════════════════════════════
        // P3: 输出设置
        // ═══════════════════════════════════════

        [PropertyOrder(-80)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置", ShowLabel = false)]
        [Title("输出设置")]
        [SerializeField, LabelText("路径模式")]
        [Tooltip("框架路径或业务路径，决定输出的根目录")]
        private CodePathMode codePathMode;

        [PropertyOrder(-79)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [ShowInInspector, ReadOnly, LabelText("输出根目录")]
        [Tooltip("由路径模式决定（在 Project Settings 中配置）")]
        private string CodeRootPath => OnGetCodeRootPath?.Invoke(codePathMode) ?? "（未配置）";

        [PropertyOrder(-78)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("输出子目录")]
        [Tooltip("相对于根目录的子目录，如 MainMenu 或 Battle/UI")]
        private string classPath;

        [PropertyOrder(-77)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("类名")]
        [Tooltip("生成的 partial class 名称，如 UIMainMenu")]
        [Required("请输入类名（如 UIMainMenu）")]
        private string className;

        [PropertyOrder(-77)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("预制体名")]
        [Tooltip("生成的 .prefab 文件名。留空则使用类名。")]
        [ShowIf("@!noCodeGen")]
        private string prefabName;

        [PropertyOrder(-77)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("不生成代码到文件")]
        [Tooltip("勾选后代码只复制到剪贴板，不写入文件")]
        private bool noCodeGen;

        [PropertyOrder(-76)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/输出设置")]
        [SerializeField, LabelText("生成自定义参数")]
        [Tooltip("勾选后生成 {类名}Settings.cs 模板文件，用于定义页面专属的可视化参数")]
        [ShowIf("@!noCodeGen")]
        private bool generateCustomSettings;

        // ═══════════════════════════════════════
        // P4: 页面配置（含自身控件）
        // ═══════════════════════════════════════

        [PropertyOrder(-70)]
        [FoldoutGroup("$GROUP", Expanded = true)]
        [BoxGroup("$GROUP/页面配置", ShowLabel = false)]
        [Title("页面配置")]
        [SerializeField, LabelText("是否为 Page")]
        [Tooltip("勾选后此 binding 为页面级，会生成 EUIPageDef")]
        [ShowIf("@!noCodeGen")]
        private bool isPage;

        [PropertyOrder(-69)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("页面名称")]
        [Tooltip("EUIPageDef 常量名，如 MainMenu、Settings")]
        [ShowIf("@isPage && !noCodeGen"), Required("请输入页面名称")]
        private string pageName;

        [PropertyOrder(-68)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("页面类型")]
        [Tooltip("MainPage / Popup / TopMost / SubPage / FreePage / Background")]
        [ShowIf("@isPage && !noCodeGen")]
        [InfoBox("Background 不能与其他页面类型组合。", InfoMessageType.Error, "HasConflictingPageFlags")]
        private PageFlags pageFlags;

        [PropertyOrder(0)]
        [FoldoutGroup("过渡动画")]
        [SerializeField, LabelText("使用预设渐入渐出")]
        [Tooltip("勾选后使用 CanvasGroup alpha 渐入渐出，时长由下方滑条控制。与「使用方块过渡」互斥二选一。")]
        [ShowIf("@isPage && !noCodeGen")]
        [OnValueChanged("OnPresetFadeChanged")]
        private bool usePresetFade = true;

        [PropertyOrder(0.5f)]
        [FoldoutGroup("过渡动画")]
        [SerializeField, LabelText("使用方块过渡")]
        [Tooltip("勾选后预设过渡改用方块扫入/扫出（EUITransitionBlock），与「使用预设渐入渐出」互斥二选一。需在页面下挂 m_TransitionBlock 子物体。")]
        [ShowIf("@isPage && !noCodeGen && HasTransitionBlock")]
        [OnValueChanged("OnTransitionBlockChanged")]
        private bool useTransitionBlock;

        [PropertyOrder(1)]
        [FoldoutGroup("过渡动画")]
        [SerializeField, LabelText("进入时长 (秒)")]
        [ShowIf("@isPage && usePresetFade && !noCodeGen")]
        private float fadeInTime = 0.3f;

        [PropertyOrder(2)]
        [FoldoutGroup("过渡动画")]
        [SerializeField, LabelText("退出时长 (秒)")]
        [ShowIf("@isPage && usePresetFade && !noCodeGen")]
        private float fadeOutTime = 0.2f;

        [PropertyOrder(3)]
        [FoldoutGroup("过渡动画")]
        [SerializeField, LabelText("使用自定义动画")]
        [Tooltip("勾选后调用业务脚本中的 OnCustomEnter / OnCustomExit 方法。可与预设叠加：先播预设再播自定义。")]
        [ShowIf("@isPage && !noCodeGen")]
        [OnValueChanged("CheckCustomTransitionMethods")]
        [InfoBox("已勾选自定义动画，但业务脚本中缺少 OnCustomEnter / OnCustomExit 方法。\n请点击下方的\"重新生成\"按钮添加。", InfoMessageType.Warning, "_needCustomTransitionMethods")]
        private bool useCustomTransition;

        // -- 自身控件（归入页面配置） --

        [PropertyOrder(-64)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [ValidateInput("ValidateSelfWidgetType", "自身控件类型不能为 UI Logic")]
        [SerializeField, LabelText("自身控件类型")]
        private WidgetTypes selfWidgetType;

        [PropertyOrder(-63)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [SerializeField, LabelText("自身控件类名")]
        [ShowIf("@selfWidgetType == WidgetTypes.Extension")]
        private string selfWidgetClassName;

        /// <summary>
        /// 自定义页面参数。由生成的代码定义具体类型（如 EUILoadingSettings），
        /// 通过 SerializeReference 序列化到预制体上，Odin InlineProperty 内联显示。
        /// 为 null 时不显示自定义折叠框。
        /// </summary>
        [PropertyOrder(10)]
        [FoldoutGroup("$className")]
        [ShowIf("HasCustomSettings")]
        [SerializeField, SerializeReference, InlineProperty, HideLabel]
        private object _pageSettings;

#if UNITY_EDITOR
        [PropertyOrder(-62)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [ShowInInspector, ReadOnly, LabelText("可用控件类型")]
        [ShowIf("@selfWidgetType != WidgetTypes.UILogic")]
        private string AvailableWidgetTypeHint
        {
            get
            {
                var types = OnGetAvailableSelfWidgetTypes?.Invoke(this);
                if (types == null || types.Length == 0) return "（检测中...）";
                return string.Join("、", types);
            }
        }

        [PropertyOrder(-61)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/页面配置")]
        [Button("自动识别类型", ButtonSizes.Small), GUIColor(0.4f, 0.7f, 0.9f)]
        private void AutoDetectSelfWidgetType()
        {
            OnAutoDetectSelfWidgetType?.Invoke(this);
        }
#endif

        // ═══════════════════════════════════════
        // P5: 搜索
        // ═══════════════════════════════════════

        // (editor-only, see #if UNITY_EDITOR below)

        // ═══════════════════════════════════════
        // P6: 控件绑定
        // ═══════════════════════════════════════

        [PropertyOrder(-50)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/绑定列表", ShowLabel = false)]
        [Title("控件绑定")]
        [EUIBindingList]
        [SerializeField, HideLabel]
        private BindingEntry[] bindings = Array.Empty<BindingEntry>();

        // ═══════════════════════════════════════
        // P7: 代码生成
        // ═══════════════════════════════════════

        // (editor-only, see #if UNITY_EDITOR below)

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>自定义页面参数（生成的代码中通过此属性读取）</summary>
        public object PageSettings => _pageSettings;

        public BindingEntry[] Bindings => bindings;
        public WidgetTypes SelfWidgetType => selfWidgetType;
        public string SelfWidgetClassName => selfWidgetClassName;
        public bool IsPage => isPage;
        public string PageName => pageName;
        public string ClassName => className;
        public string PrefabName => !string.IsNullOrEmpty(prefabName) ? prefabName : ClassName;
        public CodePathMode PathMode => codePathMode;
        public string CodePath => OnGetCodeRootPath?.Invoke(codePathMode);
        public string ClassPath => classPath;
        public bool NoCodeGeneration => noCodeGen;
        public bool GenerateCustomSettings => generateCustomSettings;
        public PageFlags PageFlags => pageFlags;
        public bool UsePresetFade => usePresetFade;
        public bool UseTransitionBlock => useTransitionBlock;
        public bool UseCustomTransition => useCustomTransition;
        public float FadeInTime => fadeInTime;
        public float FadeOutTime => fadeOutTime;

        // Custom 过渡方法缺失警告缓存（非序列化，由 OnValueChanged 和代码生成工具设置）
        private bool _needCustomTransitionMethods;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 过渡模式切换时校验业务脚本是否包含 OnCustomEnter/OnCustomExit。
        /// 作为 [OnValueChanged] 回调，仅在 Inspector 中修改 transitionMode 时触发。
        /// </summary>
        private void CheckCustomTransitionMethods()
        {
            _needCustomTransitionMethods = false;
            if (!useCustomTransition) return;

            var path = OnGetGeneratedPath?.Invoke(this);
            if (string.IsNullOrEmpty(path) || path == "—") return;

            var fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", path));
            if (!System.IO.File.Exists(fullPath)) return;

            var content = System.IO.File.ReadAllText(fullPath);
            _needCustomTransitionMethods = !content.Contains("OnCustomEnter") || !content.Contains("OnCustomExit");
        }

        /// <summary>
        /// 刷新 Custom 过渡方法状态（由代码生成工具在注入/移除方法后调用）。
        /// </summary>
        public void RefreshCustomTransitionCheck()
        {
            CheckCustomTransitionMethods();
        }

        /// <summary>勾选「使用预设渐入渐出」时，取消「使用方块过渡」（互斥二选一）。</summary>
        private void OnPresetFadeChanged()
        {
            if (usePresetFade) useTransitionBlock = false;
        }

        /// <summary>勾选「使用方块过渡」时，取消「使用预设渐入渐出」（互斥二选一）。</summary>
        private void OnTransitionBlockChanged()
        {
            if (useTransitionBlock) usePresetFade = false;
        }

        /// <summary>页面下是否挂了方块过渡组件（EUITransitionBlock），决定「使用方块过渡」选项是否显示。</summary>
        private bool HasTransitionBlock => GetComponentInChildren<EUITransitionBlock>(true) != null;

        private bool ValidateSelfWidgetType(WidgetTypes type)
        {
            return type != WidgetTypes.UILogic;
        }

        private bool HasConflictingPageFlags =>
            pageFlags.HasFlag(PageFlags.Background) && (pageFlags & ~PageFlags.Background) != 0;

        /// <summary>
        /// 组件添加时自动从 GameObject 名称推断脚本名和页面名。
        /// 移除常见 UI 尾缀（Panel/Page/PopUp 等）作为脚本名，保留原名作为页面名。
        /// </summary>
        private void Reset()
        {
            // 只在首次添加时自动填充（className 为空）
            if (!string.IsNullOrEmpty(className)) return;

            var goName = gameObject.name;

            // 去除常见的 UI 尾缀
            var scriptName = goName;
            string[] suffixes = { "Panel", "Page", "PopUp", "Popup", "Window", "Dialog", "View", "Tab", "Menu", "Screen" };
            foreach (var suffix in suffixes)
            {
                if (scriptName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase)
                    && scriptName.Length > suffix.Length)
                {
                    scriptName = scriptName.Substring(0, scriptName.Length - suffix.Length);
                    break;
                }
            }

            className = scriptName;
            pageName = goName;
            isPage = true;
        }

        #endregion

        // --------------------------------------------------------

#if UNITY_EDITOR
        #region 编辑器：警告

        public static System.Func<EUIBinding, bool> OnIsOnPrefab;

        private bool NotOnPrefab => !(OnIsOnPrefab?.Invoke(this) ?? true);

        [PropertyOrder(-110)]
        [FoldoutGroup("$GROUP")]
        [InfoBox("当前 UI 不在预制体上，生成代码时会自动创建预制体到 Prefabs 目录下。", InfoMessageType.Warning, "NotOnPrefab")]

        #endregion

        // --------------------------------------------------------

        #region 编辑器：模板（P1）

        public static System.Action<EUIBinding> OnSaveAsTemplate;
        public static System.Action<EUIBinding> OnLoadTemplate;
        public static System.Action<EUIBinding> OnCopyTemplate;
        public static System.Action<EUIBinding> OnPasteTemplate;
        public static bool HasCopiedTemplate { get; set; }
        private bool HasTemplateCopied => HasCopiedTemplate;

        // 标题占位：Title + DisplayAsString 空行，防止分割线与下方按钮渲染冲突
        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板", ShowLabel = false)]
        [Title("模板")]
        [ShowInInspector, DisplayAsString, HideLabel]
        private string _templateTitleDummy => "";

        // 4 按钮同一行
        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [HorizontalGroup("$GROUP/模板/Buttons")]
        [Button("加载", ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.9f)]
        private void LoadTemplate() => OnLoadTemplate?.Invoke(this);

        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [HorizontalGroup("$GROUP/模板/Buttons")]
        [Button("保存", ButtonSizes.Medium), GUIColor(0.3f, 0.7f, 0.3f)]
        private void SaveAsTemplate() => OnSaveAsTemplate?.Invoke(this);

        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [HorizontalGroup("$GROUP/模板/Buttons")]
        [Button("复制", ButtonSizes.Medium), GUIColor(0.5f, 0.5f, 0.8f)]
        private void CopyTemplate() => OnCopyTemplate?.Invoke(this);

        [PropertyOrder(-100)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/模板")]
        [HorizontalGroup("$GROUP/模板/Buttons")]
        [Button("粘贴", ButtonSizes.Medium), GUIColor(0.5f, 0.8f, 0.5f)]
        [EnableIf("HasTemplateCopied")]
        private void PasteTemplate()
        {
            if (!HasCopiedTemplate) return;
            OnPasteTemplate?.Invoke(this);
        }

        /// <summary>自动检测自身控件类型回调</summary>
        public static System.Action<EUIBinding> OnAutoDetectSelfWidgetType;

        /// <summary>获取 GO 上可用的组件类型名列表</summary>
        public static System.Func<EUIBinding, string[]> OnGetAvailableSelfWidgetTypes;

        #endregion

        // --------------------------------------------------------

        #region 编辑器：继承（P2）

        public static System.Func<EUIBinding, GameObject> OnGetBasePrefabObject;
        public static System.Action<EUIBinding, GameObject> OnSetBasePrefabObject;
        public static System.Func<string, string> OnGetBaseInfoSummary;
        public static System.Action<EUIBinding> OnAutoFixMissingBindings;
        public static System.Func<EUIBinding, int> OnGetMissingFieldCount;
        public static System.Func<EUIBinding, bool> OnHasInheritanceConflict;

        private bool HasMissingFields => (OnGetMissingFieldCount?.Invoke(this) ?? 0) > 0;
        private bool HasInheritanceConflict => OnHasInheritanceConflict?.Invoke(this) ?? false;

        [PropertyOrder(-89)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/继承", ShowLabel = false)]
        [Title("继承")]
        [InfoBox("Page 和非 Page 对象无法相互继承。请检查基类与当前的是否为 Page 设置是否一致。", InfoMessageType.Error, "HasInheritanceConflict")]
        [ShowInInspector, LabelText("基类 Prefab")]
        private GameObject BasePrefabObject
        {
            get => OnGetBasePrefabObject?.Invoke(this);
            set => OnSetBasePrefabObject?.Invoke(this, value);
        }

        [PropertyOrder(-88)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/继承")]
        [ShowInInspector, ReadOnly, LabelText("基类信息")]
        [ShowIf("@!string.IsNullOrEmpty(baseBindingUUID)")]
        private string BaseInfoSummary
        {
            get
            {
                if (string.IsNullOrEmpty(baseBindingUUID)) return "（未设置）";
                return OnGetBaseInfoSummary?.Invoke(baseBindingUUID) ?? "...";
            }
        }

        [PropertyOrder(-87)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/继承")]
        [Button("自动添加缺失的绑定", ButtonSizes.Medium), GUIColor(0.9f, 0.6f, 0.2f)]
        [ShowIf("HasMissingFields")]
        private void AutoFixMissingBindings()
        {
            OnAutoFixMissingBindings?.Invoke(this);
        }

        #endregion

        // --------------------------------------------------------

        #region 编辑器：搜索（P5）

        public static string BindingSearchText { get; set; }
        public static GameObject BindingSearchObject { get; set; }

        [PropertyOrder(-60)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/搜索", ShowLabel = false)]
        [Title("搜索绑定")]
        [ShowInInspector, LabelText("按名称")]
        private string SearchText
        {
            get => BindingSearchText;
            set => BindingSearchText = value;
        }

        [PropertyOrder(-59)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/搜索")]
        [ShowInInspector, LabelText("按节点")]
        private GameObject SearchObject
        {
            get => BindingSearchObject;
            set => BindingSearchObject = value;
        }

        [PropertyOrder(-58)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/搜索")]
        [Button("清除搜索条件", ButtonSizes.Small), GUIColor(0.6f, 0.6f, 0.6f)]
        [ShowIf("@!string.IsNullOrEmpty(BindingSearchText) || BindingSearchObject")]
        private void ClearSearch()
        {
            BindingSearchText = string.Empty;
            BindingSearchObject = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 编辑器：扫描子组件

        public static System.Action<EUIBinding> OnScanUnboundChildren;
        public static string ScanUnboundResult { get; set; }

        private bool HasUnboundWarning => !string.IsNullOrEmpty(ScanUnboundResult);
        private string UnboundWarningMessage => ScanUnboundResult;

        [PropertyOrder(-45)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/绑定列表")]
        [Button("扫描子组件", ButtonSizes.Medium), GUIColor(0.9f, 0.7f, 0.2f)]
        private void ScanUnboundChildren() => OnScanUnboundChildren?.Invoke(this);

        [PropertyOrder(-44)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/绑定列表")]
        [InfoBox("$UnboundWarningMessage", InfoMessageType.Warning, "HasUnboundWarning")]
        [ShowInInspector, DisplayAsString, HideLabel, HideIf("@true")]
        private string _scanResultDummy => ScanUnboundResult;

        #endregion

        // --------------------------------------------------------

        #region 编辑器：代码生成（P7）

        /// <summary>根据路径模式获取根目录（由 settings 提供）</summary>
        public static System.Func<CodePathMode, string> OnGetCodeRootPath;

        public static int CodeGenLogicIndex { get; set; }
        public static System.Func<string[]> OnGetLogicNames;
        public static System.Func<EUIBinding, string> OnGetGeneratedPath;
        public static System.Func<EUIBinding, bool> OnHasGeneratedFile;
        public static System.Func<EUIBinding, UnityEngine.Object> OnGetGeneratedScript;
        public static System.Action<EUIBinding> OnGenerateCode;
        public static System.Action<EUIBinding> OnGenerateToClipboard;
        public static System.Action<EUIBinding> OnAutoCollectBindings;
        public static System.Action<EUIBinding> OnClearAndRecollect;
        public static System.Action<EUIBinding> OnClearAllBindings;
        public static System.Action<EUIBinding> OnCopyGeneratedPath;
        public static System.Action OnOpenCodeGenSettings;

        private bool CanGenerate => OnGenerateCode != null;
        private bool HasGeneratedFile => OnHasGeneratedFile?.Invoke(this) ?? false;

        private string CurrentLogicName
        {
            get
            {
                var names = OnGetLogicNames?.Invoke();
                if (names == null || names.Length == 0) return "（未配置）";
                var idx = CodeGenLogicIndex;
                if (idx < 0 || idx >= names.Length) idx = 0;
                return names[idx];
            }
        }

        [PropertyOrder(-40)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成", ShowLabel = false)]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/TitleRow")]
        [Title("代码生成", bold: true, horizontalLine: true), HideLabel]
        [ShowInInspector, DisplayAsString]
        private string _genTitleDummy => "";

        [PropertyOrder(-40)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/TitleRow", Width = 25)]
        [Button("⚙", ButtonSizes.Small), GUIColor(0.6f, 0.6f, 0.6f)]
        private void OpenSettings() => OnOpenCodeGenSettings?.Invoke();

        // ── noCodeGen 模式 ──

        [PropertyOrder(-39)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@noCodeGen")]
        [Button("生成到剪贴板", ButtonSizes.Large), GUIColor(0.7f, 0.4f, 0.7f)]
        private void GenerateToClipboard()
        {
            OnGenerateToClipboard?.Invoke(this);
        }

        // ── 逻辑实现下拉 ──

        public static System.Action<EUIBinding> OnShowLogicMenu;

        [PropertyOrder(-38)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/LogicRow")]
        [ShowInInspector, DisplayAsString, GUIColor(0.8f, 0.8f, 0.8f)]
        [LabelText("逻辑实现"), LabelWidth(55)]
        private string _logicLabelDummy => "";

        [PropertyOrder(-38)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/LogicRow")]
        [Button("$CurrentLogicButtonLabel", ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 0.8f)]
        private void ShowLogicMenu() => OnShowLogicMenu?.Invoke(this);

        private string CurrentLogicButtonLabel
        {
            get
            {
                var name = CurrentLogicName;
                return string.IsNullOrEmpty(name) ? "选择..." : $"{name} ▾";
            }
        }

        // ── 生成路径 + 复制 ──

        [PropertyOrder(-37)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/PathRow")]
        [ShowInInspector, ReadOnly, LabelText("生成路径")]
        private string GeneratedPath => OnGetGeneratedPath?.Invoke(this) ?? "—";

        [PropertyOrder(-37)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/PathRow", Width = 60)]
        [Button("复制", ButtonSizes.Small), GUIColor(0.5f, 0.5f, 0.5f)]
        private void CopyPath() => OnCopyGeneratedPath?.Invoke(this);

        // ── 逻辑脚本 ──

        [PropertyOrder(-36)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [ShowInInspector, LabelText("逻辑脚本")]
        [InlineButton("SelectScript", "定位")]
        private UnityEngine.Object GeneratedScript
        {
            get => OnGetGeneratedScript?.Invoke(this);
            set
            {
                // 自动修正：点击选择框时定位到真正的生成脚本
                var script = OnGetGeneratedScript?.Invoke(this);
                if (script) { UnityEditor.Selection.activeObject = script; }
            }
        }

        private void SelectScript()
        {
            var script = OnGetGeneratedScript?.Invoke(this);
            if (script) { UnityEditor.Selection.activeObject = script; }
        }

        // ── 操作按钮行 ──

        [PropertyOrder(-35)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/ActionRow")]
        [Button("自动收集子控件", ButtonSizes.Medium), GUIColor(0.4f, 0.7f, 0.9f)]
        private void AutoCollectBindings() => OnAutoCollectBindings?.Invoke(this);

        [PropertyOrder(-35)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/ActionRow")]
        [Button("清除并重新收集", ButtonSizes.Medium), GUIColor(0.7f, 0.5f, 0.3f)]
        private void ClearAndRecollect() => OnClearAndRecollect?.Invoke(this);

        [PropertyOrder(-35)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen")]
        [HorizontalGroup("$GROUP/代码生成/ActionRow")]
        [Button("清除所有绑定", ButtonSizes.Medium), GUIColor(0.8f, 0.3f, 0.3f)]
        private void ClearAllBindings() => OnClearAllBindings?.Invoke(this);

        // ── 生成代码按钮 ──

        [PropertyOrder(-34)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen && !HasGeneratedFile")]
        [Button("生成代码", ButtonSizes.Large), GUIColor(0.2f, 0.7f, 0.2f)]
        [EnableIf("CanGenerate")]
        private void GenerateCode() => OnGenerateCode?.Invoke(this);

        [PropertyOrder(-34)]
        [FoldoutGroup("$GROUP")]
        [BoxGroup("$GROUP/代码生成")]
        [ShowIf("@!noCodeGen && HasGeneratedFile")]
        [Button("重新生成", ButtonSizes.Large), GUIColor(0.9f, 0.6f, 0.2f)]
        [EnableIf("CanGenerate")]
        private void RegenerateCode() => OnGenerateCode?.Invoke(this);

        #endregion

        // --------------------------------------------------------

        #region 编辑器：自定义参数

        /// <summary>根据 className 创建自定义 settings 实例的回调</summary>
        public static System.Func<string, object> OnCreateCustomSettings;

        private bool HasCustomSettings =>
            _pageSettings != null && _pageSettings.GetType().Name == $"{ClassName}Settings";

        #endregion
#endif
    }
}
