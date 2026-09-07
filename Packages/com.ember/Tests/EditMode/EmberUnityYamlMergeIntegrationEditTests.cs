// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;
using System.Text;

using Ember.Core.Editor;

using NUnit.Framework;

namespace Ember.UI.Tests
{
    /// <summary>
    /// 使用当前 Unity Editor 随附的 UnityYAMLMerge 验证真实 O/N/C 语义行为。
    /// 输入与输出均位于测试 Temp，不修改模板或项目正式资源。
    /// </summary>
    [Category("UnityYAMLMergeIntegration")]
    public class EmberUnityYamlMergeIntegrationEditTests
    {
        #region 内部参数

        private const string GameBootTransformHeader = "--- !u!4 &1027624532";
        private const string MainCameraHeader = "--- !u!20 &330585545";
        private const string PositionPrefix = "  m_LocalPosition:";
        private const string OrthographicPrefix = "  orthographic:";

        private string _testRoot;
        private string _originalScene;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "EmberUnityYamlMergeIntegrationTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(EmberProjectSetup).Assembly);
            Assert.IsNotNull(package, "无法解析 com.ember 包路径。");
            var sourceScenePath = Path.Combine(
                package.resolvedPath,
                "Templates~",
                "base",
                "Assets",
                "Game",
                "Scenes",
                "FrameworkScene.unity");
            Assert.IsTrue(File.Exists(sourceScenePath), "缺少集成测试场景基线。 ");
            _originalScene = NormalizeLineEndings(File.ReadAllText(sourceScenePath));
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Test]
        [Timeout(60000)]
        public void RealMerge_DifferentSceneObjectsShouldPreserveBothSidesAndPrefabDocuments()
        {
            const string parentPosition =
                "  m_LocalPosition: {x: 101, y: 102, z: 103}";
            var childOrthographic = GetDifferentScalarLine(
                _originalScene,
                MainCameraHeader,
                OrthographicPrefix,
                "0",
                "1");
            var parent = ReplaceDocumentLine(
                _originalScene,
                GameBootTransformHeader,
                PositionPrefix,
                parentPosition);
            var child = ReplaceDocumentLine(
                _originalScene,
                MainCameraHeader,
                OrthographicPrefix,
                childOrthographic);

            var result = Merge(_originalScene, parent, child);

            AssertMergeSucceeded(result);
            var merged = NormalizeLineEndings(Encoding.UTF8.GetString(result.MergedBytes));
            AssertDocumentContains(merged, GameBootTransformHeader, parentPosition);
            AssertDocumentContains(merged, MainCameraHeader, childOrthographic);
            StringAssert.Contains("PrefabInstance:", merged);
            StringAssert.Contains(" stripped\n", merged);
            Assert.IsFalse(merged.Contains("<<<<<<<", StringComparison.Ordinal));
            Assert.IsFalse(merged.Contains(">>>>>>>", StringComparison.Ordinal));
        }

        [Test]
        [Timeout(60000)]
        public void RealMerge_DifferentValuesForSamePropertyShouldReturnConflict()
        {
            var parent = ReplaceDocumentLine(
                _originalScene,
                GameBootTransformHeader,
                PositionPrefix,
                "  m_LocalPosition: {x: 201, y: 202, z: 203}");
            var child = ReplaceDocumentLine(
                _originalScene,
                GameBootTransformHeader,
                PositionPrefix,
                "  m_LocalPosition: {x: 301, y: 302, z: 303}");

            var result = Merge(_originalScene, parent, child);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(UnityYamlMergeStatus.Conflict, result.Status);
            Assert.IsNull(result.MergedBytes);
        }

