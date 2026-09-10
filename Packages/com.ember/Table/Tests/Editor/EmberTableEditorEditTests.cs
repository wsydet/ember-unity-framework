// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor.Tests
{
    public sealed class EmberTableEditorEditTests
    {
        private const string RowScriptPath =
            "Packages/com.ember/Table/Tests/Editor/EditorTableTestRow.cs";
        private const string SourcePath =
            "Packages/com.ember/Table/Tests/Editor/editor-test.etable.csv";

        [Test]
        public void CsvParserSupportsBomCrLfQuotesEscapesDelimiterAndCellNewline()
        {
            const string crlf = "id,text,note\r\nA,\"x,y\",\"line1\r\nline2\"\r\nB,\"x\"\"y\",end\r\n";
            byte[] source = new UTF8Encoding(true).GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(crlf))
                .ToArray();

            Assert.That(EmberDelimitedTextParser.TryParse(
                source, ',', "memory.etable.csv", out EmberDelimitedDocument document, out _), Is.True);
            Assert.That(document.Rows.Count, Is.EqualTo(3));
            Assert.That(document.Rows[1].Cells[1].Value, Is.EqualTo("x,y"));
            Assert.That(document.Rows[1].Cells[2].Value, Is.EqualTo("line1\nline2"));
            Assert.That(document.Rows[2].Cells[1].Value, Is.EqualTo("x\"y"));

            string lf = crlf.Replace("\r\n", "\n");
            Assert.That(EmberDelimitedTextParser.TryParse(
                Encoding.UTF8.GetBytes(lf), ',', "memory.etable.csv", out EmberDelimitedDocument lfDocument, out _), Is.True);
            Assert.That(ToMatrix(document), Is.EqualTo(ToMatrix(lfDocument)));
        }

        [Test]
        public void TsvParserAndSyntaxDiagnosticsArePrecise()
        {
            Assert.That(EmberDelimitedTextParser.TryParse(
                Encoding.UTF8.GetBytes("id\ttext\nA\t\"x\ty\""),
                '\t',
                "memory.etable.tsv",
                out EmberDelimitedDocument document,
                out _), Is.True);
            Assert.That(document.Rows[1].Cells[1].Value, Is.EqualTo("x\ty"));

            Assert.That(EmberDelimitedTextParser.TryParse(
                Encoding.UTF8.GetBytes("id,text\nA,\"unterminated"),
                ',',
                "bad.etable.csv",
                out _,
                out EmberTableDiagnostic diagnostic), Is.False);
            Assert.That(diagnostic.Code, Is.EqualTo(EmberTableErrorCode.CsvSyntax));
            Assert.That(diagnostic.Line, Is.EqualTo(2));
            Assert.That(diagnostic.Column, Is.EqualTo(3));
        }

        [Test]
        public void SchemaFreezesStableIdCanonicalOrderAndImmutableConstructor()
        {
            Assert.That(EmberTableSchemaAnalyzer.TryAnalyze(
                typeof(EditorTableTestRow),
                "editor_test",
                "row.cs",
                out EmberTableSchema schema,
                out IReadOnlyList<EmberTableDiagnostic> diagnostics), Is.True, Join(diagnostics));

            Assert.That(schema.RowTypeId, Is.EqualTo("Ember.Table.Editor.Tests:Ember.Table.Editor.Tests.EditorTableTestRow"));
            Assert.That(schema.Columns.Select(item => item.ColumnName),
                Is.EqualTo(new[] { "id", "amount", "enabled", "optional", "rank" }));
            Assert.That(schema.ConstructorColumns.Select(item => item.ColumnName),
                Is.EqualTo(new[] { "id", "enabled", "amount", "rank", "optional" }));
            // V1 用 i32 -1 编码“无引用”的 null 元数据，不能与空字符串混同。
            Assert.That(schema.SchemaHash,
                Is.EqualTo("a3b03e21fe3bc09bc42a41ee4ae650583f1552b5eb5ba318af508440faccc08c"));
        }

        [Test]
        public void StrictValueConversionUsesInvariantCultureAndRejectsFallbacks()
        {
            var decimalColumn = Column(typeof(decimal), "amount");
            var boolColumn = Column(typeof(bool), "enabled");
            var byteColumn = Column(typeof(byte), "count");
            var enumColumn = Column(typeof(EditorTableTestRank), "rank");
            var nullableColumn = Column(typeof(int?), "optional");
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                Assert.That(EmberTableValueCodec.TryParse("1.5", decimalColumn, out object number, out _), Is.True);
                Assert.That(number, Is.EqualTo(1.5m));
                Assert.That(EmberTableValueCodec.TryParse("TRUE", boolColumn, out object boolean, out _), Is.True);
                Assert.That(boolean, Is.EqualTo(true));
                Assert.That(EmberTableValueCodec.TryParse("1", boolColumn, out _, out _), Is.False);
                Assert.That(EmberTableValueCodec.TryParse("256", byteColumn, out _, out _), Is.False);
                Assert.That(EmberTableValueCodec.TryParse("Missing", enumColumn, out _, out _), Is.False);
                Assert.That(EmberTableValueCodec.TryParse(string.Empty, nullableColumn, out object nullable, out _), Is.True);
                Assert.That(nullable, Is.Null);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void DefinitionValidationAndBakeAreDeterministic()
        {
            EmberTableDefinition definition = CreateDefinition();
            try
            {
                EmberTableValidationResult validation = EmberTableValidationService.Validate(definition);
                Assert.That(validation.Succeeded, Is.True, Join(validation.Diagnostics));
                Assert.That(validation.Tables.Count, Is.EqualTo(1));
                EmberTableValidatedData table = validation.Tables[0];
                Assert.That(table.Rows.Count, Is.EqualTo(2));
                Assert.That(table.Rows[0][0], Is.EqualTo("alpha"));
                Assert.That(table.Rows[1][0], Is.EqualTo("Beta"));
                Assert.That(table.SourceHash,
                    Is.EqualTo("ceaa45f271f1a344751349c055883b7ff9b3f711ea007c886abed1ef3085148e"));

                Assert.That(EmberTableBaker.TryBake(table, out EmberTableBakeArtifact first, out EmberTableDiagnostic error), Is.True, error?.ToString());
                Assert.That(EmberTableBaker.TryBake(table, out EmberTableBakeArtifact second, out error), Is.True, error?.ToString());
                Assert.That(first.FileBytes, Is.EqualTo(second.FileBytes));

                IReadOnlyList<EmberTableArtifactContent> code = EmberTableCodeGenerator.Generate(validation.Tables);
                string binding = Encoding.UTF8.GetString(code.First(item => item.AssetPath.EndsWith("TableBinding.g.cs")).Bytes);
                Assert.That(binding, Does.Contain("new global::Ember.Table.Editor.Tests.EditorTableTestRow"));
                Assert.That(binding, Does.Not.Contain("Activator"));
                Assert.That(binding, Does.Not.Contain("Reflection"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ExplorerBuildsTypedPreviewLiteralsAndCopyableUsageCode()
        {
            EmberTableDefinition definition = CreateDefinition();
            try
            {
                var serialized = new SerializedObject(definition);
                SerializedProperty indexes = serialized.FindProperty("_secondaryIndexes");
                indexes.arraySize = 1;
                SerializedProperty index = indexes.GetArrayElementAtIndex(0);
                index.FindPropertyRelative("_name").stringValue = "by_rank";
                index.FindPropertyRelative("_columnName").stringValue = "rank";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EmberTableValidationResult validation = EmberTableValidationService.Validate(definition);
                Assert.That(validation.Succeeded, Is.True, Join(validation.Diagnostics));
                EmberTableValidatedData table = validation.Tables.Single();
                string code = EmberTableExplorerPresenter.BuildUsageCode(
                    definition,
                    table.Schema,
                    table.Rows);

                Assert.That(code, Does.Contain(
                    "TryGetModule<global::Game.Module.GameTableModule>(out var tableModule)"));
                Assert.That(code, Does.Contain(
                    "TryGetTable<global::Ember.Table.Editor.Tests.EditorTableTestRow>(\"editor_test\", out var table)"));
                Assert.That(code, Does.Contain("table.TryGet(\"alpha\", out var row)"));
                Assert.That(code, Does.Contain("var amountValue = row.@Amount; // amount : decimal"));
                Assert.That(code, Does.Contain(
                    "TryGetIndex<global::Ember.Table.Editor.Tests.EditorTableTestRank>(\"by_rank\", out var index0)"));
                Assert.That(
                    EmberTableExplorerPresenter.FormatCSharpLiteral("line1\n\"line2\"", typeof(string)),
                    Is.EqualTo("\"line1\\n\\\"line2\\\"\""));
                Assert.That(
                    EmberTableExplorerPresenter.FormatCSharpLiteral("a\0b\u001f", typeof(string)),
                    Is.EqualTo("\"a\\0b\\u001F\""));
                Assert.That(
                    EmberTableExplorerPresenter.FormatCSharpLiteral(123.45m, typeof(decimal)),
                    Is.EqualTo("123.45m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SourceHashIgnoresBomAndPhysicalLineEndings()
        {
            byte[] bomCrLf = new UTF8Encoding(true).GetPreamble()
                .Concat(Encoding.UTF8.GetBytes("id,text\r\nA,\"x\r\ny\"\r\n"))
                .ToArray();
            byte[] lf = Encoding.UTF8.GetBytes("id,text\nA,\"x\ny\"\n");
            Assert.That(EmberDelimitedTextParser.TryParse(bomCrLf, ',', "a", out EmberDelimitedDocument left, out _), Is.True);
            Assert.That(EmberDelimitedTextParser.TryParse(lf, ',', "b", out EmberDelimitedDocument right, out _), Is.True);
            byte[] leftHash = EmberTableValidationService.ComputeSourceHash(ToMatrix(left));
            byte[] rightHash = EmberTableValidationService.ComputeSourceHash(ToMatrix(right));
            Assert.That(leftHash, Is.EqualTo(rightHash));
            Assert.That(
                EmberTableBinaryFormat.BuildFile("t", "A:R", new byte[32], leftHash, 0, Array.Empty<byte>()),
                Is.EqualTo(EmberTableBinaryFormat.BuildFile("t", "A:R", new byte[32], rightHash, 0, Array.Empty<byte>())));
        }

        [Test]
        public void ValidationReportsMissingDuplicateAndInvalidColumnsWithoutDefaults()
        {
            AssertDefinitionFails(
                "Packages/com.ember/Table/Tests/Editor/editor-test-missing.etable.csv",
                EmberTableErrorCode.MissingColumn);
            AssertDefinitionFails(
                "Packages/com.ember/Table/Tests/Editor/editor-test-duplicate.etable.csv",
                EmberTableErrorCode.DuplicateColumn);
            AssertDefinitionFails(
                "Packages/com.ember/Table/Tests/Editor/editor-test-duplicate-key.etable.csv",
                EmberTableErrorCode.DuplicatePrimaryKey);

            EmberTableDefinition invalid = CreateDefinition(
                "editor_test",
                "Packages/com.ember/Table/Tests/Editor/editor-test-invalid.etable.csv",
                RowScriptPath);
            try
            {
                EmberTableValidationResult result = EmberTableValidationService.Validate(invalid);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Diagnostics.Count(item => item.Code == EmberTableErrorCode.InvalidValue), Is.EqualTo(3));
                Assert.That(result.Diagnostics.All(item => item.Line == 2 && item.Column > 0), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalid);
            }
        }

        [Test]
        public void UnknownColumnPolicyIsExplicitErrorOrWarning()
        {
            EmberTableDefinition definition = CreateDefinition(
                "editor_test",
                "Packages/com.ember/Table/Tests/Editor/editor-test-unknown.etable.csv",
                RowScriptPath);
            try
            {
                EmberTableValidationResult error = EmberTableValidationService.Validate(definition);
                Assert.That(error.Succeeded, Is.False);
                Assert.That(error.Diagnostics.Single(item => item.Code == EmberTableErrorCode.UnknownColumn).Severity,
                    Is.EqualTo(EmberTableDiagnosticSeverity.Error));

                var serialized = new SerializedObject(definition);
                serialized.FindProperty("_unknownColumnPolicy").enumValueIndex =
                    (int)EmberTableUnknownColumnPolicy.Warning;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EmberTableValidationResult warning = EmberTableValidationService.Validate(definition);
                Assert.That(warning.Succeeded, Is.True, Join(warning.Diagnostics));
                Assert.That(warning.Diagnostics.Single(item => item.Code == EmberTableErrorCode.UnknownColumn).Severity,
                    Is.EqualTo(EmberTableDiagnosticSeverity.Warning));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void CrossTableReferencesAcceptExistingAndRejectMissingKeys()
        {
            EmberTableDefinition target = CreateDefinition(
                "reference_target",
                "Packages/com.ember/Table/Tests/Editor/reference-target.etable.csv",
                "Packages/com.ember/Table/Tests/Editor/EditorReferenceTargetRow.cs");
            EmberTableDefinition valid = CreateDefinition(
                "reference_source",
                "Packages/com.ember/Table/Tests/Editor/reference-source-valid.etable.csv",
                "Packages/com.ember/Table/Tests/Editor/EditorReferenceSourceRow.cs");
            EmberTableDefinition missing = CreateDefinition(
                "reference_source",
                "Packages/com.ember/Table/Tests/Editor/reference-source-missing.etable.csv",
                "Packages/com.ember/Table/Tests/Editor/EditorReferenceSourceRow.cs");
            try
            {
                EmberTableValidationResult validResult = EmberTableValidationService.ValidateAll(new[] { target, valid });
                Assert.That(validResult.Succeeded, Is.True, Join(validResult.Diagnostics));

                EmberTableValidationResult missingResult = EmberTableValidationService.ValidateAll(new[] { target, missing });
                Assert.That(missingResult.Succeeded, Is.False);
                Assert.That(missingResult.Diagnostics.Any(item => item.Code == EmberTableErrorCode.MissingReference), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(valid);
                UnityEngine.Object.DestroyImmediate(missing);
            }
        }

        [Test]
        public void ArtifactBatchProtectsPathsOwnershipAndRollsBackTheWholeCommit()
        {
            string root = Path.Combine(Path.GetTempPath(), "EmberTableArtifactTests", Guid.NewGuid().ToString("N"));
            const string firstPath = "Assets/Game/Table/Generated/A.g.cs";
            const string secondPath = "Assets/Game/Table/Generated/B.g.cs";
            const string userPath = "Assets/Game/Table/Generated/User.cs";
            try
            {
                Directory.CreateDirectory(root);
                EmberTableArtifactBatch.ProjectRootOverrideForTests = root;

                Assert.That(EmberTableArtifactBatch.TryPreview(
                    new[] { EmberTableArtifactContent.Text("Assets/Outside.cs", "x") },
                    out _,
                    out EmberTableDiagnostic outside), Is.False);
                Assert.That(outside.Code, Is.EqualTo(EmberTableErrorCode.OutputPathInvalid));

                Assert.That(EmberTableArtifactBatch.TryPreview(
                    new[]
                    {
                        EmberTableArtifactContent.Text(firstPath, "a"),
                        EmberTableArtifactContent.Text(firstPath, "b"),
                    },
                    out _,
                    out EmberTableDiagnostic conflict), Is.False);
                Assert.That(conflict.Code, Is.EqualTo(EmberTableErrorCode.OutputPathConflict));

                string userFullPath = FullPath(root, userPath);
                Directory.CreateDirectory(Path.GetDirectoryName(userFullPath));
                File.WriteAllText(userFullPath, "user");
                Assert.That(EmberTableArtifactBatch.TryPreview(
                    new[] { EmberTableArtifactContent.Text(userPath, "generated") },
                    out _,
                    out EmberTableDiagnostic protectedFile), Is.False);
                Assert.That(protectedFile.Code, Is.EqualTo(EmberTableErrorCode.UserFileProtected));

                var original = new[]
                {
                    EmberTableArtifactContent.Text(firstPath, "old-a"),
                    EmberTableArtifactContent.Text(secondPath, "old-b"),
                };
                Assert.That(EmberTableArtifactBatch.TryCommit(original, out _, out _), Is.True);
                EmberTableArtifactBatch.CommitFailureForTests = _ => true;
                Assert.That(EmberTableArtifactBatch.TryCommit(original, out _, out _), Is.True,
                    "Unchanged artifacts must not be replaced during a full commit.");
                EmberTableArtifactBatch.CommitFailureForTests = null;
                File.WriteAllText(FullPath(root, firstPath) + ".meta", "owned meta");

                EmberTableArtifactBatch.CommitFailureForTests = path => path == secondPath;
                var replacement = new[]
                {
                    EmberTableArtifactContent.Text(firstPath, "new-a"),
                    EmberTableArtifactContent.Text(secondPath, "new-b"),
                };
                Assert.That(EmberTableArtifactBatch.TryCommit(
                    replacement,
                    out _,
                    out EmberTableDiagnostic rolledBack), Is.False);
                Assert.That(rolledBack.Code, Is.EqualTo(EmberTableErrorCode.ArtifactCommitFailed));
                Assert.That(File.ReadAllText(FullPath(root, firstPath)), Is.EqualTo("old-a"));
                Assert.That(File.ReadAllText(FullPath(root, secondPath)), Is.EqualTo("old-b"));

                EmberTableArtifactBatch.CommitFailureForTests = null;
                Assert.That(EmberTableArtifactBatch.TryCommit(
                    new[] { EmberTableArtifactContent.Text(secondPath, "kept-b") },
                    out _,
                    out _), Is.True);
                Assert.That(File.Exists(FullPath(root, firstPath)), Is.False);
                Assert.That(File.Exists(FullPath(root, firstPath) + ".meta"), Is.False);
                Assert.That(File.ReadAllText(userFullPath), Is.EqualTo("user"));
            }
            finally
            {
                EmberTableArtifactBatch.CommitFailureForTests = null;
                EmberTableArtifactBatch.ProjectRootOverrideForTests = null;
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void OwnedPartialCommitOnlyReplacesSelectedArtifactAndPreservesManifest()
        {
            string root = Path.Combine(Path.GetTempPath(), "EmberTablePartialArtifactTests", Guid.NewGuid().ToString("N"));
            const string selectedPath = "Assets/GameResource/Resources/Config/Tables/selected.bytes";
            const string otherPath = "Assets/GameResource/Resources/Config/Tables/other.bytes";
            const string unownedPath = "Assets/GameResource/Resources/Config/Tables/unowned.bytes";
            try
            {
                Directory.CreateDirectory(root);
                EmberTableArtifactBatch.ProjectRootOverrideForTests = root;
                Assert.That(EmberTableArtifactBatch.TryCommit(
                    new[]
                    {
                        EmberTableArtifactContent.Text(selectedPath, "old-selected"),
                        EmberTableArtifactContent.Text(otherPath, "old-other"),
                    },
                    out _,
                    out _), Is.True);
                string manifestBefore = File.ReadAllText(FullPath(root, EmberTableArtifactBatch.MANIFEST_PATH));

                Assert.That(EmberTableArtifactBatch.TryCheckOwnedArtifactsCurrent(
                    new[] { EmberTableArtifactContent.Text(otherPath, "old-other") },
                    out _), Is.True);
                Assert.That(EmberTableArtifactBatch.TryCheckOwnedArtifactsCurrent(
                    new[] { EmberTableArtifactContent.Text(otherPath, "changed-other") },
                    out EmberTableDiagnostic stale), Is.False);
                Assert.That(stale.Code, Is.EqualTo(EmberTableErrorCode.ArtifactStale));

                Assert.That(EmberTableArtifactBatch.TryCommitOwned(
                    new[] { EmberTableArtifactContent.Text(selectedPath, "new-selected") },
                    out IReadOnlyList<EmberTableArtifactAction> actions,
                    out _), Is.True);
                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions[0].Kind, Is.EqualTo(EmberTableArtifactActionKind.Replace));
                Assert.That(File.ReadAllText(FullPath(root, selectedPath)), Is.EqualTo("new-selected"));
                Assert.That(File.ReadAllText(FullPath(root, otherPath)), Is.EqualTo("old-other"));
                Assert.That(
                    File.ReadAllText(FullPath(root, EmberTableArtifactBatch.MANIFEST_PATH)),
                    Is.EqualTo(manifestBefore));

                Assert.That(EmberTableArtifactBatch.TryCommitOwned(
                    new[] { EmberTableArtifactContent.Text(unownedPath, "value") },
                    out _,
                    out EmberTableDiagnostic unowned), Is.False);
                Assert.That(unowned.Code, Is.EqualTo(EmberTableErrorCode.ArtifactStale));
                Assert.That(File.Exists(FullPath(root, unownedPath)), Is.False);

                EmberTableArtifactBatch.CommitFailureForTests = path => path == selectedPath;
                Assert.That(EmberTableArtifactBatch.TryCommitOwned(
                    new[] { EmberTableArtifactContent.Text(selectedPath, "failed-selected") },
                    out _,
                    out EmberTableDiagnostic rolledBack), Is.False);
                Assert.That(rolledBack.Code, Is.EqualTo(EmberTableErrorCode.ArtifactCommitFailed));
                Assert.That(File.ReadAllText(FullPath(root, selectedPath)), Is.EqualTo("new-selected"));
            }
            finally
            {
                EmberTableArtifactBatch.CommitFailureForTests = null;
                EmberTableArtifactBatch.ProjectRootOverrideForTests = null;
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static EmberTableDefinition CreateDefinition()
        {
            return CreateDefinition("editor_test", SourcePath, RowScriptPath);
        }

        private static EmberTableDefinition CreateDefinition(
            string tableId,
            string sourcePath,
            string rowScriptPath)
        {
            var definition = ScriptableObject.CreateInstance<EmberTableDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_tableId").stringValue = tableId;
            serialized.FindProperty("_source").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
            serialized.FindProperty("_rowScript").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MonoScript>(rowScriptPath);
            serialized.FindProperty("_runtimeOutputPath").stringValue =
                "Assets/GameResource/Resources/Config/Tables/" + tableId + ".bytes";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void AssertDefinitionFails(string sourcePath, EmberTableErrorCode code)
        {
            EmberTableDefinition definition = CreateDefinition("editor_test", sourcePath, RowScriptPath);
            try
            {
                EmberTableValidationResult result = EmberTableValidationService.Validate(definition);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Diagnostics.Any(item => item.Code == code), Is.True, Join(result.Diagnostics));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static EmberTableColumnSchema Column(Type type, string name)
        {
            Type storage = Nullable.GetUnderlyingType(type) ?? type;
            string wire = storage.IsEnum ? "enum" : storage.Name;
            return new EmberTableColumnSchema(
                name,
                name,
                type,
                storage,
                wire,
                storage != type,
                false,
                null,
                null);
        }

        private static string FullPath(string root, string assetPath)
        {
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static IReadOnlyList<IReadOnlyList<string>> ToMatrix(EmberDelimitedDocument document)
        {
            return document.Rows
                .Select(row => (IReadOnlyList<string>)row.Cells.Select(cell => cell.Value).ToArray())
                .ToArray();
        }

        private static string Join(IReadOnlyList<EmberTableDiagnostic> diagnostics)
        {
            return string.Join("\n", diagnostics.Select(item => item.ToString()));
        }
    }
}
