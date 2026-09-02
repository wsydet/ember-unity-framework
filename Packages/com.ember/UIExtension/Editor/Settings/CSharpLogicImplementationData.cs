// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Ember.Basic;
using Ember.UI;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// C# 逻辑代码实现 —— 通过模板文件生成 partial class 的绑定代码和逻辑骨架。
    ///
    /// <para>模板语法（类 Cottle）：</para>
    /// <list type="bullet">
    ///   <item>{variable} —— 简单变量替换</item>
    ///   <item>{for v in list: ... } —— 循环</item>
    ///   <item>{if cond = val: ... } —— 条件判断</item>
    /// </list>
    ///
    /// <para>工作流：</para>
    /// <list type="bullet">
    ///   <item>1. 绑定代码模板 → 生成 .Binding.cs（每次覆盖）</item>
    ///   <item>2. 逻辑代码模板 → 生成 .cs 骨架（仅首次）</item>
    ///   <item>3. EUIPageDef 模板 → 更新页面常量定义文件</item>
    ///   <item>4. 剪贴板模板 → noCodeGen 模式使用</item>
    /// </list>
    /// </summary>
    public class CSharpLogicImplementationData : LogicImplementationData
    {
        private const string TAG = LogTags.EmberUI;

        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("页面逻辑基类（含命名空间），如 Ember.UI.EUIPage")]
        private string baseClassName = "Ember.UI.EUIPage";

        [SerializeField]
        [Tooltip("EUIPageDef 源码文件路径（如 Assets/Game/UI/GamePages.User.cs，用户页面注册区；框架页面在 GamePages.cs，两者 partial 拼接）")]
        private string pageDefFile;

        /// <summary>EUIPageDef 文件路径（公开给菜单项使用）</summary>
        public string PageDefFile => pageDefFile;

        [Header("代码生成模板")]
        [SerializeField]
        [Tooltip("绑定代码模板 → 生成 .Binding.cs")]
        private DefaultAsset bindingCodeTemplate;

        [SerializeField]
        [Tooltip("逻辑代码模板 → 生成 .cs 骨架（仅首次）")]
        private DefaultAsset codeTemplate;

        [SerializeField]
        [Tooltip("EUIPageDef 生成模板")]
        private DefaultAsset pageDefTemplate;

        [SerializeField]
        [Tooltip("剪贴板代码模板（noCodeGen 模式）")]
        private DefaultAsset codeTemplateForNoGen;

        [SerializeField]
        [Tooltip("框架模式代码骨架模板 → 生成带 [EmberManaged] Lifecycle 管理块与块外 XxxUser 钩子的 .cs")]
        private DefaultAsset frameworkCodeTemplate;

        [SerializeField]
        [Tooltip("UI 资源根目录（完整 Assets 路径）。框架模式生成到 Common/Prefabs；用户模式按输出子目录首段生成到 Module/<模块>/Prefabs。")]
        private string uiResourceRoot = "Assets/GameResource/Resources/UI";

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public override string CodeFileExtension => ".cs";

        /// <summary>UI 资源根目录（完整 Assets 路径，与逻辑代码目录分离）</summary>
        public string UIResourceRoot => uiResourceRoot;

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
            "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
            "object", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
            "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while",
        };

        private static readonly HashSet<string> ReservedWindowsNames = new HashSet<string>(
            new[]
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            }, StringComparer.OrdinalIgnoreCase);

        internal static bool TryValidateIdentifier(string value, string displayName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"{displayName}为空。";
                return false;
            }

            if (value != value.Trim() || !IsIdentifierStart(value[0]))
            {
                error = $"{displayName}“{value}”不是合法的 C# 标识符。";
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                if (!IsIdentifierPart(value[i]))
                {
                    error = $"{displayName}“{value}”不是合法的 C# 标识符。";
                    return false;
                }
            }

            if (CSharpKeywords.Contains(value))
            {
                error = $"{displayName}“{value}”不能使用 C# 关键字。";
                return false;
            }
            return true;
        }

        internal static bool TryResolveAssetsPath(string value, string displayName,
            out string assetPath, out string fullPath, out string error)
        {
            assetPath = null;
            fullPath = null;
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"{displayName}为空。";
                return false;
            }

            string normalized = value.Trim().Replace('\\', '/').TrimEnd('/');
            if (Path.IsPathRooted(normalized)
                || (!normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                    && !normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)))
            {
                error = $"{displayName}必须是 Assets/ 下的项目相对路径，不能使用绝对路径或 Packages 路径：{value}";
                return false;
            }

            string[] segments = normalized.Split('/');
            foreach (string segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment) || segment != segment.Trim()
                    || segment == "." || segment == ".."
                    || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || IsReservedWindowsName(segment))
                {
                    error = $"{displayName}包含非法或穿越路径段：{value}";
                    return false;
                }
            }

            try
            {
                string assetsRoot = Path.GetFullPath(Application.dataPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string projectRoot = Directory.GetParent(assetsRoot)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    error = "无法解析 Unity 项目根目录。";
                    return false;
                }

                string candidate = Path.GetFullPath(Path.Combine(projectRoot,
                    normalized.Replace('/', Path.DirectorySeparatorChar)));
                if (!candidate.Equals(assetsRoot, StringComparison.OrdinalIgnoreCase)
                    && !candidate.StartsWith(assetsRoot + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    error = $"{displayName}越出了项目 Assets 根目录：{value}";
                    return false;
                }

                string relative = candidate.Length == assetsRoot.Length
                    ? string.Empty
                    : candidate.Substring(assetsRoot.Length + 1).Replace('\\', '/');
                assetPath = string.IsNullOrEmpty(relative) ? "Assets" : $"Assets/{relative}";
                fullPath = candidate;
                return true;
            }
            catch (Exception exception)
            {
                error = $"{displayName}无效：{exception.Message}";
                return false;
            }
        }

        internal static bool TryResolveGeneratedFilePath(string codeRoot, string classPath,
            string fileName, out string assetPath, out string fullPath, out string error)
        {
            assetPath = null;
            fullPath = null;
            error = null;
            if (!TryResolveAssetsPath(codeRoot, "代码生成根目录",
                    out var normalizedRoot, out _, out error))
                return false;

            if (!TryNormalizeRelativeDirectory(classPath, "输出子目录",
                    out var normalizedClassPath, out error))
                return false;

            if (!TryValidateSingleFileName(fileName, "生成文件名", out error))
                return false;

            string candidate = string.IsNullOrEmpty(normalizedClassPath)
                ? $"{normalizedRoot}/{fileName}"
                : $"{normalizedRoot}/{normalizedClassPath}/{fileName}";
            return TryResolveAssetsPath(candidate, "生成文件路径", out assetPath, out fullPath, out error);
        }

        private static bool TryNormalizeRelativeDirectory(string value, string displayName,
            out string normalized, out string error)
        {
            normalized = string.Empty;
            error = null;
            if (string.IsNullOrWhiteSpace(value)) return true;

            string candidate = value.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(candidate) || candidate.StartsWith("/", StringComparison.Ordinal))
            {
                error = $"{displayName}必须是相对目录：{value}";
                return false;
            }

            candidate = candidate.Trim('/');

            string[] segments = candidate.Split('/');
            foreach (string segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment) || segment != segment.Trim()
                    || segment == "." || segment == ".."
                    || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || IsReservedWindowsName(segment))
                {
                    error = $"{displayName}包含非法或穿越路径段：{value}";
                    return false;
                }
            }

            normalized = string.Join("/", segments);
            return true;
        }

        private static bool TryValidateSingleFileName(string value, string displayName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim()
                || value == "." || value == ".."
                || value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0
                || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || IsReservedWindowsName(value))
            {
                error = $"{displayName}“{value}”不是合法的单一文件名。";
                return false;
            }

            return true;
        }

        private static bool IsReservedWindowsName(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var dot = value.IndexOf('.');
            var stem = dot >= 0 ? value.Substring(0, dot) : value;
            return ReservedWindowsNames.Contains(stem);
        }

        private static bool IsIdentifierStart(char value)
        {
            return value == '_' || char.IsLetter(value);
        }

        private static bool IsIdentifierPart(char value)
        {
            return value == '_' || char.IsLetterOrDigit(value);
        }

        /// <summary>从全局 EUI Binding 设置中取 C# 实现数据（未配置或无 C# 实现返回 null）。</summary>
        public static CSharpLogicImplementationData FindDefault()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null) return null;
            foreach (var impl in settings.LogicImplementations)
            {
                if (impl is CSharpLogicImplementationData c)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// 解析 binding 的最终预制体路径。
        /// 框架模式固定进入 Common；用户模式从输出子目录第一段提取模块名。
        /// </summary>
        public bool TryResolvePrefabPath(EUIBinding binding, out string prefabPath, out string error)
        {
            prefabPath = null;
            error = null;

            if (binding == null)
            {
                error = "EUIBinding 为空。";
                return false;
            }

            var root = string.IsNullOrWhiteSpace(uiResourceRoot)
                ? "Assets/GameResource/Resources/UI"
                : uiResourceRoot.Trim().Replace('\\', '/').TrimEnd('/');
            if (!TryResolveAssetsPath(root, "UI 资源根目录", out root, out _, out error))
                return false;

            var rawPrefabName = binding.PrefabName?.Trim();
            if (string.IsNullOrEmpty(rawPrefabName))
            {
                error = "预制体名和类名不能同时为空。";
                return false;
            }

            if (rawPrefabName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                rawPrefabName = rawPrefabName.Substring(0, rawPrefabName.Length - ".prefab".Length);
            if (!TryValidateSingleFileName(rawPrefabName, "预制体名", out error))
                return false;
            var prefabName = rawPrefabName;

            if (!TryNormalizeRelativeDirectory(binding.ClassPath, "输出子目录",
                    out var classPath, out error))
                return false;

            string categoryPath;
            if (binding.PathMode == EUIBinding.CodePathMode.Framework)
            {
                categoryPath = "Common";
            }
            else
            {
                var separatorIndex = classPath?.IndexOf('/') ?? -1;
                var moduleName = separatorIndex >= 0 ? classPath.Substring(0, separatorIndex) : classPath;
                if (string.IsNullOrWhiteSpace(moduleName) || moduleName == "." || moduleName == "..")
                {
                    error = "用户模式的输出子目录必须以模块名开头，例如 Inventory/Page。";
                    return false;
                }

                categoryPath = $"Module/{moduleName.Trim()}";
            }

            return TryResolveAssetsPath($"{root}/{categoryPath}/Prefabs/{prefabName}.prefab",
                "预制体输出路径", out prefabPath, out _, out error);
        }

        public override bool CanGenerate(EUIBinding binding)
        {
            return base.CanGenerate(binding)
                && (binding.PathMode == EUIBinding.CodePathMode.Framework
                    ? frameworkCodeTemplate != null
                    : codeTemplate != null)
                && bindingCodeTemplate != null;
        }

        public override bool CanGenerateForNoGen(EUIBinding binding)
        {
            return codeTemplateForNoGen != null;
        }

        public override string GetNameForCode(string name)
        {
            name = name.Replace(" ", "_");
            if (name.StartsWith("m_"))
                return name.Substring(2);
            if (name.StartsWith("m") && name.Length > 1 && char.IsUpper(name[1]))
                return name.Substring(1);
            return name;
        }

        /// <summary>
        /// 根据路径模式推导生成的代码命名空间。
        /// 双路径合并（v0.8.0）后：框架/用户统一生成到业务层，命名空间一律 Game.UI；
        /// 框架与用户的区别只体现在生成文件的 [EmberManaged] 块标记上。
        /// </summary>
        public static string GetDefaultNamespace(EUIBinding.CodePathMode pathMode)
        {
            return "Game.UI";
        }

        public override void GenerateCodeForNoGen(EUIBinding binding, string className)
        {
            string templateContent = ReadTemplate(codeTemplateForNoGen);
            if (string.IsNullOrEmpty(templateContent))
            {
                EmberDebug.LogWarning(TAG, "剪贴板模板为空，无法生成。");
                return;
            }

            string result = RenderTemplate(templateContent, BuildTemplateContext(binding, className, binding.Bindings));
            GUIUtility.systemCopyBuffer = result;
            EmberDebug.Log(TAG, "代码已复制至剪贴板");
        }

        public override void GenerateCode(EUIBinding binding, string baseClsName, EUIBinding.BindingEntry[] declaredFields)
        {
            if (!TryGenerateCode(binding, baseClsName, declaredFields, refreshAssets: true, out var error)
                && !string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("生成代码失败", error, "确定");
            }
        }

        /// <summary>
        /// 无弹窗地生成 C# 页面代码。供统一生成流程调用；当 <paramref name="refreshAssets"/> 为 false 时，
        /// 本方法不会触发 AssetDatabase.Refresh，由外层在全部文件写入完成后统一刷新。
        /// </summary>
        internal bool TryGenerateCode(EUIBinding binding, string baseClsName,
            EUIBinding.BindingEntry[] declaredFields, bool refreshAssets, out string error)
        {
            error = null;
            bool skeletonWritten = false;
            bool bindingWritten = false;
            bool pageDefinitionUpdated = false;
            if (!binding)
            {
                error = "EUIBinding 为空。";
                return false;
            }

            if (!TryValidateIdentifier(binding.ClassName, "类名", out error))
                return false;

            if (binding.IsPage && !TryValidateIdentifier(binding.PageName, "页面名称", out error))
                return false;

            try
            {
                bool embedded = EUIBindingCodeGenUtility.IsEmbeddedPackage();
                if (binding.PathMode == EUIBinding.CodePathMode.Framework && !embedded)
                {
                    error = "消费端项目不允许使用 Framework 生成模式，请改为 User。";
                    return false;
                }

                bool frameworkMode = binding.PathMode == EUIBinding.CodePathMode.Framework;

                if (!TryGetPrefabName(binding, out var prefabName)
                    || string.IsNullOrEmpty(prefabName))
                {
                    error = "EUIBinding 尚未保存为可识别的 prefab。";
                    return false;
                }

                prefabName = Path.GetFileNameWithoutExtension(prefabName);
                baseClsName = !string.IsNullOrEmpty(baseClsName) ? baseClsName : this.baseClassName;
                declaredFields = declaredFields ?? binding.Bindings;

                string effectiveCodePath = !string.IsNullOrEmpty(binding.CodePath) ? binding.CodePath : codePath;
                if (string.IsNullOrWhiteSpace(effectiveCodePath))
                {
                    error = "未配置代码生成根目录。";
                    return false;
                }

                if (!TryResolveGeneratedFilePath(effectiveCodePath, binding.ClassPath,
                        binding.ClassName + CodeFileExtension,
                        out var assetPath, out var path, out error))
                    return false;

                if (!TryResolveGeneratedFilePath(effectiveCodePath, binding.ClassPath,
                        binding.ClassName + ".Binding" + CodeFileExtension,
                        out var bindingAssetPath, out var bindingsPath, out error))
                    return false;

                string folder = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(folder))
                {
                    error = $"无法解析代码生成目录：{assetPath}";
                    return false;
                }

                // 所有模板先完成预检，避免 PageDef 已写入后才发现代码模板缺失。
                bool needsSkeleton = !File.Exists(path);
                var skeletonAsset = frameworkMode ? frameworkCodeTemplate : codeTemplate;
                if (needsSkeleton && skeletonAsset == null)
                {
                    error = frameworkMode
                        ? "框架模式未配置「框架模式代码模板」（frameworkCodeTemplate）。\n"
                            + "请在 EmberCSharpImplementation Inspector 中重新指定后重试。"
                        : "未配置逻辑代码模板（codeTemplate）。";
                    return false;
                }

                string skeletonTpl = needsSkeleton ? ReadTemplate(skeletonAsset) : null;
                if (needsSkeleton && string.IsNullOrEmpty(skeletonTpl))
                {
                    error = "逻辑代码模板为空或无法读取。";
                    return false;
                }

                if (bindingCodeTemplate == null)
                {
                    error = "未配置绑定代码模板（bindingCodeTemplate）。";
                    return false;
                }

                string bindingTpl = ReadTemplate(bindingCodeTemplate);
                if (string.IsNullOrEmpty(bindingTpl))
                {
                    error = "绑定代码模板为空或无法读取。";
                    return false;
                }

                var ctx = BuildTemplateContext(binding, prefabName, declaredFields, baseClsName, frameworkMode);
                var renderedSkeleton = needsSkeleton ? RenderTemplate(skeletonTpl, ctx) : null;
                var renderedBinding = RenderTemplate(bindingTpl, ctx);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // 1. 生成 .cs 骨架（仅首次，不覆盖已存在文件）
                if (needsSkeleton)
                {
                    skeletonWritten = true;
                    File.WriteAllText(path, renderedSkeleton, new UTF8Encoding(false));
                }

                // 2. 生成 .Binding.cs（每次覆盖）
                bindingWritten = true;
                File.WriteAllText(bindingsPath, renderedBinding, new UTF8Encoding(false));

                // 3. 脚本全部写入成功后才提交 PageDef，避免后续模板/目录/脚本 IO 失败
                // 留下指向不存在页面代码的注册项。
                pageDefinitionUpdated = true;
                if (!TryGenerateOrUpdatePageDefinition(binding, frameworkMode, out error))
                {
                    error += $"\n脚本已写入但 PageDef 未完成，请检查后重试：\n{assetPath}\n"
                        + bindingAssetPath;
                    return false;
                }

                if (refreshAssets)
                    AssetDatabase.Refresh();
                EmberDebug.Log(TAG, "代码生成成功");
                return true;
            }
            catch (Exception exception)
            {
                error = $"C# 代码生成失败：{exception.Message}";
                if (skeletonWritten || bindingWritten || pageDefinitionUpdated)
                {
                    error += "\n本次操作已产生部分写入："
                        + (skeletonWritten ? " 逻辑骨架" : string.Empty)
                        + (bindingWritten ? " Binding" : string.Empty)
                        + (pageDefinitionUpdated ? " PageDef" : string.Empty)
                        + "。现有产物已保留，请按错误信息检查。";
                }
                EmberDebug.LogWarning(TAG, error);
                return false;
            }
        }

        /// <summary>在 EUIPageDef 文件中追加或同步 EUIPageDef 条目。框架模式写入 GamePages.cs（框架区），用户模式写入 GamePages.User.cs。</summary>
        public bool GenerateOrUpdatePageDefinition(EUIBinding binding, bool frameworkMode = false)
        {
            if (TryGenerateOrUpdatePageDefinition(binding, frameworkMode, out var error))
                return true;

            if (!string.IsNullOrEmpty(error))
                EditorUtility.DisplayDialog("生成代码失败", error, "确定");
            return false;
        }

        /// <summary>无弹窗地追加或同步 EUIPageDef 条目。</summary>
        internal bool TryGenerateOrUpdatePageDefinition(EUIBinding binding, bool frameworkMode,
            out string error)
        {
            error = null;
            if (!binding)
            {
                error = "EUIBinding 为空。";
                return false;
            }

            if (!binding.IsPage) return true;

            if (!IsSupportedBindingPageType(binding.PageType))
            {
                error = $"页面类型 {binding.PageType} 暂不受 EUIBinding 代码生成支持。";
                return false;
            }

            if (!TryValidateIdentifier(binding.ClassName, "类名", out error))
                return false;

            if (!TryValidateIdentifier(binding.PageName, "页面名称", out error))
                return false;

            string targetFile = frameworkMode ? GetFrameworkPageDefFile() : pageDefFile;
            if (string.IsNullOrEmpty(targetFile))
            {
                error = frameworkMode
                    ? "未配置 Framework 页面注册文件。"
                    : "未配置 User 页面注册文件。";
                return false;
            }

            if (!TryResolveAssetsPath(targetFile, "页面注册文件",
                    out targetFile, out var fullPath, out error))
                return false;

            if (!targetFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                error = $"页面注册文件必须是 Assets/ 下的 C# 文件：{targetFile}";
                return false;
            }

            if (!File.Exists(fullPath))
            {
                error = $"页面注册文件不存在：{targetFile}";
                return false;
            }

            var lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();

            string targetLayer = PageTypeToLayerName(binding.PageType);
            bool isFreePage = binding.PageType == PageType.FreePage;
            string targetPageType = $"PageType.{binding.PageType}";

            if (!TryResolvePrefabPath(binding, out var newPrefabPath, out var pathError))
            {
                error = pathError;
                return false;
            }

            string newLine = $"        public static readonly EUIPageDef {binding.PageName} = new(\"{newPrefabPath}\", UILayer.{targetLayer}, {targetPageType}{(isFreePage ? ", freePageSortingOrder: 30000" : "")});";

            // 写入前同时统计目标与对侧 partial；任一文件内或跨文件重复都 fail-closed，
            // 不能先改目标文件再遗漏 sibling 中的同名定义。
            string siblingFile = GetSiblingPageDefFile(targetFile);
            string siblingFull = null;
            List<string> siblingLines = null;
            if (!string.IsNullOrEmpty(siblingFile))
            {
                if (!TryResolveAssetsPath(siblingFile, "对侧页面注册文件",
                        out siblingFile, out siblingFull, out error))
                    return false;

                if (File.Exists(siblingFull))
                    siblingLines = File.ReadAllLines(siblingFull, Encoding.UTF8).ToList();
            }

            var targetMatches = FindPageDefLines(lines, binding.PageName);
            var siblingMatches = siblingLines != null
                ? FindPageDefLines(siblingLines, binding.PageName)
                : new List<int>();
            var totalMatches = targetMatches.Count + siblingMatches.Count;
            if (totalMatches > 1)
            {
                error = $"GamePages.cs 与 GamePages.User.cs 共有 {totalMatches} 个同名 EUIPageDef "
                    + $"{binding.PageName}，已拒绝自动更新。请先消除重复定义。";
                return false;
            }

            if (targetMatches.Count == 1)
            {
                if (TrySyncExistingPageDefinition(lines, targetMatches[0], binding.PageName,
                        newPrefabPath, newLine, fullPath, targetFile))
                    return true;

                error = $"EUIPageDef {binding.PageName} 不是可安全更新的标准 "
                    + "public static readonly 格式，已拒绝自动修改。";
                return false;
            }

            if (siblingMatches.Count == 1)
            {
                if (TrySyncExistingPageDefinition(siblingLines, siblingMatches[0], binding.PageName,
                        newPrefabPath, newLine, siblingFull, siblingFile))
                    return true;

                error = $"EUIPageDef {binding.PageName} 在 {siblingFile} 不是可安全更新的标准 "
                    + "public static readonly 格式，已拒绝自动修改。";
                return false;
            }

            // 不存在：追加新行
            string sectionHeader = targetLayer switch
            {
                "Background" => "Background 层",
                "TopMost" => "TopMost 层",
                "Popup" => "Popup 层",
                _ => "Normal 层"
            };

            int insertAfter = -1;
            int sectionContentStart = -1; // section header 块结束后的第一行（空 section 时用作插入点）
            bool inTargetSection = false;
            bool skippedHeaderSeparator = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(sectionHeader))
                {
                    inTargetSection = true;
                    skippedHeaderSeparator = false;
                    continue;
                }
                // 跳过 section header 块的装饰分隔线（// ====）
                if (inTargetSection && !skippedHeaderSeparator)
                {
                    skippedHeaderSeparator = true;
                    sectionContentStart = i + 1; // 分隔线之后的第一行 = section 内容起点
                    continue;
                }
                // 下一个 section 开始 = 离开当前 section
                if (inTargetSection && (lines[i].Contains("===") || lines[i].Contains("// ---")))
                {
                    break;
                }
                if (inTargetSection && lines[i].Contains("new(\""))
                {
                    insertAfter = i;
                }
            }

            // 在找到的最后一行 EUIPageDef 之后插入
            string docLine = $"        /// <summary>{binding.PageName} 页面</summary>";

            // 空 section：在 section header 之后插入
            if (insertAfter < 0 && sectionContentStart >= 0)
                insertAfter = sectionContentStart - 1;

            if (insertAfter >= 0)
            {
                // 获取缩进（跟随前一行）
                lines.Insert(insertAfter + 1, "");
                lines.Insert(insertAfter + 1, newLine);
                lines.Insert(insertAfter + 1, docLine);
            }
            else
            {
                // 回退：找不到位置时，追加到类末尾（类最后一行 "    }" 之前）
                int classEnd = -1;
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    // 只匹配含缩进的 "}"（类的），跳过无缩进的 "}"（命名空间的）
                    if ((lines[i].Trim() == "}" && lines[i].StartsWith(" ") || lines[i].StartsWith("\t"))
                        || lines[i].Contains("TODO: 在此处继续添加"))
                    {
                        classEnd = lines[i].Contains("TODO") ? i : i - 1;
                        break;
                    }
                }
                if (classEnd < 0) classEnd = lines.Count - 2;

                lines.Insert(classEnd + 1, "");
                lines.Insert(classEnd + 1, newLine);
                lines.Insert(classEnd + 1, docLine);
            }

            File.WriteAllText(fullPath, string.Join("\n", lines.ToArray()), new UTF8Encoding(false));

            // 校验失效 EUIPageDef
            ValidateAndPromptStalePageDefs(fullPath);

            return true;
        }

        /// <summary>框架模式的 EUIPageDef 目标文件：GamePages.User.cs → GamePages.cs（框架注册区，全文件头标记）。非标准配置时回退原配置。</summary>
        private string GetFrameworkPageDefFile()
        {
            const string userFile = "GamePages.User.cs";
            if (!string.IsNullOrEmpty(pageDefFile) && pageDefFile.EndsWith(userFile, StringComparison.Ordinal))
                return pageDefFile.Substring(0, pageDefFile.Length - userFile.Length) + "GamePages.cs";
            return pageDefFile;
        }

        /// <summary>GamePages.cs 与 GamePages.User.cs 互为 partial：返回目标文件的对侧注册文件（非该对时返回 null）。</summary>
        private static string GetSiblingPageDefFile(string targetFile)
        {
            if (string.IsNullOrEmpty(targetFile)) return null;
            if (targetFile.EndsWith("GamePages.User.cs", StringComparison.Ordinal))
                return targetFile.Substring(0, targetFile.Length - "GamePages.User.cs".Length) + "GamePages.cs";
            if (targetFile.EndsWith("GamePages.cs", StringComparison.Ordinal))
                return targetFile.Substring(0, targetFile.Length - "GamePages.cs".Length) + "GamePages.User.cs";
            return null;
        }

        /// <summary>查找真实代码中的同名 EUIPageDef 声明行；注释和所有字面量中的示例均忽略。</summary>
        private static List<int> FindPageDefLines(List<string> lines, string pageName)
        {
            return EUIPrefabCatalogService.FindPageDefinitions(string.Join("\n", lines))
                .Where(match => string.Equals(match.Name, pageName, StringComparison.Ordinal))
                .Select(match => match.DeclarationLine)
                .ToList();
        }

        private static string StripCommentsAndLiterals(string line, ref bool inBlockComment,
            bool maskLiterals = true)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;

            var result = new StringBuilder(line.Length);
            bool inString = false;
            bool inVerbatimString = false;
            bool inChar = false;
            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                char next = i + 1 < line.Length ? line[i + 1] : '\0';

                if (inBlockComment)
                {
                    result.Append(' ');
                    if (current == '*' && next == '/')
                    {
                        result.Append(' ');
                        i++;
                        inBlockComment = false;
                    }
                    continue;
                }

                if (inString || inVerbatimString || inChar)
                {
                    result.Append(maskLiterals ? ' ' : current);
                    if (inVerbatimString && current == '"' && next == '"')
                    {
                        result.Append(maskLiterals ? ' ' : next);
                        i++;
                    }
                    else if (inVerbatimString && current == '"')
                    {
                        inVerbatimString = false;
                    }
                    else if ((inString || inChar) && current == '\\' && next != '\0')
                    {
                        result.Append(maskLiterals ? ' ' : next);
                        i++;
                    }
                    else if (inString && current == '"')
                    {
                        inString = false;
                    }
                    else if (inChar && current == '\'')
                    {
                        inChar = false;
                    }
                    continue;
                }

                if (current == '/' && next == '/')
                {
                    result.Append(' ', line.Length - i);
                    break;
                }
                if (current == '/' && next == '*')
                {
                    result.Append("  ");
                    i++;
                    inBlockComment = true;
                    continue;
                }
                if (current == '@' && next == '"')
                {
                    result.Append(maskLiterals ? "  " : "@\"");
                    i++;
                    inVerbatimString = true;
                    continue;
                }
                if (current == '"')
                {
                    result.Append(maskLiterals ? ' ' : current);
                    inString = true;
                    continue;
                }
                if (current == '\'')
                {
                    result.Append(maskLiterals ? ' ' : current);
                    inChar = true;
                    continue;
                }

                result.Append(current);
            }

            return result.ToString();
        }

        private static bool TrySyncExistingPageDefinition(List<string> lines, int definitionIndex,
            string pageName, string prefabPath, string generatedLine, string fullPath, string displayFile)
        {
            if (definitionIndex < 0 || definitionIndex >= lines.Count
                || !Regex.IsMatch(lines[definitionIndex],
                    @"^\s*public\s+static\s+readonly\s+EUIPageDef\s+"
                    + Regex.Escape(pageName) + @"\b", RegexOptions.CultureInvariant))
                return false;

            bool inBlockComment = false;
            for (int i = definitionIndex; i < System.Math.Min(definitionIndex + 3, lines.Count); i++)
            {
                string code = StripCommentsAndLiterals(lines[i], ref inBlockComment);
                if (!Regex.IsMatch(code, @"(?<![\p{L}\p{Nd}_])new\s*\(")) continue;

                string oldLine = lines[i];
                bool isSimpleGeneratedLine = i == definitionIndex && Regex.IsMatch(oldLine,
                    $@"^\s*public static readonly EUIPageDef\s+{Regex.Escape(pageName)}\s*=\s*new\(.*\);\s*$");
                string updatedLine = isSimpleGeneratedLine
                    ? generatedLine
                    : Regex.Replace(oldLine, @"new\(""[^""]+""", $"new(\"{prefabPath}\"");

                if (updatedLine != oldLine)
                {
                    lines[i] = updatedLine;
                    File.WriteAllText(fullPath, string.Join("\n", lines.ToArray()), new UTF8Encoding(false));
                    EmberDebug.Log(TAG, isSimpleGeneratedLine
                        ? $"EUIPageDef {pageName} 已在 {displayFile} 同步路径、层级和页面类型。"
                        : $"EUIPageDef {pageName} 已在 {displayFile} 更新路径。复杂手写格式的页面类型请人工检查。");
                }
                else if (!isSimpleGeneratedLine)
                {
                    EmberDebug.LogWarning(TAG,
                        $"EUIPageDef {pageName} 在 {displayFile} 使用复杂手写格式，页面类型未自动同步，请人工检查。");
                }

                return true;
            }

            return false;
        }

        private static bool IsSupportedBindingPageType(PageType pageType)
        {
            return pageType == PageType.Background
                || pageType == PageType.MainPage
                || pageType == PageType.Popup
                || pageType == PageType.FullScreenPopup
                || pageType == PageType.TopMost
                || pageType == PageType.SubPage
                || pageType == PageType.FreePage;
        }

        private static bool IsPopupPageType(PageType pageType)
        {
            return pageType == PageType.Popup || pageType == PageType.FullScreenPopup;
        }

        private static string PageTypeToLayerName(PageType pageType)
        {
            return pageType switch
            {
                PageType.Background => "Background",
                PageType.Popup => "Popup",
                PageType.FullScreenPopup => "Popup",
                PageType.TopMost => "TopMost",
                PageType.FreePage => "TopMost",
                _ => "Normal",
            };
        }

        /// <summary>扫描 EUIPageDef 条目，若有失效项仅 Console 警告，不弹窗。</summary>
        private static void ValidateAndPromptStalePageDefs(string fullPath)
        {
            var stale = FindStalePageDefs(fullPath);
            if (stale.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append("发现 ").Append(stale.Count).Append(" 条 EUIPageDef 预制体尚未创建（");
            for (int i = 0; i < stale.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(stale[i].Name);
            }
            sb.Append("）。如需清理请使用菜单 Ember/UI/Clean Stale PageDefs。");
            EmberDebug.LogWarning(TAG, sb.ToString());
        }

        public struct StalePageDef
        {
            public string Name;
            public string PrefabPath;
            public int LineIndex;
        }

        /// <summary>查找所有预制体已不存在的 EUIPageDef 条目（公开给 Play Mode Guard 使用）</summary>
        public static List<StalePageDef> FindStalePageDefsPublic(string fullPath)
        {
            return FindStalePageDefs(fullPath);
        }

        /// <summary>查找所有预制体已不存在的 EUIPageDef 条目</summary>
        private static List<StalePageDef> FindStalePageDefs(string fullPath)
        {
            var result = new List<StalePageDef>();
            if (!File.Exists(fullPath)) return result;

            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            foreach (var match in EUIPrefabCatalogService.FindPageDefinitions(content))
            {
                // 旧清理器仅自动处理标准单行生成格式；复杂多行声明保持人工确认。
                if (!match.IsStandardField
                    || match.DeclarationLine != match.DeclarationEndLine
                    || string.IsNullOrEmpty(match.PrefabPath)) continue;

                // 直接检查预制体文件是否存在
                if (!File.Exists(match.PrefabPath))
                {
                    result.Add(new StalePageDef
                    {
                        Name = match.Name,
                        PrefabPath = match.PrefabPath,
                        LineIndex = match.DeclarationLine,
                    });
                }
            }
            return result;
        }

        /// <summary>清理指定的失效 EUIPageDef 条目</summary>
        public static int CleanStalePageDefs(string pageDefFilePath)
        {
            if (!TryResolveAssetsPath(pageDefFilePath, "页面注册文件",
                    out var assetPath, out var fullPath, out var error))
            {
                EmberDebug.LogWarning(TAG, $"拒绝清理 EUIPageDef：{error}");
                return 0;
            }

            if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                EmberDebug.LogWarning(TAG, $"拒绝清理非 C# 页面注册文件：{assetPath}");
                return 0;
            }

            if (!File.Exists(fullPath)) return 0;

            var stale = FindStalePageDefs(fullPath);
            if (stale.Count == 0) return 0;

            return CleanStalePageDefsInternal(fullPath, stale);
        }

        private static int CleanStalePageDefsInternal(string fullPath, List<StalePageDef> stale)
        {
            var lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
            var toRemove = new HashSet<int>();

            foreach (var s in stale)
            {
                toRemove.Add(s.LineIndex);
                for (int i = s.LineIndex - 1; i >= 0 && lines[i].TrimStart().StartsWith("///"); i--)
                    toRemove.Add(i);
            }

            var cleaned = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (!toRemove.Contains(i))
                    cleaned.Add(lines[i]);
            }

            File.WriteAllText(fullPath, string.Join("\n", cleaned.ToArray()), new UTF8Encoding(false));
            return stale.Count;
        }

        #endregion

        // --------------------------------------------------------

        #region 模板引擎

        internal const string OptionalUIUpdateMember =
            "        // [EmberOptional:begin UIUpdate]\n" +
            "        /// <summary>是否需要每帧 Update；由 EUIBinding「使用 UIUpdate」生成。</summary>\n" +
            "        public override bool NeedUpdate => true;\n" +
            "        // [EmberOptional:end UIUpdate]\n";

        internal const string OptionalOnUpdateMember =
            "        // [EmberOptional:begin OnUpdate]\n" +
            "        public override void OnUpdate()\n" +
            "        {\n" +
            "            base.OnUpdate();\n" +
            "        }\n" +
            "        // [EmberOptional:end OnUpdate]\n";

        internal const string FrameworkOptionalOnUpdateMember =
            "        // [EmberOptional:begin OnUpdate]\n" +
            "        public override void OnUpdate()\n" +
            "        {\n" +
            "            base.OnUpdate();\n" +
            "            OnUpdateUser();\n" +
            "        }\n" +
            "        // [EmberOptional:end OnUpdate]\n";

        internal const string FrameworkOnUpdateUserHook =
            "        /// <summary>用户逐帧更新钩子：框架 OnUpdate 结束时调用。</summary>\n" +
            "        private void OnUpdateUser()\n" +
            "        {\n" +
            "            // 在此编写逐帧业务逻辑\n" +
            "        }\n";

        internal const string OptionalAutoCreateClickableMaskMember =
            "        // [EmberOptional:begin AutoCreateClickableMask]\n" +
            "        /// <summary>\n" +
            "        /// 是否自动创建可点击遮罩（仅 Popup 生效，默认 true）。\n" +
            "        /// 普通开关请使用 EUIBinding「创建遮罩」；此覆写用于条件式代码控制。\n" +
            "        /// </summary>\n" +
            "        protected override bool AutoCreateClickableMask => true;\n" +
            "        // [EmberOptional:end AutoCreateClickableMask]\n";

        internal const string OptionalOnClickMaskMember =
            "        // [EmberOptional:begin OnClickMask]\n" +
            "        /// <summary>\n" +
            "        /// 点击遮罩回调（默认关闭本弹窗）。\n" +
            "        /// 不允许点遮罩关闭：清空方法体；\n" +
            "        /// 自定义点击行为：替换方法体，需要关闭时调用 base.OnClickMask()。\n" +
            "        /// </summary>\n" +
            "        protected override void OnClickMask()\n" +
            "        {\n" +
            "            base.OnClickMask();\n" +
            "        }\n" +
            "        // [EmberOptional:end OnClickMask]\n";

        internal const string FrameworkOptionalOnClickMaskMember =
            "        // [EmberOptional:begin OnClickMask]\n" +
            "        /// <summary>点击遮罩时先调用用户钩子，再执行面板配置的默认关闭行为。</summary>\n" +
            "        protected override void OnClickMask()\n" +
            "        {\n" +
            "            OnClickMaskUser();\n" +
            "            base.OnClickMask();\n" +
            "        }\n" +
            "        // [EmberOptional:end OnClickMask]\n";

        internal const string FrameworkOnClickMaskUserHook =
            "        /// <summary>用户遮罩点击钩子：默认关闭行为之前调用。</summary>\n" +
            "        private void OnClickMaskUser()\n" +
            "        {\n" +
            "            // 在此编写遮罩点击后的自定义逻辑\n" +
            "        }\n";

        /// <summary>按 EUIBinding 可视化选项构建首次生成的页面可选成员。</summary>
        internal static string BuildOptionalPageFeatureMembers(EUIBinding binding)
        {
            if (binding == null || !binding.IsPage) return string.Empty;

            var members = new StringBuilder();
            if (binding.UseUIUpdate)
            {
                members.Append(OptionalUIUpdateMember).AppendLine();
                members.Append(OptionalOnUpdateMember).AppendLine();
            }

            bool isPopup = IsPopupPageType(binding.PageType);
            if (isPopup && binding.GenerateAutoCreateClickableMaskOverride)
                members.Append(OptionalAutoCreateClickableMaskMember).AppendLine();
            if (isPopup && binding.GenerateOnClickMaskOverride)
                members.Append(OptionalOnClickMaskMember).AppendLine();

            return members.ToString();
        }

        /// <summary>构建 Framework 模式放入 [EmberManaged] 块的页面可选成员。</summary>
        internal static string BuildFrameworkOptionalPageFeatureMembers(EUIBinding binding)
        {
            if (binding == null || !binding.IsPage) return string.Empty;

            var members = new StringBuilder();
            if (binding.UseUIUpdate)
            {
                members.Append(OptionalUIUpdateMember).AppendLine();
                members.Append(FrameworkOptionalOnUpdateMember).AppendLine();
            }

            bool isPopup = IsPopupPageType(binding.PageType);
            if (isPopup && binding.GenerateAutoCreateClickableMaskOverride)
                members.Append(OptionalAutoCreateClickableMaskMember).AppendLine();
            if (isPopup && binding.GenerateOnClickMaskOverride)
                members.Append(FrameworkOptionalOnClickMaskMember).AppendLine();

            return members.ToString().TrimEnd();
        }

        /// <summary>构建 Framework 模式块外的可选用户钩子。</summary>
        internal static string BuildFrameworkOptionalUserHooks(EUIBinding binding)
        {
            if (binding == null || !binding.IsPage) return string.Empty;

            var hooks = new StringBuilder();
            if (binding.UseUIUpdate)
                hooks.Append(FrameworkOnUpdateUserHook).AppendLine();

            bool needsMaskHook = IsPopupPageType(binding.PageType)
                && binding.GenerateOnClickMaskOverride;
            if (needsMaskHook)
                hooks.Append(FrameworkOnClickMaskUserHook).AppendLine();

            return hooks.ToString().TrimEnd();
        }

        /// <summary>构建模板上下文变量（fields_decl 和 fields_bind 已预渲染为字符串）</summary>
        private Dictionary<string, object> BuildTemplateContext(EUIBinding binding, string prefabName,
            EUIBinding.BindingEntry[] entries, string baseClsName = null, bool frameworkMode = false)
        {
            var decl = new StringBuilder();
            var bind = new StringBuilder();

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string typeName = GetCSharpTypeName(entry);
                    string comment = entry.GameObject
                        ? (GetTransformPath(binding.transform, entry.GameObject.transform) ?? entry.Name)
                        : entry.Name;

                    decl.AppendLine($"        /// <summary>");
                    decl.AppendLine($"        /// {comment}");
                    decl.AppendLine($"        /// </summary>");
                    decl.AppendLine($"        private {typeName} {entry.Name};");
                    decl.AppendLine();

                    bind.AppendLine($"            {entry.Name} = ControlMap[\"{entry.Name}\"] as {typeName};");
                }
            }

            return new Dictionary<string, object>
            {
                ["author_name"] = LogicImplementationData.GenerateAuthorName,
                ["prefab_name"] = prefabName ?? binding.gameObject.name,
                ["page_name"] = binding.PageName ?? "",
                ["class_name"] = binding.ClassName ?? "",
                ["base_class_name"] = !string.IsNullOrEmpty(baseClsName) ? baseClsName : this.baseClassName,
                ["namespace_name"] = GetDefaultNamespace(binding.PathMode),
                ["create_date"] = System.DateTime.Now.ToString(),
                ["fields_decl"] = decl.ToString(),
                ["fields_bind"] = bind.ToString(),
                ["page_feature_members"] = BuildOptionalPageFeatureMembers(binding),
                ["framework_page_feature_members"] = frameworkMode
                    ? BuildFrameworkOptionalPageFeatureMembers(binding)
                    : string.Empty,
                ["framework_optional_user_hooks"] = frameworkMode
                    ? BuildFrameworkOptionalUserHooks(binding)
                    : string.Empty,
            };
        }

        /// <summary>读取模板文件内容</summary>
        private static string ReadTemplate(DefaultAsset asset)
        {
            if (asset == null) return null;
            string path = AssetDatabase.GetAssetPath(asset);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }

        /// <summary>简易模板渲染引擎（仅支持 {var} 替换，{{ → {，}} → }）</summary>
        private static string RenderTemplate(string template, Dictionary<string, object> context)
        {
            // 预处理：{{ → \x01, }} → \x02（保护转义花括号）
            template = template.Replace("{{", "\x01").Replace("}}", "\x02");

            var sb = new StringBuilder();
            int pos = 0;

            while (pos < template.Length)
            {
                int open = template.IndexOf('{', pos);
                if (open < 0)
                {
                    sb.Append(template.Substring(pos));
                    break;
                }

                sb.Append(template.Substring(pos, open - pos));

                int close = template.IndexOf('}', open + 1);
                if (close < 0)
                {
                    sb.Append(template.Substring(open));
                    break;
                }

                string expr = template.Substring(open + 1, close - open - 1).Trim();

                if (context.TryGetValue(expr, out var value) && value != null)
                {
                    sb.Append(value.ToString());
                }

                pos = close + 1;
            }

            // 后处理：\x01 → {, \x02 → }
            return sb.ToString().Replace("\x01", "{").Replace("\x02", "}");
        }

        /// <summary>将 WidgetType 映射为 C# 类型名</summary>
        private static string GetCSharpTypeName(EUIBinding.BindingEntry entry)
        {
            if (entry.Type == EUIBinding.WidgetTypes.Extension && !string.IsNullOrEmpty(entry.ClassName))
                return EUIBindingEditorUtility.GetExtensionFullTypeName(entry.ClassName);
            if (entry.Type == EUIBinding.WidgetTypes.UILogic && !string.IsNullOrEmpty(entry.ClassName))
                return entry.ClassName;

            return entry.Type switch
            {
                EUIBinding.WidgetTypes.Component   => "Component",
                EUIBinding.WidgetTypes.Text        => "TMP_Text",
                EUIBinding.WidgetTypes.Image       => "Image",
                EUIBinding.WidgetTypes.RawImage    => "RawImage",
                EUIBinding.WidgetTypes.Button      => "Button",
                EUIBinding.WidgetTypes.Toggle      => "Toggle",
                EUIBinding.WidgetTypes.ToggleGroup => "ToggleGroup",
                EUIBinding.WidgetTypes.InputField  => "TMP_InputField",
                EUIBinding.WidgetTypes.ScrollRect  => "ScrollRect",
                EUIBinding.WidgetTypes.ProgressBar => "Slider",
                EUIBinding.WidgetTypes.Canvas      => "Canvas",
                EUIBinding.WidgetTypes.CanvasGroup => "CanvasGroup",
                _ => "Component",
            };
        }

        /// <summary>解析已有 EUIPageDef 文件中的常量定义</summary>
        private static void ParseExistingPageDefs(string filePath, List<Dictionary<string, object>> pages)
        {
            string fullPath = GetFullPath(filePath);
            if (!File.Exists(fullPath)) return;

            foreach (var line in File.ReadAllLines(fullPath, Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("public const string"))
                {
                    int eqIdx = trimmed.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        string name = trimmed.Substring(20, eqIdx - 21).Trim();
                        string value = trimmed.Substring(eqIdx + 1).Trim(' ', ';', '"');
                        pages.Add(new Dictionary<string, object>
                        {
                            ["name"] = name,
                            ["info"] = value,
                        });
                    }
                }
            }
        }

        private static string GetTransformPath(Transform root, Transform target)
        {
            if (!root || !target) return null;
            if (root == target) return string.Empty;
            var names = new Stack<string>();
            var current = target;
            while (current && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return current == root ? string.Join("/", names.ToArray()) : null;
        }

        private static string GetFullPath(string assetPath)
        {
            return assetPath.StartsWith("Assets/")
                ? Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length))
                : assetPath;
        }

        #endregion

        // --------------------------------------------------------

        #region 菜单项

        [MenuItem("Assets/Create/Ember/UI/C# 实现数据")]
        public static void CreateAsset()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                var instance = ScriptableObject.CreateInstance<CSharpLogicImplementationData>();
                AssetDatabase.CreateAsset(instance,
                    AssetDatabase.GenerateUniqueAssetPath(path + "/C#实现.asset"));
            }
        }

        [MenuItem("Ember/UI/Clean Stale PageDefs")]
        public static void CleanStalePageDefsMenu()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null || settings.LogicImplementations.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未配置 CSharpLogicImplementationData，请在 Project Settings → EUI Binding 中添加。", "确认");
                return;
            }

            string pageDefFile = null;
            foreach (var impl in settings.LogicImplementations)
            {
                if (impl is CSharpLogicImplementationData csharp && !string.IsNullOrEmpty(csharp.PageDefFile))
                {
                    pageDefFile = csharp.PageDefFile;
                    break;
                }
            }

            if (string.IsNullOrEmpty(pageDefFile))
            {
                EditorUtility.DisplayDialog("提示", "CSharpLogicImplementationData 中未配置 EUIPageDef 文件路径。", "确认");
                return;
            }

            int removed = CleanStalePageDefs(pageDefFile);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(removed > 0 ? "完成" : "提示",
                removed > 0 ? $"已清理 {removed} 条失效 EUIPageDef。" : "未发现失效的 EUIPageDef 条目。", "确认");
        }

        #endregion
    }

    // --------------------------------------------------------

    /// <summary>
    /// Play Mode 前置校验：扫描失效 EUIPageDef，阻止进入 Play。
    ///
    /// <para>性能优化：引入脏标记机制，仅在 EUIPageDef 文件或 Prefabs 目录变动时才检查。
    /// 避免每次进入 Play Mode 都做不必要的扫描。</para>
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public static class PlayModePageDefGuard
    {
        private const string TAG = LogTags.EmberUI;

        /// <summary>脏标记：有变动时为 true，检查后重置为 false</summary>
        private static bool _dirty = true; // 首次启动默认检查一次

        static PlayModePageDefGuard()
        {
            // 域重载（编译/脚本变更）后标记脏
            _dirty = true;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void MarkDirty()
        {
            _dirty = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!_dirty) return; // 无变动，跳过检查

            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null || settings.LogicImplementations.Length == 0)
                return;

            string pageDefFile = null;
            foreach (var impl in settings.LogicImplementations)
            {
                if (impl is CSharpLogicImplementationData csharp && !string.IsNullOrEmpty(csharp.PageDefFile))
                {
                    pageDefFile = csharp.PageDefFile;
                    break;
                }
            }

            if (string.IsNullOrEmpty(pageDefFile)) return;

            if (!CSharpLogicImplementationData.TryResolveAssetsPath(
                    pageDefFile, "页面注册文件", out var assetPath, out var fullPath, out var pathError))
            {
                EmberDebug.LogWarning(TAG, $"Play Mode EUIPageDef 校验已拒绝非法路径：{pathError}");
                return;
            }

            if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                EmberDebug.LogWarning(TAG, $"Play Mode EUIPageDef 校验已拒绝非 C# 文件：{assetPath}");
                return;
            }

            if (!System.IO.File.Exists(fullPath)) return;

            var stale = CSharpLogicImplementationData.FindStalePageDefsPublic(fullPath);
            if (stale.Count == 0)
            {
                _dirty = false;
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("以下 EUIPageDef 对应的预制体已不存在：");
            sb.AppendLine();
            foreach (var s in stale)
            {
                sb.Append("  · ");
                sb.Append(s.Name);
                sb.Append(" → ");
                sb.Append(s.PrefabPath);
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.Append("是否自动清理这些失效条目？");

            if (EditorUtility.DisplayDialog("禁止进入 Play Mode", sb.ToString(), "清理并进入", "取消"))
            {
                int removed = CSharpLogicImplementationData.CleanStalePageDefs(pageDefFile);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", $"已清理 {removed} 条失效 EUIPageDef。", "确认");
                _dirty = false;
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }

        /// <summary>
        /// 监控 EUIPageDef 文件和 Prefabs 目录的变动，变动时标记脏。
        /// </summary>
        private class GuardAssetPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] imported, string[] deleted, string[] movedFrom, string[] movedTo)
            {
                foreach (var path in imported)
                    if (IsPageDefOrPrefab(path)) { MarkDirty(); return; }
                foreach (var path in deleted)
                    if (IsPageDefOrPrefab(path)) { MarkDirty(); return; }
                foreach (var path in movedTo)
                    if (IsPageDefOrPrefab(path)) { MarkDirty(); return; }
            }

            private static bool IsPageDefOrPrefab(string path)
            {
                if (path.EndsWith(".prefab") && path.Contains("Prefabs"))
                    return true;
                if (path.EndsWith("GamePages.cs") || path.EndsWith("GamePages.User.cs"))
                    return true;
                return false;
            }
        }
    }

    // --------------------------------------------------------

    /// <summary>
    /// CSharpLogicImplementationData 的自定义 Inspector。
    /// </summary>
    [CustomEditor(typeof(CSharpLogicImplementationData), true)]
    public class CSharpLogicImplementationDataEditor : LogicImplementationDataEditor
    {
        private SerializedProperty pageDefFile;
        private SerializedProperty baseClassName;
        private SerializedProperty bindingCodeTemplate;
        private SerializedProperty codeTemplate;
        private SerializedProperty pageDefTemplate;
        private SerializedProperty codeTemplateForNoGen;
        private SerializedProperty frameworkCodeTemplate;
        private SerializedProperty uiResourceRoot;

        protected override void OnEnable()
        {
            base.OnEnable();
            pageDefFile = serializedObject.FindProperty("pageDefFile");
            baseClassName = serializedObject.FindProperty("baseClassName");
            bindingCodeTemplate = serializedObject.FindProperty("bindingCodeTemplate");
            codeTemplate = serializedObject.FindProperty("codeTemplate");
            pageDefTemplate = serializedObject.FindProperty("pageDefTemplate");
            codeTemplateForNoGen = serializedObject.FindProperty("codeTemplateForNoGen");
            frameworkCodeTemplate = serializedObject.FindProperty("frameworkCodeTemplate");
            uiResourceRoot = serializedObject.FindProperty("uiResourceRoot");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(baseClassName, new GUIContent("页面逻辑基类"));
            if (string.IsNullOrEmpty(baseClassName.stringValue))
                EditorGUILayout.HelpBox("请输入正确的基类名（如 Ember.UI.EUIPage）", MessageType.Error);

            EditorGUILayout.PropertyField(pageDefFile, new GUIContent("EUIPageDef 文件路径"));
            if (string.IsNullOrEmpty(pageDefFile.stringValue))
                EditorGUILayout.HelpBox("请输入 EUIPageDef 源码文件路径（如 Assets/Game/UI/GamePages.User.cs，用户页面注册区）", MessageType.Error);

            EditorGUILayout.PropertyField(uiResourceRoot, new GUIContent("UI 资源根目录"));
            EditorGUILayout.HelpBox(
                "框架模式 → Common/Prefabs；用户模式 → Module/<输出子目录首段>/Prefabs。\n" +
                "示例：输出子目录 Inventory/Page → Module/Inventory/Prefabs。",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("代码生成模板", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(codeTemplate, new GUIContent("逻辑代码模板 (.cs 骨架)"));
            EditorGUILayout.PropertyField(bindingCodeTemplate, new GUIContent("绑定代码模板 (.Binding.cs)"));
            EditorGUILayout.PropertyField(pageDefTemplate, new GUIContent("EUIPageDef 模板"));
            EditorGUILayout.PropertyField(codeTemplateForNoGen, new GUIContent("剪贴板代码模板"));
            EditorGUILayout.PropertyField(frameworkCodeTemplate, new GUIContent("框架模式代码模板 (.cs 骨架·块标记)"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