        [Test]
        [Timeout(60000)]
        public void RealMerge_IdenticalPropertyChangeShouldBeStableWithoutConflict()
        {
            const string identicalPosition =
                "  m_LocalPosition: {x: 401, y: 402, z: 403}";
            var changed = ReplaceDocumentLine(
                _originalScene,
                GameBootTransformHeader,
                PositionPrefix,
                identicalPosition);

            var result = Merge(_originalScene, changed, changed);

            AssertMergeSucceeded(result);
            var merged = NormalizeLineEndings(Encoding.UTF8.GetString(result.MergedBytes));
            AssertDocumentContains(merged, GameBootTransformHeader, identicalPosition);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private UnityYamlMergeResult Merge(string oldScene, string parentScene, string childScene)
        {
            var oldPath = WriteScene("O.unity", oldScene);
            var parentPath = WriteScene("N.unity", parentScene);
            var childPath = WriteScene("C.unity", childScene);
            return new EmberUnityYamlMerge().Merge(oldPath, parentPath, childPath);
        }

        private string WriteScene(string fileName, string content)
        {
            var path = Path.Combine(_testRoot, fileName);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        private static string ReplaceDocumentLine(
            string scene,
            string documentHeader,
            string linePrefix,
            string replacement)
        {
            GetDocumentRange(scene, documentHeader, out var start, out var end);
            var lineStart = FindUniqueLine(scene, start, end, linePrefix);
            var lineEnd = scene.IndexOf('\n', lineStart);
            if (lineEnd < 0 || lineEnd > end) lineEnd = end;
            return scene.Substring(0, lineStart)
                + replacement
                + scene.Substring(lineEnd);
        }

        private static string GetDifferentScalarLine(
            string scene,
            string documentHeader,
            string linePrefix,
            string firstValue,
            string secondValue)
        {
            GetDocumentRange(scene, documentHeader, out var start, out var end);
            var lineStart = FindUniqueLine(scene, start, end, linePrefix);
            var lineEnd = scene.IndexOf('\n', lineStart);
            if (lineEnd < 0 || lineEnd > end) lineEnd = end;
            var current = scene.Substring(lineStart, lineEnd - lineStart).Trim();
            var firstLine = (linePrefix + " " + firstValue).Trim();
            return linePrefix + " " + (string.Equals(current, firstLine, StringComparison.Ordinal)
                ? secondValue
                : firstValue);
        }

        private static void AssertDocumentContains(
            string scene,
            string documentHeader,
            string expected)
        {
            GetDocumentRange(scene, documentHeader, out var start, out var end);
            var document = scene.Substring(start, end - start);
            StringAssert.Contains(expected, document);
        }

        private static int FindUniqueLine(
            string scene,
            int documentStart,
            int documentEnd,
            string linePrefix)
        {
            var lineStart = scene.IndexOf(linePrefix, documentStart, StringComparison.Ordinal);
            if (lineStart < 0 || lineStart >= documentEnd)
                throw new InvalidOperationException($"文档中缺少字段：{linePrefix}");
            var duplicate = scene.IndexOf(linePrefix, lineStart + linePrefix.Length,
                StringComparison.Ordinal);
            if (duplicate >= 0 && duplicate < documentEnd)
                throw new InvalidOperationException($"文档中的字段不唯一：{linePrefix}");
            return lineStart;
        }

        private static void GetDocumentRange(
            string scene,
            string documentHeader,
            out int start,
            out int end)
        {
            start = scene.IndexOf(documentHeader, StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidOperationException($"场景基线缺少文档：{documentHeader}");
            end = scene.IndexOf("\n--- !u!", start + documentHeader.Length,
                StringComparison.Ordinal);
            if (end < 0) end = scene.Length;
        }

        private static void AssertMergeSucceeded(UnityYamlMergeResult result)
        {
            Assert.IsNotNull(result);
            Assert.IsTrue(
                result.IsSuccess,
                result.FailureReason + "\n" + result.StandardError);
            Assert.IsNotNull(result.MergedBytes);
            Assert.IsNotEmpty(result.MergedHash);
            Assert.IsNotEmpty(result.ToolVersion);
            Assert.IsNotEmpty(result.RulesHash);
        }

        private static string NormalizeLineEndings(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        }

        #endregion
    }
}
