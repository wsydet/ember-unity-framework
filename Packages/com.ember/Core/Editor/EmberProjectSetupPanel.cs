// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ember.Basic;

using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>统一项目中心的消费端初始化页。</summary>
    internal sealed class EmberProjectSetupPanel
    {
        #region 内部参数

        private const string TAG = LogTags.CoreEditor;

        private readonly EmberSetupWindowContext _context;
        private string _lastResult;
        private int _legacyActiveIndex;
        private TemplatePatchPlan _patchPlan;
        private readonly Dictionary<string, TemplateConflictChoice> _patchChoices = new();

        private static GUIStyle _patchStyle;
        private static GUIStyle _previewStyle;

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

        internal EmberProjectSetupPanel(EmberSetupWindowContext context)
        {
            _context = context;
        }

        internal void Draw()
        {
            var packageVersion = GetPackageVersion();
            DrawStatusRow(
                "com.ember（框架）",
                string.IsNullOrEmpty(packageVersion) ? "未安装" : "v" + packageVersion,
                !string.IsNullOrEmpty(packageVersion));

            DrawActiveTemplateState();

            if (GUILayout.Button("检查项目生成物与引用", GUILayout.Width(210)))
                EmberSetupWindow.ShowProjectValidation(true);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("可用模板", EditorStyles.boldLabel);
            var templates = EmberProjectSetup.GetCompatibleTemplates();
            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "包内没有与当前框架兼容的可部署模板。",
                    MessageType.Warning);
            }
            else
            {
                foreach (var template in templates)
                {
                    DrawTemplateRow(template);
                    GUILayout.Space(4);
                }

                int hidden = EmberProjectSetup.GetTemplates().Count - templates.Count;
                if (hidden > 0)
                {
                    EditorGUILayout.LabelField(
                        $"另有 {hidden} 个模板不兼容或已弃用（已隐藏）。",
                        EditorStyles.miniLabel);
                }
            }

            if (_context.IsBusy)
                EditorGUILayout.LabelField("⏳ 正在部署...", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "消费项目可从包内模板初始化或进行补丁增量更新，不会修改框架模板。跨前两位版本或切换模板时，先保护本地内容，再完整部署。",
                MessageType.Info);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void DrawActiveTemplateState()
        {
            var active = EmberProjectSetup.GetActiveDeployedTemplate();
            if (active != null)
            {
                DrawStatusRow(
                    "当前活动模板",
                    $"{active.templateId} · v{active.version}",
                    true);
                return;
            }

            if (!EmberProjectSetup.HasAmbiguousDeploymentState())
            {
                DrawStatusRow("当前活动模板", "尚未部署", false);
                return;
            }

            EditorGUILayout.HelpBox(
                "检测到旧版多条部署记录，无法自动判断当前项目内容来自哪个模板。请明确认定后再补齐。",
                MessageType.Warning);
            var records = EmberProjectSetup.GetDeployedTemplates();
            if (records.Count == 0) return;
            _legacyActiveIndex = Mathf.Clamp(_legacyActiveIndex, 0, records.Count - 1);
            var options = records
                .Select(record => $"{record.templateId} · v{record.version}")
                .ToArray();
            _legacyActiveIndex = EditorGUILayout.Popup(
                "认定当前模板",
                _legacyActiveIndex,
                options);
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("确认活动模板（只迁移记录，不改项目内容）", GUILayout.Width(300))
                && EditorUtility.DisplayDialog(
                    "确认活动模板",
                    $"确认当前项目业务层来自模板 [{records[_legacyActiveIndex].templateId}]？\n此操作只迁移部署记录，不复制或删除项目文件。",
                    "确认",
                    "取消"))
            {
                RunOperation(() =>
                {
                    EmberProjectSetup.SetActiveDeployedTemplate(
                        records[_legacyActiveIndex].templateId);
                    _lastResult = $"✅ 已认定活动模板 [{records[_legacyActiveIndex].templateId}]。";
                }, "迁移部署记录失败");
            }
            GUI.enabled = true;
        }

        private void DrawTemplateRow(TemplateInfo template)
        {
            var active = EmberProjectSetup.GetActiveDeployedTemplate();
            bool activeTemplate = active != null
                && string.Equals(active.templateId, template.id, System.StringComparison.Ordinal);
            bool replacingActiveTemplate = active != null && !activeTemplate;
            bool ambiguous = EmberProjectSetup.HasAmbiguousDeploymentState();
            var record = EmberProjectSetup.GetDeployedTemplate(template.id);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                (activeTemplate ? "✅ " : "⬜ ") + template.displayName,
                EditorStyles.boldLabel,
                GUILayout.Width(220));
            if (template.channel == "preview")
                EditorGUILayout.LabelField("🧪 preview", PreviewStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField(
                $"模板 v{template.version} · 框架 v{template.frameworkVersion}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(template.description))
                EditorGUILayout.LabelField("    " + template.description, EditorStyles.miniLabel);

            if (activeTemplate)
                DrawUpgradeStatus(template, record);
            else if (replacingActiveTemplate)
            {
                EditorGUILayout.HelpBox(
                    $"部署完整模板 [{template.id}] 会替换当前活动模板 [{active.templateId}] 管理的 Game、Resources、Ember/Editor、Settings 和 GameResource；不会把消费项目内容写回框架模板。",
                    MessageType.Warning);
            }

            GUI.enabled = !_context.OperationsBlocked && !ambiguous;
            GUI.enabled &= !activeTemplate || (active.version == template.version && active.contentHash == template.contentHash);
            string buttonText = activeTemplate ? "补齐缺失" : replacingActiveTemplate ? "部署此模板" : "一键部署";
            if (GUILayout.Button(buttonText, GUILayout.Width(180)))
            {
                if (replacingActiveTemplate)
                    DeployReplacingActiveTemplate(template, active);
                else
                    DeployTemplate(template);
            }
            GUI.enabled = true;

            if (activeTemplate)
            {
                GUI.enabled = !_context.OperationsBlocked && !ambiguous;
                if (EmberProjectSetup.IsForwardTemplatePatch(active.version, template.version))
                    DrawPatchUpdate(template);
                GUI.enabled = !_context.OperationsBlocked && !ambiguous
                    && !EmberProjectSetup.IsForwardTemplatePatch(active.version, template.version);
                if (GUILayout.Button("完整重新部署", GUILayout.Width(180)))
                    DeployReplacingActiveTemplate(template, active);
                GUI.enabled = true;

                GUILayout.Space(2);
                var scenes = EmberProjectSetup.GetTemplateScenes(template.id);
                DrawStatusRow(
                    "Build Settings 场景注册",
                    scenes.Count > 0 ? $"{scenes.Count} 个场景" : "模板未声明场景",
                    HasTemplateScenesRegistered(scenes));
                DrawStatusRow(
                    "场景映射 SO",
                    "Assets/Ember/Editor/SOs/EmberSceneMapping.asset",
                    File.Exists(ToFullPath(
                        "Assets/Ember/Editor/SOs/EmberSceneMapping.asset")));
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawUpgradeStatus(
            TemplateInfo template,
            DeployedTemplateRecord record)
        {
            if (record == null)
            {
                EditorGUILayout.LabelField("    已部署（无版本记录）", EditorStyles.miniLabel);
                return;
            }

            switch (EmberProjectSetup.GetTemplateUpgradeLevel(record.version, template.version))
            {
                case TemplateUpgradeLevel.None:
                    EditorGUILayout.LabelField(
                        $"    已部署 v{record.version} · 已是最新",
                        EditorStyles.miniLabel);
                    break;
                case TemplateUpgradeLevel.Patch:
                    EditorGUILayout.LabelField(
                        $"    可选升级 v{record.version} → v{template.version}（预览增量差异，保留本地开发内容）",
                        PatchStyle);
                    break;
                case TemplateUpgradeLevel.Minor:
                    EditorGUILayout.HelpBox(
                        $"结构升级 v{record.version} → v{template.version}；请先保护本地改动，再完整部署并恢复。",
                        MessageType.Warning);
                    break;
                case TemplateUpgradeLevel.Major:
                    EditorGUILayout.HelpBox(
                        $"重大升级 v{record.version} → v{template.version}；请先保护本地改动，再完整部署并合并恢复。",
                        MessageType.Error);
                    break;
            }
        }

        private void DrawPatchUpdate(TemplateInfo template)
        {
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("预览补丁增量更新", GUILayout.Width(180)))
                RunOperation(() =>
                {
                    _patchPlan = null;
                    _patchChoices.Clear();
                    _patchPlan = EmberProjectSetup.PreviewTemplatePatch(template.id);
                }, "增量预览失败");
            if (GUILayout.Button("从旧模板恢复部署基线", GUILayout.Width(180)))
            {
                string folder = EditorUtility.OpenFolderPanel("选择部署时旧版本模板的 Assets 目录", "", "");
                if (!string.IsNullOrEmpty(folder))
                    RunOperation(() =>
                    {
                        EmberProjectSetup.RestoreTemplateDeploymentBaseline(folder);
                        _patchPlan = null;
                        _lastResult = "部署基线已恢复，项目业务内容未改动。请重新预览。";
                    }, "恢复基线失败");
            }
            GUI.enabled = true;
            if (_patchPlan == null || _patchPlan.TemplateId != template.id) return;
            EditorGUILayout.LabelField($"增量预览：{_patchPlan.FromVersion} → {_patchPlan.ToVersion}", EditorStyles.boldLabel);
            if (GUILayout.Button("复制冲突日志", GUILayout.Width(180)))
            {
                EditorGUIUtility.systemCopyBuffer = EmberProjectSetup.BuildTemplatePatchConflictLog(_patchPlan, _patchChoices);
                _lastResult = "冲突日志已复制，可交给本地改动恢复 Skill。合并后请重新预览。";
            }
            bool unresolved = false;
            foreach (var change in _patchPlan.Changes)
            {
                if (!change.IsConflict && change.RecommendedChoice == TemplateConflictChoice.KeepChild) continue;
                if (change.IsConflict)
                {
                    _patchChoices.TryGetValue(change.UnitPath, out var choice);
                    int selected = choice == TemplateConflictChoice.KeepChild ? 1
                        : choice == TemplateConflictChoice.AcceptParent ? 2 : 0;
                    EditorGUILayout.LabelField("冲突：" + change.UnitPath, EditorStyles.wordWrappedLabel);
                    // 路径类型改变不是 patch 契约，仍可保留本地并交给作者发布结构升级。
                    string[] choices = change.UnitKind == TemplateChangeUnitKind.PathTypeConflict
                        ? new[] { "请选择", "保留本地" }
                        : new[] { "请选择", "保留本地", "采用新版（覆盖此文件单元）" };
                    selected = EditorGUILayout.Popup("处理方式", selected, choices);
                    _patchChoices[change.UnitPath] = selected == 1 ? TemplateConflictChoice.KeepChild
                        : selected == 2 ? TemplateConflictChoice.AcceptParent : TemplateConflictChoice.Unresolved;
                    unresolved |= selected == 0;
                }
                else
                    EditorGUILayout.LabelField("更新：" + change.UnitPath + " · " + change.Kind, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.HelpBox("自动应用仅新版改动，保留本地独有内容。冲突选择作用于整个资源及其 .meta；需要合并双方逻辑时，可先编辑本地文件，再重新预览。", MessageType.Info);
            GUI.enabled = !_context.OperationsBlocked && !unresolved;
            if (GUILayout.Button("应用增量更新", GUILayout.Width(180)))
                RunOperation(() =>
                {
                    int count = EmberProjectSetup.ApplyTemplatePatch(_patchPlan, _patchChoices);
                    _patchPlan = null;
                    _patchChoices.Clear();
                    _lastResult = $"增量更新完成：变更 {count} 个文件，已推进部署基线。";
                }, "增量更新失败");
            GUI.enabled = true;
        }

        private void DeployTemplate(TemplateInfo template)
        {
            RunOperation(() =>
            {
                int deployed = EmberProjectSetup.Initialize(template.id);
                _lastResult = deployed > 0
                    ? $"✅ [{template.displayName}] 部署完成：新增 {deployed} 个文件。"
                    : $"✅ [{template.displayName}] 没有缺失文件。";

                if (deployed <= 0) return;
                var scenePath = ToFullPath("Assets/Game/Scenes/FrameworkScene.unity");
                if (File.Exists(scenePath))
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }, "部署失败");
        }

        private void DeployReplacingActiveTemplate(
            TemplateInfo template,
            DeployedTemplateRecord active)
        {
            bool redeploying = string.Equals(
                template.id,
                active.templateId,
                System.StringComparison.Ordinal);
            string title = redeploying ? "完整重新部署" : "部署完整模板";
            string action = redeploying ? "重新部署此模板" : "部署此模板";
            string description = redeploying
                ? $"确定完整重新部署 [{template.id}]？\n\n"
                  + "Game、Resources、Ember/Editor、Settings 和 GameResource 会全部替换，"
                  + "这些目录中的项目修改将被覆盖。Assets/Art、Assets/ThirdParty 等非模板目录不受影响。"
                : $"确定部署 [{template.id}] 并替换当前活动模板 [{active.templateId}]？\n\n"
                  + "此操作只把包内目标模板部署到消费项目，不会保存或修改框架模板。"
                  + "Assets/Art、Assets/ThirdParty 等非模板目录不受影响。";
            if (!EditorUtility.DisplayDialog(
                    title,
                    description,
                    action,
                    "取消"))
            {
                return;
            }

            RunOperation(() =>
            {
                int deployed = EmberProjectSetup.DeployReplacingActiveTemplate(template.id);
                _lastResult = $"✅ [{template.displayName}] 部署完成：写入 {deployed} 个模板文件。";

                var scenePath = ToFullPath("Assets/Game/Scenes/FrameworkScene.unity");
                if (File.Exists(scenePath))
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }, "部署失败");
        }

        private void RunOperation(System.Action action, string failureLabel)
        {
            if (_context.OperationsBlocked) return;
            _context.IsBusy = true;
            _lastResult = null;
            _context.Repaint();
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                _lastResult = $"❌ {failureLabel}：{ex.Message}";
                EmberDebug.LogError(TAG, failureLabel + "：" + ex);
            }
            finally
            {
                _context.Invalidate();
                _context.IsBusy = false;
                _context.Repaint();
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
            var paths = new HashSet<string>(EditorBuildSettings.scenes.Select(scene => scene.path));
            return scenes.All(paths.Contains);
        }

        private static string ToFullPath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName;
            return root == null
                ? assetPath
                : Path.Combine(
                    root,
                    assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void DrawStatusRow(string label, string value, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((ok ? "✅ " : "🔴 ") + label, GUILayout.Width(260));
            EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}
