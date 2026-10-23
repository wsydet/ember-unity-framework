// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberProjectRulesSyncEditTests
    {
        #region 内部参数
        private string _root;
        private string _project;
        private string _package;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private string Target(string name = "AGENTS.md") => Path.Combine(_project, name);

        private static void Write(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private void Sync()
        {
            string message = EmberProjectRulesSync.Sync(_project, _package, out bool errors);
            Assert.IsFalse(errors, message);
        }

        private static byte[] Slice(byte[] bytes, int offset, int count)
        {
            var result = new byte[count];
            Array.Copy(bytes, offset, result, 0, count);
            return result;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberRulesTest-" + Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_root, "Consumer Project");
            _package = Path.Combine(_project, "Library/PackageCache/com.ember@old");
            Directory.CreateDirectory(_project);
            Write(Path.Combine(_package, EmberProjectRulesSync.RULES_PATH), "# 公共规则\n");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void Install_CreatesBothRootEntrypointsWithoutTemplate()
        {
            Sync();
            foreach (string name in new[] { "AGENTS.md", "CLAUDE.md" })
            {
                string text = File.ReadAllText(Target(name));
                StringAssert.Contains(EmberProjectRulesSync.BEGIN, text);
                StringAssert.Contains("Library/PackageCache/com.ember@old/" + EmberProjectRulesSync.RULES_PATH, text);
                StringAssert.Contains("必须完整阅读并遵守", text);
            }
            Assert.IsFalse(Directory.Exists(Path.Combine(_project, "Assets")));
        }

        [TestCase("utf8")]
        [TestCase("utf8-bom")]
        [TestCase("utf16")]
        [TestCase("utf16-be")]
        [TestCase("utf32")]
        [TestCase("utf32-be")]
        public void Upgrade_PreservesCustomContentAndEncoding(string kind)
        {
            Encoding encoding = kind.StartsWith("utf32", StringComparison.Ordinal) ? (Encoding)new UTF32Encoding(kind == "utf32-be", true, true)
                : kind.StartsWith("utf16", StringComparison.Ordinal) ? (Encoding)new UnicodeEncoding(kind == "utf16-be", true, true)
                : new UTF8Encoding(kind == "utf8-bom", true);
            const string prefix = "# 自定义规则\r\n不要修改我的要求。\r\n\r\n";
            const string suffix = "\r\n\r\n## 项目专属\n保留混合换行和末尾空格  ";
            File.WriteAllText(Target(), prefix + EmberProjectRulesSync.BEGIN + "\r\n旧版入口\r\n"
                + EmberProjectRulesSync.END + suffix, encoding);
            Sync();
            string text = File.ReadAllText(Target());
            StringAssert.StartsWith(prefix, text);
            StringAssert.EndsWith(suffix, text);
            StringAssert.DoesNotContain("旧版入口", text);
            CollectionAssert.AreEqual(encoding.GetBytes(prefix),
                Slice(File.ReadAllBytes(Target()), encoding.GetPreamble().Length, encoding.GetByteCount(prefix)));
            CollectionAssert.AreEqual(encoding.GetPreamble(), Slice(File.ReadAllBytes(Target()), 0, encoding.GetPreamble().Length));
        }

        [Test]
        public void Install_AppendsWithoutChangingCustomRulesWithNoTrailingNewline()
        {
            const string original = "# 我的规则\n测试必须先询问我。";
            Write(Target(), original);
            Sync();
            StringAssert.StartsWith(original + "\n\n" + EmberProjectRulesSync.BEGIN, File.ReadAllText(Target()));
        }

        [Test]
        public void Reload_AndTemplateReplacement_DoNotRewriteOrDuplicateEntrypoints()
        {
            Sync();
            byte[] before = File.ReadAllBytes(Target());
            var oldTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(Target(), oldTime);
            Write(Path.Combine(_project, "Assets/Game/Documentation/README.md"), "另一个模板");
            Write(Path.Combine(_project, "ProjectSettings/EmberDeployedTemplates.json"), "另一个模板身份");
            Sync();
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Target()));
            Assert.AreEqual(oldTime, File.GetLastWriteTimeUtc(Target()));
        }

        [Test]
        public void Upgrade_UpdatesResolvedPackagePathEvenWhenVersionIsUnchanged()
        {
            Sync();
            _package = Path.Combine(_root, "External Package 新版本");
            Write(Path.Combine(_package, EmberProjectRulesSync.RULES_PATH), "new");
            Sync();
            string text = File.ReadAllText(Target());
            StringAssert.DoesNotContain("com.ember@old", text);
            int linkStart = text.IndexOf("](<", StringComparison.Ordinal) + 3;
            int linkEnd = text.IndexOf(">)", linkStart, StringComparison.Ordinal);
            Assert.GreaterOrEqual(linkStart, 3);
            Assert.Greater(linkEnd, linkStart);
            string link = Uri.UnescapeDataString(text.Substring(linkStart, linkEnd - linkStart));
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_package, EmberProjectRulesSync.RULES_PATH)),
                Path.GetFullPath(Path.Combine(_project, link)));
            Assert.AreEqual(1, text.Split(new[] { EmberProjectRulesSync.BEGIN }, StringSplitOptions.None).Length - 1);
        }

        [TestCase("missing-end")]
        [TestCase("duplicate")]
        [TestCase("reversed")]
        [TestCase("inline")]
        public void DamagedMarkers_PreserveEntireFileAndStillSyncOtherEntry(string kind)
        {
            string begin = EmberProjectRulesSync.BEGIN;
            string end = EmberProjectRulesSync.END;
            string text = kind == "missing-end" ? begin + "\ncustom"
                : kind == "duplicate" ? begin + "\n" + end + "\n" + begin + "\n" + end
                : kind == "reversed" ? end + "\n" + begin : "example " + begin + "\n" + end;
            Write(Target(), text);
            EmberProjectRulesSync.Sync(_project, _package, out bool errors);
            Assert.IsTrue(errors);
            Assert.AreEqual(text, File.ReadAllText(Target()));
            Assert.IsTrue(File.Exists(Target("CLAUDE.md")));
        }

        [Test]
        public void InvalidEncoding_PreservesOriginalBytes()
        {
            var bytes = new byte[] { 0xff, 0x80, 0x81 };
            File.WriteAllBytes(Target(), bytes);
            EmberProjectRulesSync.Sync(_project, _package, out bool errors);
            Assert.IsTrue(errors);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Target()));
        }

        [Test]
        public void MissingPublicDocument_DoesNotCreateDanglingEntrypoints()
        {
            File.Delete(Path.Combine(_package, EmberProjectRulesSync.RULES_PATH));
            Assert.Throws<FileNotFoundException>(() => Sync());
            Assert.IsFalse(File.Exists(Target()));
        }

        [Test]
        public void FrameworkSourceProject_IsNotAutomaticallyModified()
        {
            Write(Path.Combine(_project, "Packages/com.ember/package.json"), "{}");
            Write(Path.Combine(_project, ".agents/skills/catalog.json"), "{}");
            Write(Target(), "开发仓库规则");
            Sync();
            Assert.AreEqual("开发仓库规则", File.ReadAllText(Target()));
            Assert.IsFalse(File.Exists(Target("CLAUDE.md")));
        }

        [Test]
        public void ReadOnlyFile_IsPreservedAndLeavesNoTemporaryFiles()
        {
            Write(Target(), "用户只读规则");
            File.SetAttributes(Target(), FileAttributes.ReadOnly);
            try
            {
                EmberProjectRulesSync.Sync(_project, _package, out bool errors);
                Assert.IsTrue(errors);
                Assert.AreEqual("用户只读规则", File.ReadAllText(Target()));
                Assert.IsEmpty(Directory.GetFiles(_project, "*.tmp"));
            }
            finally { File.SetAttributes(Target(), FileAttributes.Normal); }
        }
        #endregion
    }
}
