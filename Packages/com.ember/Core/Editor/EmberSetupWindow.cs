// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Ember 项目初始化窗口 —— 初始化状态总览 + 模板选择 + 一键部署/切换。
    ///
    /// 模板体系见 docs/dev/upm-migration-plan.md §6.7：
    /// 框架交付的就是"演示形态"，用户一键部署后直接在状态钩子里写业务。
    /// 模板自动扫描：未来新增模板（如 2D 平台游戏）无需改本窗口即出现在列表。
    ///
    /// 兼容闸门（docs/dev/template-upgrade-system.md §三）：
    /// 模板 frameworkVersion 与当前框架 major.minor 一致才显示；channel=deprecated 隐藏。
    /// 升级提示矩阵（§四）：patch=绿色安全 / minor=橙色结构 / major=红色弃用，只提示不自动合并。
    /// </summary>
    public class EmberSetupWindow : EditorWindow
    {
        #region 内部参数

        private const string TAG = LogTags.CoreEditor;

        private bool _busy;
        private string _lastResult;

        private static GUIStyle _patchStyle;
        private static GUIStyle _previewStyle;

        /// <summary>patch 升级提示样式（懒加载：避免静态初始化期访问 EditorStyles，域重载未就绪时抛 NRE，见测试问题-3）</summary>
        private static GUIStyle PatchStyle
        {
            get
            {
                if (_patchStyle == null)
                    _patchStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.4f, 0.75f, 0.4f) }
                    };
                return _patchStyle;
            }
        }

        /// <summary>preview 实验性徽标样式（懒加载，同上）</summary>
        private static GUIStyle PreviewStyle
        {
            get
            {
                if (_previewStyle == null)
                    _previewStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(1f, 0.6f, 0.2f) }
                    };
                return _previewStyle;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [MenuItem("Ember/Setup/初始化项目", false, 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<EmberSetupWindow>("Ember 项目初始化");
            win.minSize = new Vector2(480, 380);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            // 触发重绘即可（状态实时读取）
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Ember 项目初始化", EditorStyles.boldLabel);

            // ---- 框架状态 ----
            var packageVersion = GetPackageVersion();
            DrawStatusRow("com.ember（框架）",
                string.IsNullOrEmpty(packageVersion) ? "未安装" : "v" + packageVersion,
                !string.IsNullOrEmpty(packageVersion));

            GUILayout.Space(8);
            EditorGUILayout.LabelField("模板", EditorStyles.boldLabel);

            var templates = EmberProjectSetup.GetCompatibleTemplates();
            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "包内没有可用模板（Templates~/ 下无 template.json，或全部与当前框架版本不兼容/已弃用）。",
                    MessageType.Warning);
            }
            else
            {
                foreach (var t in templates)
                {
                    DrawTemplateRow(t);
                    GUILayout.Space(4);
                }

                int hidden = EmberProjectSetup.GetTemplates().Count - templates.Count;
                if (hidden > 0)
                    EditorGUILayout.LabelField(
                        $"另有 {hidden} 个模板与当前框架版本不兼容或已弃用（已隐藏）。",
                        EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "部署 = 整树复制模板到 Assets/（.meta 随行，引用全链有效）。\n已存在的文件一律跳过——你的改动不会被覆盖；重复部署可补齐缺失文件。\n业务代码从 Assets/Game/State 的状态钩子开始写。",
                MessageType.Info);
        }

        private void DrawTemplateRow(TemplateInfo template)
        {
            var deployed = EmberProjectSetup.IsTemplateDeployed(template.id);
            var record = EmberProjectSetup.GetDeployedTemplate(template.id);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((deployed ? "✅ " : "⬜ ") + template.displayName,
                EditorStyles.boldLabel, GUILayout.Width(200));
            if (template.channel == "preview")
                EditorGUILayout.LabelField("🧪 实验性", PreviewStyle, GUILayout.Width(70));
            EditorGUILayout.LabelField($"模板 v{template.version} · 框架 v{template.frameworkVersion}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(template.description))
                EditorGUILayout.LabelField("    " + template.description, EditorStyles.miniLabel);

            // ---- 升级提示矩阵（只提示，不自动合并；P-B 上线 diff 向导后此处挂入口）----
            if (deployed)
            {
                if (record != null)
                {
                    var level = EmberProjectSetup.GetTemplateUpgradeLevel(record.version, template.version);
                    switch (level)
                    {
                        case TemplateUpgradeLevel.None:
                            EditorGUILayout.LabelField($"    已部署 v{record.version} · 已是最新", EditorStyles.miniLabel);
                            break;
                        case TemplateUpgradeLevel.Patch:
                            EditorGUILayout.LabelField(
                                $"    可选升级 v{record.version} → v{template.version}（安全，不覆盖你的改动）",
                                PatchStyle);
                            break;
                        case TemplateUpgradeLevel.Minor:
                            EditorGUILayout.HelpBox(
                                $"⚠ 结构升级 v{record.version} → v{template.version}：框架预写区将更新，可能影响你的改动。" +
                                "自动合并（diff 向导）尚未上线，当前只能补齐新增文件，不合并已有文件。",
                                MessageType.Warning);
                            break;
                        case TemplateUpgradeLevel.Major:
                            EditorGUILayout.HelpBox(
                                $"🚫 重大升级 v{record.version} → v{template.version}：钩子可能弃用，需人工迁移。" +
                                "详见 docs/dev/template-upgrade-system.md，升级前先备份。",
                                MessageType.Error);
                            break;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("    已部署（无版本记录，升级前为旧版部署）", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (deployed)
            {
                if (GUILayout.Button("补齐缺失", GUILayout.Width(100)))
                    DeployTemplate(template, false);
                if (GUILayout.Button("重新部署", GUILayout.Width(100)))
                    DeployTemplate(template, true);
            }
            else
            {
                if (GUILayout.Button("一键部署", GUILayout.Width(200)))
                    DeployTemplate(template, false);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // ---- 场景与配置（模板部署产物，归入本模板线框；多模板时各自展示）----
            EditorGUILayout.Space(2);
            var templateScenes = EmberProjectSetup.GetTemplateScenes(template.id);
            DrawStatusRow("Build Settings 场景注册",
                templateScenes.Count > 0 ? $"{templateScenes.Count} 个演示场景" : "模板未声明场景",
                HasTemplateScenesRegistered(templateScenes));
            DrawStatusRow("场景映射 SO", "Assets/Ember/Editor/SOs/EmberSceneMapping.asset",
                System.IO.File.Exists(ToFullPath("Assets/Ember/Editor/SOs/EmberSceneMapping.asset")));

            if (_busy)
                EditorGUILayout.LabelField("    ⏳ 正在部署...", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DeployTemplate(TemplateInfo template, bool openScene)
        {
            if (_busy) return;
            _busy = true;
            _lastResult = null;
            Repaint();

            try
            {
                int deployed = EmberProjectSetup.Initialize(template.id);
                _lastResult = deployed > 0
                    ? $"✅ [{template.displayName}] 部署完成：新增 {deployed} 个文件。"
                    : $"✅ [{template.displayName}] 已是最新，无缺失文件。";

                if (openScene || deployed > 0)
                {
                    var scenePath = ToFullPath("Assets/Game/Scenes/FrameworkScene.unity");
                    if (System.IO.File.Exists(scenePath))
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
            }
            catch (System.Exception ex)
            {
                _lastResult = "❌ 部署失败：" + ex.Message;
                EmberDebug.LogError(TAG, "部署失败：" + ex);
            }
            finally
            {
                _busy = false;
                Repaint();
            }
        }

        private static string GetPackageVersion()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.ember");
            return info?.version;
        }

        private static bool HasTemplateScenesRegistered(List<string> scenes)
        {
            if (scenes == null || scenes.Count == 0) return false;
            var paths = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));
            return scenes.All(s => paths.Contains(s));
        }

        private static string ToFullPath(string assetPath)
        {
            var root = System.IO.Directory.GetParent(Application.dataPath)?.FullName;
            if (root == null) return assetPath;
            return System.IO.Path.Combine(root, assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        private void DrawStatusRow(string label, string value, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((ok ? "✅ " : "🔴 ") + label, GUILayout.Width(260));
            EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}
