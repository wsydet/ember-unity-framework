using System;
using System.Collections.Generic;
using System.IO;
using Ember.UPMManager.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    public static partial class EmberProjectSetup
    {
        #region 内部方法
        internal static void CommitExtensionDiscoveryPruning(EmberAISkillInstaller.TemplatePreview preview)
        {
            var targets = new List<TemplateTransactionTarget>();
            AddPreparedSkills(targets, preview, true);
            EmberAISkillInstaller.ValidateTemplatePreview(preview);
            if (targets.Count > 0) EmberTemplateTransaction.CommitPreparedTargets(targets);
        }
        internal static EmberAISkillInstaller.TemplatePreview PreviewExtensionSkills(EmberExtensionProject.Configuration config)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            var editing = IsEmbeddedPackage() ? GetEditingTemplate() : null;
            var deployed = IsEmbeddedPackage() ? null : GetActiveDeployedTemplate();
            string id = editing?.templateId ?? deployed?.templateId;
            if (string.IsNullOrEmpty(id))
            {
                if (EmberAISkillInstaller.HasTemplateSkillOwnership(root)) throw new InvalidOperationException("缺少正式模板身份，请先恢复模板记录。");
                return null;
            }
            var preview = EmberAISkillInstaller.PreviewExtensionComposition(root, Application.dataPath,
                new EmberAISkillInstaller.TemplateContext(id, editing?.templateVersion ?? deployed.version,
                    editing?.contentHash ?? deployed.contentHash, id, GetFrameworkVersion(), HasSkillEuiApi(), editing != null,
                    extensions: EmberExtensionProject.Identities(EmberExtensionProject.ResolveConfiguration(config, id, EmberExtensionProject.CurrentTemplateVersion)),
                    moduleScopes: EmberExtensionDocuments.EnabledScopes));
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, EditingRecordPath);
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, DeployedRecordsPath);
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, EmberExtensionProject.ConfigurationPath);
            EmberAISkillInstaller.BindReadOnlyInput(preview, EmberExtensionProject.DefaultsPath);
            return preview;
        }

        internal static void CommitExtensionConfiguration(EmberExtensionProject.Configuration config)
        {
            var preview = PreviewExtensionSkills(config);
            if (preview != null) EmberAISkillInstaller.ValidateTemplatePreview(preview);
            string root = Directory.GetParent(Application.dataPath).FullName;
            string stage = Path.Combine(root, "Temp", "EmberExtension-" + Guid.NewGuid().ToString("N") + "~");
            var targets = new List<TemplateTransactionTarget>();
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                string file = Path.Combine(stage, "configuration.json");
                EmberTemplateTransaction.WriteJson(file, config);
                targets.Add(new TemplateTransactionTarget(file, Path.Combine(root, EmberExtensionProject.ConfigurationPath)));
                if (preview != null) AddPreparedSkills(targets, preview, true);
                if (preview != null) EmberAISkillInstaller.ValidateTemplatePreview(preview);
                EmberTemplateTransaction.CommitPreparedTargets(targets);
            }
            finally { TryCleanTransactionPath(stage); AssetDatabase.AllowAutoRefresh(); }
        }
        #endregion
    }
}
