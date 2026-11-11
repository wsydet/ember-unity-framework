// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ember.Core.Editor;
using Ember.UPMManager.Editor;
using NUnit.Framework;
using F = Ember.UPMManager.Editor.EmberTemplateFileSystem.File;
using D = Ember.UPMManager.Editor.EmberTemplateFileSystem.Directory;
using O = Ember.UI.Tests.PatchObservation;

namespace Ember.UI.Tests
{
    public sealed class EmberTemplateFileSystemEditTests
    {
        #region 内部参数
        private string _root;
        #endregion

        #region 外部方法
        [SetUp] public void SetUp() { _root = Path.Combine(Path.GetTempPath(), "EmberIO-" + Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(_root); }
        [TearDown] public void TearDown() { O.Directory.Delete(_root, true); }

        [Test] public void Paths_DosUncDeviceDotAndEnumerationBoundaries()
        {
            if (Path.DirectorySeparatorChar != '\\') { Assert.AreEqual("a/../b", Native("a/../b")); return; }
            Assert.IsNull(Native(null)); Assert.AreEqual("", Native(""));
            Assert.AreEqual(@"\\?\C:\b", Native(@"C:\a\..\b"));
            Assert.AreEqual(@"\\?\C:\b", Native(@"\\?\C:\b"));
            Assert.AreEqual(@"\\?\UNC\server\share\b", Native(@"\\server\share\a\..\b"));
            Assert.AreEqual(@"\\?\UNC\server\share\b", Native(@"\\?\UNC\server\share\b"));
            foreach (string p in new[] { @"\\.\C:\x", @"\\?\GLOBALROOT\x", @"C:\bad.\x", @"C:\bad \x" })
                Assert.Throws<ArgumentException>(() => Native(p));
            var map = typeof(EmberTemplateFileSystem).GetMethod("LogicalEntries", BindingFlags.NonPublic | BindingFlags.Static);
            var error = Assert.Throws<TargetInvocationException>(() => map.Invoke(null, new object[] { @"C:\root", @"\\?\C:\root", new[] { @"\\?\C:\root2\escape" } }));
            Assert.IsInstanceOf<IOException>(error.InnerException);
            TestContext.Out.WriteLine("UNC format only; no network share exercised.");
        }

        [Test] public void LongPrimitives_EnumerationRestoresLogicalPaths_AndRawMd5Matches()
        {
            string parent = Deep("content", 280); D.CreateDirectory(parent);
            string file = Path.Combine(parent, "a.txt"); F.WriteAllText(file, "one\r\ntwo");
            Assert.AreEqual("one\r\ntwo", O.File.ReadAllText(file));
            CollectionAssert.AreEqual(new[] { "one", "two" }, F.ReadLines(file));
            Assert.AreEqual(O.File.ReadAllBytes(file).Length, F.Length(file));
            Assert.AreEqual(O.File.ReadAllText(file), F.ReadAllText(file));
            string copy = Path.Combine(parent, "b.txt"); F.Copy(file, copy);
            var entries = D.GetFiles(parent);
            Assert.IsTrue(entries.All(p => !p.StartsWith(@"\\?\") && p.StartsWith(parent)));
            using (var md5 = System.Security.Cryptography.MD5.Create())
                Assert.AreEqual(BitConverter.ToString(md5.ComputeHash(O.File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant(), EmberTemplateFileSystem.RawMD5(file));
            Assert.AreEqual(EmberProjectSetup.ComputeTemplateContentHash(parent), CopyShortHash(parent));
            string moved = parent + "-moved"; D.Move(parent, moved); D.Delete(moved, true);
            Assert.IsFalse(O.Directory.Exists(moved));
        }

        [TestCase(false)] [TestCase(true)] public void Owned_CreationOrOwnerWriteFailurePreservesPrimaryAndUnownedRoot(bool write)
        {
            string stage = null; var primary = new IOException("owner fault");
            var hooks = new EmberTemplateTemporaryDirectory.TestHooks();
            if (write) hooks.BeforeOwnerWrite = (p, lease) => { stage = p; throw primary; };
            else hooks.BeforeOwner = p => { stage = p; throw primary; };
            var actual = Assert.Throws<IOException>(() => Run(_ => 0, hooks));
            Assert.AreSame(primary, actual); Assert.IsTrue(O.Directory.Exists(stage));
            Assert.IsNotNull(actual.Data["EmberStageCleanupException"]);
            StringAssert.Contains(stage, actual.Data["EmberStageCleanupException"].ToString());
        }

        [TestCase(false)] [TestCase(true)] public void Owned_MissingOrCorruptMarkerStopsDeletion(bool corrupt)
        {
            string stage = null;
            var hooks = new EmberTemplateTemporaryDirectory.TestHooks { BeforeCleanupLease = (p, lease) =>
            {
                stage = p; lease.Dispose(); string owner = Path.Combine(p, ".ember-owner");
                if (corrupt) O.File.WriteAllText(owner, "wrong"); else O.File.Delete(owner);
            }};
            var failure = Assert.Throws<IOException>(() => Run(_ => 1, hooks));
            StringAssert.Contains(stage, failure.Message); Assert.IsTrue(O.Directory.Exists(stage));
        }

        [Test] public void Owned_LeaseRejectsOtherWriters_AndOccupiedRootIsPreserved()
        {
            string token = Guid.NewGuid().ToString("N"), stage = Path.Combine(_root, "Temp", "EmberPatchPreview-" + token + "~");
            O.Directory.CreateDirectory(stage); O.File.WriteAllText(Path.Combine(stage, "unknown"), "keep");
            Assert.Throws<IOException>(() => Run(_ => 1, new EmberTemplateTemporaryDirectory.TestHooks { Token = () => token }));
            Assert.AreEqual("keep", O.File.ReadAllText(Path.Combine(stage, "unknown")));
            Run(p => { Assert.Throws<IOException>(() => O.File.WriteAllText(Path.Combine(p, ".ember-owner"), "takeover")); return 1; });
            Assert.IsTrue(O.Directory.Exists(stage));
        }

        [Test] public void Owned_ConcurrentNestedAndInterruptedScopesStayIsolated()
        {
            string stale = Path.Combine(_root, "Temp", "EmberPatchPreview-interrupted~");
            O.Directory.CreateDirectory(stale); O.File.WriteAllText(Path.Combine(stale, "keep"), "interrupted");
            using var ready = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
            string concurrent = null;
            var worker = Task.Run(() => Run(p => { concurrent = p; ready.Set(); if (!release.Wait(10000)) throw new TimeoutException(); return 1; }));
            try
            {
                Assert.IsTrue(ready.Wait(10000));
                Run(outer => { Run(inner => { Assert.AreNotEqual(outer, inner); Assert.IsTrue(O.Directory.Exists(concurrent)); return 1; }); Assert.IsTrue(O.Directory.Exists(concurrent)); return 1; });
                Assert.IsTrue(O.Directory.Exists(concurrent));
            }
            finally { release.Set(); Assert.IsTrue(worker.Wait(10000)); }
            Assert.IsFalse(O.Directory.Exists(concurrent)); Assert.AreEqual("interrupted", O.File.ReadAllText(Path.Combine(stale, "keep")));
        }

        [TestCase(false)] [TestCase(true)] public void Owned_LockedCleanupFailsPreview_ButCommittedResultSurvives(bool committed)
        {
            string stage = null; FileStream held = null;
            try
            {
                Func<string, int> action = p => { stage = p; O.File.WriteAllText(Path.Combine(p, "locked"), "body"); held = new FileStream(O.Native(Path.Combine(p, "locked")), FileMode.Open, FileAccess.Read, FileShare.None); return 17; };
                if (committed) Assert.AreEqual(17, Run(action, committed: true));
                else { var failure = Assert.Throws<IOException>(() => Run(action)); Assert.IsNotNull(failure.InnerException); StringAssert.Contains(stage, failure.Message); }
                Assert.IsTrue(O.Directory.Exists(stage));
            }
            finally { held?.Dispose(); }
        }

        [TestCase(false)] [TestCase(true)] public void Transaction_BackupBeyond260_CommitsOrRestoresRawFiles(bool interrupt)
        {
            string dir = Deep("transaction", 245); O.Directory.CreateDirectory(dir);
            string destination = Path.Combine(dir, "target.asset"); string stage = Path.Combine(_root, "stage");
            O.File.WriteAllText(destination, "old\r\nbytes"); O.File.WriteAllText(destination + ".meta", "guid: original");
            O.File.WriteAllText(stage, "new"); O.File.WriteAllText(stage + ".meta", "guid: original");
            Assert.Greater((destination + ".ember-backup~").Length, 260);
            var targets = new[] { new TemplateTransactionTarget(stage, destination), new TemplateTransactionTarget(stage + ".meta", destination + ".meta") };
            var primary = new IOException("after first placed");
            if (interrupt) Assert.AreSame(primary, Assert.Throws<IOException>(() => EmberTemplateTransaction.CommitPreparedTargets(targets, i => { if (i == 0) { Assert.AreEqual("new", O.File.ReadAllText(destination)); throw primary; } })));
            else EmberTemplateTransaction.CommitPreparedTargets(targets);
            Assert.AreEqual(interrupt ? "old\r\nbytes" : "new", O.File.ReadAllText(destination));
            Assert.AreEqual("guid: original", O.File.ReadAllText(destination + ".meta"));
            Assert.IsFalse(O.File.Exists(destination + ".ember-backup~"));
            TestContext.Out.WriteLine("target=" + destination.Length + " backup=" + (destination + ".ember-backup~").Length);
        }

        [Test] public void Transaction_UnknownBackupAndNestedCommitAreRejected()
        {
            string destination = Path.Combine(_root, "live"), stage = Path.Combine(_root, "stage");
            O.File.WriteAllText(destination, "old"); O.File.WriteAllText(stage, "new"); O.File.WriteAllText(destination + ".ember-backup~", "unknown");
            var targets = new[] { new TemplateTransactionTarget(stage, destination) };
            Assert.Throws<InvalidOperationException>(() => EmberTemplateTransaction.CommitPreparedTargets(targets));
            Assert.AreEqual("unknown", O.File.ReadAllText(destination + ".ember-backup~")); O.File.Delete(destination + ".ember-backup~");
            EmberTemplateTransaction.CommitPreparedTargets(targets, _ => Assert.Throws<InvalidOperationException>(() => EmberTemplateTransaction.CommitPreparedTargets(targets)));
        }

        [Test] public void Transaction_BackupCleanupFailureReportsCommittedAndKeepsBackup()
        {
            string destination = Path.Combine(_root, "live"), stage = Path.Combine(_root, "stage"); FileStream held = null;
            O.File.WriteAllText(destination, "old"); O.File.WriteAllText(stage, "new");
            try
            {
                EmberTemplateTransaction.CommitPreparedTargets(new[] { new TemplateTransactionTarget(stage, destination) }, _ => held = new FileStream(destination + ".ember-backup~", FileMode.Open, FileAccess.Read, FileShare.None));
                Assert.AreEqual("new", O.File.ReadAllText(destination)); Assert.IsTrue(O.File.Exists(destination + ".ember-backup~"));
            }
            finally { held?.Dispose(); }
        }

        [Test] public void Owned_PermissionFailureReportsPathAndPreservesUnownedDirectory()
        {
            string parent = Path.Combine(_root, "Temp"); System.IO.Directory.CreateDirectory(parent);
            string sddl = Path.Combine(_root, "acl.txt");
            try
            {
                PowerShell("$p='" + parent + "';$a=Get-Acl -LiteralPath $p;$a.Sddl | Set-Content -LiteralPath '" + sddl + "';"
                    + "$id=[System.Security.Principal.WindowsIdentity]::GetCurrent().User;"
                    + "$rule=New-Object System.Security.AccessControl.FileSystemAccessRule($id,'CreateDirectories,CreateFiles','Deny');$a.AddAccessRule($rule);Set-Acl -LiteralPath $p -AclObject $a");
                var failure = Assert.Throws<UnauthorizedAccessException>(() => Run(_ => 1));
                StringAssert.StartsWith(parent, (string)failure.Data["EmberTemporaryDirectory"]);
                Assert.IsNotNull(failure.Data["EmberStageCleanupException"]);
            }
            finally { if (System.IO.File.Exists(sddl)) PowerShell("$p='" + parent + "';$a=Get-Acl -LiteralPath $p;$a.SetSecurityDescriptorSddlForm((Get-Content -Raw -LiteralPath '" + sddl + "').Trim());Set-Acl -LiteralPath $p -AclObject $a"); }
        }

        [TestCase("parent")] [TestCase("root")] [TestCase("directory")] [TestCase("file")]
        public void Owned_LinksAreRejectedWithoutFollowingTargets(string kind)
        {
            string outside = Path.Combine(_root, "outside"); System.IO.Directory.CreateDirectory(outside);
            string targetFile = Path.Combine(outside, "keep.txt"); System.IO.File.WriteAllText(targetFile, "untouched");
            string link = null;
            try
            {
                // 在 Assert.Throws 外探测权限；不可用场景真实记为 skipped。
                string probe = Path.Combine(_root, "probe"); MakeLink(probe, kind == "file" ? targetFile : outside, kind != "file");
                if (kind == "file") System.IO.File.Delete(probe); else System.IO.Directory.Delete(probe);
                if (kind == "parent")
                {
                    link = Path.Combine(_root, "Temp"); MakeLink(link, outside, true);
                    Assert.Throws<IOException>(() => Run(_ => 1));
                }
                else if (kind == "root")
                {
                    var hooks = new EmberTemplateTemporaryDirectory.TestHooks { BeforeOwner = p => { System.IO.Directory.Delete(p); link = p; MakeLink(p, outside, true); } };
                    Assert.Throws<IOException>(() => Run(_ => 1, hooks));
                }
                else
                {
                    string stage = null;
                    Assert.Throws<IOException>(() => Run(p => { stage = p; link = Path.Combine(p, "link"); MakeLink(link, kind == "file" ? targetFile : outside, kind != "file"); return 1; }));
                    Assert.IsTrue(System.IO.Directory.Exists(stage));
                }
                Assert.AreEqual("untouched", System.IO.File.ReadAllText(targetFile));
            }
            finally
            {
                if (link != null && (System.IO.File.Exists(link) || System.IO.Directory.Exists(link)) && (System.IO.File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0)
                { if (kind == "file") System.IO.File.Delete(link); else System.IO.Directory.Delete(link); }
            }
        }

        [Test] public void Owned_UnrelatedSharedTempLinkDoesNotBlockOrGetSwept()
        {
            string parent = Path.Combine(_root, "Temp"); System.IO.Directory.CreateDirectory(parent);
            string outside = Path.Combine(_root, "outside"); System.IO.Directory.CreateDirectory(outside);
            string link = Path.Combine(parent, "unrelated-link"); MakeLink(link, outside, true);
            try { Assert.AreEqual(1, Run(_ => 1)); Assert.IsTrue(System.IO.Directory.Exists(link)); }
            finally { System.IO.Directory.Delete(link); }
        }
        #endregion

        #region 内部方法
        private int Run(Func<string, int> action, EmberTemplateTemporaryDirectory.TestHooks hooks = null, bool committed = false) => EmberTemplateTemporaryDirectory.Run(_root, committed ? "EmberPatchApply" : "EmberPatchPreview", action, committed, hooks);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        private static extern bool CreateSymbolicLinkW(string link, string target, int flags);
        private static void MakeLink(string link, string target, bool directory)
        {
            if (CreateSymbolicLinkW(link, target, (directory ? 1 : 0) | 2)) return;
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (directory)
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
                Assert.IsTrue(process.WaitForExit(10000)); Assert.AreEqual(0, process.ExitCode, process.StandardError.ReadToEnd());
                TestContext.Out.WriteLine("Directory junction fallback exercised; symlink unavailable Win32=" + error);
                return;
            }
            Assert.Ignore("文件 symlink 未测；创建权限不可用，Win32=" + error);
        }
        private static void PowerShell(string script)
        {
            string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop';" + script));
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + encoded) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            Assert.IsTrue(process.WaitForExit(10000)); Assert.AreEqual(0, process.ExitCode, process.StandardError.ReadToEnd());
        }
        private string Deep(string name, int length)
        {
            string p = Path.Combine(_root, name); while (p.Length < length) p = Path.Combine(p, new string('a', Math.Min(30, length - p.Length - 1)));
            return p;
        }
        private static string Native(string p)
        {
            try { return (string)typeof(EmberTemplateFileSystem).GetMethod("NativePath", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { p }); }
            catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }
        private string CopyShortHash(string parent)
        {
            string shortRoot = Path.Combine(_root, "short"); EmberTemplateTransaction.CopyDirectory(parent, shortRoot);
            return EmberProjectSetup.ComputeTemplateContentHash(shortRoot);
        }
        #endregion
    }
}
