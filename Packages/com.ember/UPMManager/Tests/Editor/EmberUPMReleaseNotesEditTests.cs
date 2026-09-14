using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMReleaseNotesEditTests
    {
        private string _repository;
        private string _cache;

        [SetUp]
        public void SetUp()
        {
            _repository = Path.Combine(Path.GetTempPath(), "EmberNotesRepo-" + Guid.NewGuid().ToString("N"));
            _cache = Path.Combine(Path.GetTempPath(), "EmberNotesCache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_repository);
            Git("init");
        }

        [TearDown]
        public void TearDown()
        {
            RemoveOwnedDirectory(_repository);
            // Request cleanup runs in the background and may still be releasing its own files.
            try { RemoveOwnedDirectory(_cache); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        [Test]
        public void Parser_SeparatesVersionsAndIgnoresUnreleasedAndFencedHeadings()
        {
            var notes = EmberUPMReleaseNotes.Parse("\uFEFF# Changelog\r\n## [Unreleased]\r\n未发布\r\n" +
                "## [0.12.9] - 2026-09-14\r\n- **修复场景**\r\n### 注意\r\n内容\r\n```text\r\n" +
                "## [9.9.9]\r\n```\r\n## [0.12.8]\r\n- 字体\r\n## 附录\r\n不属于版本\r\n");
            Assert.AreEqual(2, notes.Count);
            StringAssert.Contains("修复场景", notes[new Version(0, 12, 9)]);
            StringAssert.Contains("## [9.9.9]", notes[new Version(0, 12, 9)]);
            Assert.AreEqual("- 字体", notes[new Version(0, 12, 8)]);
            Assert.IsFalse(notes.ContainsKey(new Version(0, 12, 90)));
            Assert.IsFalse(notes.ContainsKey(new Version(9, 9, 9)));
            Assert.IsEmpty(EmberUPMReleaseNotes.Parse("## [Unreleased]\n未发布"));
            StringAssert.DoesNotContain("**", EmberUPMReleaseNotes.ToDisplayText(notes[new Version(0, 12, 9)]));
        }

        [UnityTest]
        public IEnumerator ReadsTaggedChineseChangelogWithoutUsingLaterUnreleasedChanges()
        {
            PrepareRelease(true);
            File.AppendAllText(Path.Combine(_repository, "Packages/com.ember/CHANGELOG.md"), "\n后来未提交的错误内容");
            using var request = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 9), _cache);
            yield return Wait(request);
            Assert.IsNull(request.Error);
            Assert.AreEqual("- 修复刷新后无法打开场景。", request.Notes[new Version(0, 12, 9)]);
            Assert.AreEqual("- 增加符号字体。", request.Notes[new Version(0, 12, 8)]);
            Assert.IsTrue(File.Exists(Path.Combine(_repository, "Packages/com.ember/CHANGELOG.md")));
        }

        [UnityTest]
        public IEnumerator MissingChangelogReportsFailureAndCanRetry()
        {
            PrepareRelease(false);
            using (var missing = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 9), _cache))
            {
                yield return Wait(missing);
                Assert.IsNotEmpty(missing.Error);
                Assert.IsNull(missing.Notes);
            }
            WriteChangelog();
            Git("add .");
            Git("-c user.name=EmberTest -c user.email=test@example.invalid -c commit.gpgsign=false commit -m notes");
            Git("-c tag.gpgsign=false tag v0.12.10");
            using var retry = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 10), _cache);
            yield return Wait(retry);
            Assert.IsNull(retry.Error);
            Assert.IsTrue(retry.Notes.ContainsKey(new Version(0, 12, 9)));
            Assert.IsFalse(retry.Notes.ContainsKey(new Version(0, 12, 10)), "Missing version must not borrow another version's notes.");
        }

        [UnityTest]
        public IEnumerator CancellationAndTimeoutDoNotPublishNotesAndAllowNewRequest()
        {
            PrepareRelease(true);
            using (var cancelled = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 9), _cache))
            {
                cancelled.Dispose();
                cancelled.Poll();
                Assert.IsNull(cancelled.Notes);
            }
            using (var expired = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 9), _cache, TimeSpan.Zero))
            {
                expired.Poll();
                Assert.IsTrue(expired.IsCompleted);
                StringAssert.Contains("超时", expired.Error);
                Assert.IsNull(expired.Notes);
            }
            using var retry = new EmberUPMReleaseNotes(_repository, new Version(0, 12, 9), _cache);
            yield return Wait(retry);
            Assert.IsNull(retry.Error);
        }

        [UnityTest]
        public IEnumerator AsyncCompletion_RepaintsNotesWithoutClearingUpgradeCandidates()
        {
            PrepareRelease(true);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var manager = ScriptableObject.CreateInstance<EmberUPMManager>();
            var host = ScriptableObject.CreateInstance<EmberUPMUpgradePromptTestWindow>();
            var type = typeof(EmberUPMManager);
            var version = new Version(0, 12, 9);
            try
            {
                var candidates = (List<Version>)type.GetField("_newerTags", fields).GetValue(manager);
                candidates.Add(version);
                type.GetField("_releaseNotesSourceVersion", fields).SetValue(manager, version);
                ((HashSet<Version>)type.GetField("_expandedReleaseNotes", fields).GetValue(manager)).Add(version);
                type.GetField("_releaseNotesRequest", fields).SetValue(manager,
                    new EmberUPMReleaseNotes(_repository, version, _cache));
                // No remote latest is set: only this injected local request may run.
                host.DrawContent = () =>
                {
                    if (Event.current.type == EventType.Layout)
                        type.GetMethod("UpdateReleaseNotes", fields).Invoke(manager, null);
                    type.GetMethod("DrawReleaseNotes", fields).Invoke(manager, new object[] { version });
                };
                host.position = new Rect(50, 50, 600, 300);
                host.ShowUtility();
                var deadline = EditorApplication.timeSinceStartup + 10;
                int completedRepaints = 0;
                while (EditorApplication.timeSinceStartup < deadline && completedRepaints < 2)
                {
                    host.Repaint();
                    yield return null;
                    if (type.GetField("_releaseNotesByVersion", fields).GetValue(manager) != null)
                        completedRepaints++;
                }
                Assert.IsNull(type.GetField("_releaseNotesError", fields).GetValue(manager));
                Assert.IsNotNull(type.GetField("_releaseNotesByVersion", fields).GetValue(manager));
                Assert.GreaterOrEqual(host.RepaintCount, 2);
                CollectionAssert.AreEqual(new[] { version }, candidates);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                host.DrawContent = null;
                host.Close();
                UnityEngine.Object.DestroyImmediate(manager);
            }
        }

        private void WriteChangelog()
        {
            Directory.CreateDirectory(Path.Combine(_repository, "Packages/com.ember"));
            File.WriteAllText(Path.Combine(_repository, "Packages/com.ember/CHANGELOG.md"),
                "# Changelog\n## [0.12.9]\n- 修复刷新后无法打开场景。\n## [0.12.8]\n- 增加符号字体。\n");
        }

        private void PrepareRelease(bool withNotes)
        {
            if (withNotes) WriteChangelog();
            Git("add .");
            Git("-c user.name=EmberTest -c user.email=test@example.invalid -c commit.gpgsign=false commit --allow-empty -m test");
            Git("-c tag.gpgsign=false tag v0.12.9");
        }

        private static IEnumerator Wait(EmberUPMReleaseNotes request)
        {
            var clock = Stopwatch.StartNew();
            while (!request.IsCompleted && clock.Elapsed.TotalSeconds < 10)
            {
                request.Poll();
                yield return null;
            }
            Assert.IsTrue(request.IsCompleted, "本地 Git 更新说明读取应在 10 秒内结束。");
        }

        private void Git(string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = _repository, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            });
            Assert.IsNotNull(process);
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000)) { process.Kill(); Assert.Fail("测试准备 Git 超时。"); }
            Assert.AreEqual(0, process.ExitCode, stderr.GetAwaiter().GetResult());
            stdout.GetAwaiter().GetResult();
        }

        private static void RemoveOwnedDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(path, true);
        }
    }
}
