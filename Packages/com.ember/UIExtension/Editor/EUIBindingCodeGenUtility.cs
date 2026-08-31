// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;

using Ember.Basic;

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
        #region 生命周期（初始化）

        static EUIBindingCodeGenUtility()
        {
            EUIBinding.OnIsOnPrefab = HandleIsOnPrefab;
            EUIBinding.OnIsEmbeddedPackage = IsEmbeddedPackage;
            EUIBinding.OnGetCodeRootPath = HandleGetCodeRootPath;
            EUIBinding.OnGetLogicNames = HandleGetLogicNames;
            EUIBinding.OnGetGeneratedPath = HandleGetGeneratedPath;
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
            if (!binding) return;

            bool frameworkMode = binding.PathMode == EUIBinding.CodePathMode.Framework
                && IsEmbeddedPackage();
            EmberDebug.Log("EmberUI", $"代码生成模式: {(frameworkMode ? "框架（块标记 + 框架注册区）" : "用户")}（PathMode={binding.PathMode}，embedded={IsEmbeddedPackage()}）");

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("生成代码失败",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            if (!logic.CanGenerate(binding))
            {
                EditorUtility.DisplayDialog("生成代码失败",
                    "配置不完整。请检查类名、页面名等字段。", "确定");
                return;
            }

            // 二次确认
            var hasExistingFile = HandleHasGeneratedFile(binding);
            var confirmMsg = hasExistingFile
                ? $"重新生成将刷新 .Binding.cs 文件（.cs 骨架不受影响），是否继续？"
                : $"确认生成 {binding.ClassName}.cs 和 {binding.ClassName}.Binding.cs？";
            var confirmBtn = hasExistingFile ? "重新生成" : "生成";
            if (!EditorUtility.DisplayDialog("确认生成代码", confirmMsg, confirmBtn, "取消"))
                return;

            // 框架页面守卫：预制体位于包内（Packages/ 开头）时禁止生成——框架代码已随包发布，只读
            var prefabPath = GetBindingPrefabPath(binding);
            if (!string.IsNullOrEmpty(prefabPath)
                && prefabPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("无法生成代码",
                    "该绑定属于框架页面（预制体位于 com.ember 包内），代码已随包发布，不可生成。", "确定");
                return;
            }

            // 1. 先生成/更新预制体（如果不在预制体上）—— 后续步骤依赖 prefab 路径
            if (!HandleIsOnPrefab(binding))
            {
                GeneratePrefab(binding);
            }

            // 2. 生成脚本代码
            if (binding.IsPage)
            {
                var csharp = logic as CSharpLogicImplementationData;
                if (csharp)
                    csharp.GenerateOrUpdatePageDefinition(binding);
            }

            string baseClsName = null;
            EUIBinding.BindingEntry[] declaredFields = null;
            var baseBinding = GetBaseBinding(binding);
            if (baseBinding)
            {
                baseClsName = baseBinding.ClassName;
                declaredFields = GetDeclaredFields(binding, baseBinding);
            }

            logic.GenerateCode(binding, baseClsName, declaredFields);

            // 3. 生成自定义参数模板（如果勾选了"生成自定义参数"且文件不存在）
            if (binding.GenerateCustomSettings)
            {
                GenerateCustomSettingsTemplate(binding);
            }

            EmberDebug.Log("EmberUI", $"代码生成完成：{binding.ClassName}");

            // 框架模式：把当前全部绑定条目标记为框架子组件（清除/重收集操作将保护它们）
            if (frameworkMode)
                MarkFrameworkBindings(binding);

            // 4. 自动创建自定义页面参数实例（如果生成的代码定义了 Settings 类型）
            AssetDatabase.Refresh();
            CreateCustomSettingsIfExists(binding);
            AssetDatabase.Refresh();

            // 5. 同步 Custom 过渡方法（Custom 模式注入/非 Custom 模式移除）
            SyncCustomTransitionMethods(binding);
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

            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, list.ToArray());
            Undo.RecordObject(binding, "标记框架子组件");
            EditorUtility.SetDirty(binding);
            EmberDebug.Log("EmberUI", $"已标记 {list.Count} 个框架子组件条目（受保护：清除操作会保留）");
        }

        /// <summary>
        /// 生成预制体到 {根目录}/Prefabs/{预制体名}.prefab
        /// </summary>
        private static void GeneratePrefab(EUIBinding binding)
        {
            var root = binding.CodePath;
            if (string.IsNullOrEmpty(root)) return;

            var prefabDir = $"{root}/Prefabs";
            if (!Directory.Exists(prefabDir))
                Directory.CreateDirectory(prefabDir);

            var prefabName = binding.PrefabName;
            var prefabPath = $"{prefabDir}/{prefabName}.prefab";
            var go = binding.gameObject;

            // 同步场景 GameObject 名与预制体名
            if (go.name != prefabName)
                go.name = prefabName;

            // 清理所有子节点上的 MissingScript 残留
            var allGOs = go.GetComponentsInChildren<Transform>(true);
            var removedCount = 0;
            foreach (var t in allGOs)
            {
                removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            if (removedCount > 0)
                EmberDebug.LogWarning("EmberUI", $"已移除 {removedCount} 个缺失脚本残留");

            // 如果已存在同路径预制体，询问是否覆盖
            if (File.Exists(prefabPath))
            {
                var overwrite = EditorUtility.DisplayDialog("预制体已存在",
                    $"\"{prefabPath}\" 已存在，是否覆盖？", "覆盖", "取消");
                if (!overwrite) return;
            }

            try
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);
                EmberDebug.Log("EmberUI", $"预制体已生成：{prefabPath}");
            }
            catch (System.Exception e)
            {
                EmberDebug.LogWarning("EmberUI", $"预制体生成失败（代码已生成）：{e.Message}");
            }
        }

        /// <summary>
        /// 生成 {className}Settings.cs 模板文件（仅首次）。
        /// 文件放在与 .cs 同级的目录下。
        /// </summary>
        private static void GenerateCustomSettingsTemplate(EUIBinding binding)
        {
            var root = binding.CodePath;
            if (string.IsNullOrEmpty(root)) return;

            var subDir = string.IsNullOrEmpty(binding.ClassPath) ? "" : binding.ClassPath + "/";
            var folder = $"{root}/{subDir}";
            var filePath = $"{folder}{binding.ClassName}Settings.cs";

            if (File.Exists(filePath)) return; // 已存在，不覆盖

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
            EmberDebug.Log("EmberUI", $"已生成自定义参数模板：{binding.ClassName}Settings.cs");
        }

        /// <summary>
        /// 根据"使用自定义动画"开关同步 OnCustomEnter/OnCustomExit 方法：
        /// <list type="bullet">
        ///   <item><b>勾选自定义</b>：如果缺少则注入方法骨架</item>
        ///   <item><b>取消勾选</b>：如果存在且为默认骨架则删除（含用户代码则弹窗确认）</item>
        /// </list>
        /// </summary>
        private static void SyncCustomTransitionMethods(EUIBinding binding)
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
                    var confirmed = EditorUtility.DisplayDialog(
                        "删除自定义过渡方法",
                        $"已取消勾选'使用自定义动画'，但 {binding.ClassName}.cs 中存在 OnCustomEnter/OnCustomExit 方法。\n\n"
                        + "检测到方法体包含自定义代码，是否确认删除？\n（可在删除前手动备份代码）",
                        "确认删除", "保留");
                    if (!confirmed) return;
                }

                RemoveCustomTransitionMethods(fullPath, content);
                AssetDatabase.Refresh();
                binding.RefreshCustomTransitionCheck();
            }
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
            EmberDebug.Log("EmberUI", $"已注入 OnCustomEnter/OnCustomExit 到 {Path.GetFileName(fullPath)}");
        }

        /// <summary>从 .cs 文件中移除 OnCustomEnter 和 OnCustomExit 方法（含签名和整个方法体）。</summary>
        private static void RemoveCustomTransitionMethods(string fullPath, string content)
        {
            var lines = new List<string>(content.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None));

            // 分别查找并移除两个方法
            lines = RemoveMethodBlock(lines, "OnCustomEnter");
            lines = RemoveMethodBlock(lines, "OnCustomExit");

            File.WriteAllText(fullPath, string.Join("\n", lines), System.Text.Encoding.UTF8);
            EmberDebug.Log("EmberUI", $"已移除 OnCustomEnter/OnCustomExit 从 {Path.GetFileName(fullPath)}");
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
        /// 代码生成后自动查找 {className}Settings 类型并创建实例，
        /// 赋值给 binding 的 _pageSettings，使 Inspector 显示自定义参数折叠框。
        /// </summary>
        private static void CreateCustomSettingsIfExists(EUIBinding binding)
        {
            if (string.IsNullOrEmpty(binding.ClassName)) return;

            // 查找 {className}Settings 类型
            var settingsTypeName = $"{binding.ClassName}Settings";
            System.Type settingsType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                settingsType = asm.GetType(settingsTypeName);
                if (settingsType != null) break;
                // 也尝试带命名空间查找
                foreach (var ns in new[] { "Ember.UI", "Game.UI", "" })
                {
                    var fullName = string.IsNullOrEmpty(ns) ? settingsTypeName : $"{ns}.{settingsTypeName}";
                    settingsType = asm.GetType(fullName);
                    if (settingsType != null) break;
                }
                if (settingsType != null) break;
            }

            // 先检查现有 _pageSettings 是否匹配当前类名，不匹配则清除
            var settingsField = typeof(EUIBinding).GetField("_pageSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var existingSettings = settingsField?.GetValue(binding);
            if (existingSettings != null && existingSettings.GetType().Name != settingsTypeName)
            {
                settingsField.SetValue(binding, null);
                EmberDebug.Log("EmberUI", $"已清除不匹配的自定义参数: {existingSettings.GetType().Name} → 期望 {settingsTypeName}");
            }

            if (settingsType == null || !settingsType.IsSerializable)
            {
                // 类型不存在，确保 _pageSettings 为 null（让 HasCustomSettings 返回 false）
                if (existingSettings != null)
                    settingsField?.SetValue(binding, null);
                return;
            }

            // 已有匹配类型则跳过
            if (existingSettings != null && existingSettings.GetType() == settingsType)
                return;

            // 创建实例并写入 binding
            var instance = System.Activator.CreateInstance(settingsType);
            settingsField?.SetValue(binding, instance);
            EditorUtility.SetDirty(binding);
            EmberDebug.Log("EmberUI", $"已创建自定义参数：{settingsTypeName}");
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

            EmberDebug.Log("EmberUI",
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
            EmberDebug.Log("EmberUI", $"清除完成：保留 {kept.Count} 个框架子组件条目");

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
            EmberDebug.Log("EmberUI", $"清除完成：保留 {kept.Count} 个框架子组件条目");
        }

        private static void HandleCopyGeneratedPath(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return;
            GUIUtility.systemCopyBuffer = path;
            EmberDebug.Log("EmberUI", $"已复制路径：{path}");
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
