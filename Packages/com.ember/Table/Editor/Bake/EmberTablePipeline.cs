// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    public sealed class EmberTablePipelineResult
    {
        public bool Succeeded { get; }
        public IReadOnlyList<EmberTableDiagnostic> Diagnostics { get; }
        public IReadOnlyList<EmberTableArtifactAction> Actions { get; }

        public EmberTablePipelineResult(
            bool succeeded,
            IList<EmberTableDiagnostic> diagnostics,
            IReadOnlyList<EmberTableArtifactAction> actions)
        {
            Succeeded = succeeded;
            Diagnostics = new List<EmberTableDiagnostic>(diagnostics).AsReadOnly();
            Actions = actions ?? Array.Empty<EmberTableArtifactAction>();
        }
    }

    /// <summary>全量校验、确定性烘焙、回读验证、生成和单事务提交入口。</summary>
    public static class EmberTablePipeline
    {
        public static IReadOnlyList<EmberTableDefinition> FindAllDefinitions()
        {
            return AssetDatabase.FindAssets("t:EmberTableDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<EmberTableDefinition>)
                .Where(item => item)
                .ToList()
                .AsReadOnly();
        }

        public static EmberTableValidationResult ValidateAll()
        {
            return EmberTableValidationService.ValidateAll(FindAllDefinitions().ToList());
        }

        public static EmberTableValidationResult Validate(EmberTableDefinition definition)
        {
            if (!definition) return EmberTableValidationService.Validate(definition);
            var definitions = FindAllDefinitions().ToList();
            if (!definitions.Contains(definition)) definitions.Add(definition);
            EmberTableValidationResult all = EmberTableValidationService.ValidateAll(definitions);
            var diagnostics = all.Diagnostics
                .Where(item => string.Equals(item.TableId, definition.TableId, StringComparison.Ordinal))
                .ToList();
            var tables = all.Tables.Where(item => ReferenceEquals(item.Definition, definition)).ToList();
            bool succeeded = tables.Count == 1
                             && diagnostics.All(item => item.Severity != EmberTableDiagnosticSeverity.Error);
            return new EmberTableValidationResult(succeeded, diagnostics, tables);
        }

        public static EmberTablePipelineResult PreviewAll()
        {
            return Build(false);
        }

        public static EmberTablePipelineResult BakeAndGenerateAll()
        {
            return Build(true);
        }

        /// <summary>只重写当前表二进制；Schema、路径或表清单改变时要求先执行全量生成。</summary>
        public static EmberTablePipelineResult BakeCurrent(EmberTableDefinition definition)
        {
            EmberTableValidationResult validation = ValidateAll();
            var diagnostics = new List<EmberTableDiagnostic>(validation.Diagnostics);
            if (!definition || !validation.Succeeded)
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());

            EmberTableValidatedData table = validation.Tables.FirstOrDefault(item =>
                ReferenceEquals(item.Definition, definition));
            if (table == null)
            {
                diagnostics.Add(new EmberTableDiagnostic(
                    EmberTableDiagnosticSeverity.Error,
                    EmberTableErrorCode.InvalidRowType,
                    "The selected Table Definition is not part of the validated project catalog.",
                    definition.TableId,
                    AssetDatabase.GetAssetPath(definition)));
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());
            }
            int outputOwners = validation.Tables.Count(item => string.Equals(
                item.Definition.RuntimeOutputPath,
                definition.RuntimeOutputPath,
                StringComparison.OrdinalIgnoreCase));
            if (outputOwners != 1)
            {
                diagnostics.Add(new EmberTableDiagnostic(
                    EmberTableDiagnosticSeverity.Error,
                    EmberTableErrorCode.OutputPathConflict,
                    "The selected Runtime output path is shared by more than one Table Definition.",
                    definition.TableId,
                    definition.RuntimeOutputPath));
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());
            }

            IReadOnlyList<EmberTableArtifactContent> generated = EmberTableCodeGenerator.Generate(validation.Tables);
            if (!EmberTableArtifactBatch.TryCheckOwnedArtifactsCurrent(
                    generated.ToList(),
                    out EmberTableDiagnostic currentDiagnostic))
            {
                diagnostics.Add(currentDiagnostic);
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());
            }
            if (!EmberTableBaker.TryBake(table, out EmberTableBakeArtifact artifact, out EmberTableDiagnostic bakeDiagnostic))
            {
                diagnostics.Add(bakeDiagnostic);
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());
            }

            var contents = new[]
            {
                new EmberTableArtifactContent(definition.RuntimeOutputPath, artifact.FileBytes),
            };
            bool success = EmberTableArtifactBatch.TryCommitOwned(
                contents,
                out IReadOnlyList<EmberTableArtifactAction> actions,
                out EmberTableDiagnostic commitDiagnostic);
            if (!success && commitDiagnostic != null) diagnostics.Add(commitDiagnostic);
            return new EmberTablePipelineResult(success, diagnostics, actions);
        }

        public static bool IsArtifactCurrent(
            EmberTableValidatedData table,
            out string reason)
        {
            reason = null;
            if (!EmberTableBaker.TryBake(table, out EmberTableBakeArtifact baked, out EmberTableDiagnostic diagnostic))
            {
                reason = diagnostic.ToString();
                return false;
            }
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                projectRoot,
                table.Definition.RuntimeOutputPath.Replace('/', System.IO.Path.DirectorySeparatorChar)));
            if (!System.IO.File.Exists(path))
            {
                reason = "Baked artifact is missing.";
                return false;
            }
            byte[] current = System.IO.File.ReadAllBytes(path);
            if (!BytesEqual(current, baked.FileBytes))
            {
                reason = "Source, Schema or baked bytes changed.";
                return false;
            }
            return true;
        }

        private static EmberTablePipelineResult Build(bool commit)
        {
            EmberTableValidationResult validation = ValidateAll();
            var diagnostics = new List<EmberTableDiagnostic>(validation.Diagnostics);
            if (!validation.Succeeded)
                return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());

            var contents = new List<EmberTableArtifactContent>();
            for (int i = 0; i < validation.Tables.Count; i++)
            {
                EmberTableValidatedData table = validation.Tables[i];
                if (!EmberTableBaker.TryBake(table, out EmberTableBakeArtifact artifact, out EmberTableDiagnostic diagnostic))
                {
                    diagnostics.Add(diagnostic);
                    return new EmberTablePipelineResult(false, diagnostics, Array.Empty<EmberTableArtifactAction>());
                }
                contents.Add(new EmberTableArtifactContent(table.Definition.RuntimeOutputPath, artifact.FileBytes));
            }
            contents.AddRange(EmberTableCodeGenerator.Generate(validation.Tables));

            IReadOnlyList<EmberTableArtifactAction> actions;
            EmberTableDiagnostic batchDiagnostic;
            bool success = commit
                ? EmberTableArtifactBatch.TryCommit(contents, out actions, out batchDiagnostic)
                : EmberTableArtifactBatch.TryPreview(contents, out actions, out batchDiagnostic);
            if (!success && batchDiagnostic != null) diagnostics.Add(batchDiagnostic);
            return new EmberTablePipelineResult(success, diagnostics, actions);
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }
}
