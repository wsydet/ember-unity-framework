// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Ember.Basic;

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
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("生成的代码所在的命名空间")]
        private string namespaceName = "Game.UI";

        [SerializeField]
        [Tooltip("页面逻辑基类（含命名空间），如 Ember.UI.EUIPage")]
        private string baseClassName = "Ember.UI.EUIPage";

        [SerializeField]
        [Tooltip("EUIPageDef 源码文件路径（如 Assets/Game/UI/GamePages.cs）")]
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

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public override string CodeFileExtension => ".cs";

        public override bool CanGenerate(EUIBinding binding)
        {
            return base.CanGenerate(binding)
                && !string.IsNullOrEmpty(namespaceName)
                && codeTemplate != null
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

        public override void GenerateCodeForNoGen(EUIBinding binding, string className)
        {
            string templateContent = ReadTemplate(codeTemplateForNoGen);
            if (string.IsNullOrEmpty(templateContent))
            {
                EmberDebug.LogWarning("EmberUI", "剪贴板模板为空，无法生成。");
                return;
            }

            string result = RenderTemplate(templateContent, BuildTemplateContext(binding, className, binding.Bindings));
            GUIUtility.systemCopyBuffer = result;
            EmberDebug.Log("EmberUI", "代码已复制至剪贴板");
        }

        public override void GenerateCode(EUIBinding binding, string baseClsName, EUIBinding.BindingEntry[] declaredFields)
        {
            if (!GenerateOrUpdatePageDefinition(binding))
                return;

            TryGetPrefabName(binding, out var prefabName);
            prefabName = Path.GetFileNameWithoutExtension(prefabName);
            baseClsName = !string.IsNullOrEmpty(baseClsName) ? baseClsName : this.baseClassName;
            declaredFields = declaredFields ?? binding.Bindings;

            string effectiveCodePath = !string.IsNullOrEmpty(binding.CodePath) ? binding.CodePath : codePath;
            string relativePath = string.IsNullOrEmpty(binding.ClassPath)
                ? binding.ClassName
                : binding.ClassPath + "/" + binding.ClassName;
            string path = effectiveCodePath + "/" + relativePath + CodeFileExtension;
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ctx = BuildTemplateContext(binding, prefabName, declaredFields, baseClsName);

            // 1. 生成 .cs 骨架（仅首次）
            if (!File.Exists(path) && codeTemplate != null)
            {
                string skeletonTpl = ReadTemplate(codeTemplate);
                if (!string.IsNullOrEmpty(skeletonTpl))
                {
                    File.WriteAllText(path, RenderTemplate(skeletonTpl, ctx), new UTF8Encoding(false));
                }
            }

            // 2. 生成 .Binding.cs（每次覆盖）
            if (bindingCodeTemplate != null)
            {
                string bindingTpl = ReadTemplate(bindingCodeTemplate);
                if (!string.IsNullOrEmpty(bindingTpl))
                {
                    string bindingsPath = path.Replace(".cs", ".Binding.cs");
                    File.WriteAllText(bindingsPath, RenderTemplate(bindingTpl, ctx), new UTF8Encoding(false));
                }
            }

            AssetDatabase.Refresh();
            EmberDebug.Log("EmberUI", "代码生成成功");
        }

        /// <summary>在 GamePages.cs 中追加新的 EUIPageDef 条目（不覆盖已有内容）</summary>
        public bool GenerateOrUpdatePageDefinition(EUIBinding binding)
        {
            if (!binding.IsPage) return true;

            if (binding.PageFlags == PageFlags.None)
            {
                EditorUtility.DisplayDialog("生成代码失败", "界面类型 PageFlags 定义错误!", "确定");
                return false;
            }

            if (string.IsNullOrEmpty(binding.ClassName))
            {
                EditorUtility.DisplayDialog("生成代码失败", "类名为空，无法生成路径。", "确定");
                return false;
            }

            if (string.IsNullOrEmpty(pageDefFile)) return true;

            string fullPath = GetFullPath(pageDefFile);
            if (!File.Exists(fullPath)) return true;

            var lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();

            // 确定目标 UILayer
            string targetLayer;
            if ((binding.PageFlags & PageFlags.Background) != 0)
                targetLayer = "Background";
            else if ((binding.PageFlags & PageFlags.FreePage) != 0)
                targetLayer = "FreePage";
            else if ((binding.PageFlags & PageFlags.TopMost) != 0)
                targetLayer = "TopMost";
            else if ((binding.PageFlags & PageFlags.Popup) != 0)
                targetLayer = "Popup";
            else
                targetLayer = "Normal";

            // 完整 Asset 路径: {CodePath}/Prefabs/{PrefabName}.prefab
            var codeRoot = binding.CodePath;
            string newPrefabPath = string.IsNullOrEmpty(codeRoot)
                ? $"Assets/Game/UI/Runtime/Prefabs/{binding.PrefabName}.prefab"
                : $"{codeRoot}/Prefabs/{binding.PrefabName}.prefab";

            // 检查是否已存在 —— 存在则更新路径，不存在则追加
            // 兼容单行 (Xxx = new(...)) 和多行 (Xxx \n = new(...)) 两种写法
            string defPattern = $"EUIPageDef {binding.PageName}";
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(defPattern))
                {
                    // 找到 EUIPageDef 声明行，在后续几行中找 new("..." 并替换
                    for (int j = i; j < System.Math.Min(i + 3, lines.Count); j++)
                    {
                        if (lines[j].Contains("new(\""))
                        {
                            var oldLine = lines[j];
                            var updatedLine = Regex.Replace(oldLine,
                                @"new\(""[^""]+""",
                                $"new(\"{newPrefabPath}\"");
                            if (updatedLine != oldLine)
                            {
                                lines[j] = updatedLine;
                                File.WriteAllText(fullPath, string.Join("\n", lines.ToArray()), new UTF8Encoding(false));
                                EmberDebug.Log("EmberUI", $"EUIPageDef {binding.PageName} 路径已更新: {newPrefabPath}");
                            }
                            return true;
                        }
                    }
                    // 声明行存在但没找到 new("，视为已有（手动维护的复杂格式，跳过）
                    return true;
                }
            }

            // 不存在：追加新行
            string sectionHeader = targetLayer switch
            {
                "Background" => "Background 层",
                "FreePage" => "FreePage 层",
                "TopMost" => "TopMost 层",
                "Popup" => "Popup 层",
                _ => "Normal 层"
            };

            string targetPageType = targetLayer switch
            {
                "Background" => "PageType.Background",
                "FreePage" => "PageType.FreePage",
                "TopMost" => "PageType.TopMost",
                "Popup" => "PageType.Popup",
                _ => "PageType.MainPage"
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
            string newLine = $"        public static readonly EUIPageDef {binding.PageName} = new(\"{newPrefabPath}\", UILayer.{targetLayer}, {targetPageType});";
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
            EmberDebug.LogWarning("EmberUI", sb.ToString());
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

            var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith("public static readonly EUIPageDef ")) continue;

                int nameStart = line.IndexOf("EUIPageDef ") + 8;
                int nameEnd = line.IndexOf(" =");
                if (nameStart < 8 || nameEnd < 0) continue;
                string name = line.Substring(nameStart, nameEnd - nameStart).Trim();

                int pathStart = line.IndexOf("new(\"") + 5;
                int pathEnd = line.IndexOf("\"", pathStart);
                if (pathStart < 5 || pathEnd < 0) continue;
                string prefabPath = line.Substring(pathStart, pathEnd - pathStart);

                if (string.IsNullOrEmpty(prefabPath)) continue;

                // 直接检查预制体文件是否存在
                if (!File.Exists(prefabPath))
                {
                    result.Add(new StalePageDef { Name = name, PrefabPath = prefabPath, LineIndex = i });
                }
            }
            return result;
        }

        /// <summary>清理指定的失效 EUIPageDef 条目</summary>
        public static int CleanStalePageDefs(string pageDefFilePath)
        {
            string fullPath = pageDefFilePath.StartsWith("Assets/")
                ? Path.Combine(Application.dataPath, pageDefFilePath.Substring("Assets/".Length))
                : pageDefFilePath;
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
                if (s.LineIndex > 0 && lines[s.LineIndex - 1].Trim().StartsWith("///"))
                    toRemove.Add(s.LineIndex - 1);
                if (s.LineIndex > 1 && lines[s.LineIndex - 2].Trim().StartsWith("///"))
                    toRemove.Add(s.LineIndex - 2);
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

        /// <summary>构建模板上下文变量（fields_decl 和 fields_bind 已预渲染为字符串）</summary>
        private Dictionary<string, object> BuildTemplateContext(EUIBinding binding, string prefabName,
            EUIBinding.BindingEntry[] entries, string baseClsName = null)
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
                ["namespace_name"] = namespaceName ?? "Game.UI",
                ["create_date"] = System.DateTime.Now.ToString(),
                ["fields_decl"] = decl.ToString(),
                ["fields_bind"] = bind.ToString(),
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
                return entry.ClassName;
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

            string fullPath = pageDefFile.StartsWith("Assets/")
                ? System.IO.Path.Combine(Application.dataPath, pageDefFile.Substring("Assets/".Length))
                : pageDefFile;

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
                if (path.EndsWith("GamePages.cs"))
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
        private SerializedProperty namespaceName;
        private SerializedProperty bindingCodeTemplate;
        private SerializedProperty codeTemplate;
        private SerializedProperty pageDefTemplate;
        private SerializedProperty codeTemplateForNoGen;

        protected override void OnEnable()
        {
            base.OnEnable();
            pageDefFile = serializedObject.FindProperty("pageDefFile");
            baseClassName = serializedObject.FindProperty("baseClassName");
            namespaceName = serializedObject.FindProperty("namespaceName");
            bindingCodeTemplate = serializedObject.FindProperty("bindingCodeTemplate");
            codeTemplate = serializedObject.FindProperty("codeTemplate");
            pageDefTemplate = serializedObject.FindProperty("pageDefTemplate");
            codeTemplateForNoGen = serializedObject.FindProperty("codeTemplateForNoGen");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(namespaceName, new GUIContent("代码生成命名空间"));
            if (string.IsNullOrEmpty(namespaceName.stringValue))
                EditorGUILayout.HelpBox("请输入正确的命名空间", MessageType.Error);

            EditorGUILayout.PropertyField(baseClassName, new GUIContent("页面逻辑基类"));
            if (string.IsNullOrEmpty(baseClassName.stringValue))
                EditorGUILayout.HelpBox("请输入正确的基类名（如 Ember.UI.EUIPage）", MessageType.Error);

            EditorGUILayout.PropertyField(pageDefFile, new GUIContent("EUIPageDef 文件路径"));
            if (string.IsNullOrEmpty(pageDefFile.stringValue))
                EditorGUILayout.HelpBox("请输入 EUIPageDef 源码文件路径（如 Assets/Game/UI/GamePages.cs）", MessageType.Error);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("代码生成模板", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(codeTemplate, new GUIContent("逻辑代码模板 (.cs 骨架)"));
            EditorGUILayout.PropertyField(bindingCodeTemplate, new GUIContent("绑定代码模板 (.Binding.cs)"));
            EditorGUILayout.PropertyField(pageDefTemplate, new GUIContent("EUIPageDef 模板"));
            EditorGUILayout.PropertyField(codeTemplateForNoGen, new GUIContent("剪贴板代码模板"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
