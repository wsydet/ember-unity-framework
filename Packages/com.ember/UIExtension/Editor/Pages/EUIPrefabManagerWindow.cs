// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ember.Basic;
using Ember.UI;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// UI 开发中心：标准页面创建、UI 资产总览，以及带影响预览的清理与删除。
    /// </summary>
    public class EUIPrefabManagerWindow : EditorWindow
    {
        private const string TAG = LogTags.EmberUI;

        private enum DevelopmentTab
        {
            Create,
            Overview,
            Maintenance,
        }

        private static readonly string[] TabLabels = { "创建 UI", "UI 总览", "清理与删除" };

        private static readonly PageType[] SupportedPageTypes =
        {
            PageType.Background,
            PageType.MainPage,
            PageType.Popup,
            PageType.FullScreenPopup,
            PageType.TopMost,
            PageType.SubPage,
            PageType.FreePage,
        };

        private static readonly string[] SupportedPageTypeLabels =
        {
            "背景页 (Background)",
            "主页面 (MainPage)",
            "弹窗 (Popup)",
            "全屏弹窗 (FullScreenPopup)",
            "置顶页 (TopMost)",
            "子页面 (SubPage)",
            "独立页 (FreePage)",
        };

        [SerializeField] private DevelopmentTab _tab;
        [SerializeField] private EUICreationRequest _creationRequest = new EUICreationRequest();
        [SerializeField] private bool _showAdvancedCreation;
        [SerializeField] private string _overviewFilter = string.Empty;

        private Vector2 _createScroll;
        private Vector2 _overviewScroll;
        private Vector2 _maintenanceScroll;
        private EUICreationPlan _creationPlan;
        private EUICreationResult _creationResult;
        private EUIPrefabCatalogSnapshot _catalog;
        private readonly List<EUIOrphanScriptGroup> _orphanGroups = new List<EUIOrphanScriptGroup>();
        private readonly List<KeyValuePair<string, string>> _emptyLeaves =
            new List<KeyValuePair<string, string>>();
        private string _lastResult;

        [MenuItem("Ember/UI/UI 开发中心", false, 20)]
        public static void Open()
        {
            var window = GetWindow<EUIPrefabManagerWindow>("UI 开发中心");
            window.minSize = new Vector2(900f, 580f);
            window.Show();
        }

        private void OnEnable()
        {
            _creationRequest ??= new EUICreationRequest();
            Rescan();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6f);

            switch (_tab)
            {
                case DevelopmentTab.Create:
                    DrawCreationTab();
                    break;
                case DevelopmentTab.Overview:
                    DrawOverviewTab();
                    break;
                case DevelopmentTab.Maintenance:
                    DrawMaintenanceTab();
                    break;
            }

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI 开发中心", EditorStyles.boldLabel, GUILayout.Width(120f));
            _tab = (DevelopmentTab)GUILayout.Toolbar((int)_tab, TabLabels, GUILayout.Height(25f));
            EditorGUILayout.EndHorizontal();

            if (EUICreationCompilationContinuation.IsPending)
            {
                EditorGUILayout.HelpBox(
                    $"正在等待 Unity 编译/资源更新完成：{EUICreationCompilationContinuation.PendingPrefabPath}",
                    MessageType.Info);
            }
            else if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorGUILayout.HelpBox("Unity 正在编译或更新资源，创建和维护操作暂时禁用。", MessageType.Warning);
            }
        }

        #region 创建 UI

        private void DrawCreationTab()
        {
            _createScroll = EditorGUILayout.BeginScrollView(_createScroll);
            EditorGUILayout.LabelField("标准页面", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "创建根 Canvas → Animator 视觉层 → nested EUISafeArea，并生成逻辑脚本、Binding 和 GamePages 条目。"
                + " Animator 默认复用 EUICommon_Ani，资产中保持禁用，由运行时按过渡模式启用。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            DrawCreationBasicFields();
            DrawCreationBehaviourFields();
            DrawCreationAdvancedFields();
            if (EditorGUI.EndChangeCheck())
            {
                _creationPlan = null;
                _creationResult = null;
            }

            EditorGUILayout.Space(8f);
            DrawCreationPreflight();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCreationBasicFields()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("输出与命名", EditorStyles.boldLabel);

            if (EUIBindingCodeGenUtility.IsEmbeddedPackage())
            {
                _creationRequest.CodePathMode = (EUIBinding.CodePathMode)EditorGUILayout.EnumPopup(
                    "生成模式", _creationRequest.CodePathMode);
            }
            else
            {
                _creationRequest.CodePathMode = EUIBinding.CodePathMode.Business;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.EnumPopup("生成模式", EUIBinding.CodePathMode.Business);
                EditorGUILayout.LabelField("消费端只允许 Business 模式。", EditorStyles.miniLabel);
            }

            _creationRequest.ClassPath = EditorGUILayout.TextField(
                new GUIContent("输出子目录", "Business 模式首段也是资源模块名，如 Inventory/Page。"),
                _creationRequest.ClassPath);
            _creationRequest.ClassName = EditorGUILayout.TextField("类名", _creationRequest.ClassName);
            _creationRequest.PageName = EditorGUILayout.TextField("PageDef 名", _creationRequest.PageName);
            _creationRequest.PrefabName = EditorGUILayout.TextField("Prefab 名", _creationRequest.PrefabName);

            var currentIndex = Array.IndexOf(SupportedPageTypes, _creationRequest.PageType);
            if (currentIndex < 0) currentIndex = 1;
            currentIndex = EditorGUILayout.Popup("页面类型", currentIndex, SupportedPageTypeLabels);
            _creationRequest.PageType = SupportedPageTypes[currentIndex];
            EditorGUILayout.EndVertical();
        }

        private void DrawCreationBehaviourFields()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("页面行为", EditorStyles.boldLabel);
            _creationRequest.UseUIUpdate = EditorGUILayout.Toggle("使用 UIUpdate",
                _creationRequest.UseUIUpdate);
            _creationRequest.TransitionMode =
                (EUIBinding.RegularTransitionMode)EditorGUILayout.EnumPopup(
                    "普通过渡", _creationRequest.TransitionMode);
            if (_creationRequest.TransitionMode == EUIBinding.RegularTransitionMode.PresetFade)
            {
                _creationRequest.FadeInTime = EditorGUILayout.FloatField("进入时长", _creationRequest.FadeInTime);
                _creationRequest.FadeOutTime = EditorGUILayout.FloatField("退出时长", _creationRequest.FadeOutTime);
            }

            if (IsPopup(_creationRequest.PageType))
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("弹窗遮罩", EditorStyles.miniBoldLabel);
                _creationRequest.UseMask = EditorGUILayout.Toggle("创建遮罩", _creationRequest.UseMask);
                using (new EditorGUI.DisabledScope(!_creationRequest.UseMask))
                {
                    _creationRequest.MaskColor = EditorGUILayout.ColorField("遮罩颜色",
                        _creationRequest.MaskColor);
                    _creationRequest.ClickMaskToClose = EditorGUILayout.Toggle("点击遮罩关闭",
                        _creationRequest.ClickMaskToClose);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCreationAdvancedFields()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showAdvancedCreation = EditorGUILayout.Foldout(_showAdvancedCreation,
                "高级代码选项", true);
            if (_showAdvancedCreation)
            {
                _creationRequest.GenerateCustomSettings = EditorGUILayout.Toggle(
                    "生成自定义 Settings", _creationRequest.GenerateCustomSettings);
                if (IsPopup(_creationRequest.PageType))
                {
                    _creationRequest.GenerateAutoCreateClickableMaskOverride = EditorGUILayout.Toggle(
                        "生成遮罩创建覆写", _creationRequest.GenerateAutoCreateClickableMaskOverride);
                    _creationRequest.GenerateOnClickMaskOverride = EditorGUILayout.Toggle(
                        "生成遮罩点击钩子", _creationRequest.GenerateOnClickMaskOverride);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCreationPreflight()
        {
            if (_creationPlan == null)
                RefreshCreationPlan();

            EditorGUILayout.BeginHorizontal();
            var refreshRequested = GUILayout.Button("预检", GUILayout.Width(100f));
            EditorGUILayout.LabelField(_creationPlan != null && _creationPlan.IsValid
                ? "✅ 所有依赖与目标路径可用"
                : "⚠ 预检未通过", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (refreshRequested)
            {
                RefreshCreationPlan();
                Repaint();
                GUIUtility.ExitGUI();
            }

            if (_creationPlan != null && _creationPlan.IsValid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawPath("Prefab", _creationPlan.PrefabPath);
                DrawPath("逻辑脚本", _creationPlan.LogicScriptPath);
                DrawPath("Binding", _creationPlan.BindingScriptPath);
                if (!string.IsNullOrEmpty(_creationPlan.SettingsScriptPath))
                    DrawPath("Settings", _creationPlan.SettingsScriptPath);
                DrawPath("PageDef", _creationPlan.PageDefFile);
                DrawPath("Animator", _creationPlan.AnimatorControllerPath);
                DrawPath("SafeArea", _creationPlan.SafeAreaPrefabPath);
                EditorGUILayout.EndVertical();
            }
            else if (_creationPlan != null)
            {
                EditorGUILayout.HelpBox(_creationPlan.Error ?? "预检失败。", MessageType.Error);
            }

            if (_creationResult != null && !_creationResult.Success)
            {
                EditorGUILayout.HelpBox(
                    $"创建未完成：{_creationResult.Error}\n\n"
                    + _creationResult.BuildAffectedAssetsSummary(), MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(_creationPlan == null || !_creationPlan.IsValid
                       || EditorApplication.isCompiling || EditorApplication.isUpdating
                       || EUICreationCompilationContinuation.IsPending))
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.75f, 0.35f);
                if (GUILayout.Button("创建并在编译后打开 Prefab", GUILayout.Height(34f)))
                    CreateUI();
                GUI.backgroundColor = oldColor;
            }
        }

        private void RefreshCreationPlan()
        {
            EUICreationService.TryBuildPlan(_creationRequest, out _creationPlan, out _creationResult);
        }

        private void CreateUI()
        {
            if (_creationPlan == null || !_creationPlan.IsValid) return;
            var summary = new StringBuilder()
                .AppendLine($"Prefab：{_creationPlan.PrefabPath}")
                .AppendLine($"逻辑：{_creationPlan.LogicScriptPath}")
                .AppendLine($"Binding：{_creationPlan.BindingScriptPath}")
                .AppendLine($"PageDef：{_creationPlan.PageDefFile}")
                .ToString();
            if (!EditorUtility.DisplayDialog("创建标准 UI", summary + "\n继续？", "创建", "取消"))
                return;

            _creationResult = EUICreationService.Create(_creationRequest);
            if (_creationResult.Success)
            {
                EUICreationCompilationContinuation.Schedule(_creationResult.PrefabPath);
                _lastResult = $"UI 已生成，正在等待 Unity 编译：{_creationResult.PrefabPath}";
            }
            else
            {
                var failureSummary = $"UI 创建未完成：{_creationResult.Error}\n\n"
                    + _creationResult.BuildAffectedAssetsSummary();
                _lastResult = failureSummary;
                EmberDebug.LogError(TAG, failureSummary);
                // 必须在 Refresh 可能触发域重载之前把部分产物明确告知用户。
                EditorUtility.DisplayDialog("UI 创建未完成", failureSummary, "确定");
            }

            if (_creationResult.RequiresRefresh)
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Refresh 可能触发编译/域重载；此处不再进行 Unity 资产操作。
            GUIUtility.ExitGUI();
        }

        #endregion

        #region UI 总览

        private void DrawOverviewTab()
        {
            DrawCatalogToolbar();
            if (!EnsureCatalog()) return;

            _overviewScroll = EditorGUILayout.BeginScrollView(_overviewScroll);
            foreach (var entry in FilteredEntries()) DrawCatalogEntry(entry);
            EditorGUILayout.EndScrollView();
        }

        private void DrawCatalogToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新扫描", GUILayout.Width(100f)))
            {
                Rescan();
                _lastResult = _catalog?.IsConfigured == true
                    ? $"扫描完成：{_catalog.Entries.Count} 个 UI 预制体。"
                    : _catalog?.Error;
            }
            _overviewFilter = EditorGUILayout.TextField("筛选", _overviewFilter);
            if (_catalog?.IsConfigured == true)
            {
                var pageCount = _catalog.Entries.Count(entry => entry.IsPage);
                var issueCount = _catalog.Entries.Count(entry => !entry.IsHealthy);
                EditorGUILayout.LabelField(
                    $"共 {_catalog.Entries.Count} · 页面 {pageCount} · 有问题 {issueCount}",
                    EditorStyles.miniLabel, GUILayout.Width(220f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCatalogEntry(EUIPrefabCatalogEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.IsHealthy ? "✅" : "⚠", GUILayout.Width(22f));
            EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(entry.PrefabPath),
                EditorStyles.boldLabel, GUILayout.Width(190f));
            EditorGUILayout.LabelField(entry.PrefabPath, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("定位", GUILayout.Width(52f))) PingAsset(entry.PrefabPath);
            if (GUILayout.Button("打开", GUILayout.Width(52f))) OpenAsset(entry.PrefabPath);
            EditorGUILayout.EndHorizontal();

            var scriptState = entry.NoCodeGeneration
                ? "不生成代码"
                : $"脚本 {(entry.LogicScriptExists ? "✅" : "❌")}{Path.GetFileName(entry.LogicScriptPath)}"
                  + $" / {(entry.BindingScriptExists ? "✅" : "❌")}{Path.GetFileName(entry.BindingScriptPath)}";
            var description = (entry.IsPage
                    ? $"页面 {entry.PageName} · {entry.PageDesc}"
                    : "非页面")
                + $"　|　绑定 {entry.BindingCount}（框架 {entry.FrameworkBindingCount}）"
                + $"　|　{scriptState}"
                + (entry.IsPage && !entry.NoCodeGeneration
                    ? $"　|　定义 {(entry.PageDefOk ? "✅" : "❌")}{Path.GetFileName(entry.PageDefFile)}"
                    : string.Empty)
                + (entry.MissingScriptCount > 0 ? $"　|　Missing × {entry.MissingScriptCount}" : string.Empty)
                + (entry.NullBindingCount > 0 ? $"　|　空绑定 × {entry.NullBindingCount}" : string.Empty)
                + (entry.EmptyLeafCount > 0 ? $"　|　空叶子候选 × {entry.EmptyLeafCount}" : string.Empty);
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(entry.PageDefLine))
                EditorGUILayout.LabelField("      " + entry.PageDefLine, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 清理与删除

        private void DrawMaintenanceTab()
        {
            if (!EnsureCatalog()) return;
            EditorGUILayout.HelpBox(
                "本页包含会删除或重写 Assets 的操作。所有操作先展示影响范围并二次确认；"
                + "模板镜像与 Packages 内容不在作用范围。", MessageType.Warning);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling
                       || EditorApplication.isUpdating
                       || EUICreationCompilationContinuation.IsPending))
            {
                DrawMaintenanceButtons();
                _maintenanceScroll = EditorGUILayout.BeginScrollView(_maintenanceScroll);
                DrawDeleteUISection();
                DrawOrphanSection();
                DrawEmptyLeafSection();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawMaintenanceButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清理失效 PageDef", GUILayout.Width(155f))) CleanStalePageDefsAll();
            if (GUILayout.Button("移除 Missing Script", GUILayout.Width(170f))) RemoveMissingScriptsAll();
            if (GUILayout.Button("清理空引用绑定", GUILayout.Width(160f))) RemoveNullBindingsAll();
            if (GUILayout.Button("孤儿生成脚本 dry-run", GUILayout.Width(180f))) BuildOrphanList();
            if (GUILayout.Button("空叶子 dry-run", GUILayout.Width(145f))) BuildEmptyLeafList();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDeleteUISection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("删除单个 UI", EditorStyles.boldLabel);
            foreach (var entry in FilteredEntries())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(entry.PrefabPath),
                    GUILayout.Width(210f));
                EditorGUILayout.LabelField(entry.PrefabPath, EditorStyles.miniLabel);
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.85f, 0.35f, 0.3f);
                if (GUILayout.Button("预览并删除", GUILayout.Width(105f))) DeleteUI(entry);
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawOrphanSection()
        {
            if (_orphanGroups.Count == 0) return;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"孤儿生成脚本组 {_orphanGroups.Count} 个", EditorStyles.boldLabel);
            foreach (var group in _orphanGroups.ToArray())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.Join(" · ", group.AssetPaths), EditorStyles.miniLabel);
                if (GUILayout.Button("删除", GUILayout.Width(55f))) DeleteOrphanGroup(group);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawEmptyLeafSection()
        {
            if (_emptyLeaves.Count == 0) return;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"普通空叶子候选 {_emptyLeaves.Count} 个", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("nested prefab 与 EUISafeArea 九个定位节点已排除。", EditorStyles.miniLabel);
            foreach (var candidate in _emptyLeaves.ToArray())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{candidate.Key} → {candidate.Value}", EditorStyles.miniLabel);
                if (GUILayout.Button("删除", GUILayout.Width(55f))) DeleteEmptyLeaf(candidate);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DeleteUI(EUIPrefabCatalogEntry entry)
        {
            var plan = EUIPrefabMaintenanceService.BuildDeletePlan(_catalog, entry);
            if (!plan.CanExecute)
            {
                EditorUtility.DisplayDialog("无法删除 UI", plan.BuildSummary(), "确定");
                return;
            }
            if (!EditorUtility.DisplayDialog("删除 UI",
                    "将删除以下精确目标（不可恢复）：\n\n" + plan.BuildSummary(), "删除", "取消"))
                return;

            var result = EUIPrefabMaintenanceService.ExecuteDelete(plan, _catalog);
            _lastResult = result.Message;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GUIUtility.ExitGUI();
        }

        private void CleanStalePageDefsAll()
        {
            var files = PageDefFiles().Where(path => !string.IsNullOrEmpty(path)).ToArray();
            var details = new List<string>();
            var total = 0;
            foreach (var file in files)
            {
                var fullPath = EUIPrefabCatalogService.ToFullPath(file);
                if (!File.Exists(fullPath)) continue;
                var stale = CSharpLogicImplementationData.FindStalePageDefsPublic(fullPath);
                if (stale.Count == 0) continue;
                total += stale.Count;
                details.Add($"{file}：{string.Join("、", stale.Select(item => item.Name))}");
            }
            if (total == 0)
            {
                _lastResult = "未发现失效 EUIPageDef。";
                return;
            }
            if (!EditorUtility.DisplayDialog("清理失效 EUIPageDef",
                    $"将清理 {total} 条失效定义：\n\n{string.Join("\n", details)}", "清理", "取消"))
                return;
            var removed = files.Sum(CSharpLogicImplementationData.CleanStalePageDefs);
            _lastResult = $"已清理 {removed} 条失效 EUIPageDef。";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GUIUtility.ExitGUI();
        }

        private void RemoveMissingScriptsAll()
        {
            var targets = _catalog.Entries.Where(entry => entry.MissingScriptCount > 0).ToList();
            var total = targets.Sum(entry => entry.MissingScriptCount);
            if (total == 0) { _lastResult = "未发现 Missing Script。"; return; }
            if (!EditorUtility.DisplayDialog("移除 Missing Script",
                    $"将从 {targets.Count} 个预制体移除 {total} 个 Missing Script。", "移除", "取消"))
                return;
            var removed = targets.Sum(entry =>
                EUIPrefabMaintenanceService.RemoveMissingScriptsInPrefab(entry.PrefabPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Rescan();
            _lastResult = $"已移除 {removed} 个 Missing Script。";
        }

        private void RemoveNullBindingsAll()
        {
            var targets = _catalog.Entries.Where(entry => entry.NullBindingCount > 0).ToList();
            var total = targets.Sum(entry => entry.NullBindingCount);
            if (total == 0) { _lastResult = "未发现空引用绑定。"; return; }
            if (!EditorUtility.DisplayDialog("清理空引用绑定",
                    $"将从 {targets.Count} 个预制体删除 {total} 条空引用绑定。", "清理", "取消"))
                return;
            var removed = targets.Sum(entry =>
                EUIPrefabMaintenanceService.RemoveNullBindingsInPrefab(entry.PrefabPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Rescan();
            _lastResult = $"已删除 {removed} 条空引用绑定。";
        }

        private void BuildOrphanList()
        {
            _orphanGroups.Clear();
            _orphanGroups.AddRange(EUIPrefabMaintenanceService.FindOrphanScriptGroups(_catalog));
            _lastResult = _orphanGroups.Count == 0
                ? "未发现有自动生成 Binding 锚点的孤儿脚本。"
                : $"发现 {_orphanGroups.Count} 个孤儿生成脚本组，请逐项确认。";
        }

        private void DeleteOrphanGroup(EUIOrphanScriptGroup group)
        {
            if (!EditorUtility.DisplayDialog("删除孤儿生成脚本",
                    string.Join("\n", group.AssetPaths), "删除", "取消")) return;
            var result = EUIPrefabMaintenanceService.DeleteOrphanScriptGroup(
                group, _catalog.BusinessCodeRoot);
            _lastResult = result.Message;
            _orphanGroups.Remove(group);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GUIUtility.ExitGUI();
        }

        private void BuildEmptyLeafList()
        {
            _emptyLeaves.Clear();
            foreach (var entry in _catalog.Entries)
            foreach (var nodePath in EUIPrefabCatalogService.FindEmptyLeafPaths(entry.PrefabPath))
                _emptyLeaves.Add(new KeyValuePair<string, string>(entry.PrefabPath, nodePath));
            _lastResult = _emptyLeaves.Count == 0
                ? "未发现普通空叶子节点。"
                : $"发现 {_emptyLeaves.Count} 个候选；nested prefab 与 SafeArea 节点已排除。";
        }

        private void DeleteEmptyLeaf(KeyValuePair<string, string> candidate)
        {
            if (!EditorUtility.DisplayDialog("删除空叶子",
                    $"{candidate.Key}\n{candidate.Value}", "删除", "取消")) return;
            if (EUIPrefabMaintenanceService.DeleteEmptyLeaf(candidate.Key, candidate.Value,
                    out var error))
            {
                _emptyLeaves.Remove(candidate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Rescan();
                _lastResult = $"已删除空叶子：{candidate.Value}";
            }
            else
            {
                _lastResult = error;
            }
        }

        #endregion

        #region 通用

        private void Rescan()
        {
            _catalog = EUIPrefabCatalogService.Scan();
            _orphanGroups.Clear();
            _emptyLeaves.Clear();
        }

        private bool EnsureCatalog()
        {
            if (_catalog == null) Rescan();
            if (_catalog?.IsConfigured == true) return true;
            EditorGUILayout.HelpBox(_catalog?.Error ?? "UI 目录尚未扫描。", MessageType.Error);
            return false;
        }

        private IEnumerable<EUIPrefabCatalogEntry> FilteredEntries()
        {
            if (_catalog == null) return Enumerable.Empty<EUIPrefabCatalogEntry>();
            if (string.IsNullOrWhiteSpace(_overviewFilter)) return _catalog.Entries;
            var filter = _overviewFilter.Trim();
            return _catalog.Entries.Where(entry =>
                entry.PrefabPath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || (entry.PageName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        private IEnumerable<string> PageDefFiles()
        {
            yield return _catalog.UserPageDefFile;
            yield return _catalog.FrameworkPageDefFile;
        }

        private static bool IsPopup(PageType pageType)
        {
            return pageType == PageType.Popup || pageType == PageType.FullScreenPopup;
        }

        private static void DrawPath(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(75f));
            EditorGUILayout.SelectableLabel(path ?? "—", EditorStyles.miniLabel, GUILayout.Height(18f));
            EditorGUILayout.EndHorizontal();
        }

        private static void PingAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (!asset) return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void OpenAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset) AssetDatabase.OpenAsset(asset);
        }

        #endregion
    }
}
