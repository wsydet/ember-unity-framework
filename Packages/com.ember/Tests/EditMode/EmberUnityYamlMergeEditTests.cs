// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ember.Basic;
using Ember.Core.Editor;

using NUnit.Framework;

namespace Ember.UI.Tests
{
    /// <summary>UnityYAMLMerge 适配器的隔离、参数、安全回退与 fake runner 测试。</summary>
    public class EmberUnityYamlMergeEditTests
    {
        #region 内部参数

        private string _testRoot;
        private string _projectRoot;
        private string _contentsRoot;
        private FakeUnityYamlMergeEnvironment _environment;
        private FakeUnityYamlMergeProcessRunner _runner;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "Ember Unity YAML Merge Tests",
                Guid.NewGuid().ToString("N"));
            _projectRoot = Path.Combine(_testRoot, "Project With Spaces");
            _contentsRoot = Path.Combine(_testRoot, "Unity Editor With Spaces", "Data");
            Directory.CreateDirectory(_projectRoot);
            Directory.CreateDirectory(Path.Combine(_contentsRoot, "Tools"));
            WriteFile(GetToolPath(), "fake executable");
            WriteFile(GetRulesPath(), "[rules]\nfake=true\n");

            _environment = new FakeUnityYamlMergeEnvironment(
                _contentsRoot,
                _projectRoot,
                true,
                true);
            _runner = new FakeUnityYamlMergeProcessRunner();
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Test]
        public void Merge_SuccessShouldUseIsolatedHeadlessArgumentsAndReturnMergedBytes()
        {
            var scenes = CreateScenes();
            const string mergedText = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1 &1\nGameObject:\n  m_Name: Merged\n";
            _runner.OnRun = request =>
            {
                WriteFile(request.Arguments[request.Arguments.Count - 1], mergedText);
                var reportIndex = FindArgument(request.Arguments, "-o") + 1;
                WriteFile(request.Arguments[reportIndex], "merge completed");
                return SuccessProcessResult();
            };

            var result = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.IsTrue(result.IsSuccess, result.FailureReason);
            Assert.AreEqual(UnityYamlMergeStatus.Success, result.Status);
            Assert.AreEqual(1, _runner.CallCount);
            Assert.AreEqual(Encoding.UTF8.GetBytes(mergedText), result.MergedBytes);
            Assert.AreEqual(CryptographyUtils.GetMD5(result.MergedBytes), result.MergedHash);
            Assert.IsFalse(string.IsNullOrEmpty(result.ToolVersion));
            Assert.AreEqual(CryptographyUtils.GetMD5File(GetRulesPath()), result.RulesHash);
            Assert.AreEqual(CryptographyUtils.GetMD5("merge completed"), result.ReportDigest);
            StringAssert.Contains("merge completed", result.ReportSummary);

            var request = _runner.LastRequest;
            Assert.AreEqual(GetToolPath(), request.ExecutablePath);
            Assert.IsTrue(request.WorkingDirectory.StartsWith(
                Path.Combine(_projectRoot, "Temp", "EmberSceneMerge-"),
                StringComparison.Ordinal));
            CollectionAssert.AreEqual(
                new[]
                {
                    "merge", "-p", "-h", "--fallback", "none", "--rules",
                    GetRulesPath(), "--describe", "-o"
                },
                request.Arguments.Take(9).ToArray());
            Assert.AreEqual(14, request.Arguments.Count);
            Assert.AreNotEqual(scenes.Old, request.Arguments[10]);
            Assert.AreNotEqual(scenes.Parent, request.Arguments[11]);
            Assert.AreNotEqual(scenes.Child, request.Arguments[12]);
            Assert.IsTrue(request.Arguments[10].StartsWith(
                request.WorkingDirectory,
                StringComparison.Ordinal));
            Assert.AreEqual(EmberUnityYamlMerge.DefaultTimeoutMilliseconds,
                request.TimeoutMilliseconds);
            AssertIsolationWasCleaned();
            StringAssert.Contains("Old", File.ReadAllText(scenes.Old));
            StringAssert.Contains("Parent", File.ReadAllText(scenes.Parent));
            StringAssert.Contains("Child", File.ReadAllText(scenes.Child));
        }

        [Test]
        public void Merge_ForceTextDisabledShouldNotCallRunner()
        {
            var scenes = CreateScenes();
            _environment = new FakeUnityYamlMergeEnvironment(
                _contentsRoot,
                _projectRoot,
                false,
                true);

            var result = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.ForceTextRequired, result.Status);
            Assert.AreEqual(0, _runner.CallCount);
        }

        [Test]
        public void Merge_NonSceneOrInvalidYamlShouldNotCallRunner()
        {
            var scenes = CreateScenes();
            var prefab = Path.Combine(_testRoot, "NotScene.prefab");
            WriteFile(prefab, File.ReadAllText(scenes.Old));

            var notScene = CreateAdapter().Merge(prefab, scenes.Parent, scenes.Child);
            WriteFile(scenes.Old, "not yaml");
            var invalidYaml = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.InvalidInput, notScene.Status);
            Assert.AreEqual(UnityYamlMergeStatus.InvalidInput, invalidYaml.Status);
            Assert.AreEqual(0, _runner.CallCount);
        }

        [Test]
        public void Merge_MissingToolShouldReturnStructuredFallbackWithoutRunner()
        {
            var scenes = CreateScenes();
            File.Delete(GetToolPath());

            var result = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.ToolUnavailable, result.Status);
            Assert.AreEqual(0, _runner.CallCount);
            StringAssert.Contains("UnityYAMLMerge 不存在", result.FailureReason);
        }

        [Test]
        public void Merge_TimeoutShouldReturnFallbackAndCleanIsolation()
        {
            var scenes = CreateScenes();
            _runner.OnRun = _ => new UnityYamlMergeProcessResult(
                true,
                -1,
                true,
                "partial output",
                string.Empty,
                null);

            var result = CreateAdapter().Merge(
                scenes.Old,
                scenes.Parent,
                scenes.Child,
                25);

            Assert.AreEqual(UnityYamlMergeStatus.TimedOut, result.Status);
            Assert.IsTrue(result.TimedOut);
            Assert.AreEqual(1, _runner.CallCount);
            AssertIsolationWasCleaned();
        }

        [Test]
        public void Merge_NonZeroExitShouldReturnWholeSceneConflict()
        {
            var scenes = CreateScenes();
            _runner.OnRun = _ => new UnityYamlMergeProcessResult(
                true,
                1,
                false,
                string.Empty,
                "conflict",
                null);

            var result = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.Conflict, result.Status);
            Assert.AreEqual(1, result.ExitCode);
            Assert.IsNull(result.MergedBytes);
            AssertIsolationWasCleaned();
        }

        [Test]
        public void Merge_MissingOrInvalidOutputShouldReturnStructuredFallback()
        {
            var scenes = CreateScenes();
            _runner.OnRun = _ => SuccessProcessResult();
            var missing = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            _runner.OnRun = request =>
            {
                WriteFile(request.Arguments[request.Arguments.Count - 1], "not unity yaml");
                return SuccessProcessResult();
            };
            var invalid = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.InvalidOutput, missing.Status);
            Assert.AreEqual(UnityYamlMergeStatus.InvalidOutput, invalid.Status);
            Assert.AreEqual(2, _runner.CallCount);
            AssertIsolationWasCleaned();
        }

        [Test]
        public void Merge_OutputConflictMarkersShouldNeverBeAccepted()
        {
            var scenes = CreateScenes();
            _runner.OnRun = request =>
            {
                WriteFile(
                    request.Arguments[request.Arguments.Count - 1],
                    "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n<<<<<<< mine\n=======\n>>>>>>> theirs\n");
                return SuccessProcessResult();
            };

            var result = CreateAdapter().Merge(scenes.Old, scenes.Parent, scenes.Child);

            Assert.AreEqual(UnityYamlMergeStatus.Conflict, result.Status);
            Assert.IsNull(result.MergedBytes);
            AssertIsolationWasCleaned();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private EmberUnityYamlMerge CreateAdapter()
        {
            return new EmberUnityYamlMerge(_runner, _environment);
        }

        private ScenePaths CreateScenes()
        {
            var scenesRoot = Path.Combine(_testRoot, "Formal Scenes");
            var oldScene = Path.Combine(scenesRoot, "Old.unity");
            var parentScene = Path.Combine(scenesRoot, "Parent.unity");
            var childScene = Path.Combine(scenesRoot, "Child.unity");
            WriteScene(oldScene, "Old");
            WriteScene(parentScene, "Parent");
            WriteScene(childScene, "Child");
            return new ScenePaths(oldScene, parentScene, childScene);
        }

        private string GetToolPath()
        {
            return Path.Combine(_contentsRoot, "Tools", "UnityYAMLMerge.exe");
        }

        private string GetRulesPath()
        {
            return Path.Combine(_contentsRoot, "Tools", "mergerules.txt");
        }

        private void AssertIsolationWasCleaned()
        {
            var tempRoot = Path.Combine(_projectRoot, "Temp");
            if (!Directory.Exists(tempRoot)) return;
            Assert.AreEqual(
                0,
                Directory.GetDirectories(tempRoot, "EmberSceneMerge-*").Length,
                "适配器结束后不得遗留场景合并隔离目录。");
        }

        private static int FindArgument(IReadOnlyList<string> arguments, string value)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                if (string.Equals(arguments[i], value, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        private static UnityYamlMergeProcessResult SuccessProcessResult()
        {
            return new UnityYamlMergeProcessResult(
                true,
                0,
                false,
                "success",
                string.Empty,
                null);
        }

        private static void WriteScene(string path, string name)
        {
            WriteFile(
                path,
                $"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1 &1\nGameObject:\n  m_Name: {name}\n");
        }

        private static void WriteFile(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private sealed class ScenePaths
        {
            internal string Old { get; }
            internal string Parent { get; }
            internal string Child { get; }

            internal ScenePaths(string old, string parent, string child)
            {
                Old = old;
                Parent = parent;
                Child = child;
            }
        }

        private sealed class FakeUnityYamlMergeEnvironment : IUnityYamlMergeEnvironment
        {
            internal FakeUnityYamlMergeEnvironment(
                string applicationContentsPath,
                string projectRoot,
                bool isForceText,
                bool isWindowsEditor)
            {
                ApplicationContentsPath = applicationContentsPath;
                ProjectRoot = projectRoot;
                IsForceText = isForceText;
                IsWindowsEditor = isWindowsEditor;
            }

            public string ApplicationContentsPath { get; }
            public string ProjectRoot { get; }
            public bool IsForceText { get; }
            public bool IsWindowsEditor { get; }
        }

        private sealed class FakeUnityYamlMergeProcessRunner : IUnityYamlMergeProcessRunner
        {
            internal int CallCount { get; private set; }
            internal UnityYamlMergeProcessRequest LastRequest { get; private set; }
            internal Func<UnityYamlMergeProcessRequest, UnityYamlMergeProcessResult> OnRun { get; set; }

            public UnityYamlMergeProcessResult Run(UnityYamlMergeProcessRequest request)
            {
                CallCount++;
                LastRequest = request;
                return OnRun?.Invoke(request);
            }
        }

        #endregion
    }
}
