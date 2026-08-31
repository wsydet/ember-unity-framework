// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ember.Basic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// UI 预制体管理器 —— 总览所有含 EUIBinding 的 UI 预制体：
    /// 预制体位置 / 页面定义 / 生成脚本位置 / GamePages 定义内容 / 状态标记。
    ///
    /// <para>一键清理（安全项直接执行、dry-run 项先列清单逐项确认）：</para>
    /// <list type="bullet">
    ///   <item>清理失效 EUIPageDef（GamePages.cs + GamePages.User.cs）</item>
    ///   <item>移除预制体 Missing Script</item>
    ///   <item>清理空引用绑定条目（GameObject 为 null）</item>
    ///   <item>孤儿脚本排查（.cs/.Binding.cs 无对应预制体，dry-run 后删除）</item>
    ///   <item>空叶子节点（仅 Transform 的叶子，dry-run 后删除）</item>
    /// </list>
    /// </summary>
    public class EUIPrefabManagerWindow : EditorWindow
    {
        private const string TAG = "EmberUI";

        #region 内部参数

        private sealed class PrefabEntry
        {
            public string PrefabPath;
            public bool IsPage;
            public string PageName;
            public string PageDesc;
            public int BindingCount;
            public int FrameworkBindingCount;
            public bool GenerateCustomSettings;
            public string LogicScriptPath;
            public bool LogicScriptExists;
            public string BindingScriptPath;
            public bool BindingScriptExists;
            public string SettingsScriptPath;
            public bool SettingsScriptExists;
            public string PageDefFile;   // 定义所在文件（GamePages.cs / GamePages.User.cs），无则 null
            public string PageDefLine;   // 定义行预览（截断）
            public bool PageDefOk;       // 定义存在且路径匹配
            public int MissingScriptCount;
            public int NullBindingCount;
            public int EmptyLeafCount;
        }

        private Vector2 _listScroll;
        private Vector2 _cleanupScroll;
        private List<PrefabEntry> _entries = new List<PrefabEntry>();
        private List<string> _orphanScripts = new List<string>();
        private List<KeyValuePair<string, string>> _emptyLeaves = new List<KeyValuePair<string, string>>();
        private string _lastResult;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/UI/UI 预制体管理器", false, 20)]
        public static void Open()
        {
            var win = GetWindow<EUIPrefabManagerWindow>("UI 预制体管理器");
            win.minSize = new Vector2(820, 520);
            win.Show();
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnEnable()
        {
            Rescan();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 绘制

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新扫描", GUILayout.Width(100)))
            {
                Rescan();
                _lastResult = $"扫描完成：{_entries.Count} 个 UI 预制体。";
            }

            int pageCount = _entries.Count(e => e.IsPage);
            int problemCount = _entries.Count(e => !IsHealthy(e));
            EditorGUILayout.LabelField(
                $"共 {_entries.Count} 个（页面 {pageCount} 个 · 框架绑定 {_entries.Sum(e => e.FrameworkBindingCount)} 条 · 有问题 {problemCount} 个）",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            foreach (var e in _entries)
                DrawEntryRow(e);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            DrawCleanupSection();

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);
        }

        private static bool IsHealthy(PrefabEntry e)
        {
            return e.MissingScriptCount == 0
                && e.NullBindingCount == 0
                && e.LogicScriptExists
                && e.BindingScriptExists
                && (!e.GenerateCustomSettings || e.SettingsScriptExists)
                && (!e.IsPage || e.PageDefOk);
        }

        private void DrawEntryRow(PrefabEntry e)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                IsHealthy(e) ? "✅" : (e.MissingScriptCount > 0 || e.NullBindingCount > 0 ? "❌" : "⚠"),
                GUILayout.Width(20));
            EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(e.PrefabPath),
                EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField(e.PrefabPath, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("定位", GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
                if (obj) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
            }
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.35f, 0.3f);
            if (GUILayout.Button("删除 UI", GUILayout.Width(70)))
                DeleteUI(e);
            GUI.backgroundColor = prevColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                (e.IsPage
                    ? $"页面 {e.PageName} · {e.PageDesc}"
                    : "非页面（无 EUIPageDef）")
                + $"　|　绑定 {e.BindingCount} 条（🔒框架 {e.FrameworkBindingCount}）"
                + $"　|　脚本 {(e.LogicScriptExists ? "✅" : "❌")}{Path.GetFileName(e.LogicScriptPath)}"
                + $" / {(e.BindingScriptExists ? "✅" : "❌")}{Path.GetFileName(e.BindingScriptPath)}"
                + (e.GenerateCustomSettings
                    ? $" / {(e.SettingsScriptExists ? "✅" : "❌")}{Path.GetFileName(e.SettingsScriptPath)}"
                    : "")
                + (e.IsPage
                    ? $"　|　定义 {(e.PageDefOk ? "✅" : "❌")}{(string.IsNullOrEmpty(e.PageDefFile) ? "缺失" : Path.GetFileName(e.PageDefFile))}"
                    : "")
                + (e.MissingScriptCount > 0 ? $"　|　Missing Script × {e.MissingScriptCount}" : "")
                + (e.NullBindingCount > 0 ? $"　|　空引用绑定 × {e.NullBindingCount}" : "")
                + (e.EmptyLeafCount > 0 ? $"　|　空叶子 × {e.EmptyLeafCount}" : ""),
                EditorStyles.miniLabel);

            if (e.IsPage && !string.IsNullOrEmpty(e.PageDefLine))
                EditorGUILayout.LabelField("      " + e.PageDefLine, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawCleanupSection()
        {
            EditorGUILayout.LabelField("一键清理", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清理失效 EUIPageDef", GUILayout.Width(180)))
                CleanStalePageDefsAll();
            if (GUILayout.Button("移除 Missing Script", GUILayout.Width(180)))
                RemoveMissingScriptsAll();
            if (GUILayout.Button("清理空引用绑定条目", GUILayout.Width(180)))
                RemoveNullBindingsAll();
            if (GUILayout.Button("孤儿脚本排查（dry-run）", GUILayout.Width(200)))
                BuildOrphanScriptList();
            if (GUILayout.Button("空叶子节点（dry-run）", GUILayout.Width(180)))
                BuildEmptyLeafList();
            EditorGUILayout.EndHorizontal();

            _cleanupScroll = EditorGUILayout.BeginScrollView(_cleanupScroll, GUILayout.Height(120));

            if (_orphanScripts.Count > 0)
            {
                EditorGUILayout.LabelField($"孤儿脚本 {_orphanScripts.Count} 个（无对应预制体；确认后删除）：", EditorStyles.miniLabel);
                for (int i = 0; i < _orphanScripts.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_orphanScripts[i], EditorStyles.miniLabel);
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        AssetDatabase.DeleteAsset(_orphanScripts[i]);
                        _orphanScripts.RemoveAt(i);
                        AssetDatabase.Refresh();
                        Rescan();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("删除全部孤儿脚本", GUILayout.Width(160)))
                {
                    if (EditorUtility.DisplayDialog("删除全部孤儿脚本",
                            $"确认删除 {_orphanScripts.Count} 个孤儿脚本（文件 + .meta，不可恢复）？",
                            "删除全部", "取消"))
                    {
                        foreach (var p in _orphanScripts) AssetDatabase.DeleteAsset(p);
                        _orphanScripts.Clear();
                        AssetDatabase.Refresh();
                        Rescan();
                        _lastResult = "孤儿脚本已全部删除。";
                    }
                }
            }

            if (_emptyLeaves.Count > 0)
            {
                EditorGUILayout.LabelField($"空叶子节点 {_emptyLeaves.Count} 个（仅 Transform 的叶子；确认后删除）：", EditorStyles.miniLabel);
                for (int i = 0; i < _emptyLeaves.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{_emptyLeaves[i].Key} → {_emptyLeaves[i].Value}", EditorStyles.miniLabel);
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        DeleteLeafNode(_emptyLeaves[i].Key, _emptyLeaves[i].Value);
                        _emptyLeaves.RemoveAt(i);
                        AssetDatabase.Refresh();
                        Rescan();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 扫描

        private void Rescan()
        {
            _entries.Clear();
            var pageDefInfo = GetPageDefFiles(); // (userFile, frameworkFile)

            var guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;

                var binding = prefab.GetComponent<EUIBinding>();
                if (!binding) continue;

                _entries.Add(BuildEntry(path, prefab, binding, pageDefInfo));
            }

            _entries.Sort((a, b) => string.Compare(a.PrefabPath, b.PrefabPath, System.StringComparison.Ordinal));
            _orphanScripts.Clear();
            _emptyLeaves.Clear();
        }

        private static PrefabEntry BuildEntry(string path, GameObject prefab, EUIBinding binding,
            (string userFile, string frameworkFile) pageDefInfo)
        {
            var entry = new PrefabEntry { PrefabPath = path };

            entry.IsPage = binding.IsPage;
            entry.PageName = binding.IsPage ? (binding.PageName ?? "") : "";
            entry.PageDesc = GetPageDesc(binding);
            entry.BindingCount = binding.Bindings?.Length ?? 0;
            entry.FrameworkBindingCount = binding.Bindings?.Count(b => b.IsFramework) ?? 0;

            var root = !string.IsNullOrEmpty(binding.CodePath)
                ? binding.CodePath
                : EUIBindingSettingData.GetOrCreateSettings().BusinessCodeRoot;
            var sub = string.IsNullOrEmpty(binding.ClassPath) ? "" : binding.ClassPath + "/";
            entry.LogicScriptPath = $"{root}/{sub}{binding.ClassName}.cs";
            entry.BindingScriptPath = $"{root}/{sub}{binding.ClassName}.Binding.cs";
            entry.LogicScriptExists = File.Exists(ToFullPath(entry.LogicScriptPath));
            entry.BindingScriptExists = File.Exists(ToFullPath(entry.BindingScriptPath));

            entry.GenerateCustomSettings = binding.GenerateCustomSettings;
            entry.SettingsScriptPath = $"{root}/{sub}{binding.ClassName}Settings.cs";
            entry.SettingsScriptExists = File.Exists(ToFullPath(entry.SettingsScriptPath));

            if (entry.IsPage && !string.IsNullOrEmpty(entry.PageName))
                FillPageDefInfo(entry, path, pageDefInfo);

            entry.MissingScriptCount = CountMissingScripts(prefab);
            entry.NullBindingCount = binding.Bindings?.Count(b => b.GameObject == null) ?? 0;
            entry.EmptyLeafCount = CountEmptyLeaves(prefab);
            return entry;
        }

        private static void FillPageDefInfo(PrefabEntry entry, string prefabPath,
            (string userFile, string frameworkFile) pageDefInfo)
        {
            foreach (var file in new[] { pageDefInfo.userFile, pageDefInfo.frameworkFile })
            {
                if (string.IsNullOrEmpty(file)) continue;
                var full = ToFullPath(file);
                if (!File.Exists(full)) continue;

                string line = null;
                foreach (var l in File.ReadAllLines(full, Encoding.UTF8))
                {
                    if (l.Contains($"EUIPageDef {entry.PageName} ="))
                    {
                        line = l;
                        break;
                    }
                }

                if (line == null) continue;

                entry.PageDefFile = file;
                entry.PageDefLine = line.Trim();
                if (entry.PageDefLine.Length > 110)
                    entry.PageDefLine = entry.PageDefLine.Substring(0, 110) + "…";

                var m = System.Text.RegularExpressions.Regex.Match(line, @"new\(""([^""]+)""");
                entry.PageDefOk = m.Success && m.Groups[1].Value == prefabPath;
                return;
            }

            entry.PageDefFile = null;
            entry.PageDefOk = false;
        }

        private static string GetPageDesc(EUIBinding binding)
        {
            var f = binding.PageFlags;
            string layer = (f & PageFlags.Background) != 0 ? "Background"
                : (f & PageFlags.FreePage) != 0 ? "FreePage"
                : (f & PageFlags.TopMost) != 0 ? "TopMost"
                : (f & PageFlags.Popup) != 0 ? "Popup"
                : "Normal";
            string type = (f & PageFlags.Background) != 0 ? "Background"
                : (f & PageFlags.FreePage) != 0 ? "FreePage"
                : (f & PageFlags.TopMost) != 0 ? "TopMost"
                : (f & PageFlags.Popup) != 0 ? "Popup"
                : (f & PageFlags.MainPage) != 0 ? "MainPage"
                : "SubPage";
            return $"{layer} · {type}";
        }

        private static int CountMissingScripts(GameObject prefab)
        {
            int count = 0;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            return count;
        }

        private static int CountEmptyLeaves(GameObject prefab)
        {
            int count = 0;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t == prefab.transform || t.childCount > 0) continue;
                var comps = t.GetComponents<Component>();
                if (comps.Length == 1 && comps[0] is Transform) count++;
            }
            return count;
        }

        /// <summary>取 EUIPageDef 两个目标文件（用户文件 + 框架文件，均为 Assets-relative 路径）。</summary>
        private static (string userFile, string frameworkFile) GetPageDefFiles()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            CSharpLogicImplementationData csharp = null;
            if (settings.LogicImplementations != null)
            {
                foreach (var impl in settings.LogicImplementations)
                {
                    if (impl is CSharpLogicImplementationData c) { csharp = c; break; }
                }
            }

            if (csharp == null || string.IsNullOrEmpty(csharp.PageDefFile))
                return (null, null);

            string userFile = csharp.PageDefFile;
            string frameworkFile = userFile.EndsWith("GamePages.User.cs")
                ? userFile.Substring(0, userFile.Length - "GamePages.User.cs".Length) + "GamePages.cs"
                : null;
            return (userFile, frameworkFile);
        }

        private static string ToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return assetPath.StartsWith("Assets/")
                ? Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length))
                : assetPath;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 删除 UI（预制体 + 脚本 + 定义整体删除）

        /// <summary>一键删除一个 UI 的全部关联资产：预制体、.cs / .Binding.cs / Settings.cs、EUIPageDef 条目。不可恢复。</summary>
        private void DeleteUI(PrefabEntry e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"预制体：{e.PrefabPath}");
            if (e.LogicScriptExists) sb.AppendLine($"逻辑脚本：{e.LogicScriptPath}");
            if (e.BindingScriptExists) sb.AppendLine($"绑定脚本：{e.BindingScriptPath}");
            if (e.SettingsScriptExists) sb.AppendLine($"自定义参数脚本：{e.SettingsScriptPath}");
            if (e.IsPage)
            {
                sb.AppendLine(string.IsNullOrEmpty(e.PageDefFile)
                    ? "EUIPageDef：未找到条目（无需处理）"
                    : $"EUIPageDef：{Path.GetFileName(e.PageDefFile)} 中 {e.PageName}（随失效清理移除）");
            }

            string warn = (e.IsPage && e.PageDefFile != null && Path.GetFileName(e.PageDefFile) == "GamePages.cs")
                ? "\n⚠ 这是框架页面（定义位于 GamePages.cs），删除后框架注册表将缺失该页面！\n"
                : "";

            if (!EditorUtility.DisplayDialog("删除 UI",
                    $"将整体删除该 UI 及其全部关联资产（不可恢复）：\n\n{sb}{warn}\n继续？",
                    "删除", "取消"))
                return;

            if (e.LogicScriptExists) AssetDatabase.DeleteAsset(e.LogicScriptPath);
            if (e.BindingScriptExists) AssetDatabase.DeleteAsset(e.BindingScriptPath);
            if (e.SettingsScriptExists) AssetDatabase.DeleteAsset(e.SettingsScriptPath);
            AssetDatabase.DeleteAsset(e.PrefabPath);

            // 预制体删除后，其 EUIPageDef 条目自动失效 → 复用失效清理（含其注释行）
            int removedDefs = 0;
            if (e.IsPage)
            {
                var files = GetPageDefFiles();
                foreach (var file in new[] { files.userFile, files.frameworkFile })
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    removedDefs += CSharpLogicImplementationData.CleanStalePageDefs(file);
                }
            }

            AssetDatabase.Refresh();
            Rescan();
            _lastResult = $"已删除 UI「{Path.GetFileNameWithoutExtension(e.PrefabPath)}」：预制体 + 脚本 + EUIPageDef 条目 {removedDefs} 条。";
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 清理（安全项）

        private void CleanStalePageDefsAll()
        {
            var files = GetPageDefFiles();
            int total = 0;
            var sb = new StringBuilder();
            foreach (var file in new[] { files.userFile, files.frameworkFile })
            {
                if (string.IsNullOrEmpty(file)) continue;
                var full = ToFullPath(file);
                if (!File.Exists(full)) continue;
                var stale = CSharpLogicImplementationData.FindStalePageDefsPublic(full);
                if (stale.Count == 0) continue;
                sb.AppendLine($"{file}: {stale.Count} 条失效（{string.Join("、", stale.Select(s => s.Name))}）");
                total += stale.Count;
            }

            if (total == 0)
            {
                _lastResult = "未发现失效 EUIPageDef。";
                return;
            }

            if (!EditorUtility.DisplayDialog("清理失效 EUIPageDef",
                    $"将清理 {total} 条失效 EUIPageDef：\n{sb}", "清理", "取消"))
                return;

            int removed = 0;
            foreach (var file in new[] { files.userFile, files.frameworkFile })
            {
                if (string.IsNullOrEmpty(file)) continue;
                removed += CSharpLogicImplementationData.CleanStalePageDefs(file);
            }

            AssetDatabase.Refresh();
            Rescan();
            _lastResult = $"已清理 {removed} 条失效 EUIPageDef。";
        }

        private void RemoveMissingScriptsAll()
        {
            var targets = _entries.Where(e => e.MissingScriptCount > 0).ToList();
            int total = targets.Sum(e => e.MissingScriptCount);
            if (total == 0)
            {
                _lastResult = "未发现 Missing Script。";
                return;
            }

            if (!EditorUtility.DisplayDialog("移除 Missing Script",
                    $"将从 {targets.Count} 个预制体中移除 {total} 个 Missing Script。", "移除", "取消"))
                return;

            int removed = 0;
            foreach (var e in targets)
                removed += RemoveMissingScriptsInPrefab(e.PrefabPath);

            AssetDatabase.Refresh();
            Rescan();
            _lastResult = $"已移除 {removed} 个 Missing Script。";
        }

        private void RemoveNullBindingsAll()
        {
            var targets = _entries.Where(e => e.NullBindingCount > 0).ToList();
            int total = targets.Sum(e => e.NullBindingCount);
            if (total == 0)
            {
                _lastResult = "未发现空引用绑定条目。";
                return;
            }

            if (!EditorUtility.DisplayDialog("清理空引用绑定条目",
                    $"将从 {targets.Count} 个预制体中删除 {total} 条 GameObject 为 null 的绑定条目（框架条目同样处理，属损坏数据）。",
                    "清理", "取消"))
                return;

            int removed = 0;
            foreach (var e in targets)
                removed += RemoveNullBindingsInPrefab(e.PrefabPath);

            AssetDatabase.Refresh();
            Rescan();
            _lastResult = $"已删除 {removed} 条空引用绑定。";
        }

        private static int RemoveMissingScriptsInPrefab(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            int removed = 0;
            try
            {
                foreach (var t in contents.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            return removed;
        }

        private static int RemoveNullBindingsInPrefab(string prefabPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            int removed = 0;
            try
            {
                var binding = contents.GetComponent<EUIBinding>();
                if (!binding) return 0;

                var so = new SerializedObject(binding);
                var sp = so.FindProperty("bindings");
                for (int i = sp.arraySize - 1; i >= 0; i--)
                {
                    var go = sp.GetArrayElementAtIndex(i).FindPropertyRelative("GameObject");
                    if (go.objectReferenceValue == null)
                    {
                        sp.DeleteArrayElementAtIndex(i);
                        removed++;
                    }
                }
                if (removed > 0)
                {
                    so.ApplyModifiedProperties();
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                so.Dispose();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            return removed;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 清理（dry-run 项）

        private void BuildOrphanScriptList()
        {
            _orphanScripts.Clear();

            var root = EUIBindingSettingData.GetOrCreateSettings().BusinessCodeRoot;
            if (string.IsNullOrEmpty(root)) { _lastResult = "未配置业务代码根目录。"; return; }
            var rootFull = ToFullPath(root);
            if (string.IsNullOrEmpty(rootFull) || !Directory.Exists(rootFull)) { _lastResult = $"根目录不存在：{root}"; return; }

            var referenced = new HashSet<string>();
            foreach (var e in _entries)
            {
                referenced.Add(e.LogicScriptPath);
                referenced.Add(e.BindingScriptPath);
                if (!string.IsNullOrEmpty(e.LogicScriptPath))
                    referenced.Add(e.LogicScriptPath.Replace(".cs", "Settings.cs"));
            }

            foreach (var file in Directory.GetFiles(rootFull, "*.cs", SearchOption.AllDirectories))
            {
                var rel = root.TrimEnd('/') + "/" + file.Substring(rootFull.Length + 1).Replace('\\', '/');
                if (referenced.Contains(rel)) continue;
                _orphanScripts.Add(rel);
            }

            _orphanScripts.Sort(System.StringComparer.Ordinal);
            _lastResult = _orphanScripts.Count > 0
                ? $"发现 {_orphanScripts.Count} 个孤儿脚本（见下方清单，逐项确认删除）。"
                : "未发现孤儿脚本。";
        }

        private void BuildEmptyLeafList()
        {
            _emptyLeaves.Clear();
            foreach (var e in _entries)
            {
                if (e.EmptyLeafCount <= 0) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
                if (!prefab) continue;

                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (t == prefab.transform || t.childCount > 0) continue;
                    var comps = t.GetComponents<Component>();
                    if (comps.Length == 1 && comps[0] is Transform)
                        _emptyLeaves.Add(new KeyValuePair<string, string>(e.PrefabPath, GetNodePath(prefab.transform, t)));
                }
            }

            _lastResult = _emptyLeaves.Count > 0
                ? $"发现 {_emptyLeaves.Count} 个空叶子节点（见下方清单，逐项确认删除）。"
                : "未发现空叶子节点。";
        }

        private static string GetNodePath(Transform root, Transform target)
        {
            if (root == target) return "";
            var names = new List<string>();
            var cur = target;
            while (cur && cur != root)
            {
                names.Add(cur.name);
                cur = cur.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }

        private static void DeleteLeafNode(string prefabPath, string nodePath)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var t = contents.transform.Find(nodePath);
                if (t && t.childCount == 0)
                {
                    Object.DestroyImmediate(t.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        #endregion
    }
}
