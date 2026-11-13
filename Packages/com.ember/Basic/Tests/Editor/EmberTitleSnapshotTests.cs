// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ember.Basic.Editor.Tests
{
    public sealed class EmberTitleSnapshotTests
    {
        #region 内部参数
        private string _root;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberTitle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown] public void Teardown()
        {
            // 只删除本测试新建的随机目录；不接触项目 Git 或身份记录。
            if (!Directory.Exists(_root)) return;
            foreach (string path in Directory.GetFiles(_root, "*", SearchOption.AllDirectories))
                File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(_root, true);
        }

        [Test] public void RepeatedInternalDescriptorCallbacksOnlyCopyThePublishedTitle()
        {
            var type = typeof(EditorMainWindowTitle);
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            type.GetMethod("ApplyTitle", flags).Invoke(null, null);
            int requests = EditorMainWindowTitle.QueryRequests;
            string title = (string)type.GetField("_currentTitle", flags).GetValue(null);
            var descriptorType = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.ApplicationTitleDescriptor");
            var constructor = descriptorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var parameters = constructor.GetParameters();
            var arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) arguments[i] = parameters[i].ParameterType == typeof(bool) ? (object)false : "";
            var descriptor = constructor.Invoke(arguments);
            var callback = type.GetMethod("UpdateTitleDescriptor", flags);
            for (int i = 0; i < 100; i++) callback.Invoke(null, new[] { descriptor });
            Assert.AreEqual(title, descriptorType.GetField("title").GetValue(descriptor));
            Assert.AreEqual(requests, EditorMainWindowTitle.QueryRequests);
        }

        [TestCase(true, "editing")]
        [TestCase(false, "active")]
        public void DisplayIdentityUsesOnlyTheFormalRecordForItsPackageSource(bool embedded, string expected)
        {
            var data = new EmberTitleSnapshot.Data { Embedded = embedded,
                TemplateJson = "{\"templateId\":\"editing\",\"activeTemplateId\":\"active\"}" };
            Assert.AreEqual(expected, EmberTitleSnapshot.TemplateId(data));
            data.TemplateJson = embedded ? "{\"activeTemplateId\":\"stale\"}" : "{\"templateId\":\"stale\"}";
            Assert.AreEqual("?", EmberTitleSnapshot.TemplateId(data));
            data.TemplateJson = "broken";
            Assert.AreEqual("?", EmberTitleSnapshot.TemplateId(data));
        }

        [Test] public void FormattingRetainsAllFieldsAndFilesystemRoots()
        {
            string project = Path.Combine(_root, "Games", "Example");
            Assert.AreEqual("Example | Games/Example | topic | base | StandaloneWindows64 | " + _root.Replace('\\', '/'),
                EditorMainWindowTitle.BuildProjectTitle("Example", project, "topic", "base", "StandaloneWindows64", _root));
            string drive = Path.GetPathRoot(_root);
            StringAssert.EndsWith(drive.Replace('\\', '/'), EditorMainWindowTitle.BuildProjectTitle("P", drive, "HEAD", "?", "T", drive));
            Assert.AreEqual("P | Games/P | HEAD | ? | T | " + drive.Replace('\\', '/'),
                EditorMainWindowTitle.BuildProjectTitle("P", Path.Combine(drive, "Games", "P"), "HEAD", "?", "T", drive));
            Assert.AreEqual("P | ? | ? | Unknown | " + _root.Replace('\\', '/'),
                EditorMainWindowTitle.BuildProjectTitle("P", _root, null, null, null, _root));
        }

        [UnityTest] public IEnumerator InFlightRequestsAreCoalescedAndLateProjectOrDisposedResultsAreRejected()
        {
            var first = new TaskCompletionSource<EmberTitleSnapshot.Data>();
            var second = new TaskCompletionSource<EmberTitleSnapshot.Data>();
            int calls = 0;
            using var cache = new EmberTitleSnapshot((root, embedded, token) =>
                Interlocked.Increment(ref calls) == 1 ? first.Task : second.Task);
            cache.SetContext(_root, true); cache.Tick(true);
            yield return Until(() => Volatile.Read(ref calls) == 1);
            for (int i = 0; i < 100; i++) cache.Tick(true);
            Assert.AreEqual(1, cache.Requests);
            string next = Path.Combine(_root, "other");
            cache.SetContext(next, false);
            first.SetResult(new EmberTitleSnapshot.Data { Root = _root, Branch = "late" });
            yield return Until(() => { cache.Tick(false); return calls == 2; });
            Assert.AreEqual(next, cache.Current.Root);
            Assert.AreEqual("?", cache.Current.Branch);
            cache.Dispose();
            second.SetResult(new EmberTitleSnapshot.Data { Root = next, Branch = "disposed" });
            yield return null;
            Assert.IsFalse(cache.Tick(true)); Assert.IsNull(cache.Current);
            Assert.AreEqual(2, cache.Requests);
        }

        [UnityTest] public IEnumerator InvalidationDiscardsSameProjectGenerationAndPublishesOnlyTheNewResult()
        {
            var first = new TaskCompletionSource<EmberTitleSnapshot.Data>();
            int calls = 0;
            using var cache = new EmberTitleSnapshot((root, embedded, token) =>
                Interlocked.Increment(ref calls) == 1 ? first.Task : Task.FromResult(new EmberTitleSnapshot.Data { Root = root, Branch = "new" }));
            cache.SetContext(_root, true); cache.Tick(true);
            yield return Until(() => calls == 1);
            cache.Invalidate();
            first.SetResult(new EmberTitleSnapshot.Data { Root = _root, Branch = "old" });
            yield return Until(() => { cache.Tick(false); return cache.Current.Branch == "new"; });
            Assert.AreEqual(2, cache.Requests);
        }

        [UnityTest] public IEnumerator GitBranchSwitchDetachedHeadSubdirectoryWorktreeAndNoGitAreReadInBackground()
        {
            var init = Git(_root, "init -b main"); yield return Until(() => init.IsCompleted); Assert.IsTrue(init.Result.Success, init.Result.Error);
            var identity = Git(_root, "-c user.name=EmberTest -c user.email=test@example.invalid commit --allow-empty -m fixture");
            yield return Until(() => identity.IsCompleted); Assert.IsTrue(identity.Result.Success, identity.Result.Error);
            string project = Path.Combine(_root, "Games", "Unity");
            Directory.CreateDirectory(Path.Combine(project, "Assets/Editor"));
            File.WriteAllText(Path.Combine(project, "Assets/Editor/EmberEditingTemplate.json"), "{\"templateId\":\"dev\"}");
            File.WriteAllText(Path.Combine(project, "Assets/Editor/EmberDeployedTemplates.json"), "{\"activeTemplateId\":\"consumer\"}");
            var read = Read(project, false); yield return Until(() => read.IsCompleted);
            Assert.AreEqual("main", read.Result.Branch); Assert.AreEqual(_root, read.Result.GitRoot);
            Assert.AreEqual("consumer", EmberTitleSnapshot.TemplateId(read.Result));
            var branch = Git(_root, "checkout -b changed"); yield return Until(() => branch.IsCompleted); Assert.IsTrue(branch.Result.Success);
            read = Read(project, true); yield return Until(() => read.IsCompleted);
            Assert.AreEqual("changed", read.Result.Branch); Assert.AreEqual("dev", EmberTitleSnapshot.TemplateId(read.Result));
            var detach = Git(_root, "checkout --detach"); yield return Until(() => detach.IsCompleted); Assert.IsTrue(detach.Result.Success);
            read = Read(project, false); yield return Until(() => read.IsCompleted); Assert.AreEqual("HEAD", read.Result.Branch);
            string worktree = Path.Combine(_root, "worktree");
            // separate-git-dir 生成与 worktree 相同的 .git 文件边界，不创建工作区/worktree。
            var add = Git(_root, "clone --separate-git-dir=\"" + Path.Combine(_root, "git-storage") + "\" \"" + _root + "\" \"" + worktree + "\"");
            yield return Until(() => add.IsCompleted); Assert.IsTrue(add.Result.Success, add.Result.Error);
            Assert.IsTrue(File.Exists(Path.Combine(worktree, ".git")));
            read = Read(worktree, false); yield return Until(() => read.IsCompleted);
            Assert.AreEqual(worktree, read.Result.GitRoot); Assert.AreEqual("changed", read.Result.Branch);
            detach = Git(worktree, "checkout --detach"); yield return Until(() => detach.IsCompleted); Assert.IsTrue(detach.Result.Success);
            read = Read(worktree, false); yield return Until(() => read.IsCompleted); Assert.AreEqual("HEAD", read.Result.Branch);
            // 无效工作目录和无仓库都是可诊断失败，不能伪装成 detached HEAD。
            read = Read(Path.Combine(_root, "missing"), false); yield return Until(() => read.IsCompleted);
            Assert.AreEqual("?", read.Result.Branch); Assert.IsNotEmpty(read.Result.Error);
            string noGit = Path.Combine(Path.GetDirectoryName(_root), "EmberTitleNoGit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(noGit);
            try
            {
                read = Read(noGit, false); yield return Until(() => read.IsCompleted);
                Assert.AreEqual("?", read.Result.Branch); Assert.IsNotEmpty(read.Result.Error);
            }
            finally { Directory.Delete(noGit); }
        }

        [UnityTest] public IEnumerator ProcessTimeoutCancellationFailureAndBothPipesAreBounded()
        {
            var missing = Run(new ProcessStartInfo { FileName = "ember-title-no-such-executable" }, 1000, CancellationToken.None);
            yield return Until(() => missing.IsCompleted); Assert.IsFalse(missing.Result.Success); Assert.IsNotEmpty(missing.Result.Error);
#if UNITY_EDITOR_WIN
            var slow = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 10\"" };
            var flood = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -Command \"1..2000 | ForEach-Object { [Console]::Out.WriteLine('output'); [Console]::Error.WriteLine('error') }; exit 7\"" };
