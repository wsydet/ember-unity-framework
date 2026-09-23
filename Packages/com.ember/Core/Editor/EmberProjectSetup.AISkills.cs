// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Basic;
using Ember.UPMManager.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    public static partial class EmberProjectSetup
    {
        #region 内部方法
        private static EmberAISkillInstaller.TemplatePreview PreviewSkills(
            string projectRoot, string assets, TemplateInfo template, string previousId, bool editing = false)
        {
            var preview = EmberAISkillInstaller.PreviewTemplateSkills(projectRoot, assets,
                new EmberAISkillInstaller.TemplateContext(template.id, template.version, template.contentHash,
                    previousId, GetFrameworkVersion(), HasSkillEuiApi(), editing));
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, EditingRecordPath);
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, DeployedRecordsPath);
            return preview;
        }

        private static void EnsureSkillSourceGuids(string projectRoot, string sourceAssets)
        {
            string reason = GetTemplateGuidCollisionBlockReason(projectRoot, sourceAssets, true,
                EmberAISkillInstaller.TemplateSourceDirectory);
            if (!string.IsNullOrEmpty(reason)) throw new InvalidOperationException(reason);
        }

        private static bool HasSkillEuiApi()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Ember.UIExtension.Editor.EUIBindingCodeGenUtility"))
                .FirstOrDefault(t => t != null);
            return type != null && type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Any(m => m.Name == "TryRegenerateCode" && m.ReturnType == typeof(bool)
                    && m.GetParameters().Length == 2 && m.GetParameters()[1].IsOut
                    && m.GetParameters()[0].ParameterType.FullName == "Ember.UIExtension.EUIBinding"
                    && m.GetParameters()[1].ParameterType == typeof(string).MakeByRefType());
        }

        private static EmberAISkillInstaller.TemplatePreview ConfirmLifecycleSkills(
            string projectRoot, string assets, TemplateInfo template, bool editing)
        {
            string previous = editing ? GetEditingTemplate()?.templateId : GetActiveDeployedTemplate()?.templateId;
            var preview = PreviewSkills(projectRoot, assets, template, previous, editing);
            if (!editing && IsEmbeddedPackage() && preview.Differences.Count > 0)
                throw new InvalidOperationException("框架开发项目请通过加载模板建立编辑身份，不通过消费端部署启用模板技能。");
            EmberAISkillInstaller.ValidateTemplatePreview(preview);
            if (preview.HasChanges && !EditorUtility.DisplayDialog("模板 Skill 同步预览",
                    string.Join("\n", preview.Differences) + "\n\n旧副本备份到 .utmp/ember-ai-skills。"
                    + "\nAI 客户端可能需要重新加载会话；已载入的内容不能即时撤回。",
                    preview.NeedsBackupConfirmation ? "备份并同步" : "同步", "取消"))
                throw new OperationCanceledException("已取消模板和技能操作，项目内容未改动。");
            return preview;
        }

        private static void AddPreparedSkills(List<TemplateTransactionTarget> targets,
            EmberAISkillInstaller.TemplatePreview preview, bool allowBackup)
        {
            var prepared = EmberAISkillInstaller.PrepareTemplateSkills(preview, allowBackup, out var backup);
            foreach (var target in prepared)
                targets.Add(new TemplateTransactionTarget(target.StagedPath, target.DestinationPath, target.Remove));
            if (backup != null) EmberDebug.LogInit(TAG, "模板技能已暂存；旧副本备份：" + backup);
            // Preparation only copies backups; commit remains owned by the template transaction.
        }

        private static void AddDeploymentRecord(List<TemplateTransactionTarget> targets, string stageRoot,
            string projectRoot, TemplateInfo template)
        {
            string path = Path.Combine(projectRoot, DeployedRecordsPath);
            var data = File.Exists(path) ? JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(path))
                : new DeployedTemplatesData();
            if (data?.records == null) throw new InvalidDataException("部署记录损坏，不能提交模板技能。");
            UpdateDeploymentRecord(data, template);
            string staged = Path.Combine(stageRoot, "deployment.json");
            EmberTemplateTransaction.WriteJson(staged, data);
            targets.Add(new TemplateTransactionTarget(staged, path));
        }
        #endregion

        #region 外部方法
        /// <summary>模板技能执行前调用；开发身份与消费身份按正式模式区分。</summary>
        public static string GetTemplateSkillExecutionBlockReason(string skillId)
        {
            var editing = IsEmbeddedPackage() ? GetEditingTemplate() : null;
            var deployed = IsEmbeddedPackage() ? null : GetActiveDeployedTemplate();
            return EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(
                Directory.GetParent(Application.dataPath).FullName, skillId,
                editing?.templateId ?? deployed?.templateId,
                editing?.templateVersion ?? deployed?.version,
                editing?.contentHash ?? deployed?.contentHash, editing != null);
        }

        /// <summary>查看包内模板技能；此预览不代表项目已获得该模板的使用条件。</summary>
        public static EmberAISkillInstaller.TemplatePreview PreviewSelectedTemplateSkills(string templateId)
        {
            var template = GetTemplates().Find(t => t.id == templateId)
                ?? throw new InvalidOperationException("模板不存在。");
            var root = Directory.GetParent(Application.dataPath).FullName;
            return PreviewSkills(root, Path.Combine(GetResolvedPath(PACKAGE), "Templates~", templateId, "Assets"),
                template, IsEmbeddedPackage() ? GetEditingTemplate()?.templateId : GetActiveDeployedTemplate()?.templateId, IsEmbeddedPackage());
        }

        /// <summary>当前正式编辑/部署模板的技能预览。消费端可独立更新兼容的技能，保留业务部署基线。</summary>
        public static EmberAISkillInstaller.TemplatePreview PreviewCurrentTemplateSkills()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            var editing = IsEmbeddedPackage() ? GetEditingTemplate() : null;
            var deployed = IsEmbeddedPackage() ? null : GetActiveDeployedTemplate();
            string id = editing?.templateId ?? deployed?.templateId;
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("没有正式编辑/部署模板记录；请先加载或部署模板。");
            var template = GetTemplates().Find(t => t.id == id)
                ?? throw new InvalidOperationException("包内已无当前模板，请恢复对应框架版本。");
            if (editing != null)
            {
                EnsureEditingCopyCanSave(template);
                // Source editing is explicit. Save/Bump do not silently overwrite a modified discovery copy.
                return PreviewSkills(root, Application.dataPath, template, id, true);
            }
            string assets = Path.Combine(GetResolvedPath(PACKAGE), "Templates~", id, "Assets");
            return PreviewDeployedTemplateSkillUpdate(root, assets, template, deployed, GetFrameworkVersion(), HasSkillEuiApi());
        }

        internal static EmberAISkillInstaller.TemplatePreview PreviewDeployedTemplateSkillUpdate(
            string projectRoot, string assets, TemplateInfo template, DeployedTemplateRecord deployed,
            string frameworkVersion, bool hasEuiApi)
        {
            if (deployed == null || deployed.templateId != template.id || string.IsNullOrEmpty(deployed.contentHash))
                throw new InvalidOperationException("没有匹配且带 hash 的正式业务部署记录，不能独立更新技能。");
            if (template.versionedContentHash != template.contentHash)
                throw new InvalidOperationException("包内技能源尚未随模板 Bump 封存，不能分发草稿。");
            EnsureStoredContentMatchesMetadata(template, assets, "技能更新");
            EnsureSkillSourceGuids(projectRoot, assets);
            var preview = EmberAISkillInstaller.PreviewTemplateSkills(projectRoot, assets,
                new EmberAISkillInstaller.TemplateContext(template.id, template.version, template.contentHash,
                    deployed.templateId, frameworkVersion, hasEuiApi, false, deployed.version, deployed.contentHash, true));
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, EditingRecordPath);
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, DeployedRecordsPath);
            return preview;
        }

        /// <summary>显式刷新当前模板的发现副本；重新核对正式身份，保留本地修改备份。</summary>
        public static void SyncCurrentTemplateSkills(EmberAISkillInstaller.TemplatePreview preview, bool allowBackup)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            var current = PreviewCurrentTemplateSkills();
            EmberAISkillInstaller.ValidateTemplatePreview(current);
            if (Path.GetFullPath(current.ProjectRootPath) != Path.GetFullPath(preview.ProjectRootPath)
                || Path.GetFullPath(current.SourceAssetsPath) != Path.GetFullPath(preview.SourceAssetsPath)
                || current.TemplateId != preview.TemplateId || current.TemplateVersion != preview.TemplateVersion
                || current.IsIndependentUpdate != preview.IsIndependentUpdate || current.IsEditing != preview.IsEditing
                || current.TemplateContentHash != preview.TemplateContentHash || current.BusinessContentHash != preview.BusinessContentHash
                || current.BusinessTemplateVersion != preview.BusinessTemplateVersion
                || !current.Differences.SequenceEqual(preview.Differences))
                throw new IOException("模板身份或技能计划在预览后变化，请重新预览。");
            EmberAISkillInstaller.ValidateTemplatePreview(preview);
            CommitTemplateSkillUpdate(preview, allowBackup);
            AssetDatabase.Refresh();
        }

        /// <summary>技能专用事务；不会提交业务目录或部署记录。隔离测试复用同一入口。</summary>
        internal static void CommitTemplateSkillUpdate(EmberAISkillInstaller.TemplatePreview preview, bool allowBackup,
            Action<int> faultInjector = null)
        {
            if (!preview.IsEditing && !preview.IsIndependentUpdate)
                throw new InvalidOperationException("消费端技能更新需要独立更新计划，不能复用完整部署计划。");
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                if (preview.IsIndependentUpdate) EnsureSkillSourceGuids(preview.ProjectRootPath, preview.SourceAssetsPath);
                var targets = new List<TemplateTransactionTarget>();
                AddPreparedSkills(targets, preview, allowBackup);
                EmberAISkillInstaller.ValidateTemplatePreview(preview);
                if (preview.IsIndependentUpdate) EnsureSkillSourceGuids(preview.ProjectRootPath, preview.SourceAssetsPath);
                if (targets.Count > 0) EmberTemplateTransaction.CommitPreparedTargets(targets, faultInjector);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
        }

        /// <summary>部署/加载暂存与提交入口，也供隔离夹具验证；必须提供经过确认的技能计划。</summary>
        internal static int CommitTemplateDeployment(string projectRoot, string sourceAssets, TemplateInfo template,
            bool replace, EmberAISkillInstaller.TemplatePreview skills, Action<int> faultInjector = null, bool editing = false)
        {
            EmberAISkillInstaller.ValidateTemplateBinding(skills, projectRoot, sourceAssets,
                template.id, template.version, template.contentHash);
            if (skills.IsIndependentUpdate || skills.IsEditing != editing) throw new InvalidOperationException("模板编辑/部署计划模式不一致。");
            EmberTemplateTransaction.ValidateDirectorySafety(sourceAssets);
            foreach (string relative in TemplateDirNames)
                EmberTemplateTransaction.ValidateDirectorySafety(Path.Combine(projectRoot, "Assets", relative));
            EnsureNoTemplateGuidCollisions(projectRoot, sourceAssets, replace);
            string stageRoot = Path.Combine(projectRoot, "Temp", "EmberTemplateDeploy-" + Guid.NewGuid().ToString("N") + "~");
            var targets = new List<TemplateTransactionTarget>();
            int count = 0;
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (string relative in TemplateDirNames)
                {
                    string source = Path.Combine(sourceAssets, relative);
                    string destination = Path.Combine(projectRoot, "Assets", relative);
                    string staged = Path.Combine(stageRoot, "Assets", relative);
                    if (!replace && Directory.Exists(destination)) EmberTemplateTransaction.CopyDirectory(destination, staged);
                    if (Directory.Exists(source))
                    {
                        Directory.CreateDirectory(staged);
                        if (replace) count += EmberTemplateTransaction.CopyDirectory(source, staged);
                        else
                        {
                            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                            {
                                string target = Path.Combine(staged, file.Substring(source.Length + 1));
                                if (File.Exists(target)) continue;
                                Directory.CreateDirectory(Path.GetDirectoryName(target));
                                File.Copy(file, target); count++;
                            }
                        }
                        if (!editing)
                            foreach (string file in Directory.GetFiles(staged, "*", SearchOption.AllDirectories))
                                if (!IsSkillSourcePath(RelativePath(Path.Combine(stageRoot, "Assets"), file),
                                        EmberAISkillInstaller.TemplateSourceDirectory))
                                    RewriteVersionMarker(file, template.version, template.frameworkVersion);
                    }
                    targets.Add(new TemplateTransactionTarget(staged, destination, true));
                }
                EnsureStoredContentMatchesMetadata(template, sourceAssets, "部署");
                if (editing)
                {
                    string stagedRecord = Path.Combine(stageRoot, "editing.json");
                    EmberTemplateTransaction.WriteJson(stagedRecord, CreateEditingRecord(template));
                    targets.Add(new TemplateTransactionTarget(stagedRecord, Path.Combine(projectRoot, EditingRecordPath)));
                }
                else AddDeploymentRecord(targets, stageRoot, projectRoot, template);
                if (skills != null)
                {
                    AddPreparedSkills(targets, skills, true);
                    EmberAISkillInstaller.ValidateTemplatePreview(skills);
                }
                EmberTemplateTransaction.CommitPreparedTargets(targets, faultInjector);
                return count;
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
                AssetDatabase.AllowAutoRefresh();
            }
        }
        #endregion
    }
}
