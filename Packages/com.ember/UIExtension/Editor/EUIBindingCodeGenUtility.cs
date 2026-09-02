// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Ember.Basic;
using Ember.UI;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding 代码生成工具（逻辑实现选择、路径预览、生成/重新生成、剪贴板、自动收集）。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingCodeGenUtility
    {
        private const string TAG = LogTags.EmberUI;

        #region 生命周期（初始化）

        static EUIBindingCodeGenUtility()
        {
            EUIBinding.OnIsOnPrefab = HandleIsOnPrefab;
            EUIBinding.OnIsEmbeddedPackage = IsEmbeddedPackage;
            EUIBinding.OnGetCodeRootPath = HandleGetCodeRootPath;
            EUIBinding.OnGetLogicNames = HandleGetLogicNames;
            EUIBinding.OnGetGeneratedPath = HandleGetGeneratedPath;
            EUIBinding.OnGetGeneratedPrefabPath = HandleGetGeneratedPrefabPath;
            EUIBinding.OnHasGeneratedFile = HandleHasGeneratedFile;
            EUIBinding.OnGetGeneratedScript = HandleGetGeneratedScript;
            EUIBinding.OnGenerateCode = HandleGenerateCode;
            EUIBinding.OnGenerateToClipboard = HandleGenerateToClipboard;
            EUIBinding.OnAutoCollectBindings = HandleAutoCollectBindings;
            EUIBinding.OnClearAndRecollect = HandleClearAndRecollect;
            EUIBinding.OnClearAllBindings = HandleClearAllBindings;
            EUIBinding.OnCopyGeneratedPath = HandleCopyGeneratedPath;
            EUIBinding.OnOpenCodeGenSettings = HandleOpenSettings;
            EUIBinding.OnShowLogicMenu = HandleShowLogicMenu;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 数据提供

        private static bool HandleIsOnPrefab(EUIBinding binding)
        {
            if (!binding) return true;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
            if (!string.IsNullOrEmpty(path)) return true;
            // 也可能直接在 Prefab Stage 中编辑
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.IsPartOfPrefabContents(binding.gameObject);
        }

        /// <summary>获取 binding 所在预制体的 Asset 路径（实例或 Prefab Stage；场景对象返回 null）。</summary>
        private static string GetBindingPrefabPath(EUIBinding binding)
        {
            if (!binding) return null;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
            if (!string.IsNullOrEmpty(path)) return path;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.IsPartOfPrefabContents(binding.gameObject))
                return stage.assetPath;
            return null;
        }

        /// <summary>是否为框架开发仓库（com.ember 为 embedded 安装）——消费端隐藏「框架」生成模式。</summary>
        internal static bool IsEmbeddedPackage()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.ember");
            return info != null && info.source == UnityEditor.PackageManager.PackageSource.Embedded;
        }

        private static string HandleGetCodeRootPath(EUIBinding.CodePathMode mode)
        {
            // 双路径已合并：框架/用户统一生成到业务层（Assets/Game/UI/Runtime），模式只决定生成文件的块标记
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            return settings.BusinessCodeRoot;
        }

        private static string[] HandleGetLogicNames()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null
                || settings.LogicImplementations.Length == 0)
                return new[] { "（未配置）" };

            var names = new string[settings.LogicImplementations.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = settings.LogicImplementations[i]
                    ? settings.LogicImplementations[i].name
                    : "（缺失）";
            return names;
        }

        private static string HandleGetGeneratedPath(EUIBinding binding)
        {
            if (!binding) return "—";

            var logic = GetCurrentLogic(binding);
            if (!logic) return "（无逻辑实现）";

            if (string.IsNullOrEmpty(binding.ClassName))
                return "（请先填写类名）";

            // 优先使用 binding 的路径模式根目录，回退到逻辑实现的 codePath
            var root = !string.IsNullOrEmpty(binding.CodePath)
                ? binding.CodePath
                : GetLogicCodePath(logic);

            if (string.IsNullOrEmpty(root))
                return "（请先在 Project Settings 中配置代码生成路径）";

            var subDir = string.IsNullOrEmpty(binding.ClassPath)
                ? ""
                : binding.ClassPath + "/";

            return $"{root}/{subDir}{binding.ClassName}{logic.CodeFileExtension}";
        }

        private static string HandleGetGeneratedPrefabPath(EUIBinding binding)
        {
            if (!binding) return "—";

            var impl = CSharpLogicImplementationData.FindDefault();
            if (!impl) return "（无 C# 逻辑实现）";

            return impl.TryResolvePrefabPath(binding, out var prefabPath, out var error)
                ? prefabPath
                : $"（{error}）";
        }

        private static string GetLogicCodePath(LogicImplementationData logic)
        {
            // logic.GetCodeFilePath("X") = "{codePath}/X.cs"
            // Strip "X.cs" to get the root
            var sample = logic.GetCodeFilePath("__ember_tmp__");
            return System.IO.Path.GetDirectoryName(sample)?.Replace("\\", "/") ?? "Assets";
        }

        private static bool HandleHasGeneratedFile(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            return !string.IsNullOrEmpty(path) && path != "—"
                && File.Exists(GetFullPath(path));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 代码生成

        private static void HandleGenerateCode(EUIBinding binding)
        {
            if (TryGenerateCode(binding, showConfirmation: true, createPrefabIfNeeded: true,
                    refreshAssets: true, out var error))
                return;

            // 用户取消确认/覆盖时 error 为空，不再额外弹失败提示。
            if (!string.IsNullOrEmpty(error))
                EditorUtility.DisplayDialog("生成代码失败", error, "确定");
        }

        /// <summary>
        /// 统一代码生成入口。创建器可关闭确认弹窗、禁止自动创建 prefab，并将 AssetDatabase.Refresh
        /// 延后到自身流程末尾。当 <paramref name="refreshAssets"/> 为 false 时，本调用链不会主动刷新资产。
        /// </summary>
        internal static bool TryGenerateCode(EUIBinding binding, bool showConfirmation,
            bool createPrefabIfNeeded, bool refreshAssets, out string error)
        {
            error = null;
            if (!binding)
            {
                error = "EUIBinding 为空。";
                return false;
            }

            bool embedded = IsEmbeddedPackage();
            bool frameworkMode = binding.PathMode == EUIBinding.CodePathMode.Framework && embedded;
            if (binding.PathMode == EUIBinding.CodePathMode.Framework && !embedded)
            {
                error = "消费端项目不允许使用 Framework 生成模式，请改为 User。";
                return false;
            }

            EmberDebug.Log(TAG,
                $"代码生成模式: {(frameworkMode ? "框架（块标记 + 框架注册区）" : "用户")}"
                + $"（PathMode={binding.PathMode}，embedded={embedded}）");

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                error = "未配置逻辑实现。请进入 Project Settings/EUI Binding 添加。";
                return false;
            }

            if (!logic.CanGenerate(binding))
            {
                error = "配置不完整。请检查类名、页面名和代码生成模板。";
                return false;
            }

            var csharpLogic = logic as CSharpLogicImplementationData;
            if (csharpLogic != null
                && !csharpLogic.TryResolvePrefabPath(binding, out _, out var prefabPathError))
            {
                error = prefabPathError;
                return false;
            }

            // 无弹窗入口必须保持真正非交互；目前只有 C# 实现提供可报告失败且可延迟刷新的内部 API。
            if (csharpLogic == null && (!showConfirmation || !refreshAssets))
            {
                error = "无弹窗或延迟刷新的生成当前仅支持 CSharpLogicImplementationData。";
                return false;
            }

            // 包内 prefab 为发布资产，保持只读。
            var existingPrefabPath = GetBindingPrefabPath(binding);
            if (!string.IsNullOrEmpty(existingPrefabPath)
                && existingPrefabPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                error = "该绑定属于 com.ember 包内页面，代码已随包发布，不可生成。";
                return false;
            }

            if (showConfirmation)
            {
                var hasExistingFile = HandleHasGeneratedFile(binding);
                var confirmMsg = hasExistingFile
                    ? "重新生成将刷新 .Binding.cs 文件（.cs 骨架不受影响），是否继续？"
                    : $"确认生成 {binding.ClassName}.cs 和 {binding.ClassName}.Binding.cs？";
                var confirmBtn = hasExistingFile ? "重新生成" : "生成";
                if (!EditorUtility.DisplayDialog("确认生成代码", confirmMsg, confirmBtn, "取消"))
                    return false;
            }

            if (!HandleIsOnPrefab(binding))
            {
                if (!createPrefabIfNeeded)
                {
                    error = "EUIBinding 尚未保存为 prefab，且当前调用禁止自动创建 prefab。";
                    return false;
                }

                if (!TryGeneratePrefab(binding, showConfirmation, out error))
                    return false;
            }

            string baseClsName = null;
            EUIBinding.BindingEntry[] declaredFields = null;
            bool coreGenerationCompleted = false;
            var baseBinding = GetBaseBinding(binding);
            if (baseBinding)
            {
                baseClsName = baseBinding.ClassName;
                declaredFields = GetDeclaredFields(binding, baseBinding);
            }

            try
            {
                // C# 实现自行按真实 Framework/User 模式写入唯一的 PageDef 目标；不得在此预写默认 User 目标。
                if (csharpLogic != null)
                {
                    if (!csharpLogic.TryGenerateCode(binding, baseClsName, declaredFields,
                            refreshAssets: false, out error))
                        return false;
                    coreGenerationCompleted = true;
                }
                else
                {
                    logic.GenerateCode(binding, baseClsName, declaredFields);
                }

                SyncOptionalPageFeatureMembers(binding, showConfirmation, refreshAssets: false);

                if (binding.GenerateCustomSettings)
                {
                    if (!TryGenerateCustomSettingsTemplate(binding, out error))
                    {
                        if (coreGenerationCompleted)
                            error += "\n逻辑、Binding 与 PageDef 已生成；自定义 Settings 后续步骤未完成，现有产物已保留。";
                        return false;
                    }
                    // 已编译过的 Settings 类型可立即复用；首次生成的类型由创建器在编译完成后补建实例。
                    TryCreateCustomSettingsIfExists(binding, out _);
                }

                if (frameworkMode)
                    MarkFrameworkBindings(binding);

                SyncCustomTransitionMethods(binding, showConfirmation, refreshAssets: false);

                if (refreshAssets)
                    AssetDatabase.Refresh();

                EmberDebug.Log(TAG, $"代码生成完成：{binding.ClassName}");
                return true;
            }
            catch (Exception exception)
            {
                error = $"生成 {binding.ClassName} 时发生异常：{exception.Message}";
                if (coreGenerationCompleted)
                    error += "\n逻辑、Binding 与 PageDef 已生成；后续可选同步未完成，现有产物已保留。";
                EmberDebug.LogWarning(TAG, error);
                return false;
            }
        }

        /// <summary>
        /// 框架模式：把当前全部绑定条目标记为框架子组件（IsFramework=true）。
        /// 此后「清除用户绑定」「清除并重新收集」会保留这些条目，用户无法清除框架子组件。
        /// </summary>
        private static void MarkFrameworkBindings(EUIBinding binding)
        {
            var list = new List<EUIBinding.BindingEntry>(
                binding.Bindings ?? System.Array.Empty<EUIBinding.BindingEntry>());
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                e.IsFramework = true;
                list[i] = e;
            }

            Undo.RecordObject(binding, "标记框架子组件");
            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, list.ToArray());
            EditorUtility.SetDirty(binding);
            EmberDebug.Log(TAG, $"已标记 {list.Count} 个框架子组件条目（受保护：清除操作会保留）");
        }

        /// <summary>
        /// 按生成模式和输出子目录，将预制体生成到 Common 或对应的 Module 目录。
        /// </summary>
        private static bool TryGeneratePrefab(EUIBinding binding, bool showConfirmation, out string error)
        {
            error = null;
            var impl = CSharpLogicImplementationData.FindDefault();
            if (!impl)
            {
                error = "未配置 C# 逻辑实现。";
                return false;
            }

            if (!impl.TryResolvePrefabPath(binding, out var prefabPath, out var pathError))
            {
                error = pathError;
                return false;
            }

            var prefabDir = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(prefabDir))
            {
                error = $"无法解析 prefab 目录：{prefabPath}";
                return false;
            }

            // 如果已存在同路径预制体，询问是否覆盖
            if (File.Exists(prefabPath))
            {
                if (!showConfirmation)
                {
                    error = $"prefab 已存在，非交互生成不会覆盖：{prefabPath}";
                    return false;
                }

                var overwrite = EditorUtility.DisplayDialog("预制体已存在",
                    $"\"{prefabPath}\" 已存在，是否覆盖？", "覆盖", "取消");
                if (!overwrite) return false;
            }

            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var go = binding.gameObject;
            var originalName = go.name;

            try
            {
                if (!Directory.Exists(prefabDir))
                    Directory.CreateDirectory(prefabDir);

                // 仅在全部预检与覆盖确认通过后修改场景对象，并允许用户通过 Undo 撤回清理。
                Undo.RegisterFullObjectHierarchyUndo(go, "生成 UI Prefab");
                if (go.name != prefabName)
                    go.name = prefabName;

                var allGOs = go.GetComponentsInChildren<Transform>(true);
                var removedCount = 0;
                foreach (var t in allGOs)
                {
                    removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
                if (removedCount > 0)
                    EmberDebug.LogWarning(TAG, $"已移除 {removedCount} 个缺失脚本残留");

                var savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    go, prefabPath, InteractionMode.UserAction);
                if (!savedPrefab)
                {
                    if (go && go.name != originalName)
                        go.name = originalName;
                    error = $"Unity 未能保存 prefab：{prefabPath}";
                    return false;
                }

                EmberDebug.Log(TAG, $"预制体已生成：{prefabPath}");
                return true;
            }
            catch (Exception exception)
            {
                if (go && go.name != originalName)
                    go.name = originalName;
                error = $"预制体生成失败：{exception.Message}";
                EmberDebug.LogWarning(TAG, error);
                return false;
            }
        }

        /// <summary>
        /// 生成 {className}Settings.cs 模板文件（仅首次）。
        /// 文件放在与 .cs 同级的目录下。
        /// </summary>
        private static bool TryGenerateCustomSettingsTemplate(EUIBinding binding, out string error)
        {
            error = null;
            var root = binding.CodePath;
            if (string.IsNullOrEmpty(root))
            {
                error = "未配置自定义 Settings 的代码生成根目录。";
                return false;
            }

            if (!CSharpLogicImplementationData.TryValidateIdentifier(
                    binding.ClassName, "类名", out error))
                return false;

            if (!CSharpLogicImplementationData.TryResolveGeneratedFilePath(
                    root, binding.ClassPath, binding.ClassName + "Settings.cs",
                    out var assetPath, out var filePath, out error))
                return false;

            var folder = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folder))
            {
                error = $"无法解析自定义 Settings 输出目录：{assetPath}";
                return false;
            }

            if (File.Exists(filePath)) return true; // 已存在，不覆盖

            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var namespaceName = CSharpLogicImplementationData.GetDefaultNamespace(binding.PathMode);

                var template = $@"using System;

