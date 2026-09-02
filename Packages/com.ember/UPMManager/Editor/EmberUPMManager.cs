// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    /// <summary>
    /// Ember UPM 管理器 —— 框架版本升级 + 前置依赖体检 + 未来扩展包预留。
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

        /// <summary>未来扩展包（预留区，Phase 2/3 规划）</summary>
        private static readonly (string name, string desc)[] PlannedPackages =
        {
            ("com.ember.blueprint", "蓝图/节点编辑器（Phase 3 规划）"),
            ("com.ember.network", "网络层（Phase 2 规划）"),
        };

        private bool _installing;
        private bool _checking;
        private bool _upgrading;
        private string _checkMessage;
        private bool _checkFailed;
        private Version _currentVersion;
        private readonly List<Version> _newerTags = new();
        private Version _latestRemote;

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
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember UPM 管理器", EditorStyles.boldLabel);

            DrawFrameworkVersionSection();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("前置依赖体检", EditorStyles.boldLabel);

            // ---- Odin ----
            DrawDependencyRow(
                "Odin Inspector（付费）",
                IsAssemblyLoaded("Sirenix.OdinInspector.Editor") || IsAssemblyLoaded("Sirenix.OdinInspector.Attributes"),
                OdinUrl,
                "https://odininspector.com/",
                "Inspector 增强，框架部分类型使用其属性");

            // ---- DOTween ----
            DrawDependencyRow(
                "DOTween（免费，禁止再分发）",
                IsAssemblyLoaded("DOTween"),
                DotweenUrl,
                "https://dotween.demigiant.com/",
                "补间动画（UI 过渡等）");

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
        }

        /// <summary>框架版本区：当前版本 + 检查更新 + 一键升级（按版本语义标注强制/可选）。</summary>
        private void DrawFrameworkVersionSection()
        {
            var currentVersionText = GetPackageVersion(PackageName);
            if (string.IsNullOrEmpty(currentVersionText))
            {
                EditorGUILayout.HelpBox("未检测到 com.ember 包。请先在 Package Manager 中添加：\nhttps://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.10.0", MessageType.Error);
                return;
            }

            Version.TryParse(currentVersionText, out _currentVersion);
            DrawStatusRow("com.ember（框架）", currentVersionText, true);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_checking && !_upgrading;
            if (GUILayout.Button("检查更新", GUILayout.Width(100)))
                CheckForUpdates(currentVersionText);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_checking)
                EditorGUILayout.LabelField("    ⏳ 正在查询远程 tag（git ls-remote）...", EditorStyles.miniLabel);

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
                    : $"远程最新：v{_latestRemote}（与当前一致）";
                EditorGUILayout.LabelField($"    {latestText}", EditorStyles.miniLabel);
            }

            // 强制更新：major/minor 比当前高（框架已变化，强烈建议）
            foreach (var tag in _newerTags.Where(IsForcedUpgrade))
            {
                EditorGUILayout.BeginHorizontal();
                var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, 0.55f, 0.2f) } };
                EditorGUILayout.LabelField($"    ⬆ 强制更新：v{tag}（框架已变化）", style, GUILayout.Width(260));
                GUI.enabled = !_upgrading;
                if (GUILayout.Button("升级到 v" + tag, GUILayout.Width(120)))
                    UpgradeTo(tag);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            // 可选更新：仅 patch 高于当前（小修补，框架不变）
            foreach (var tag in _newerTags.Where(t => !IsForcedUpgrade(t)))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"    可选更新：v{tag}（小修补，可不升）", EditorStyles.miniLabel, GUILayout.Width(260));
                GUI.enabled = !_upgrading;
                if (GUILayout.Button("升级到 v" + tag, GUILayout.Width(120)))
                    UpgradeTo(tag);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            if (_upgrading)
                EditorGUILayout.LabelField("    ⏳ 正在重装新版本并等待 Unity 解析...", EditorStyles.miniLabel);
        }

        /// <summary>版本语义：major/minor 高于当前 = 框架变化 = 强制更新；仅 patch 高 = 可选。</summary>
        private bool IsForcedUpgrade(Version tag)
        {
            if (_currentVersion == null) return true;
            return tag.Major > _currentVersion.Major || tag.Minor > _currentVersion.Minor;
        }

        private void CheckForUpdates(string currentVersion)
        {
            if (!Version.TryParse(currentVersion, out var current))
            {
                _checkMessage = "当前版本号无法解析：" + currentVersion;
                _checkFailed = true;
                return;
            }

            _checking = true;
            _checkMessage = null;
            _checkFailed = false;
            _newerTags.Clear();
            Repaint();

            try
            {
                var tags = ListRemoteTags();
                _latestRemote = tags.Count > 0 ? tags.Max() : null;
                _newerTags.AddRange(tags.Where(v => v > current).OrderByDescending(v => v));
                if (_newerTags.Count == 0)
                {
                    _checkMessage = $"已是最新版本（v{current}），远程没有更新的 tag。";
                    _checkFailed = false;
                }
            }
            catch (Exception ex)
            {
                _checkMessage = "检查更新失败：" + ex.Message +
                    "\n（需要本机安装 git，且能访问 " + FrameworkRepoUrl + "）";
                _checkFailed = true;
            }
            finally
            {
                _checking = false;
                Repaint();
            }
        }

        private static List<Version> ListRemoteTags()
        {
            var psi = new ProcessStartInfo("git", $"ls-remote --tags {FrameworkRepoUrl}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("无法启动 git 进程。请确认本机已安装 git 并加入 PATH。");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                    ? $"git ls-remote 失败（exit {process.ExitCode}）"
                    : stderr.Trim());

            var versions = new List<Version>();
            foreach (var line in stdout.Split('\n'))
            {
                var idx = line.IndexOf("refs/tags/", StringComparison.Ordinal);
                if (idx < 0) continue;

                // 归一化 tag：去 annotated tag 的 "^{}"（或 "^{"）后缀、去 "v" 前缀，再交给 Version.TryParse
                // （Version.TryParse 不认 "v" 前缀，历史 bug：v 前缀 tag 全部解析失败 → 永远显示「已是最新」）
                var tag = line.Substring(idx + "refs/tags/".Length).Trim();
                tag = tag.Replace("^{}", "").TrimEnd('^', '{', '}');
                if (tag.StartsWith("v", StringComparison.Ordinal))
                    tag = tag.Substring(1);
                if (Version.TryParse(tag, out var v))
                    versions.Add(v);
            }
            // annotated tag 会同时列出 tag 行与解引用行，去重
            return versions.Distinct().ToList();
        }

        private void UpgradeTo(Version target)
        {
            if (_upgrading) return;
            _upgrading = true;
            Repaint();

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
                var request = UnityEditor.PackageManager.Client.Add(newUrl);
                EditorApplication.update += PollResolve;

                void PollResolve()
                {
                    if (!request.IsCompleted) return;
                    EditorApplication.update -= PollResolve;
                    _upgrading = false;
                    _newerTags.Clear();
                    _checkMessage = null;

                    if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        EditorUtility.DisplayDialog("升级完成",
                            $"com.ember 已升级到 v{target}。\n若当前有编译报错属解析中间态，稍候即恢复。", "确定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("升级失败",
                            "解析失败：" + (request.Error?.message ?? "未知错误") +
                            "\n\n可手动修改 manifest 的 #tag 后重试。", "确定");
                    }
                    Repaint();
                }
            }
            catch (Exception ex)
            {
                _upgrading = false;
                EditorUtility.DisplayDialog("升级失败", ex.Message, "确定");
                Repaint();
            }
        }

        /// <summary>绘制单个依赖体检行：状态 + 一键安装 + 手动指引。</summary>
        private void DrawDependencyRow(string label, bool installed, string installUrl, string manualUrl, string desc)
        {
            DrawStatusRow(label, installed ? "已安装" : "未安装", installed);

            if (!installed)
            {
                EditorGUILayout.LabelField($"    {desc}", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !_installing;
                if (GUILayout.Button("一键安装（团队仓库）", GUILayout.Width(180)))
                    InstallPackage(installUrl, label);
                if (GUILayout.Button("手动安装指引", GUILayout.Width(140)))
                    Application.OpenURL(manualUrl);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (_installing)
                    EditorGUILayout.LabelField("    ⏳ 正在安装，请稍候（Unity 后台解析）...", EditorStyles.miniLabel);
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
            if (_installing) return;
            _installing = true;

            var request = UnityEditor.PackageManager.Client.Add(url);
            EditorApplication.update += Poll;

            void Poll()
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= Poll;
                _installing = false;

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
                        "• 无权限用户请走「手动安装指引」（Odin 官网购买 / DOTween 官网下载）", "确定");
                }

                Repaint();
            }
        }

        #endregion
    }
}
