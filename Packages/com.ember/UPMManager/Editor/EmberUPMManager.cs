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
    public class EmberUPMManager : EditorWindow
    {
        #region 内部参数

        private const string FrameworkRepoUrl = "https://github.com/wsydet/ember-unity-framework.git";
        private const string PackageName = "com.ember";

        private const string OdinUrl =
            "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2";

        private const string DotweenUrl =
            "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.demigiant.dotween#dotween-v1.2.815";

        private const string ThirdPartyRepoUrl = "https://github.com/wsydet/ember-thirdparty-upm.git";
        private const string ThirdPartyTag = "ember-v0.11.1";

        /// <summary>与随包 Dependencies~ 发布清单对应；类型探针兼容直接导入的插件。</summary>
        private static readonly (string packageName, string label, string typeName, string desc)[] OptionalPackages =
        {
            ("com.borodar.rainbow-folders", "Rainbow Folders", "Borodar.RainbowFolders.RainbowFoldersGUI",
                "Project 文件夹颜色与图标"),
            ("com.borodar.rainbow-hierarchy", "Rainbow Hierarchy", "Borodar.RainbowHierarchy.RainbowHierarchyGUI",
                "Hierarchy 层级颜色与分组"),
            ("com.flyingworm.consolepro", "Console Pro", "FlyingWormConsole3.ConsoleProDebug",
                "Console 日志增强"),
            ("com.ryanindiedev.inputdevicedetector", "InputDeviceDetector", "InputDeviceDetection.InputDeviceDetector",
                "键鼠与手柄输入设备识别"),
            ("com.moremountains.feel", "Feel", "MoreMountains.Feedbacks.MMF_Player",
                "反馈、动效与振动工具")
        };

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

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember UPM 管理器", EditorStyles.boldLabel);

            DrawFrameworkVersionSection();

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
            foreach (var package in OptionalPackages)
                DrawOptionalDependencyRow(package.packageName, package.label, package.typeName, package.desc);

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
            }

            DrawUpgradeOperation(upgrade);
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
            StopUpdateCheck();
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
            if (_checking || _installing || EmberUPMUpgradeTracker.IsActive)
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

        private void DrawOptionalDependencyRow(string packageName, string label, string typeName, string desc)
        {
            var version = GetPackageVersion(packageName);
            var imported = version == null && IsPluginTypeLoaded(typeName);
            var installed = version != null || imported;
            var status = version != null ? $"已安装 v{version}（UPM）"
                : imported ? "已检测到插件（直接导入/自定义包）" : "未安装（可选）";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
            if (!installed)
            {
                using (new EditorGUI.DisabledScope(_checking || _installing || EmberUPMUpgradeTracker.IsActive))
                {
                    if (GUILayout.Button("一键安装（团队仓库）", GUILayout.Width(180)))
                        InstallPackage($"{ThirdPartyRepoUrl}?path=/{packageName}#{ThirdPartyTag}", label);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static bool IsPluginTypeLoaded(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(typeName, false) != null);
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
                _installing = false;
                _installingLabel = null;

                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    EditorUtility.DisplayDialog("安装完成",
                        $"{label} 安装成功。若编译报错属预期中间态，等 Unity 解析编译完成后即恢复。", "确定");
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
