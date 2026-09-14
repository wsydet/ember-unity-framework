using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberAISkillDownloadEditTests
    {
        private string _root;
        private string _repo;
        private string _cache;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberSkillGit-" + Guid.NewGuid().ToString("N"));
            _repo = Path.Combine(_root, "repo"); _cache = Path.Combine(_root, "cache");
            Directory.CreateDirectory(_repo);
            Git("init -b main");
            Write(".agents/skills/ember-test/SKILL.md", "---\nname: ember-test\ndescription: test\n---\noriginal");
            Write(".agents/skills/catalog.json", "{\"schemaVersion\":1,\"skills\":[{\"id\":\"ember-test\"}]}");
            Write("Assets/do-not-checkout.txt", "project assets");
            Write("Packages/com.ember/package.json", "{}");
            Git("add .");
            Git("-c user.name=EmberTest -c user.email=test@example.invalid -c commit.gpgsign=false commit -m fixture");
            Git("-c tag.gpgsign=false tag skill-v1");
        }

        private void Write(string path, string text)
        {
            string full = Path.Combine(_repo, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)); File.WriteAllText(full, text);
        }

        private void Git(string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = _repo, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            });
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10000)) { process.Kill(); Assert.Fail("Fixture Git timeout"); }
            Assert.AreEqual(0, process.ExitCode, error.GetAwaiter().GetResult());
            _ = output.GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (!Directory.Exists(_root)) return;
                foreach (string file in Directory.GetFiles(_root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(_root, true);
            }
            catch (IOException) { } // Download cache cleanup may still be releasing Git files.
            catch (UnauthorizedAccessException) { }
        }

        private static IEnumerator Wait(EmberAISkillDownload request)
        {
            var clock = Stopwatch.StartNew();
            while (!request.IsCompleted && clock.Elapsed.TotalSeconds < 20) { request.Poll(); yield return null; }
            Assert.IsTrue(request.IsCompleted, "Download did not complete");
        }

        [UnityTest]
        public IEnumerator SparseDownload_UsesSelectedTagAndExcludesProjectAssets()
        {
            Write(".agents/skills/ember-test/SKILL.md", "later change");
            Git("add .");
            Git("-c user.name=EmberTest -c user.email=test@example.invalid -c commit.gpgsign=false commit -m later");
            using var request = new EmberAISkillDownload(_repo, "skill-v1", _cache);
            yield return Wait(request);
            Assert.IsNull(request.Error);
            Assert.AreEqual(40, request.Commit.Length);
            StringAssert.EndsWith("original", File.ReadAllText(Path.Combine(request.SkillsRoot, "ember-test/SKILL.md")));
            string checkout = Directory.GetParent(Directory.GetParent(request.SkillsRoot).FullName).FullName;
            Assert.IsFalse(Directory.Exists(Path.Combine(checkout, "Assets")));
            Assert.IsFalse(Directory.Exists(Path.Combine(checkout, "Packages")));
            Assert.AreEqual(1, EmberAISkillInstaller.ReadCatalog(request.SkillsRoot).Count);
        }

        [UnityTest]
        public IEnumerator MissingRefOrTimeout_CanRetry()
        {
            using (var missing = new EmberAISkillDownload(_repo, "missing", _cache))
            { yield return Wait(missing); Assert.IsNotEmpty(missing.Error); }
            using (var timeout = new EmberAISkillDownload(_repo, "main", _cache, TimeSpan.Zero))
            { timeout.Poll(); Assert.IsNotEmpty(timeout.Error); }
            using (var cancelled = new EmberAISkillDownload(_repo, "main", _cache)) cancelled.Dispose();
            using var retry = new EmberAISkillDownload(_repo, "main", _cache);
            yield return Wait(retry);
            Assert.IsNull(retry.Error);
        }

        [UnityTest]
        public IEnumerator CompletedDownload_PopulatesRowsOnlyAtLayoutAndSurvivesRepaint()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var manager = ScriptableObject.CreateInstance<EmberUPMManager>();
            var host = ScriptableObject.CreateInstance<EmberUPMUpgradePromptTestWindow>();
            var type = typeof(EmberUPMManager);
            var request = new EmberAISkillDownload(_repo, "skill-v1", _cache);
            try
            {
                type.GetField("_aiSkillDownload", flags).SetValue(manager, request);
                type.GetField("_aiDownloadedRevision", flags).SetValue(manager, "skill-v1");
                yield return Wait(request);
                var rows = (IList)type.GetField("_aiSkillRows", flags).GetValue(manager);
                Assert.AreEqual(0, rows.Count, "Polling alone must not mutate IMGUI rows");
                host.DrawContent = () =>
                {
                    if (Event.current.type == EventType.Layout)
                        type.GetMethod("UpdateAiSkillsLayout", flags).Invoke(manager, null);
                    type.GetMethod("DrawAiSkillsSection", flags).Invoke(manager, null);
                };
                host.position = new Rect(50, 50, 640, 500);
                host.ShowUtility();
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (host.RepaintCount < 3 && EditorApplication.timeSinceStartup < deadline)
                { host.Repaint(); yield return null; }
                Assert.AreEqual(1, rows.Count);
                Assert.GreaterOrEqual(host.RepaintCount, 3);
                Assert.IsFalse((bool)type.GetField("_aiSkillError", flags).GetValue(manager));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                host.DrawContent = null; host.Close();
                UnityEngine.Object.DestroyImmediate(manager);
            }
        }
    }
}
