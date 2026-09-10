// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Core.Editor;

namespace Ember.Table.Editor
{
    /// <summary>把表 Schema、源数据、引用和生成物陈旧状态接入项目中心校验。</summary>
    public sealed class EmberTableProjectValidator : IEmberProjectValidator
    {
        public string DisplayName => "配置表";

        public void Validate(EmberProjectValidationReport report)
        {
            EmberTableValidationResult validation = EmberTablePipeline.ValidateAll();
            for (int i = 0; i < validation.Diagnostics.Count; i++)
            {
                EmberTableDiagnostic diagnostic = validation.Diagnostics[i];
                report.Add(
                    ToSeverity(diagnostic.Severity),
                    "配置表",
                    diagnostic.ToString(),
                    diagnostic.FilePath,
                    "在 Ember/配置表中心修复并重新执行全量烘焙与生成。");
            }
            if (!validation.Succeeded) return;

            EmberTablePipelineResult preview = EmberTablePipeline.PreviewAll();
            for (int i = 0; i < preview.Diagnostics.Count; i++)
            {
                EmberTableDiagnostic diagnostic = preview.Diagnostics[i];
                report.Add(EmberValidationSeverity.Error, "配置表生成物", diagnostic.ToString(), diagnostic.FilePath);
            }
            if (!preview.Succeeded) return;

            int changed = 0;
            for (int i = 0; i < preview.Actions.Count; i++)
            {
                EmberTableArtifactAction action = preview.Actions[i];
                if (action.Kind == EmberTableArtifactActionKind.Unchanged) continue;
                changed++;
                report.Add(
                    EmberValidationSeverity.Warning,
                    "配置表生成物",
                    $"{action.Kind}: {action.AssetPath}",
                    action.AssetPath,
                    "预览确认后执行“烘焙并生成全部”。");
            }
            if (changed == 0)
                report.Add(EmberValidationSeverity.Passed, "配置表", "Schema、源数据、引用和全部生成物一致。");
        }

        private static EmberValidationSeverity ToSeverity(EmberTableDiagnosticSeverity severity)
        {
            if (severity == EmberTableDiagnosticSeverity.Error) return EmberValidationSeverity.Error;
            if (severity == EmberTableDiagnosticSeverity.Warning) return EmberValidationSeverity.Warning;
            return EmberValidationSeverity.Information;
        }
    }
}
