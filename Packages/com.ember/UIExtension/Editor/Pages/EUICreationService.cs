// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Ember.UI;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension.Editor
{
    /// <summary>创建标准 EUI 页面所需的全部配置。</summary>
    [Serializable]
    public sealed class EUICreationRequest
    {
        public EUIBinding.CodePathMode CodePathMode = EUIBinding.CodePathMode.Business;
        public string PrefabName = "NewPanel";
        public string PageName = "NewPage";
        public string ClassPath = "Module/Page";
        public string ClassName = "NewPage";
        public PageType PageType = PageType.MainPage;
        public bool UseUIUpdate;
        public bool UseMask = true;
        public Color MaskColor = new Color(0f, 0f, 0f, 0.5f);
        public bool ClickMaskToClose = true;
        public EUIBinding.RegularTransitionMode TransitionMode = EUIBinding.RegularTransitionMode.PresetFade;
        public float FadeInTime = 0.3f;
        public float FadeOutTime = 0.2f;
        public bool GenerateAutoCreateClickableMaskOverride;
        public bool GenerateOnClickMaskOverride;
        public bool GenerateCustomSettings;

        internal EUICreationRequest Clone()
        {
            return (EUICreationRequest)MemberwiseClone();
        }
    }

    /// <summary>零写入预检解析出的标准页面创建计划。</summary>
    [Serializable]
    public sealed class EUICreationPlan
    {
        public bool IsValid;
        public string Error;
        public EUICreationRequest Request;
        public string PrefabPath;
        public string LogicScriptPath;
        public string BindingScriptPath;
        public string SettingsScriptPath;
        public string PageDefFile;
        public string AnimatorControllerPath;
        public string SafeAreaPrefabPath;

        [NonSerialized] internal string PrefabDirectory;
        [NonSerialized] internal string PageDefSiblingFile;
        [NonSerialized] internal CSharpLogicImplementationData Implementation;
        [NonSerialized] internal RuntimeAnimatorController AnimatorController;
        [NonSerialized] internal GameObject SafeAreaPrefab;
    }

    /// <summary>创建结果。失败时保留已经生成的资产，并通过本对象明确返回。</summary>
    [Serializable]
    public sealed class EUICreationResult
    {
        public bool Success;
        public string Error;
        public List<string> CreatedAssetPaths = new List<string>();
        public List<string> ModifiedAssetPaths = new List<string>();
        public string PrefabPath;
        public bool RequiresRefresh;
        public bool WaitingForCompilation;
        public EUICreationPlan Plan;

        /// <summary>构建创建失败时的部分产物摘要，包括新建和已修改资产。</summary>
        public string BuildAffectedAssetsSummary()
        {
            var created = CreatedAssetPaths.Count == 0
                ? "无"
                : string.Join("\n", CreatedAssetPaths);
            var modified = ModifiedAssetPaths.Count == 0
                ? "无"
                : string.Join("\n", ModifiedAssetPaths);
            return $"已保留的新建资产：\n{created}\n\n已修改的现有资产：\n{modified}";
        }
    }

    /// <summary>
    /// 标准 EUI 页面创建服务。预检阶段只读取配置与资产；正式创建不覆盖任何同名目标，
    /// 所有文件写入后由调用方先登记编译续接状态，再根据 RequiresRefresh 统一刷新一次。
    /// </summary>
    public static class EUICreationService
    {
        private const string SettingsAssetPath = "Assets/Ember/Editor/SOs/EUIBindingSettings.asset";
        private const string CommonAnimatorRelativePath = "Common/Animator/EUICommon_Ani.controller";
        private const string CommonSafeAreaRelativePath = "Common/Prefabs/EUISafeArea.prefab";
        private const string UserPageDefFileName = "GamePages.User.cs";
        private const string FrameworkPageDefFileName = "GamePages.cs";

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while",
        };

        private static readonly HashSet<string> ReservedWindowsNames = new HashSet<string>(
            new[]
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            }, StringComparer.OrdinalIgnoreCase);

        #region 外部方法

        /// <summary>执行零写入预检并解析全部目标路径。</summary>
        public static bool TryBuildPlan(EUICreationRequest request, out EUICreationPlan plan,
            out EUICreationResult result)
        {
            plan = new EUICreationPlan();
            result = new EUICreationResult { Plan = plan };

            if (!TryNormalizeRequest(request, out var normalized, out var error))
                return FailPlan(plan, result, error);

            plan.Request = normalized;

            if (normalized.CodePathMode == EUIBinding.CodePathMode.Framework
                && !EUIBindingCodeGenUtility.IsEmbeddedPackage())
            {
                return FailPlan(plan, result,
                    "框架模式仅允许在 com.ember embedded 开发仓库中使用；消费端请使用用户模式。");
            }

            var settings = AssetDatabase.LoadAssetAtPath<EUIBindingSettingData>(SettingsAssetPath);
            if (!settings)
                return FailPlan(plan, result, $"未找到 EUI Binding 设置资产：{SettingsAssetPath}");

            var implementation = settings.LogicImplementations?
                .OfType<CSharpLogicImplementationData>()
                .FirstOrDefault(i => i);
            if (!implementation)
                return FailPlan(plan, result, "EUI Binding 设置中未配置有效的 C# 逻辑实现。");

            if (!ValidateCodeTemplates(implementation, normalized.CodePathMode, out error))
                return FailPlan(plan, result, error);

            if (!TryNormalizeAssetRoot(implementation.UIResourceRoot, "UI 资源根目录",
                    out var uiResourceRoot, out error))
                return FailPlan(plan, result, error);

            if (!TryNormalizeAssetRoot(settings.BusinessCodeRoot, "业务代码根目录",
                    out var codeRoot, out error))
                return FailPlan(plan, result, error);

            if (!TryFindGameUIClassDeclaration(codeRoot, normalized.ClassName,
                    out var conflictingClassPath, out error))
                return FailPlan(plan, result, error);
            if (!string.IsNullOrEmpty(conflictingClassPath))
            {
                return FailPlan(plan, result,
                    $"Game.UI 命名空间中已存在同名类 {normalized.ClassName}：{conflictingClassPath}\n"
                    + "包括既有自动生成的 Binding partial 类在内，请更换类名或先处理已有页面。");
            }
            if (normalized.GenerateCustomSettings)
            {
                var settingsClassName = normalized.ClassName + "Settings";
                if (!TryFindGameUIClassDeclaration(codeRoot, settingsClassName,
                        out conflictingClassPath, out error))
                    return FailPlan(plan, result, error);
                if (!string.IsNullOrEmpty(conflictingClassPath))
                {
                    return FailPlan(plan, result,
                        $"Game.UI 命名空间中已存在同名 Settings 类型 {settingsClassName}："
                        + $"{conflictingClassPath}\n请更换类名或先处理已有类型。");
                }
            }

            plan.Implementation = implementation;
            plan.AnimatorControllerPath = $"{uiResourceRoot}/{CommonAnimatorRelativePath}";
            plan.SafeAreaPrefabPath = $"{uiResourceRoot}/{CommonSafeAreaRelativePath}";

            plan.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                plan.AnimatorControllerPath);
            plan.SafeAreaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(plan.SafeAreaPrefabPath);
            if (!plan.AnimatorController || !plan.SafeAreaPrefab)
            {
                return FailPlan(plan, result,
                    "标准页面依赖不完整。必须同时存在：\n"
                    + $"- {plan.AnimatorControllerPath}\n"
                    + $"- {plan.SafeAreaPrefabPath}");
            }

            if (!plan.SafeAreaPrefab.GetComponent<EUISafeArea>()
                || !plan.SafeAreaPrefab.transform.Find("Center"))
            {
                return FailPlan(plan, result,
                    $"EUISafeArea 预制体结构无效（缺少 EUISafeArea 组件或 Center 节点）：{plan.SafeAreaPrefabPath}");
            }

            if (LayerMask.NameToLayer("UI") < 0)
                return FailPlan(plan, result, "项目缺少名为 UI 的 Layer，无法创建标准 UI 预制体。");

            string categoryPath;
            if (normalized.CodePathMode == EUIBinding.CodePathMode.Framework)
            {
                categoryPath = "Common";
            }
            else
            {
                var moduleName = normalized.ClassPath.Split('/')[0];
                categoryPath = $"Module/{moduleName}";
            }

            plan.PrefabDirectory = $"{uiResourceRoot}/{categoryPath}/Prefabs";
            plan.PrefabPath = $"{plan.PrefabDirectory}/{normalized.PrefabName}.prefab";
            var logicBase = string.IsNullOrEmpty(normalized.ClassPath)
                ? $"{codeRoot}/{normalized.ClassName}"
                : $"{codeRoot}/{normalized.ClassPath}/{normalized.ClassName}";
            plan.LogicScriptPath = logicBase + ".cs";
            // 与 CSharpLogicImplementationData 的实际输出算法保持完全一致，避免预检漏掉目标冲突。
            plan.BindingScriptPath = logicBase + ".Binding.cs";
            plan.SettingsScriptPath = normalized.GenerateCustomSettings
                ? logicBase + "Settings.cs"
                : string.Empty;

            plan.PageDefFile = ResolvePageDefFile(implementation.PageDefFile, normalized.CodePathMode);
            plan.PageDefSiblingFile = ResolveSiblingPageDefFile(plan.PageDefFile);
            result.PrefabPath = plan.PrefabPath;

            if (string.IsNullOrEmpty(plan.PageDefFile))
                return FailPlan(plan, result, "C# 逻辑实现未配置 EUIPageDef 文件路径。");
            if (!TryValidateAssetFilePath(plan.PageDefFile, "EUIPageDef 文件",
                    out var pageDefFullPath, out error))
                return FailPlan(plan, result, error);
            if (!File.Exists(pageDefFullPath))
                return FailPlan(plan, result, $"EUIPageDef 文件不存在：{plan.PageDefFile}");

            if (HasPageDefinition(plan.PageDefFile, normalized.PageName)
                || HasPageDefinition(plan.PageDefSiblingFile, normalized.PageName))
            {
                return FailPlan(plan, result,
                    $"GamePages 中已存在同名 EUIPageDef：{normalized.PageName}。创建服务不会覆盖或更新已有页面。");
            }

            if (HasPrefabPathDefinition(plan.PageDefFile, plan.PrefabPath)
                || HasPrefabPathDefinition(plan.PageDefSiblingFile, plan.PrefabPath))
            {
                return FailPlan(plan, result,
                    $"GamePages 中已存在指向同一预制体路径的页面定义：{plan.PrefabPath}");
            }

            foreach (var target in EnumerateNewTargets(plan))
            {
                if (TargetExists(target))
                    return FailPlan(plan, result, $"目标已存在，不允许覆盖：{target}");
            }

            plan.IsValid = true;
            result.Success = true;
            return true;
        }

        /// <summary>创建标准页面预制体并生成代码。失败时不会删除已创建的资产。</summary>
        public static EUICreationResult Create(EUICreationRequest request)
        {
            if (!TryBuildPlan(request, out var plan, out var preflightResult))
                return preflightResult;

            var result = new EUICreationResult
            {
                Plan = plan,
                PrefabPath = plan.PrefabPath,
            };

            UnityEngine.SceneManagement.Scene previewScene = default;
            bool creationStarted = false;
            bool codeGenerationAttempted = false;
            var pageDefBefore = TryReadAllText(plan.PageDefFile);

            try
            {
                EnsureAssetFolder(plan.PrefabDirectory);
                creationStarted = true;

                previewScene = EditorSceneManager.NewPreviewScene();
                var root = BuildStandardPage(plan, previewScene);
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, plan.PrefabPath, out var prefabSaved);
                if (!prefabSaved || !savedPrefab)
                    throw new InvalidOperationException($"预制体保存失败：{plan.PrefabPath}");

                AddUnique(result.CreatedAssetPaths, plan.PrefabPath);

                var binding = savedPrefab.GetComponent<EUIBinding>();
                if (!binding)
                    throw new InvalidOperationException("已保存的预制体缺少 EUIBinding，已保留该预制体供排查。");

                codeGenerationAttempted = true;
                if (!EUIBindingCodeGenUtility.TryGenerateCode(binding,
                        showConfirmation: false,
                        createPrefabIfNeeded: false,
                        refreshAssets: false,
                        out var generationError))
                {
                    throw new InvalidOperationException(string.IsNullOrEmpty(generationError)
                        ? "代码生成失败，已保留当前已创建资产。"
                        : generationError);
                }

                result.Success = true;
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Error = exception.Message;
            }
            finally
            {
                if (previewScene.IsValid())
                    EditorSceneManager.ClosePreviewScene(previewScene);

                CollectCreatedTargets(plan, result.CreatedAssetPaths);
                var pageDefChanged = !string.Equals(pageDefBefore, TryReadAllText(plan.PageDefFile),
                    StringComparison.Ordinal);
                if (pageDefChanged)
                    AddUnique(result.ModifiedAssetPaths, plan.PageDefFile);
                var scriptsCreated = result.CreatedAssetPaths.Any(path =>
                    path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
                result.RequiresRefresh = creationStarted || codeGenerationAttempted;
                result.WaitingForCompilation = scriptsCreated || pageDefChanged;
            }

            return result;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static GameObject BuildStandardPage(EUICreationPlan plan,
            UnityEngine.SceneManagement.Scene previewScene)
        {
            var request = plan.Request;
            var root = new GameObject(request.PrefabName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(EUIBinding));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, previewScene);

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.localScale = Vector3.one;
            rootRect.localPosition = Vector3.zero;
            rootRect.localRotation = Quaternion.identity;

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = null;
            canvas.planeDistance = 100f;
            canvas.sortingOrder = EUIBindingEditorUtility.GetDefaultSortingOrder(request.PageType);
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2560f, 1440f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            var rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = request.PageType != PageType.Background;
            rootGroup.ignoreParentGroups = false;

            EUIBindingEditorUtility.ApplyCreationConfig(root.GetComponent<EUIBinding>(), request);

            var animatorObject = new GameObject("Animator",
                typeof(RectTransform),
                typeof(Animator),
                typeof(CanvasGroup),
                typeof(EmberPageAnimatorBridge));
            var animatorRect = animatorObject.GetComponent<RectTransform>();
            animatorRect.SetParent(rootRect, false);
            Stretch(animatorRect);

            var animator = animatorObject.GetComponent<Animator>();
            animator.runtimeAnimatorController = plan.AnimatorController;
            animator.applyRootMotion = false;
            animator.enabled = false;

            var animatorGroup = animatorObject.GetComponent<CanvasGroup>();
            animatorGroup.alpha = 1f;
            animatorGroup.interactable = true;
            animatorGroup.blocksRaycasts = false;
            animatorGroup.ignoreParentGroups = false;

            var safeAreaObject = PrefabUtility.InstantiatePrefab(plan.SafeAreaPrefab, animatorRect) as GameObject;
            if (!safeAreaObject)
                throw new InvalidOperationException($"EUISafeArea 嵌套实例创建失败：{plan.SafeAreaPrefabPath}");

            SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            return root;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            for (int i = 0; i < gameObject.transform.childCount; i++)
                SetLayerRecursively(gameObject.transform.GetChild(i).gameObject, layer);
        }

        private static bool TryNormalizeRequest(EUICreationRequest request,
            out EUICreationRequest normalized, out string error)
        {
            normalized = null;
            error = null;
            if (request == null)
            {
                error = "创建请求为空。";
                return false;
            }

            if (!Enum.IsDefined(typeof(EUIBinding.CodePathMode), request.CodePathMode))
            {
                error = $"无效的生成模式：{request.CodePathMode}";
                return false;
            }

            if (!IsSupportedPageType(request.PageType))
            {
                error = $"页面类型 {request.PageType} 不受标准页面创建器支持。";
                return false;
            }

            if (!Enum.IsDefined(typeof(EUIBinding.RegularTransitionMode), request.TransitionMode))
            {
                error = $"无效的普通过渡模式：{request.TransitionMode}";
                return false;
            }

            normalized = request.Clone();
            normalized.PrefabName = NormalizePrefabName(request.PrefabName);
            normalized.PageName = request.PageName?.Trim();
            normalized.ClassName = request.ClassName?.Trim();

            if (!TryNormalizeRelativePath(request.ClassPath, out var classPath, out error))
                return false;
            normalized.ClassPath = classPath;

            if (!IsValidFileName(normalized.PrefabName))
            {
                error = "预制体名无效。请只填写文件名，不要包含路径、保留名称或非法字符。";
                return false;
            }

            if (!IsValidCSharpIdentifier(normalized.PageName))
            {
                error = $"页面名称不是有效的 C# 标识符：{normalized.PageName}";
                return false;
            }

            if (!IsValidCSharpIdentifier(normalized.ClassName))
            {
                error = $"类名不是有效的 C# 标识符：{normalized.ClassName}";
                return false;
            }
            if (!IsValidFileName(normalized.ClassName + ".cs")
                || (normalized.GenerateCustomSettings
                    && !IsValidFileName(normalized.ClassName + "Settings.cs")))
            {
                error = $"类名会生成 Windows 保留或非法文件名：{normalized.ClassName}";
                return false;
            }

            if (normalized.CodePathMode == EUIBinding.CodePathMode.Business
                && string.IsNullOrEmpty(normalized.ClassPath))
            {
                error = "用户模式的输出子目录必须以模块名开头，例如 Inventory/Page。";
                return false;
            }

            if (!IsFiniteNonNegative(normalized.FadeInTime)
                || !IsFiniteNonNegative(normalized.FadeOutTime))
            {
                error = "进入/退出时长必须是大于或等于 0 的有限数值。";
                return false;
            }

            if (!IsFinite(normalized.MaskColor.r) || !IsFinite(normalized.MaskColor.g)
                || !IsFinite(normalized.MaskColor.b) || !IsFinite(normalized.MaskColor.a))
            {
                error = "遮罩颜色包含无效数值。";
                return false;
            }

            if (!IsPopupPage(normalized.PageType))
            {
                normalized.UseMask = false;
                normalized.ClickMaskToClose = false;
                normalized.GenerateAutoCreateClickableMaskOverride = false;
                normalized.GenerateOnClickMaskOverride = false;
            }

            return true;
        }

        private static bool ValidateCodeTemplates(CSharpLogicImplementationData implementation,
            EUIBinding.CodePathMode pathMode, out string error)
        {
            error = null;
            using (var serialized = new SerializedObject(implementation))
            {
                var bindingTemplate = serialized.FindProperty("bindingCodeTemplate")?.objectReferenceValue;
                var userTemplate = serialized.FindProperty("codeTemplate")?.objectReferenceValue;
                var frameworkTemplate = serialized.FindProperty("frameworkCodeTemplate")?.objectReferenceValue;
                if (!bindingTemplate)
                {
                    error = "C# 逻辑实现缺少绑定代码模板。";
                    return false;
                }

                if (pathMode == EUIBinding.CodePathMode.Business && !userTemplate)
                {
                    error = "用户模式未配置 codeTemplate，无法生成页面骨架。";
                    return false;
                }

                if (pathMode == EUIBinding.CodePathMode.Framework && !frameworkTemplate)
                {
                    error = "框架模式未配置 frameworkCodeTemplate，无法生成 [EmberManaged] 页面骨架。";
                    return false;
                }
            }
            return true;
        }

        private static bool TryNormalizeAssetRoot(string value, string label,
            out string normalized, out string error)
        {
            normalized = value?.Trim().Replace('\\', '/').TrimEnd('/');
            error = null;
            if (string.IsNullOrEmpty(normalized)
                || (!string.Equals(normalized, "Assets", StringComparison.Ordinal)
                    && !normalized.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                error = $"{label}必须是 Assets/ 开头的项目内路径。";
                return false;
            }

            if (!TryNormalizeRelativePath(normalized == "Assets"
                    ? string.Empty
                    : normalized.Substring("Assets/".Length), out var relative, out error))
            {
                error = $"{label}无效：{error}";
                return false;
            }

            normalized = string.IsNullOrEmpty(relative) ? "Assets" : "Assets/" + relative;
            return true;
        }

        private static bool TryNormalizeRelativePath(string value, out string normalized, out string error)
        {
            normalized = value?.Trim().Replace('\\', '/') ?? string.Empty;
            error = null;
            if (normalized.StartsWith("/", StringComparison.Ordinal)
                || normalized.EndsWith("/", StringComparison.Ordinal)
                || normalized.Contains(":")
                || normalized.Contains("//"))
            {
                error = "路径必须是规范的相对路径，不能包含根路径、盘符或空目录段。";
                return false;
            }

            if (string.IsNullOrEmpty(normalized)) return true;
            var segments = normalized.Split('/');
            foreach (var segment in segments)
            {
                if (segment == "." || segment == ".." || !IsValidFileName(segment))
                {
                    error = $"路径包含非法或穿越目录段：{segment}";
                    return false;
                }
            }
            normalized = string.Join("/", segments);
            return true;
        }

        private static string NormalizePrefabName(string value)
        {
            var name = value?.Trim() ?? string.Empty;
            return name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - ".prefab".Length)
                : name;
        }

        private static bool IsValidCSharpIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || CSharpKeywords.Contains(value)) return false;
            if (!(value[0] == '_' || char.IsLetter(value[0]))) return false;
            for (int i = 1; i < value.Length; i++)
                if (!(value[i] == '_' || char.IsLetterOrDigit(value[i]))) return false;
            return true;
        }

        private static bool IsValidFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "." || value == "..") return false;
            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            if (value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith(" ", StringComparison.Ordinal))
                return false;
            var stem = value.Split('.')[0];
            return !ReservedWindowsNames.Contains(stem);
        }

        private static bool IsSupportedPageType(PageType pageType)
        {
            return pageType == PageType.Background
                || pageType == PageType.MainPage
                || pageType == PageType.Popup
                || pageType == PageType.FullScreenPopup
                || pageType == PageType.TopMost
                || pageType == PageType.SubPage
                || pageType == PageType.FreePage;
        }

        private static bool IsPopupPage(PageType pageType)
        {
            return pageType == PageType.Popup || pageType == PageType.FullScreenPopup;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static string ResolvePageDefFile(string configuredPath, EUIBinding.CodePathMode pathMode)
        {
            var normalized = configuredPath?.Trim().Replace('\\', '/');
            if (pathMode == EUIBinding.CodePathMode.Framework
                && !string.IsNullOrEmpty(normalized)
                && normalized.EndsWith(UserPageDefFileName, StringComparison.Ordinal))
            {
                return normalized.Substring(0, normalized.Length - UserPageDefFileName.Length)
                    + FrameworkPageDefFileName;
            }
            return normalized;
        }

        private static string ResolveSiblingPageDefFile(string targetFile)
        {
            if (string.IsNullOrEmpty(targetFile)) return null;
            if (targetFile.EndsWith(UserPageDefFileName, StringComparison.Ordinal))
            {
                return targetFile.Substring(0, targetFile.Length - UserPageDefFileName.Length)
                    + FrameworkPageDefFileName;
            }
            if (targetFile.EndsWith(FrameworkPageDefFileName, StringComparison.Ordinal))
            {
                return targetFile.Substring(0, targetFile.Length - FrameworkPageDefFileName.Length)
                    + UserPageDefFileName;
            }
            return null;
        }

        private static bool TryValidateAssetFilePath(string value, string label,
            out string fullPath, out string error)
        {
            fullPath = null;
            error = null;
            if (!CSharpLogicImplementationData.TryResolveAssetsPath(value, label,
                    out var normalized, out fullPath, out error))
                return false;
            if (string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase)
                || !normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                error = $"{label}必须是 Assets/ 下的规范 C# 文件路径：{value}";
                return false;
            }
            return true;
        }

        private static bool HasPageDefinition(string assetPath, string pageName)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            var fullPath = ToFullPath(assetPath);
            if (!File.Exists(fullPath)) return false;
            return EUIPrefabCatalogService.FindPageDefinitions(File.ReadAllText(fullPath))
                .Any(match => string.Equals(match.Name, pageName, StringComparison.Ordinal));
        }

        private static bool HasPrefabPathDefinition(string assetPath, string prefabPath)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(prefabPath)) return false;
            var fullPath = ToFullPath(assetPath);
            if (!File.Exists(fullPath)) return false;
            return EUIPrefabCatalogService.FindPageDefinitions(File.ReadAllText(fullPath))
                .Any(match => string.Equals(match.PrefabPath, prefabPath,
                    StringComparison.Ordinal));
        }

        private static bool TryFindGameUIClassDeclaration(string codeRoot, string className,
            out string conflictingAssetPath, out string error)
        {
            conflictingAssetPath = null;
            error = null;
            try
            {
                var fullTypeName = "Game.UI." + className;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type existingType;
                    try
                    {
                        existingType = assembly.GetType(fullTypeName, throwOnError: false,
                            ignoreCase: false);
                    }
                    catch
                    {
                        continue;
                    }
                    if (existingType == null) continue;
                    conflictingAssetPath = $"已编译类型 {existingType.Assembly.GetName().Name}:{fullTypeName}";
                    return true;
                }

                var fullRoot = ToFullPath(codeRoot);
                if (!Directory.Exists(fullRoot)) return true;

                var namespacePattern = @"\bnamespace\s+Game\s*\.\s*UI\s*(?:;|\{)";
                var classPattern = $@"\b(?:class|record\s+class)\s+{Regex.Escape(className)}\b";
                foreach (var file in Directory.GetFiles(fullRoot, "*.cs", SearchOption.AllDirectories))
                {
                    var content = EUIPrefabCatalogService.MaskCommentsAndLiterals(
                        File.ReadAllText(file));
                    if (!ContainsClassDeclarationInGameUI(content, namespacePattern, classPattern))
                        continue;

                    conflictingAssetPath = codeRoot.TrimEnd('/') + "/"
                        + file.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar).Replace('\\', '/');
                    return true;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"扫描 Game.UI 类名冲突失败：{exception.Message}";
                return false;
            }
        }

        private static bool ContainsClassDeclarationInGameUI(string content,
            string namespacePattern, string classPattern)
        {
            var depths = new int[content.Length + 1];
            for (int i = 0; i < content.Length; i++)
            {
                depths[i + 1] = depths[i];
                if (content[i] == '{') depths[i + 1]++;
                else if (content[i] == '}') depths[i + 1]--;
            }

            var classMatches = Regex.Matches(content, classPattern, RegexOptions.CultureInvariant);
            if (classMatches.Count == 0) return false;

            foreach (Match namespaceMatch in Regex.Matches(content, namespacePattern,
                         RegexOptions.CultureInvariant))
            {
                var delimiterIndex = namespaceMatch.Index + namespaceMatch.Length - 1;
                var namespaceDepth = depths[delimiterIndex];
                if (content[delimiterIndex] == ';')
                {
                    foreach (Match classMatch in classMatches)
                        if (classMatch.Index > delimiterIndex && depths[classMatch.Index] == namespaceDepth)
                            return true;
                    continue;
                }

                var closingBrace = FindClosingBrace(content, delimiterIndex);
                if (closingBrace < 0) continue;
                foreach (Match classMatch in classMatches)
                {
                    if (classMatch.Index > delimiterIndex && classMatch.Index < closingBrace
                        && depths[classMatch.Index] == namespaceDepth + 1)
                        return true;
                }
            }

            // 等价的块式写法：namespace Game { namespace UI { class Xxx ... } }
            foreach (Match gameNamespace in Regex.Matches(content,
                         @"\bnamespace\s+Game\s*\{", RegexOptions.CultureInvariant))
            {
                var gameBrace = gameNamespace.Index + gameNamespace.Length - 1;
                var gameEnd = FindClosingBrace(content, gameBrace);
                if (gameEnd < 0) continue;
                var gameDepth = depths[gameBrace];

                foreach (Match uiNamespace in Regex.Matches(content,
                             @"\bnamespace\s+UI\s*\{", RegexOptions.CultureInvariant))
                {
                    var uiBrace = uiNamespace.Index + uiNamespace.Length - 1;
                    if (uiBrace <= gameBrace || uiBrace >= gameEnd
                        || depths[uiBrace] != gameDepth + 1) continue;

                    var uiEnd = FindClosingBrace(content, uiBrace);
                    if (uiEnd < 0 || uiEnd > gameEnd) continue;
                    foreach (Match classMatch in classMatches)
                    {
                        if (classMatch.Index > uiBrace && classMatch.Index < uiEnd
                            && depths[classMatch.Index] == gameDepth + 2)
                            return true;
                    }
                }
            }

            return false;
        }

        private static int FindClosingBrace(string content, int openingBrace)
        {
            var depth = 0;
            for (int i = openingBrace; i < content.Length; i++)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}' && --depth == 0) return i;
            }
            return -1;
        }

        private static IEnumerable<string> EnumerateNewTargets(EUICreationPlan plan)
        {
            yield return plan.PrefabPath;
            yield return plan.LogicScriptPath;
            yield return plan.BindingScriptPath;
            if (!string.IsNullOrEmpty(plan.SettingsScriptPath))
                yield return plan.SettingsScriptPath;
        }

        private static bool TargetExists(string assetPath)
        {
            var fullPath = ToFullPath(assetPath);
            return File.Exists(fullPath)
                || File.Exists(fullPath + ".meta")
                || Directory.Exists(fullPath)
                || AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("无法解析 Unity 项目根目录。");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var segments = assetFolder.Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
                throw new InvalidOperationException($"无法创建非 Assets 目录：{assetFolder}");

            var current = "Assets";
            for (int i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, segments[i]);
                    if (string.IsNullOrEmpty(guid))
                        throw new IOException($"创建目录失败：{next}");
                }
                current = next;
            }
        }

        private static void CollectCreatedTargets(EUICreationPlan plan, List<string> createdAssetPaths)
        {
            foreach (var target in EnumerateNewTargets(plan))
                if (File.Exists(ToFullPath(target)) || AssetDatabase.LoadMainAssetAtPath(target))
                    AddUnique(createdAssetPaths, target);
        }

        private static void AddUnique(List<string> paths, string path)
        {
            if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                paths.Add(path);
        }

        private static string TryReadAllText(string assetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath)) return null;
                var fullPath = ToFullPath(assetPath);
                return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool FailPlan(EUICreationPlan plan, EUICreationResult result, string error)
        {
            plan.IsValid = false;
            plan.Error = error;
            result.Success = false;
            result.Error = error;
            result.PrefabPath = plan.PrefabPath;
            return false;
        }

        #endregion
    }
}
