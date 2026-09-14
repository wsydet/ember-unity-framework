// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    public partial class EmberUPMManager
    {
        #region 内部参数
        [SerializeField] private string _aiSkillRevision = "main";
        private string _aiDownloadedRevision;
        private EmberAISkillDownload _aiSkillDownload;
        private List<EmberAISkillInstaller.Package> _aiSkillPackages;
        private readonly List<AiSkillRow> _aiSkillRows = new List<AiSkillRow>();
        private bool _aiSkillsDirty = true;
        private bool _aiSkillsBusyAtLayout;
        private bool _aiSkillsSourceAtLayout;
        private string _aiSkillMessage;
        private bool _aiSkillError;
        private string _aiSkillCommit;

        private sealed class AiSkillRow
        {
            internal EmberAISkillInstaller.Preview Preview;
            internal string Error;
        }
        #endregion

        #region 内部方法
        private static string AiSkillProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static bool HasEuiRegenerateApi()
        {
            // This assembly must still load without framework/Odin dependencies.
            var utility = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Ember.UIExtension.Editor.EUIBindingCodeGenUtility"))
                .FirstOrDefault(t => t != null);
            return utility != null && utility.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(m =>
                m.Name == "TryRegenerateCode" && m.ReturnType == typeof(bool)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType.FullName == "Ember.UIExtension.EUIBinding"
                && m.GetParameters()[1].IsOut && m.GetParameters()[1].ParameterType == typeof(string).MakeByRefType());
        }

        private void UpdateAiSkillsLayout()
        {
            _aiSkillsBusyAtLayout = _aiSkillDownload != null && !_aiSkillDownload.IsCompleted;
            _aiSkillsSourceAtLayout = EmberAISkillInstaller.IsSourceProject(AiSkillProjectRoot);
            if (_aiSkillDownload != null && _aiSkillDownload.IsCompleted && _aiSkillPackages == null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_aiSkillDownload.Error)) throw new IOException(_aiSkillDownload.Error);
                    _aiSkillPackages = EmberAISkillInstaller.ReadCatalog(_aiSkillDownload.SkillsRoot);
                    _aiSkillCommit = _aiSkillDownload.Commit;
                    _aiSkillsDirty = true;
                    _aiSkillError = false;
                    _aiSkillMessage = "技能检查完成，可以选择安装或更新。";
                }
                catch (Exception ex)
                {
                    _aiSkillError = true; _aiSkillMessage = ex.Message;
                    StopAiSkillDownload();
                }
            }
            if (!_aiSkillsDirty || _aiSkillPackages == null) return;
            _aiSkillsDirty = false;
            _aiSkillRows.Clear();
            foreach (var package in _aiSkillPackages)
            {
                try
                {
                    _aiSkillRows.Add(new AiSkillRow
                    {
                        Preview = EmberAISkillInstaller.Inspect(AiSkillProjectRoot, package),
                        Error = EmberAISkillInstaller.Incompatibility(package.Definition,
                            GetPackageVersion(PackageName), HasEuiRegenerateApi())
                    });
                }
                catch (Exception ex) { _aiSkillRows.Add(new AiSkillRow { Error = package.Definition.id + "：" + ex.Message }); }
            }
        }

        private void DrawAiSkillsSection()
        {
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("AI Skill · 独立安装与更新", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("从框架仓库获取技能，安装到当前项目；技能更新与框架升级分开进行。",
                    EditorStyles.wordWrappedMiniLabel);
                bool unavailable = _installing || EmberUPMUpgradeTracker.IsActive
                    || EditorApplication.isCompiling || EditorApplication.isUpdating;
                using (new EditorGUI.DisabledScope(_aiSkillsBusyAtLayout || unavailable))
                {
                    _aiSkillRevision = EditorGUILayout.TextField("技能分支或标签", _aiSkillRevision);
                    if (GUILayout.Button("检查 AI Skill 更新")) BeginAiSkillDownload();
                }
                if (_aiSkillsBusyAtLayout)
                {
                    EditorGUILayout.LabelField($"{GetUpgradeSpinner()} {_aiSkillDownload?.Progress} · "
                        + $"{_aiSkillDownload?.Elapsed.TotalSeconds:0} 秒 / 120 秒", EditorStyles.miniLabel);
                    if (GUILayout.Button("取消技能下载"))
                    {
                        StopAiSkillDownload(); _aiSkillMessage = "已取消技能下载。";
                        _aiSkillError = false; GUIUtility.ExitGUI();
                    }
                }
                if (!string.IsNullOrEmpty(_aiSkillMessage))
                    EditorGUILayout.HelpBox(_aiSkillMessage, _aiSkillError ? MessageType.Warning : MessageType.Info);
                if (_aiSkillsSourceAtLayout)
                    EditorGUILayout.HelpBox("当前是框架技能源目录，请直接维护源文件。更新器不会覆盖这些文件。", MessageType.Info);
                if (!string.IsNullOrEmpty(_aiSkillCommit))
                {
                    EditorGUILayout.LabelField("已检查：" + _aiDownloadedRevision + " · " + _aiSkillCommit.Substring(0, 12), EditorStyles.miniLabel);
                    if (GUILayout.Button("刷新项目技能状态")) { _aiSkillsDirty = true; Repaint(); }
                }
                foreach (var row in _aiSkillRows) DrawAiSkillRow(row, unavailable || _aiSkillsBusyAtLayout || _aiSkillsSourceAtLayout);
            }
        }

        private void DrawAiSkillRow(AiSkillRow row, bool unavailable)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (row.Preview == null) { EditorGUILayout.HelpBox(row.Error, MessageType.Warning); return; }
                var preview = row.Preview;
                var definition = preview.Package.Definition;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(definition.displayName) ? definition.id : definition.displayName,
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(definition.description ?? "", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(definition.id + " · " + AiSkillStatusLabel(preview.Status), EditorStyles.wordWrappedLabel);
                if (!string.IsNullOrEmpty(preview.InstalledCommit))
                    EditorGUILayout.LabelField("安装来源：" + preview.InstalledCommit, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(row.Error)) EditorGUILayout.HelpBox(row.Error, MessageType.Warning);
                using (new EditorGUI.DisabledScope(unavailable || !string.IsNullOrEmpty(row.Error)))
                {
                    string action = preview.NeedsOverwrite ? "备份并覆盖" : preview.Status == EmberAISkillInstaller.Status.Missing
                        ? "安装技能" : preview.Status == EmberAISkillInstaller.Status.Current ? "记录当前来源" : "更新技能";
                    if (GUILayout.Button(action))
                    {
                        InstallAiSkill(preview);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private static string AiSkillStatusLabel(EmberAISkillInstaller.Status status)
        {
            switch (status)
            {
                case EmberAISkillInstaller.Status.Missing: return "未安装";
                case EmberAISkillInstaller.Status.Current: return "内容与所选版本一致";
                case EmberAISkillInstaller.Status.UpdateAvailable: return "有可更新内容";
                case EmberAISkillInstaller.Status.LocalChanges: return "存在本地修改";
                default: return "已有项目副本，尚未由更新器管理";
            }
        }

        private void BeginAiSkillDownload()
        {
            StopAiSkillDownload();
            _aiSkillPackages = null; _aiSkillRows.Clear(); _aiSkillCommit = null;
            _aiSkillMessage = null; _aiSkillError = false;
            try
            {
                _aiDownloadedRevision = _aiSkillRevision.Trim();
                _aiSkillDownload = new EmberAISkillDownload(FrameworkRepoUrl, _aiDownloadedRevision,
                    Path.Combine(AiSkillProjectRoot, "Library/EmberAISkills"));
                EditorApplication.update += PollAiSkillDownload;
            }
            catch (Exception ex) { _aiSkillMessage = ex.Message; _aiSkillError = true; }
            GUIUtility.ExitGUI();
        }

        private void PollAiSkillDownload()
        {
            _aiSkillDownload?.Poll();
            if (_aiSkillDownload == null || _aiSkillDownload.IsCompleted)
                EditorApplication.update -= PollAiSkillDownload;
            Repaint(); // Completion and row changes are consumed at the next Layout event.
        }

        private void InstallAiSkill(EmberAISkillInstaller.Preview preview)
        {
            if (_installing || EmberUPMUpgradeTracker.IsActive || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            bool overwrite = preview.NeedsOverwrite;
            if (overwrite && !EditorUtility.DisplayDialog("备份并覆盖项目技能",
                    "该技能有未管理内容或本地修改。完整旧目录会保存在项目 .utmp/ember-ai-skills 中，再安装所选版本。继续？",
                    "备份并覆盖", "取消")) return;
            try
            {
                string incompatible = EmberAISkillInstaller.Incompatibility(preview.Package.Definition,
                    GetPackageVersion(PackageName), HasEuiRegenerateApi());
                if (incompatible != null) throw new InvalidOperationException(incompatible);
                string backup = EmberAISkillInstaller.Install(AiSkillProjectRoot, preview, FrameworkRepoUrl,
                    _aiDownloadedRevision, _aiSkillCommit, overwrite);
                _aiSkillError = false;
                _aiSkillMessage = "已安装 " + preview.Package.Definition.id + "。重新加载 AI 会话后使用更新的技能。"
                    + (backup == null ? "" : "\n旧版本备份：" + backup);
            }
            catch (Exception ex) { _aiSkillError = true; _aiSkillMessage = ex.Message; }
            _aiSkillsDirty = true;
            Repaint();
        }

        private void StopAiSkillDownload()
        {
            EditorApplication.update -= PollAiSkillDownload;
            _aiSkillDownload?.Dispose(); _aiSkillDownload = null;
            _aiSkillPackages = null; _aiSkillRows.Clear(); _aiSkillCommit = null;
            _aiSkillsBusyAtLayout = false;
        }
        #endregion
    }
}