using UnityEngine;

namespace {namespaceName}
{{
    [Serializable]
    public class {binding.ClassName}Settings
    {{
        // 在此处添加自定义参数，Inspector 中将显示在""{binding.ClassName}""折叠框中
    }}
}}
";
                File.WriteAllText(filePath, template, System.Text.Encoding.UTF8);
                EmberDebug.Log(TAG, $"已生成自定义参数模板：{binding.ClassName}Settings.cs");
                return true;
            }
            catch (Exception exception)
            {
                error = $"生成 {binding.ClassName}Settings.cs 失败：{exception.Message}";
                return false;
            }
        }

        /// <summary>
        /// 根据"使用自定义动画"开关同步 OnCustomEnter/OnCustomExit 方法：
        /// <list type="bullet">
        ///   <item><b>勾选自定义</b>：如果缺少则注入方法骨架</item>
        ///   <item><b>取消勾选</b>：如果存在且为默认骨架则删除（含用户代码则弹窗确认）</item>
        /// </list>
        /// </summary>
        private static void SyncCustomTransitionMethods(EUIBinding binding, bool showConfirmation,
            bool refreshAssets)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return;
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath)) return;

            var content = File.ReadAllText(fullPath);
            bool hasEnter = content.Contains("OnCustomEnter");
            bool hasExit = content.Contains("OnCustomExit");

            if (binding.UseCustomTransition)
            {
                // ── 勾选自定义：注入缺失的方法 ──
                if (hasEnter && hasExit) return;
                InjectCustomTransitionMethods(fullPath, hasEnter, hasExit);
                if (refreshAssets)
                    AssetDatabase.Refresh();
                binding.RefreshCustomTransitionCheck();
            }
            else
            {
                // ── 取消勾选：清理已存在的方法 ──
                if (!hasEnter && !hasExit) return;

                // 检测方法体是否有用户代码（超过骨架行数即认为有自定义代码）
                bool hasCustomCode = HasCustomCodeInTransitionMethods(content, hasEnter, hasExit);
                if (hasCustomCode)
                {
                    if (!showConfirmation)
                    {
                        EmberDebug.LogWarning(TAG,
                            $"{binding.ClassName}.cs 的自定义过渡方法包含用户代码，非交互生成已保留源码。");
                        return;
                    }

                    var confirmed = EditorUtility.DisplayDialog(
                        "删除自定义过渡方法",
                        $"已取消勾选'使用自定义动画'，但 {binding.ClassName}.cs 中存在 OnCustomEnter/OnCustomExit 方法。\n\n"
                        + "检测到方法体包含自定义代码，是否确认删除？\n（可在删除前手动备份代码）",
                        "确认删除", "保留");
                    if (!confirmed) return;
                }

                RemoveCustomTransitionMethods(fullPath, content);
                if (refreshAssets)
                    AssetDatabase.Refresh();
                binding.RefreshCustomTransitionCheck();
            }
        }

        /// <summary>
        /// 按 EUIBinding 可视化选项同步 NeedUpdate、OnUpdate、AutoCreateClickableMask、OnClickMask。
        /// 默认骨架可直接增删；发现用户自定义实现时，删除前必须二次确认。
        /// </summary>
        private static void SyncOptionalPageFeatureMembers(EUIBinding binding, bool showConfirmation,
            bool refreshAssets)
        {
            if (!binding || !binding.IsPage) return;

            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return;
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath)) return;

            var content = File.ReadAllText(fullPath);
            var lines = new List<string>(content.Split(
                new[] { "\r\n", "\n" }, StringSplitOptions.None));
            bool frameworkMode = binding.PathMode == EUIBinding.CodePathMode.Framework
                && IsEmbeddedPackage();
            bool isPopup = binding.PageType == PageType.Popup
                || binding.PageType == PageType.FullScreenPopup;
            bool changed = false;
            var blocksToInsert = new List<string>();

            // Framework 的 OnUpdateUser 与 UIUpdate 驱动属于同一可选能力。
            // 关闭时先处理块外用户钩子；若其中已有用户代码且未确认删除，则整次同步取消，
            // 避免只删驱动入口后留下永远不会调用的死钩子。
            if (frameworkMode && !binding.UseUIUpdate)
            {
                if (!TryRemoveFrameworkUserHook(lines, "private void OnUpdateUser",
                        "// 在此编写逐帧业务逻辑", binding.ClassName, showConfirmation,
                        out bool removedUpdateHook))
                    return;
                if (removedUpdateHook)
                    changed = true;
            }

            var uiUpdateGate = new OptionalPageFeature(
                "UIUpdate", "UIUpdate 驱动开关",
                "public override bool NeedUpdate",
                CSharpLogicImplementationData.OptionalUIUpdateMember,
                binding.UseUIUpdate,
                "publicoverrideboolNeedUpdate=>false;",
                requireManagedBlock: frameworkMode);
            var onUpdateMethod = new OptionalPageFeature(
                "OnUpdate", "OnUpdate 生命周期",
                "public override void OnUpdate",
                frameworkMode
                    ? CSharpLogicImplementationData.FrameworkOptionalOnUpdateMember
                    : CSharpLogicImplementationData.OptionalOnUpdateMember,
                binding.UseUIUpdate,
                "publicoverridevoidOnUpdate(){base.OnUpdate();}",
                preserveUnmarkedWhenDisabled: frameworkMode,
                requireManagedBlock: frameworkMode,
                legacyMarkedDefaultBlock: frameworkMode
                    ? CSharpLogicImplementationData.OptionalOnUpdateMember
                    : null);

            // 关闭 UIUpdate 时先处理 OnUpdate：若用户选择保留自定义方法，也必须保留驱动开关，
            // 避免出现 OnUpdate 源码尚在但永远不被调用的隐性行为变化。
            var features = new List<OptionalPageFeature>();
            if (binding.UseUIUpdate)
            {
                features.Add(uiUpdateGate);
                features.Add(onUpdateMethod);
            }
            else
            {
                features.Add(onUpdateMethod);
                features.Add(uiUpdateGate);
            }
            features.AddRange(new[]
            {
                new OptionalPageFeature(
                    "AutoCreateClickableMask", "遮罩创建覆写",
                    "protected override bool AutoCreateClickableMask",
                    CSharpLogicImplementationData.OptionalAutoCreateClickableMaskMember,
                    isPopup && binding.GenerateAutoCreateClickableMaskOverride,
                    "protectedoverrideboolAutoCreateClickableMask=>true;",
                    requireManagedBlock: frameworkMode),
                new OptionalPageFeature(
                    "OnClickMask", "遮罩点击钩子",
                    "protected override void OnClickMask",
                    frameworkMode
                        ? CSharpLogicImplementationData.FrameworkOptionalOnClickMaskMember
                        : CSharpLogicImplementationData.OptionalOnClickMaskMember,
                    isPopup && binding.GenerateOnClickMaskOverride,
                    "protectedoverridevoidOnClickMask(){base.OnClickMask();}",
                    preserveUnmarkedWhenDisabled: frameworkMode,
                    requireManagedBlock: frameworkMode,
                    legacyMarkedDefaultBlock: frameworkMode
                        ? CSharpLogicImplementationData.OptionalOnClickMaskMember
                        : null),
            });

            bool preserveUIUpdateGroup = false;
            foreach (var feature in features)
            {
                if (preserveUIUpdateGroup && feature.Id == "UIUpdate")
                    continue;

                if (ReconcileOptionalPageFeature(lines, feature, binding.ClassName, showConfirmation,
                    out bool shouldInsert, out bool preservedCustom))
                    changed = true;
                if (shouldInsert)
                    blocksToInsert.Add(feature.DefaultBlock);
                if (!frameworkMode && feature.Id == "OnUpdate" && preservedCustom)
                    preserveUIUpdateGroup = true;
            }

            if (blocksToInsert.Count > 0)
            {
                if (frameworkMode)
                    InsertFrameworkOptionalPageFeatureBlocks(lines, blocksToInsert);
                else
                    InsertOptionalPageFeatureBlocks(lines, blocksToInsert);
                changed = true;
            }

            if (frameworkMode)
            {
                if (binding.UseUIUpdate
                    && EnsureFrameworkUserHook(lines, "private void OnUpdateUser",
                        "用户逐帧更新钩子：框架 OnUpdate 结束时调用。",
                        "// 在此编写逐帧业务逻辑"))
                    changed = true;

                if (isPopup && binding.GenerateOnClickMaskOverride
                    && EnsureFrameworkUserHook(lines, "private void OnClickMaskUser",
                        "用户遮罩点击钩子：默认关闭行为之前调用。",
                        "// 在此编写遮罩点击后的自定义逻辑"))
                    changed = true;
            }

            if (!changed) return;

            File.WriteAllText(fullPath, string.Join("\n", lines), new UTF8Encoding(false));
            if (refreshAssets)
                AssetDatabase.Refresh();
            EmberDebug.Log(TAG, $"已同步页面可选覆写：{Path.GetFileName(fullPath)}");
        }

        /// <summary>同步单个可选覆写；返回源码是否发生变化。</summary>
        private static bool ReconcileOptionalPageFeature(List<string> lines, OptionalPageFeature feature,
            string className, bool showConfirmation, out bool shouldInsert, out bool preservedCustom)
        {
            shouldInsert = false;
            preservedCustom = false;
            if (!TryFindOptionalPageFeature(lines, feature, out var range))
            {
                shouldInsert = feature.Enabled;
                return false;
            }

            if (feature.Enabled)
            {
                // 旧版 Framework 将可选成员生成在块外；默认骨架可直接迁回管理块。
                if (feature.RequireManagedBlock && !range.IsInsideManagedBlock
                    && (range.IsDefault || range.IsLegacyDefault))
                {
                    lines.RemoveRange(range.Start, range.End - range.Start + 1);
                    shouldInsert = true;
                    return true;
                }

                // 旧模板固定生成 NeedUpdate => false；勾选 UIUpdate 后替换为新的 true 默认块。
                if (feature.Id == "UIUpdate" && range.IsLegacyDefault)
                {
                    lines.RemoveRange(range.Start, range.End - range.Start + 1);
                    shouldInsert = true;
                    return true;
                }

                // 已有有效实现（默认或自定义）一律保留，避免覆盖用户代码。
                return false;
            }

            // Framework 模式下，无标记的覆写是页面自带的框架逻辑。
            // 关闭面板开关只停止驱动，不删除该方法，便于之后重新开启。
            if (feature.PreserveUnmarkedWhenDisabled && !range.IsMarked)
                return false;

            if (!range.IsDefault && !range.IsLegacyDefault)
            {
                if (!showConfirmation)
                {
                    preservedCustom = true;
                    EmberDebug.LogWarning(TAG,
                        $"{className}.cs 的「{feature.DisplayName}」包含用户代码，非交互生成已保留源码。");
                    return false;
                }

                bool confirmed = EditorUtility.DisplayDialog(
                    "移除可选页面覆写",
                    $"已取消「{feature.DisplayName}」，但 {className}.cs 中对应成员包含自定义内容。\n\n"
                    + "是否确认删除？选择保留则源码不变。",
                    "确认删除", "保留源码");
                if (!confirmed)
                {
                    preservedCustom = true;
                    return false;
                }
            }

            lines.RemoveRange(range.Start, range.End - range.Start + 1);
            return true;
        }

        /// <summary>查找标记块或旧模板中的同名成员。</summary>
        private static bool TryFindOptionalPageFeature(List<string> lines, OptionalPageFeature feature,
            out OptionalPageFeatureRange range)
        {
            string beginMarker = $"[EmberOptional:begin {feature.Id}]";
            string endMarker = $"[EmberOptional:end {feature.Id}]";

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains(beginMarker)) continue;

                for (int j = i; j < lines.Count; j++)
                {
                    if (!lines[j].Contains(endMarker)) continue;
                    string markedCode = string.Join("\n", lines.GetRange(i, j - i + 1));
                    string normalizedMarkedCode = NormalizeOptionalMember(markedCode);
                    range = new OptionalPageFeatureRange
                    {
                        Start = i,
                        End = j,
                        IsMarked = true,
                        IsInsideManagedBlock = IsInsideManagedLifecycleBlock(lines, i),
                        IsDefault = normalizedMarkedCode
                            == NormalizeOptionalMember(feature.DefaultBlock),
                        IsLegacyDefault = !string.IsNullOrEmpty(feature.LegacyMarkedDefaultBlock)
                            && normalizedMarkedCode
                            == NormalizeOptionalMember(feature.LegacyMarkedDefaultBlock),
                    };
                    return true;
                }
                break;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains(feature.SignatureToken)) continue;

                int end = FindOptionalMemberEnd(lines, i);
                if (end < i) break;

                int start = i;
                while (start > 0)
                {
                    string previous = lines[start - 1].TrimStart();
                    if (string.IsNullOrWhiteSpace(previous) || previous.StartsWith("///", StringComparison.Ordinal))
                        start--;
                    else
                        break;
                }

                string memberCode = string.Join("\n", lines.GetRange(i, end - i + 1));
                bool isLegacyDefault = NormalizeOptionalMember(memberCode) == feature.LegacyDefaultCode;
                range = new OptionalPageFeatureRange
                {
                    Start = start,
                    End = end,
                    IsMarked = false,
                    IsInsideManagedBlock = IsInsideManagedLifecycleBlock(lines, i),
                    IsDefault = isLegacyDefault,
                    IsLegacyDefault = isLegacyDefault,
                };
                return true;
            }

            range = default;
            return false;
        }

        /// <summary>检查指定行是否位于 Lifecycle 框架管理块内。</summary>
        private static bool IsInsideManagedLifecycleBlock(List<string> lines, int lineIndex)
        {
            bool inside = false;
            for (int i = 0; i <= lineIndex && i < lines.Count; i++)
            {
                if (lines[i].Contains("[EmberManaged:begin Lifecycle]"))
                    inside = true;
                else if (lines[i].Contains("[EmberManaged:end]"))
                    inside = false;
            }
            return inside;
        }

        /// <summary>定位表达式体属性或花括号成员的结束行。</summary>
        private static int FindOptionalMemberEnd(List<string> lines, int signatureIndex)
        {
            if (lines[signatureIndex].Contains("=>") && lines[signatureIndex].Contains(";"))
                return signatureIndex;

            int depth = 0;
            bool foundOpenBrace = false;
            for (int i = signatureIndex; i < lines.Count; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{')
                    {
                        depth++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        depth--;
                    }
                }

                if (foundOpenBrace && depth == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>在页面配置标题后插入可选覆写；找不到标题时回退到类结尾。</summary>
        private static void InsertOptionalPageFeatureBlocks(List<string> lines, List<string> blocks)
        {
            int insertIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains("// ── 页面配置")) continue;
                insertIndex = i + 1;
                break;
            }

            if (insertIndex < 0)
            {
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].TrimEnd() != "    }") continue;
                    insertIndex = i;
                    break;
                }
            }
            if (insertIndex < 0) return;

            var insertedLines = new List<string>();
            insertedLines.Add(string.Empty);
            foreach (string block in blocks)
            {
                var blockLines = new List<string>(block.Split(
                    new[] { "\r\n", "\n" }, StringSplitOptions.None));
                while (blockLines.Count > 0 && string.IsNullOrWhiteSpace(blockLines[blockLines.Count - 1]))
                    blockLines.RemoveAt(blockLines.Count - 1);
                insertedLines.AddRange(blockLines);
                insertedLines.Add(string.Empty);
            }

            lines.InsertRange(insertIndex, insertedLines);
        }

        /// <summary>将 Framework 模式的可选成员放入 Lifecycle 框架管理块顶部。</summary>
        private static void InsertFrameworkOptionalPageFeatureBlocks(List<string> lines, List<string> blocks)
        {
            int insertIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains("[EmberManaged:begin Lifecycle]")) continue;
                insertIndex = i + 1;
                break;
            }

            if (insertIndex < 0)
            {
                InsertOptionalPageFeatureBlocks(lines, blocks);
                return;
            }

            var insertedLines = new List<string>();
            foreach (string block in blocks)
            {
                var blockLines = new List<string>(block.Split(
                    new[] { "\r\n", "\n" }, StringSplitOptions.None));
                while (blockLines.Count > 0 && string.IsNullOrWhiteSpace(blockLines[blockLines.Count - 1]))
                    blockLines.RemoveAt(blockLines.Count - 1);
                insertedLines.AddRange(blockLines);
                insertedLines.Add(string.Empty);
            }

            lines.InsertRange(insertIndex, insertedLines);
        }

        /// <summary>只补充缺失的 Framework 用户钩子；已有方法的内容永不触碰。</summary>
        private static bool EnsureFrameworkUserHook(List<string> lines, string signature,
            string summary, string bodyComment)
        {
            if (lines.Exists(line => line.Contains(signature))) return false;

            int classCloseIndex = -1;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].TrimEnd() != "    }") continue;
                classCloseIndex = i;
                break;
            }
            if (classCloseIndex < 0) return false;

            var hookLines = new List<string>
            {
                string.Empty,
                $"        /// <summary>{summary}</summary>",
                $"        {signature}()",
                "        {",
                $"            {bodyComment}",
                "        }",
            };
            lines.InsertRange(classCloseIndex, hookLines);
            return true;
        }

        /// <summary>
        /// 移除已关闭能力对应的 Framework 用户钩子。默认空骨架直接删除；包含用户代码时必须确认，
        /// 非交互同步则保留源码并取消本次同步，避免产生无驱动的死钩子。
        /// </summary>
        private static bool TryRemoveFrameworkUserHook(List<string> lines, string signature,
            string defaultBodyComment, string className, bool showConfirmation, out bool removed)
        {
            removed = false;
            int signatureIndex = lines.FindIndex(line => line.Contains(signature));
            if (signatureIndex < 0) return true;

            int methodEnd = FindOptionalMemberEnd(lines, signatureIndex);
            if (methodEnd < signatureIndex)
            {
                EmberDebug.LogWarning(TAG,
                    $"{className}.cs 的可选用户钩子无法完整解析，已取消本次同步: {signature}");
                return false;
            }

            string methodCode = string.Join("\n",
                lines.GetRange(signatureIndex, methodEnd - signatureIndex + 1));
            string defaultMethod = $"{signature}()\n{{\n{defaultBodyComment}\n}}";
            string emptyMethod = $"{signature}()\n{{\n}}";
            string normalized = NormalizeOptionalMember(methodCode);
            bool isDefault = normalized == NormalizeOptionalMember(defaultMethod)
                || normalized == NormalizeOptionalMember(emptyMethod);

            if (!isDefault)
            {
                if (!showConfirmation)
                {
                    EmberDebug.LogWarning(TAG,
                        $"{className}.cs 的 OnUpdateUser 包含用户代码；非交互生成已取消 UIUpdate 同步，请在交互生成中确认处理。");
                    return false;
                }

                bool confirmed = EditorUtility.DisplayDialog(
                    "移除 UIUpdate 用户钩子",
                    $"已取消「使用 UIUpdate」，但 {className}.cs 的 OnUpdateUser 包含用户代码。\n\n"
                    + "关闭后该钩子将不再被调用。是否确认删除该方法及其中代码？",
                    "确认删除并关闭", "取消生成");
                if (!confirmed)
                    return false;
            }

            int removeStart = signatureIndex;
            while (removeStart > 0)
            {
                string previous = lines[removeStart - 1].TrimStart();
                if (string.IsNullOrWhiteSpace(previous)
                    || previous.StartsWith("///", StringComparison.Ordinal))
                    removeStart--;
                else
                    break;
            }

            lines.RemoveRange(removeStart, methodEnd - removeStart + 1);
            removed = true;
            return true;
        }

        /// <summary>移除空白，供默认骨架指纹比较。</summary>
        private static string NormalizeOptionalMember(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            var sb = new StringBuilder(code.Length);
            foreach (char c in code)
                if (!char.IsWhiteSpace(c)) sb.Append(c);
            return sb.ToString();
        }

        private sealed class OptionalPageFeature
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string SignatureToken;
            public readonly string DefaultBlock;
            public readonly bool Enabled;
            public readonly string LegacyDefaultCode;
            public readonly bool PreserveUnmarkedWhenDisabled;
            public readonly bool RequireManagedBlock;
            public readonly string LegacyMarkedDefaultBlock;

            public OptionalPageFeature(string id, string displayName, string signatureToken,
                string defaultBlock, bool enabled, string legacyDefaultCode,
                bool preserveUnmarkedWhenDisabled = false, bool requireManagedBlock = false,
                string legacyMarkedDefaultBlock = null)
            {
                Id = id;
                DisplayName = displayName;
                SignatureToken = signatureToken;
                DefaultBlock = defaultBlock;
                Enabled = enabled;
                LegacyDefaultCode = legacyDefaultCode;
                PreserveUnmarkedWhenDisabled = preserveUnmarkedWhenDisabled;
                RequireManagedBlock = requireManagedBlock;
                LegacyMarkedDefaultBlock = legacyMarkedDefaultBlock;
            }
        }

        private struct OptionalPageFeatureRange
        {
            public int Start;
            public int End;
            public bool IsMarked;
            public bool IsInsideManagedBlock;
            public bool IsDefault;
            public bool IsLegacyDefault;
        }

        /// <summary>向 .cs 文件注入缺失的 OnCustomEnter/OnCustomExit 方法骨架。</summary>
        private static void InjectCustomTransitionMethods(string fullPath, bool hasEnter, bool hasExit)
        {
            var content = File.ReadAllText(fullPath);
            var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None));

            // 找到类末尾闭合括号（4 空格缩进），在其前插入
            int classCloseIndex = -1;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].TrimEnd() == "    }")
                {
                    classCloseIndex = i;
                    break;
                }
            }
            if (classCloseIndex < 0) return;

            var stubs = new List<string>();
            if (!hasEnter)
            {
                stubs.Add("");
                stubs.Add("        public override async Cysharp.Threading.Tasks.UniTask OnCustomEnter()");
                stubs.Add("        {");
                stubs.Add("            // TODO: 在此处编写自定义进入动画");
                stubs.Add("            await Cysharp.Threading.Tasks.UniTask.Yield();");
                stubs.Add("        }");
            }
            if (!hasExit)
            {
                stubs.Add("");
                stubs.Add("        public override async Cysharp.Threading.Tasks.UniTask OnCustomExit()");
                stubs.Add("        {");
                stubs.Add("            // TODO: 在此处编写自定义退出动画");
                stubs.Add("            await Cysharp.Threading.Tasks.UniTask.Yield();");
                stubs.Add("        }");
            }

            lines.InsertRange(classCloseIndex, stubs);
            File.WriteAllText(fullPath, string.Join("\n", lines), System.Text.Encoding.UTF8);
            EmberDebug.Log(TAG, $"已注入 OnCustomEnter/OnCustomExit 到 {Path.GetFileName(fullPath)}");
        }

        /// <summary>从 .cs 文件中移除 OnCustomEnter 和 OnCustomExit 方法（含签名和整个方法体）。</summary>
        private static void RemoveCustomTransitionMethods(string fullPath, string content)
        {
            var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None));

            // 分别查找并移除两个方法
            lines = RemoveMethodBlock(lines, "OnCustomEnter");
            lines = RemoveMethodBlock(lines, "OnCustomExit");

            File.WriteAllText(fullPath, string.Join("\n", lines), System.Text.Encoding.UTF8);
            EmberDebug.Log(TAG, $"已移除 OnCustomEnter/OnCustomExit 从 {Path.GetFileName(fullPath)}");
        }

        /// <summary>从行列表中移除指定方法名的完整方法块（签名行→闭合括号）。</summary>
        private static List<string> RemoveMethodBlock(List<string> lines, string methodName)
        {
            // 找到方法签名行
            int sigIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(methodName) && lines[i].TrimStart().StartsWith("public override"))
                {
                    sigIndex = i;
                    break;
                }
            }
            if (sigIndex < 0) return lines;

            // 从签名行向后找到 { 行
            int openBrace = -1;
            for (int i = sigIndex; i < lines.Count; i++)
            {
                if (lines[i].Contains("{"))
                {
                    openBrace = i;
                    break;
                }
            }
            if (openBrace < 0) return lines;

            // 从 { 行开始计数括号，找到匹配的 }
            int depth = 0;
            int closeBrace = -1;
            for (int i = openBrace; i < lines.Count; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                if (depth == 0)
                {
                    closeBrace = i;
                    break;
                }
            }
            if (closeBrace < 0) return lines;

            // 删除 sigIndex..closeBrace（含首尾），并清理前导空行
            int removeStart = sigIndex;
            while (removeStart > 0 && string.IsNullOrWhiteSpace(lines[removeStart - 1]))
                removeStart--;

            lines.RemoveRange(removeStart, closeBrace - removeStart + 1);
            return lines;
        }

        /// <summary>检测方法体是否包含用户自定义代码（超过骨架行数即为有自定义代码）。</summary>
        private static bool HasCustomCodeInTransitionMethods(string content, bool hasEnter, bool hasExit)
        {
            // 骨架 OnCustomEnter 共 6 行（含签名和括号），OnCustomExit 共 6 行
            const int stubLinesPerMethod = 6;

            if (hasEnter)
            {
                int enterLines = CountMethodLines(content, "OnCustomEnter");
                if (enterLines > stubLinesPerMethod) return true;
            }
            if (hasExit)
            {
                int exitLines = CountMethodLines(content, "OnCustomExit");
                if (exitLines > stubLinesPerMethod) return true;
            }
            return false;
        }

        /// <summary>计算方法体的行数（签名行到闭合括号）。</summary>
        private static int CountMethodLines(string content, string methodName)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            int sigIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(methodName) && lines[i].TrimStart().StartsWith("public override"))
                {
                    sigIndex = i;
                    break;
                }
            }
            if (sigIndex < 0) return 0;

            int openBrace = -1;
            for (int i = sigIndex; i < lines.Length; i++)
            {
                if (lines[i].Contains("{")) { openBrace = i; break; }
            }
            if (openBrace < 0) return 0;

            int depth = 0, closeBrace = -1;
            for (int i = openBrace; i < lines.Length; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
                if (depth == 0) { closeBrace = i; break; }
            }
            if (closeBrace < 0) return 0;

            return closeBrace - sigIndex + 1;
        }

        /// <summary>
        /// 编译完成后补建自定义 Settings 实例。此方法不刷新、不保存 prefab；调用方应在 Unity
        /// 完成编译与资产更新后传入仍然有效的 binding，并负责保存所属 prefab。
        /// </summary>
        internal static bool TryCreateCustomSettingsAfterCompile(EUIBinding binding, out string error)
        {
            error = null;
            if (!binding)
            {
                error = "EUIBinding 为空。";
                return false;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                error = "Unity 仍在编译或更新资产，请等待完成后再创建自定义 Settings 实例。";
                return false;
            }

            if (!binding.GenerateCustomSettings)
            {
                error = "EUIBinding 未启用自定义 Settings 生成。";
                return false;
            }

            try
            {
                return TryCreateCustomSettingsIfExists(binding, out error);
            }
            catch (Exception exception)
            {
                error = $"创建 {binding.ClassName}Settings 实例失败：{exception.Message}";
                EmberDebug.LogWarning(TAG, error);
                return false;
            }
        }

        /// <summary>查找已加载的生成命名空间下 {className}Settings 类型并创建实例；不会触发资产刷新。</summary>
        private static bool TryCreateCustomSettingsIfExists(EUIBinding binding, out string error)
        {
            error = null;
            if (!binding || string.IsNullOrEmpty(binding.ClassName))
            {
                error = "类名为空，无法创建自定义 Settings 实例。";
                return false;
            }

            var settingsTypeName = $"{binding.ClassName}Settings";
            var namespaceName = CSharpLogicImplementationData.GetDefaultNamespace(binding.PathMode);
            var expectedFullName = $"{namespaceName}.{settingsTypeName}";
            var matchingTypes = new List<System.Type>();
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type candidate;
                try
                {
                    candidate = asm.GetType(expectedFullName, throwOnError: false, ignoreCase: false);
                }
                catch
                {
                    // 单个第三方程序集的反射异常不应阻断其他已加载程序集的精确查找。
                    continue;
                }

                if (candidate != null && !matchingTypes.Contains(candidate))
                    matchingTypes.Add(candidate);
            }

            if (matchingTypes.Count == 0)
            {
                error = $"尚未加载 {expectedFullName} 类型；首次生成后请等待 Unity 编译完成再重试。";
                return false;
            }

            if (matchingTypes.Count > 1)
            {
                var assemblyNames = new List<string>(matchingTypes.Count);
                foreach (var matchingType in matchingTypes)
                    assemblyNames.Add(matchingType.Assembly.GetName().Name);
                error = $"发现多个 {expectedFullName} 类型（{string.Join(", ", assemblyNames)}），无法安全选择。";
                return false;
            }

            var settingsType = matchingTypes[0];
            if (!settingsType.IsSerializable)
            {
                error = $"{expectedFullName} 未标记为可序列化，无法写入 EUIBinding。";
                return false;
            }

            var settingsField = typeof(EUIBinding).GetField("_pageSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (settingsField == null)
            {
                error = "未找到 EUIBinding._pageSettings 字段。";
                return false;
            }

            var existingSettings = settingsField.GetValue(binding);
            // 已有匹配类型则跳过
            if (existingSettings != null && existingSettings.GetType() == settingsType)
                return true;

            // 先创建新实例，再替换旧值；实例化失败时保留原数据。
            var instance = System.Activator.CreateInstance(settingsType);
            settingsField.SetValue(binding, instance);
            EditorUtility.SetDirty(binding);
            if (existingSettings != null)
            {
                EmberDebug.Log(TAG,
                    $"已替换不匹配的自定义参数: {existingSettings.GetType().FullName} → {expectedFullName}");
            }
            EmberDebug.Log(TAG, $"已创建自定义参数：{expectedFullName}");
            return true;
        }

        private static void HandleGenerateToClipboard(EUIBinding binding)
        {
            if (!binding) return;

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("错误",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            if (!logic.CanGenerateForNoGen(binding))
            {
                EditorUtility.DisplayDialog("错误",
                    "当前逻辑实现不支持剪贴板生成。", "确定");
                return;
            }

            logic.GenerateCodeForNoGen(binding,
                string.IsNullOrEmpty(binding.ClassName)
                    ? binding.gameObject.name
                    : binding.ClassName);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 自动收集

        private static void HandleAutoCollectBindings(EUIBinding binding)
        {
            if (!binding) return;

            if (!EditorUtility.DisplayDialog("自动收集子控件",
                "将扫描当前节点的所有子节点，自动添加绑定条目。\n已有绑定不会被覆盖。",
                "开始收集", "取消"))
                return;

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("自动收集失败",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            // 收集已有绑定名
            var defined = new Dictionary<GameObject, GameObject>();
            EUIBindingEditorUtility.GatherBindingDefinitions(binding, defined);
            var definedNames = new HashSet<string>();
            if (binding.Bindings != null)
                foreach (var b in binding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name)) definedNames.Add(b.Name);

            // 直接修改 C# 对象，不通过 SerializedObject（避免 Odin 属性树缓存失效）
            var list = new List<EUIBinding.BindingEntry>(
                binding.Bindings ?? System.Array.Empty<EUIBinding.BindingEntry>());
            CollectBindingsToList(binding, defined, definedNames, binding.transform, logic, list, new HashSet<GameObject>());

            // 写入 private 字段 + Undo 强制 Unity 序列化系统感知变更
            Undo.RecordObject(binding, "自动收集子控件");
            var field = typeof(EUIBinding).GetField("bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(binding, list.ToArray());
            EditorUtility.SetDirty(binding);

            // 强制刷新 Odin：操作 SerializedObject 让 Unity 知道数据变了
            var forceSo = new SerializedObject(binding);
            forceSo.Update();
            forceSo.ApplyModifiedPropertiesWithoutUndo();
            forceSo.Dispose();

            EmberDebug.Log(TAG,
                $"自动收集完成，共 {list.Count} 个绑定（field check: {(binding.Bindings?.Length ?? 0)}）");
        }

        private static void CollectBindingsToList(
            EUIBinding binding,
            Dictionary<GameObject, GameObject> defined,
            HashSet<string> definedNames,
            Transform parent,
            LogicImplementationData logic,
            List<EUIBinding.BindingEntry> list,
            HashSet<GameObject> ownedChildren)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childGO = child.gameObject;

                // 收集当前节点增强组件通过槽位持有的子节点（这些子节点不再单独绑定）
                EUIBindingEditorUtility.CollectOwnedChildren(childGO, ownedChildren);

                // 跳过被增强组件槽位持有的子节点
                if (ownedChildren.Contains(childGO))
                    continue;

                // 跳过被 EUIBindingExclude 标记的节点及其子树
                if (childGO.GetComponent<EUIBindingExclude>())
                    continue;

                bool hasChildBinding = childGO.GetComponent<EUIBinding>() != null;

                if (!defined.ContainsKey(childGO) && IsNameSuitable(child.name))
                {
                    var detected = EUIBindingEditorUtility.DetectWidgetType(childGO);
                    list.Add(new EUIBinding.BindingEntry
                    {
                        Name = logic.GetNameForCode(child.name, definedNames),
                        GameObject = childGO,
                        Type = detected.Type,
                        ClassName = detected.ClassName,
                    });
                }

                if (!hasChildBinding)
                    CollectBindingsToList(binding, defined, definedNames, child, logic, list, ownedChildren);
            }
        }

        /// <summary>
        /// 判断节点名是否适合作为绑定。
        /// 与 Burner 对齐：以 m_ 或 mXxx（m 后跟大写字母）开头。
        /// </summary>
        private static bool IsNameSuitable(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.StartsWith("m_", System.StringComparison.Ordinal)
                || (name.StartsWith("m", System.StringComparison.Ordinal)
                    && name.Length > 1
                    && char.IsUpper(name[1]));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 辅助

        private static void HandleShowLogicMenu(EUIBinding binding)
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            var impls = settings.LogicImplementations;
            if (impls == null || impls.Length == 0)
            {
                EditorUtility.DisplayDialog("提示",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            var menu = new GenericMenu();
            for (int i = 0; i < impls.Length; i++)
            {
                if (!impls[i]) continue;
                int idx = i;
                menu.AddItem(new GUIContent(impls[i].name),
                    idx == EUIBinding.CodeGenLogicIndex,
                    () => EUIBinding.CodeGenLogicIndex = idx);
            }
            menu.ShowAsContext();
        }

        private static void HandleOpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/EUI Binding");
        }

        private static UnityEngine.Object HandleGetGeneratedScript(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return null;
            var fullPath = GetFullPath(path);
            return !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath)
                ? AssetDatabase.LoadAssetAtPath<MonoScript>(path)
                : null;
        }

        private static void HandleClearAndRecollect(EUIBinding binding)
        {
            if (!binding) return;
            if (!EditorUtility.DisplayDialog("清除并重新收集",
                "将清除所有用户绑定条目（框架子组件条目保留）并重新扫描子节点进行收集，是否继续？",
                "确认清除并收集", "取消"))
                return;

            // 清除：保留框架子组件条目
            var kept = new List<EUIBinding.BindingEntry>();
            if (binding.Bindings != null)
                foreach (var e in binding.Bindings)
                    if (e.IsFramework) kept.Add(e);

            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, kept.ToArray());
            Undo.RecordObject(binding, "清除并重新收集");
            EditorUtility.SetDirty(binding);
            EmberDebug.Log(TAG, $"清除完成：保留 {kept.Count} 个框架子组件条目");

            // 重新收集
            HandleAutoCollectBindings(binding);
        }

        private static void HandleClearAllBindings(EUIBinding binding)
        {
            if (!binding) return;
            if (!EditorUtility.DisplayDialog("清除用户绑定",
                "将清除所有用户绑定的控件条目（框架子组件条目保留）。此操作不可撤销。",
                "确认清除", "取消"))
                return;

            // 清除：保留框架子组件条目
            var kept = new List<EUIBinding.BindingEntry>();
            if (binding.Bindings != null)
                foreach (var e in binding.Bindings)
                    if (e.IsFramework) kept.Add(e);

            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, kept.ToArray());
            Undo.RecordObject(binding, "清除用户绑定");
            EditorUtility.SetDirty(binding);
            EmberDebug.Log(TAG, $"清除完成：保留 {kept.Count} 个框架子组件条目");
        }

        private static void HandleCopyGeneratedPath(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return;
            GUIUtility.systemCopyBuffer = path;
            EmberDebug.Log(TAG, $"已复制路径：{path}");
        }

        private static LogicImplementationData GetCurrentLogic(EUIBinding binding)
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null
                || settings.LogicImplementations.Length == 0)
                return null;

            int index = EUIBinding.CodeGenLogicIndex;
            if (index < 0 || index >= settings.LogicImplementations.Length)
                index = 0;

            return settings.LogicImplementations[index];
        }

        private static EUIBinding GetBaseBinding(EUIBinding binding)
        {
            if (!binding) return null;

            string guid;
            using (var so = new SerializedObject(binding))
                guid = so.FindProperty("baseBindingUUID").stringValue;

            if (string.IsNullOrEmpty(guid)) return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? prefab.GetComponent<EUIBinding>() : null;
        }

        /// <summary>计算需要声明的字段（排除基类已有字段）</summary>
        private static EUIBinding.BindingEntry[] GetDeclaredFields(
            EUIBinding binding, EUIBinding baseBinding)
        {
            var baseNames = new HashSet<string>();
            if (baseBinding.Bindings != null)
                foreach (var b in baseBinding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name))
                        baseNames.Add(b.Name);

            var declared = new List<EUIBinding.BindingEntry>();
            if (binding.Bindings != null)
                foreach (var b in binding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name) && !baseNames.Contains(b.Name))
                        declared.Add(b);

            return declared.ToArray();
        }

        private static string GetFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return assetPath.StartsWith("Assets/")
                ? Path.Combine(Application.dataPath,
                    assetPath.Substring("Assets/".Length))
                : assetPath;
        }

        #endregion
    }
}
