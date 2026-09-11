// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMUpdateCheckEditTests
    {
        private string _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = Path.Combine(Path.GetTempPath(), "EmberUPMCheck-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_repository);
            RunGit("init");
        }

        [TearDown]
        public void TearDown()
        {
            if (!Directory.Exists(_repository)) return;
            foreach (var file in Directory.GetFiles(_repository, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(_repository, true);
        }

        [UnityTest]
        public IEnumerator LocalRepository_ReturnsDistinctAnnotatedAndLightweightVersions()
        {
            RunGit("-c user.name=EmberTest -c user.email=test@example.invalid -c commit.gpgsign=false commit --allow-empty -m test");
            RunGit("-c tag.gpgsign=false tag v0.12.4");
            RunGit("-c user.name=EmberTest -c user.email=test@example.invalid -c tag.gpgsign=false tag -a v0.12.5 -m test");
            RunGit("-c tag.gpgsign=false tag unrelated");
            using var check = new EmberUPMUpdateCheck(_repository);
            yield return WaitForCheck(check);
            Assert.IsNull(check.Error);
            CollectionAssert.AreEquivalent(new[] { new Version(0, 12, 4), new Version(0, 12, 5) }, check.Versions);
        }

        [UnityTest]
        public IEnumerator EmptyRepository_DoesNotClaimAlreadyLatest()
        {
            using var check = new EmberUPMUpdateCheck(_repository);
            yield return WaitForCheck(check);
            StringAssert.Contains("未返回可识别", check.Error);
        }

        [UnityTest]
        public IEnumerator MissingRepository_ReportsGitFailure()
        {
            using var check = new EmberUPMUpdateCheck(Path.Combine(_repository, "missing"));
            yield return WaitForCheck(check);
            Assert.IsNotEmpty(check.Error);
            Assert.IsNull(check.Versions);
        }

        [UnityTest]
        public IEnumerator CancelThenRetry_DoesNotPublishCancelledResults()
        {
            using var cancelled = new EmberUPMUpdateCheck(_repository);
            cancelled.Dispose();
            cancelled.Dispose();
            cancelled.Poll();
            Assert.IsNull(cancelled.Versions);
            using var retry = new EmberUPMUpdateCheck(_repository);
            yield return WaitForCheck(retry);
            StringAssert.Contains("未返回可识别", retry.Error);
        }

        [Test]
        public void ExpiredDeadline_ReportsTimeoutAndCanBeDisposedAgain()
        {
            using var check = new EmberUPMUpdateCheck(_repository, TimeSpan.Zero);
            check.Poll();
            Assert.IsTrue(check.IsCompleted);
            StringAssert.Contains("查询超时", check.Error);
            Assert.IsNull(check.Versions);
            check.Poll();
        }

        private static IEnumerator WaitForCheck(EmberUPMUpdateCheck check)
        {
            var clock = Stopwatch.StartNew();
            while (!check.IsCompleted && clock.Elapsed.TotalSeconds < 10)
            {
                check.Poll();
                yield return null;
            }
            Assert.IsTrue(check.IsCompleted, "本地 Git 查询应在 10 秒内结束。");
        }

        private void RunGit(string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = _repository,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            Assert.IsNotNull(process, "测试环境需要 PATH 中的 Git。");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                process.Kill();
                Assert.Fail("本地 Git 测试准备超时。");
            }
            Assert.AreEqual(0, process.ExitCode, stderr.GetAwaiter().GetResult());
            stdout.GetAwaiter().GetResult();
        }
    }
}
