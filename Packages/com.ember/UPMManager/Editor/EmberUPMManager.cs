// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    /// <summary>
    /// Ember UPM 管理器 —— 前置依赖体检 + 一键安装 + 未来扩展包预留。
    ///
    /// 命名说明：与框架的 Manager/Module 体系（EmberManagerCollector、EmberModuleCollector）无关，
    /// 本窗口只负责「Unity 包（UPM）」层面的依赖管理。
    ///
    /// 设计约束：本程序集 <b>零框架/零 Sirenix 引用</b>（独立 asmdef）——
    /// 未安装 Odin 时框架主体编译会报错，本面板必须仍能编译并弹出，
    /// 否则用户将陷入「没面板 → 不知道装 Odin → 编译不过」的死锁。
    ///
    /// 检测用反射（程序集名），安装用 UnityEditor.PackageManager.Client.Add。
    /// </summary>
    public class EmberUPMManager : EditorWindow
    {
        #region 内部参数

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

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/UPM Manager", false, 50)]
        public static void ShowWindow()
        {
            var win = GetWindow<EmberUPMManager>("Ember UPM 管理器");
            win.minSize = new Vector2(460, 360);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember UPM 管理器", EditorStyles.boldLabel);

            // ---- 基础包状态 ----
            var emberVersion = GetPackageVersion("com.ember");
            if (string.IsNullOrEmpty(emberVersion))
            {
                EditorGUILayout.HelpBox("未检测到 com.ember 包。请先在 Package Manager 中添加：\nhttps://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.3.0", MessageType.Error);
            }
            else
            {
                DrawStatusRow("com.ember（基础包）", emberVersion, true);
            }

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
            EditorGUILayout.LabelField((ok ? "✅ " : "🔴 ") + label, GUILayout.Width(240));
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