#else
            var slow = new ProcessStartInfo { FileName = "/bin/sh", Arguments = "-c \"while :; do :; done\"" };
            var flood = new ProcessStartInfo { FileName = "/bin/sh", Arguments = "-c \"i=0; while [ $i -lt 2000 ]; do echo output; echo error >&2; i=$((i+1)); done; exit 7\"" };
#endif
            var watch = Stopwatch.StartNew();
            var timeout = Run(slow, 100, CancellationToken.None);
            yield return Until(() => timeout.IsCompleted);
            Assert.IsFalse(timeout.Result.Success); StringAssert.Contains("timed out", timeout.Result.Error);
            Assert.Less(watch.Elapsed.TotalSeconds, 3);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = Run(new ProcessStartInfo { FileName = "git", Arguments = "--version" }, 2000, cancellation.Token);
            yield return Until(() => cancelled.IsCompleted); Assert.IsFalse(cancelled.Result.Success);
            var drained = Run(flood, 3000, CancellationToken.None);
            yield return Until(() => drained.IsCompleted);
            Assert.IsFalse(drained.Result.Success); StringAssert.Contains("git exit 7", drained.Result.Error);
            Assert.LessOrEqual(drained.Result.Output.Length, 8194);
            Assert.LessOrEqual(drained.Result.Error.Length, 8220);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static IEnumerator Until(Func<bool> done)
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + 12;
            while (!done())
            {
                Assert.Less(UnityEditor.EditorApplication.timeSinceStartup, end, "有界后台操作未完成");
                yield return null;
            }
        }
        private static Task<EmberTitleSnapshot.Data> Read(string root, bool embedded) =>
            Task.Run(() => EmberTitleSnapshot.ReadAsync(root, embedded, CancellationToken.None));
        private static Task<EmberTitleProcess.Result> Git(string root, string args) =>
            Run(new ProcessStartInfo { FileName = "git", Arguments = args, WorkingDirectory = root }, 2000, CancellationToken.None);
        private static Task<EmberTitleProcess.Result> Run(ProcessStartInfo info, int timeout, CancellationToken token) =>
            Task.Run(() => EmberTitleProcess.RunAsync(info, timeout, token));
        #endregion
    }
}
