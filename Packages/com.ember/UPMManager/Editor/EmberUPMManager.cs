// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    /// <summary>
    /// Ember UPM 管理器 —— 框架版本升级 + 必需依赖与可选第三方包体检。
    ///
    /// 命名说明：与框架的 Manager/Module 体系（EmberManagerCollector、EmberModuleCollector）无关，
    /// 本窗口只负责「Unity 包（UPM）」层面的管理。
    ///
    /// 设计约束：本程序集 <b>零框架/零 Sirenix 引用</b>（独立 asmdef）——
    /// 未安装 Odin 时框架主体编译会报错，本面板必须仍能编译并弹出，
    /// 否则用户将陷入「没面板 → 不知道装 Odin → 编译不过」的死锁。
    ///
    /// 版本升级原理：git 安装的包无 registry「Update」按钮，本面板通过
    /// `git ls-remote --tags` 对比远程与当前版本，提取 manifest 中 com.ember 的
    /// git URL 并替换 #tag 后调用 Client.Add 重装——体验等同点击升级，零服务器。
    /// </summary>
    public partial class EmberUPMManager : EditorWindow
    {
        #region 内部参数

        private const string FrameworkRepoUrl = "https://github.com/wsydet/ember-unity-framework.git";
        private const string PackageName = "com.ember";

        private const string OdinUrl =
            "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2";

        private const string DotweenUrl =
            "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.demigiant.dotween#dotween-v1.2.815";

        /// <summary>未来扩展包（预留区，Phase 2/3 规划）</summary>
        private static readonly (string name, string desc)[] PlannedPackages =
        {
            ("com.ember.blueprint", "蓝图/节点编辑器（Phase 3 规划）"),
            ("com.ember.network", "网络层（Phase 2 规划）"),
        };

        private static readonly string[] UpgradeSpinnerFrames = { "◐", "◓", "◑", "◒" };

        private bool _installing;
        private string _installingLabel;
        private bool _checking;
        private EmberUPMUpdateCheck _updateCheck;
        private Version _checkCurrentVersion;
        private string _checkMessage;
        private bool _checkFailed;
        private Version _currentVersion;
        private readonly List<Version> _newerTags = new();
        private Version _latestRemote;
        private Vector2 _scrollPosition;
        private EmberUPMReleaseNotes _releaseNotesRequest;
        private Dictionary<Version, string> _releaseNotesByVersion;
        private readonly HashSet<Version> _expandedReleaseNotes = new();
        private Version _releaseNotesSourceVersion;
        private string _releaseNotesError;
        private bool _retryReleaseNotes;
        private Dictionary<string, EmberUPMOptionalPackages.State> _optionalPackageStates;
        private bool _optionalPackagesDirty = true;
        private string _optionalPackagesError;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/UPM Manager", false, 50)]
        public static void ShowWindow()
        {
            var win = GetWindow<EmberUPMManager>("Ember UPM 管理器");
            win.minSize = new Vector2(480, 460);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnEnable()
        {
            UnityEditor.PackageManager.Events.registeredPackages += OnPackagesRegistered;
            RequestOptionalPackageRefresh();
        }

        private void OnFocus() => RequestOptionalPackageRefresh();
        private void OnProjectChange() => RequestOptionalPackageRefresh();

        private void OnPackagesRegistered(UnityEditor.PackageManager.PackageRegistrationEventArgs args)
            => RequestOptionalPackageRefresh();

        private void RequestOptionalPackageRefresh()
        {
            _optionalPackagesDirty = true;
            _aiSkillsDirty = true;
            Repaint();
        }

        private void RefreshOptionalPackages()
        {
            if (!_optionalPackagesDirty || EditorApplication.isCompiling || EditorApplication.isUpdating || _installing) return;
            _optionalPackagesDirty = false;
            _optionalPackagesError = null;
            try { _optionalPackageStates = EmberUPMOptionalPackages.Capture(); }
            catch (Exception ex)
            {
                _optionalPackageStates = null;
                _optionalPackagesError = "无法读取安装状态：" + ex.Message;
            }
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                UpdateReleaseNotes();
                RefreshOptionalPackages();
                UpdateAiSkillsLayout();
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember UPM 管理器", EditorStyles.boldLabel);

            DrawFrameworkVersionSection();
            DrawAiSkillsSection();

            if (_installing)
                EditorGUILayout.HelpBox($"正在安装 {_installingLabel}，请等待 Unity 完成包解析。", MessageType.Info);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("前置依赖体检（框架必需）", EditorStyles.boldLabel);

            // ---- Odin ----
            DrawDependencyRow(
                "Odin Inspector（付费）",
                GetPackageVersion("com.sirenix.odin-inspector") != null ||
                IsAssemblyLoaded("Sirenix.OdinInspector.Editor") || IsAssemblyLoaded("Sirenix.OdinInspector.Attributes"),
                OdinUrl,
                "https://odininspector.com/",
                "Inspector 增强，框架部分类型使用其属性");

            // ---- DOTween ----
            DrawDependencyRow(
                "DOTween（免费，禁止再分发）",
                GetPackageVersion("com.demigiant.dotween") != null || IsAssemblyLoaded("DOTween"),
                DotweenUrl,
                "https://dotween.demigiant.com/",
                "补间动画（UI 过渡等）");

            GUILayout.Space(8);
            EditorGUILayout.LabelField("可选第三方包", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("按需安装，缺少可选包不影响框架基础运行。", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("刷新安装状态", GUILayout.Width(120))) RequestOptionalPackageRefresh();
            if (!string.IsNullOrEmpty(_optionalPackagesError))
                EditorGUILayout.HelpBox(_optionalPackagesError, MessageType.Warning);
            foreach (var package in EmberUPMOptionalPackages.All)
                DrawOptionalDependencyRow(package);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("可选扩展包（未来）", EditorStyles.boldLabel);
            foreach (var (name, desc) in PlannedPackages)
            {
                EditorGUILayout.LabelField($"  ⬜ {name}", desc);
            }

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Odin 为付费插件：一键安装走团队私有仓库（需仓库访问权限 + 正版授权）；\n无权限时请从官网购买后自行导入（Assets/Plugins 方式同样有效）。\nDOTween 免费但许可禁止再分发，团队内统一从私有仓库安装。",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>框架版本区：当前版本 + 检查更新 + 一键升级（按版本语义标注强制/可选）。</summary>
        private void DrawFrameworkVersionSection()
        {
            var upgrade = EmberUPMUpgradeTracker.GetSnapshot();
            var currentVersionText = GetPackageVersion(PackageName);
            if (string.IsNullOrEmpty(currentVersionText))
            {
                if (upgrade.IsActive)
                {
                    EditorGUILayout.HelpBox(
                        "com.ember 正在被 Unity 切换，包信息暂时不可用。升级仍在后台继续。",
                        MessageType.Info);
                    DrawUpgradeOperation(upgrade);
                }
                else
                {
                    EditorGUILayout.HelpBox("未检测到 com.ember 包。请先在 Package Manager 中添加：\nhttps://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.10.0", MessageType.Error);
                }
                return;
            }

            Version.TryParse(currentVersionText, out _currentVersion);
            DrawStatusRow("com.ember（框架）", currentVersionText, true);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_checking && !_installing && !upgrade.IsActive;
            if (GUILayout.Button("检查更新", GUILayout.Width(100)))
            {
                CheckForUpdates(currentVersionText);
                upgrade = EmberUPMUpgradeTracker.GetSnapshot();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_checking)
                DrawUpdateCheckProgress();

            if (!string.IsNullOrEmpty(_checkMessage))
            {
                if (_checkFailed)
                    EditorGUILayout.HelpBox(_checkMessage, MessageType.Warning);
                else
                    EditorGUILayout.LabelField($"    {_checkMessage}", EditorStyles.miniLabel);
            }

            // 远程最新版本总览（检查成功后显示，含当前对比）
            if (!_checking && !_checkFailed && _latestRemote != null)
            {
                var latestText = _currentVersion != null && _latestRemote > _currentVersion
                    ? $"远程最新：v{_latestRemote}（当前 v{_currentVersion}，可升级）"
                    : _latestRemote == _currentVersion
                        ? $"远程最新：v{_latestRemote}（与当前一致）"
                        : $"远程最新：v{_latestRemote}（当前 v{_currentVersion}）";
                EditorGUILayout.LabelField($"    {latestText}", EditorStyles.miniLabel);
            }

            // 强制更新：major/minor 比当前高（框架已变化，强烈建议）
            foreach (var tag in _newerTags.Where(t => (_currentVersion == null || t > _currentVersion) && IsForcedUpgrade(t)))
            {
                EditorGUILayout.BeginHorizontal();
                var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, 0.55f, 0.2f) } };
                EditorGUILayout.LabelField($"    ⬆ 强制更新：v{tag}（框架已变化）", style, GUILayout.Width(260));
                GUI.enabled = !_checking && !_installing && !upgrade.IsActive;
                if (GUILayout.Button("升级到 v" + tag, GUILayout.Width(120)))
                    UpgradeTo(tag);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                DrawReleaseNotes(tag);
            }

            // 可选更新：仅 patch 高于当前（小修补，框架不变）
            foreach (var tag in _newerTags.Where(t => (_currentVersion == null || t > _currentVersion) && !IsForcedUpgrade(t)))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"    可选更新：v{tag}（小修补，可不升）", EditorStyles.miniLabel, GUILayout.Width(260));
                GUI.enabled = !_checking && !_installing && !upgrade.IsActive;
                if (GUILayout.Button("升级到 v" + tag, GUILayout.Width(120)))
                    UpgradeTo(tag);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                DrawReleaseNotes(tag);
            }

            DrawUpgradeOperation(upgrade);
        }

        private void UpdateReleaseNotes()
        {
            // Publish asynchronous content only at Layout, keeping control structure stable for Repaint.
            if (!_checking && !_checkFailed && _newerTags.Count > 0 && _latestRemote != null
                && (_retryReleaseNotes || _releaseNotesSourceVersion != _latestRemote))
            {
                _retryReleaseNotes = false;
                StopReleaseNotes();
                _releaseNotesSourceVersion = _latestRemote;
                _releaseNotesByVersion = null;
                _releaseNotesError = null;
                _expandedReleaseNotes.Add(_latestRemote);
                try
                {
                    string root = Directory.GetParent(Application.dataPath)?.FullName
                        ?? throw new InvalidOperationException("无法定位项目目录。");
                    _releaseNotesRequest = new EmberUPMReleaseNotes(FrameworkRepoUrl, _latestRemote,
                        Path.Combine(root, "Library", "EmberUPMReleaseNotes"));
                }
                catch (Exception ex) { _releaseNotesError = ex.Message; }
            }

            if (_releaseNotesRequest == null) return;
            _releaseNotesRequest.Poll();
            if (!_releaseNotesRequest.IsCompleted) return;
            _releaseNotesError = _releaseNotesRequest.Error;
            _releaseNotesByVersion = _releaseNotesRequest.Notes;
            StopReleaseNotes();
        }

        private void DrawReleaseNotes(Version version)
        {
            bool expanded = EditorGUILayout.Foldout(_expandedReleaseNotes.Contains(version), "更新内容", true);
            if (expanded) _expandedReleaseNotes.Add(version);
            else _expandedReleaseNotes.Remove(version);
            if (!expanded) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_releaseNotesRequest != null)
                    EditorGUILayout.LabelField("正在加载更新内容…（最长等待 60 秒）", EditorStyles.wordWrappedMiniLabel);
                else if (!string.IsNullOrEmpty(_releaseNotesError))
                {
                    EditorGUILayout.HelpBox("更新内容暂时无法加载，仍可升级。\n" + _releaseNotesError, MessageType.Warning);
                    if (GUILayout.Button("重试加载更新内容", GUILayout.Width(140))) _retryReleaseNotes = true;
                }
                else if (_releaseNotesByVersion != null && _releaseNotesByVersion.TryGetValue(version, out string notes)
                    && !string.IsNullOrWhiteSpace(notes))
                {
                    var style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false };
                    EditorGUILayout.LabelField(EmberUPMReleaseNotes.ToDisplayText(notes), style);
                }
                else
                    EditorGUILayout.LabelField("该版本尚未提供更新说明，仍可升级。", EditorStyles.wordWrappedMiniLabel);

                if (_releaseNotesSourceVersion != null)
                    EditorGUILayout.LabelField($"来源：v{_releaseNotesSourceVersion} 发布日志中的 v{version} 条目",
                        EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("查看该版本发布日志原文", GUILayout.Width(180)))
                    Application.OpenURL("https://github.com/wsydet/ember-unity-framework/blob/v" + version +
                        "/Packages/com.ember/CHANGELOG.md");
            }
        }

        private void StopReleaseNotes()
        {
            _releaseNotesRequest?.Dispose();
            _releaseNotesRequest = null;
        }

        /// <summary>版本语义：major/minor 高于当前 = 框架变化 = 强制更新；仅 patch 高 = 可选。</summary>
        private bool IsForcedUpgrade(Version tag)
        {
            if (_currentVersion == null) return true;
            return tag.Major > _currentVersion.Major || tag.Minor > _currentVersion.Minor;
        }

        private void CheckForUpdates(string currentVersion)
        {
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive) return;
            if (!Version.TryParse(currentVersion, out var current))
            {
                _checkMessage = "当前版本号无法解析：" + currentVersion;
                _checkFailed = true;
                return;
            }

            // 旧操作提示只在发起新检查时清理，绘制提示不能修改本次查询结果。
            EmberUPMUpgradeTracker.ClearTerminalState();
            _checking = true;
            _checkMessage = null;
            _checkFailed = false;
            _newerTags.Clear();
            _latestRemote = null;
            _checkCurrentVersion = current;
            _retryReleaseNotes = false;
            if (_releaseNotesRequest != null)
            {
                StopReleaseNotes();
                _releaseNotesSourceVersion = null;
            }
            Repaint();

            try
            {
                _updateCheck = new EmberUPMUpdateCheck(FrameworkRepoUrl);
                EditorApplication.update += PollUpdateCheck;
            }
            catch (Exception ex)
            {
                _checkMessage = "检查更新失败：" + ex.Message +
                    "\n（需要本机安装 git，且能访问 " + FrameworkRepoUrl + "）";
                _checkFailed = true;
                StopUpdateCheck();
            }
        }

        private void PollUpdateCheck()
        {
            if (_updateCheck == null) return;
            _updateCheck.Poll();
            if (!_updateCheck.IsCompleted) return;

            try
            {
                _checkFailed = !string.IsNullOrEmpty(_updateCheck.Error);
                if (_checkFailed)
                {
                    _checkMessage = "检查更新失败：" + _updateCheck.Error;
                    return;
                }
                var tags = _updateCheck.Versions;
                _latestRemote = tags.Max();
                _newerTags.AddRange(tags.Where(v => v > _checkCurrentVersion).OrderByDescending(v => v));
                _checkMessage = _newerTags.Count == 0
                    ? $"当前 v{_checkCurrentVersion}，远程没有更新的 tag。"
                    : $"检查完成，发现 {_newerTags.Count} 个更新版本。";
            }
            finally
            {
                StopUpdateCheck();
            }
        }

        private void DrawUpdateCheckProgress()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{GetUpgradeSpinner()} 正在查询远程版本…", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.35f));
            var width = rect.width * 0.25f;
            var offset = Mathf.PingPong((float)EditorApplication.timeSinceStartup * 0.7f, 1f);
            EditorGUI.DrawRect(new Rect(rect.x + offset * (rect.width - width), rect.y, width, rect.height),
                new Color(0.25f, 0.6f, 0.95f));
            var elapsed = _updateCheck?.Elapsed ?? TimeSpan.Zero;
            EditorGUILayout.LabelField($"已等待 {FormatElapsed(elapsed)} · 60 秒后自动超时",
                EditorStyles.miniLabel);
            if (elapsed.TotalSeconds >= 15)
                EditorGUILayout.HelpBox("远程响应较慢，可以继续等待或取消后检查网络与 Git 凭据。", MessageType.Info);
            if (GUILayout.Button("取消检查", GUILayout.Width(100)))
            {
                _checkMessage = "已取消检查，可重新查询。";
                _checkFailed = false;
                StopUpdateCheck();
            }
            EditorGUILayout.EndVertical();
        }

        private void StopUpdateCheck()
        {
            EditorApplication.update -= PollUpdateCheck;
            _updateCheck?.Dispose();
            _updateCheck = null;
            _checking = false;
            Repaint();
        }

        private void OnDisable()
        {
            UnityEditor.PackageManager.Events.registeredPackages -= OnPackagesRegistered;
            StopUpdateCheck();
            StopReleaseNotes();
            StopAiSkillDownload();
            _releaseNotesSourceVersion = null;
            _retryReleaseNotes = false;
        }

        private void UpgradeTo(Version target)
        {
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive) return;

            try
            {
                // 1. 从 manifest 提取 com.ember 当前 git URL，替换 #tag
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (projectRoot == null) throw new InvalidOperationException("无法定位项目根目录。");

                var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
                if (!File.Exists(manifestPath)) throw new InvalidOperationException("未找到 Packages/manifest.json。");

                var json = File.ReadAllText(manifestPath);
                var urlMatch = Regex.Match(json, "\"com\\.ember\"\\s*:\\s*\"([^\"]+)\"");
                if (!urlMatch.Success)
                    throw new InvalidOperationException("manifest.json 中未找到 com.ember 条目。");

                var currentUrl = urlMatch.Groups[1].Value;
                var newUrl = Regex.Replace(currentUrl, "#v[\\d.]+$", "#v" + target);
                if (newUrl == currentUrl)
                {
                    if (Regex.IsMatch(currentUrl, "#v[\\d.]+$"))
                    {
                        // 已是目标 tag：无需替换（此前误报「URL 未找到 tag」）
                        EditorUtility.DisplayDialog("已是最新",
                            $"manifest 的 com.ember tag 已经是 v{target}，无需替换。", "确定");
                        return;
                    }
                    throw new InvalidOperationException("URL 中未找到 #vX.Y.Z tag，无法替换：" + currentUrl);
                }

                // 2. Client.Add 同 URL 新 tag = 重新安装新版本（返回请求句柄可用于轮询）
                var sourceVersion = GetPackageVersion(PackageName) ?? string.Empty;
                if (!EmberUPMUpgradeTracker.BeginUpgrade(
                        newUrl, sourceVersion, target.ToString(), out _))
                {
                    return;
                }

                _checkMessage = null;
                Repaint();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("升级失败", ex.Message, "确定");
                Repaint();
            }
        }

        internal void DrawUpgradeOperation(EmberUPMUpgradeSnapshot upgrade)
        {
            if (!upgrade.HasState) return;

            GUILayout.Space(6);
            if (upgrade.IsActive)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    $"框架升级：v{upgrade.SourceVersion} → v{upgrade.TargetVersion}",
                    EditorStyles.boldLabel);

                var spinner = GetUpgradeSpinner();
                var progressRect = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(
                    progressRect,
                    upgrade.StageProgress,
                    $"{spinner} 阶段 {upgrade.StageIndex}/4 · {upgrade.StageTitle}");

                EditorGUILayout.LabelField(GetUpgradeDescription(upgrade), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    $"已等待 {FormatElapsed(upgrade.Elapsed)}（阶段进度，不代表下载百分比）",
                    EditorStyles.miniLabel);

                if (upgrade.IsSlow)
                {
                    var message = upgrade.IsVerySlow
                        ? "等待时间已超过 5 分钟。升级仍在后台继续；建议检查 Console、网络和 Git 凭据，不要重复点击升级。"
                        : "此阶段耗时较长。Git 包下载与 Unity 依赖解析不提供精确进度，升级仍在后台继续。";
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                }

                if (GUILayout.Button("复制诊断信息", GUILayout.Width(120)))
                    EditorGUIUtility.systemCopyBuffer = upgrade.BuildDiagnostics();
                EditorGUILayout.EndVertical();
                return;
            }

            if (upgrade.IsSucceeded)
            {
                EditorGUILayout.HelpBox(
                    $"升级完成：com.ember 已从 v{upgrade.SourceVersion} 升级到 v{upgrade.TargetVersion}。",
                    MessageType.Info);
            }
            else if (upgrade.IsFailed)
            {
                EditorGUILayout.HelpBox(
                    "升级失败：" + upgrade.Error +
                    "\n\n可检查网络或 Git 凭据后重新检查更新，再选择目标版本重试。",
                    MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("关闭提示", GUILayout.Width(100)))
                EmberUPMUpgradeTracker.ClearTerminalState();
            if (GUILayout.Button("复制诊断信息", GUILayout.Width(120)))
                EditorGUIUtility.systemCopyBuffer = upgrade.BuildDiagnostics();
            EditorGUILayout.EndHorizontal();
        }

        private static string GetUpgradeDescription(EmberUPMUpgradeSnapshot upgrade)
        {
            if (upgrade.IsCompiling)
                return "Unity 正在编译新版本脚本，完成后会自动恢复升级状态。";
            if (upgrade.IsUpdating)
                return "Unity 正在刷新 AssetDatabase，完成后会自动验证安装版本。";

            switch (upgrade.Phase)
            {
                case EmberUPMUpgradePhase.Preparing:
                    return "正在校验 manifest 与目标版本。";
                case EmberUPMUpgradePhase.Resolving:
                    return "Package Manager 正在下载并解析 Git 包。";
                case EmberUPMUpgradePhase.Registering:
                    return "Unity 正在注册新包，随后会刷新资源并编译脚本。";
                case EmberUPMUpgradePhase.Verifying:
                    return "解析已经完成，正在核对实际安装的框架版本。";
                default:
                    return string.Empty;
            }
        }

        private static string GetUpgradeSpinner()
        {
            var index = (int)(EditorApplication.timeSinceStartup * 4d)
                        % UpgradeSpinnerFrames.Length;
            return UpgradeSpinnerFrames[index];
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            var totalSeconds = Math.Max(0, (int)elapsed.TotalSeconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void OnInspectorUpdate()
        {
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive || _releaseNotesRequest != null
                || _retryReleaseNotes || _optionalPackagesDirty)
                Repaint();
        }

        /// <summary>绘制单个依赖体检行：状态 + 一键安装 + 手动指引。</summary>
        private void DrawDependencyRow(string label, bool installed, string installUrl, string manualUrl, string desc)
        {
            DrawStatusRow(label, installed ? "已安装" : "未安装", installed);

            if (!installed)
            {
                EditorGUILayout.LabelField($"    {desc}", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !_checking && !_installing && !EmberUPMUpgradeTracker.IsActive;
                if (GUILayout.Button("一键安装（团队仓库）", GUILayout.Width(180)))
                    InstallPackage(installUrl, label);
                if (GUILayout.Button("手动安装指引", GUILayout.Width(140)))
                    Application.OpenURL(manualUrl);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

            }
        }

        private void DrawOptionalDependencyRow(EmberUPMOptionalPackages.Definition package)
        {
            var state = _optionalPackageStates != null && _optionalPackageStates.TryGetValue(package.Name, out var found)
                ? found : new EmberUPMOptionalPackages.State("状态未确认，请刷新安装状态", false, false);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(package.Label, EditorStyles.boldLabel);
                var style = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = state.Installed ? new Color(0.25f, 0.75f, 0.4f) : new Color(0.95f, 0.65f, 0.25f) }
                };
                EditorGUILayout.LabelField(state.Text, style);
                EditorGUILayout.LabelField(package.Description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(package.Name, EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool busy = _checking || _installing || EmberUPMUpgradeTracker.IsActive
                        || EditorApplication.isCompiling || EditorApplication.isUpdating || _optionalPackagesDirty;
                    using (new EditorGUI.DisabledScope(busy || !state.CanInstall))
                    {
                        string button = state.Installed ? "已安装" : state.CanInstall ? "安装 v" + package.Version : "等待状态确认";
                        if (GUILayout.Button(new GUIContent(button, package.InstallUrl), GUILayout.Width(160)))
                            InstallOptionalPackage(package);
                    }
                    if (GUILayout.Button("复制安装地址", GUILayout.Width(110)))
                        EditorGUIUtility.systemCopyBuffer = package.InstallUrl;
                }
            }
        }

        private void InstallOptionalPackage(EmberUPMOptionalPackages.Definition package)
        {
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive
                || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            // Recheck immediately before Client.Add, so a stale row cannot replace an installed plugin.
            try
            {
                var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                if (packages == null || !EmberUPMOptionalPackages.Inspect(package, packages).CanInstall)
                {
                    RequestOptionalPackageRefresh();
                    return;
                }
                InstallPackage(package.InstallUrl, package.Label);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("无法确认安装状态", ex.Message, "确定");
                RequestOptionalPackageRefresh();
            }
        }

        private void DrawStatusRow(string label, string value, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((ok ? "✅ " : "🔴 ") + label, GUILayout.Width(260));
            EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
        }

        private static string GetPackageVersion(string packageName)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            return info?.version;
        }

        private void InstallPackage(string url, string label)
        {
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive) return;
            _installing = true;
            _installingLabel = label;

            UnityEditor.PackageManager.Requests.AddRequest request;
            try
            {
                request = UnityEditor.PackageManager.Client.Add(url);
            }
            catch (Exception exception)
            {
                _installing = false;
                _installingLabel = null;
                EditorUtility.DisplayDialog("安装失败", $"{label} 安装请求未启动：{exception.Message}", "确定");
                return;
            }
            EditorApplication.update += Poll;

            void Poll()
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= Poll;
                if (this == null) return;
                _installing = false;
                _installingLabel = null;
                RequestOptionalPackageRefresh();

                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    EditorUtility.DisplayDialog("安装完成",
                        $"{label} 的 UPM 安装请求已完成，面板将刷新安装状态。请等待 Unity 完成导入与编译。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("安装失败",
                        $"{label} 安装失败：{request.Error?.message ?? "未知错误"}\n\n" +
                        "若为网络/权限问题：\n" +
                        "• 团队私有仓库需配置 git 凭据\n" +
                        "• 无权限时请从插件官网取得授权和安装文件", "确定");
                }

                Repaint();
            }
        }

        #endregion
    }
}
