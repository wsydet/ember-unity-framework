from pathlib import Path
p=Path('Packages/com.ember/Tests/EditMode/EmberTemplatePatchEditTests.cs');s=p.read_text(encoding='utf-8')
s=s.replace('using File = Ember.UPMManager.Editor.EmberTemplateFileSystem.File;', 'using File = Ember.UI.Tests.PatchObservation.File;').replace('using Directory = Ember.UPMManager.Editor.EmberTemplateFileSystem.Directory;', 'using Directory = Ember.UI.Tests.PatchObservation.Directory;')
s=s.replace('EmberTemplateFileSystem.NativePath(', 'PatchObservation.Native(')
s=s.replace('string before = Fingerprint(), sourceBefore = EmberProjectSetup.ComputeTemplateContentHash(_source);', 'string before = Fingerprint(), sourceBefore = RawTree(_source);')
s=s.replace('Assert.AreEqual(sourceBefore, EmberProjectSetup.ComputeTemplateContentHash(_source));','Assert.AreEqual(sourceBefore, RawTree(_source));')
s=s.replace('            // 源文件必须能用运行时普通 System.IO 读取；测试不能靠适配层掩盖前提。', '''            Assert.AreEqual(RuntimePlatform.WindowsEditor, Application.platform);
            Assert.AreEqual(0, (int)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\FileSystem", "LongPathsEnabled", -1));
            foreach (string input in new[] { live, Path.Combine(_source, relative) })
            {
                Assert.LessOrEqual((input + ".meta").Length, 235);
                Assert.AreEqual("old asset", System.IO.File.ReadAllText(input));
                Assert.IsNotEmpty(System.IO.File.ReadAllBytes(input + ".meta"));
                TestContext.Out.WriteLine("ordinary input=" + input + " meta length=" + (input + ".meta").Length);
            }
            TestContext.Out.WriteLine("Unity=" + Application.unityVersion + " platform=" + Application.platform
                + " CLR=" + Environment.Version + " OS=" + Environment.OSVersion + " volume=" + new DriveInfo(Path.GetPathRoot(_root)).DriveFormat);
            // 源与 live 必须能普通读取。''')
s=s.replace('                    Assert.Greater((staged + ".meta").Length, 260);','                    Assert.Greater((staged + ".meta").Length, 260);\n                    TestContext.Out.WriteLine(side + " meta length=" + (staged + ".meta").Length);')
start=s.index('            const string prefix = "GameResource/LongPaths/";');end=s.index('\n        }',start)
s=s[:start]+'''            string prefix = "GameResource/LongPaths/";
            int length = 235 - Path.Combine(_project, "Assets", prefix).Length - ".asset.meta".Length;
            Assert.Greater(length, 0);
            while (length > 40) { prefix += new string('a', 30) + "/"; length -= 31; }
            return prefix + new string('b', length) + ".asset";'''+s[end:]
start=s.index('        private string Fingerprint() =>');end=s.index('        private static string Meta',start)
s=s[:start]+'''        private string Fingerprint() => RawTree(_project, true);
        private static string RawTree(string root, bool excludeTemp = false)
        {
            bool Include(string p) => !excludeTemp || (!p.StartsWith(Path.Combine(root, "Temp") + Path.DirectorySeparatorChar)
                && !p.StartsWith(Path.Combine(root, ".utmp") + Path.DirectorySeparatorChar));
            var dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Where(Include)
                .Where(p => !excludeTemp || (p != Path.Combine(root, "Temp") && p != Path.Combine(root, ".utmp")))
                .Select(p => "D:" + p.Substring(root.Length));
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(Include)
                .Select(p => "F:" + p.Substring(root.Length) + ":" + Convert.ToBase64String(File.ReadAllBytes(p)));
            return string.Join("\\n", dirs.Concat(files).OrderBy(p => p, StringComparer.Ordinal));
        }
'''+s[end:]
# Independent oracle: never delegates to the implementation or its aliases.
s+='''
namespace Ember.UI.Tests
{
    internal static class PatchObservation
    {
        internal static string Native(string p) => Path.DirectorySeparatorChar != '\\\\' ? p
            : p.StartsWith(@"\\\\", StringComparison.Ordinal) ? @"\\\\?\\UNC\\" + p.Substring(2) : @"\\\\?\\" + Path.GetFullPath(p);
        internal static class File
        {
            internal static bool Exists(string p) => System.IO.File.Exists(Native(p));
            internal static string ReadAllText(string p) => System.IO.File.ReadAllText(Native(p));
            internal static byte[] ReadAllBytes(string p) => System.IO.File.ReadAllBytes(Native(p));
            internal static void WriteAllText(string p, string t) => System.IO.File.WriteAllText(Native(p), t);
            internal static void Copy(string a, string b, bool overwrite = false) => System.IO.File.Copy(Native(a), Native(b), overwrite);
            internal static void Delete(string p) => System.IO.File.Delete(Native(p));
        }
        internal static class Directory
        {
            internal static bool Exists(string p) => System.IO.Directory.Exists(Native(p));
            internal static void CreateDirectory(string p) => System.IO.Directory.CreateDirectory(Native(p));
            internal static void Delete(string p, bool recursive = false) => System.IO.Directory.Delete(Native(p), recursive);
            private static string[] Restore(string root, string[] entries) => entries.Select(p => Path.Combine(root, p.Substring(Native(root).TrimEnd('\\\\').Length).TrimStart('\\\\', '/'))).ToArray();
            internal static string[] GetFiles(string p, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly) => Restore(p, System.IO.Directory.GetFiles(Native(p), pattern, option));
            internal static string[] GetDirectories(string p, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly) => Restore(p, System.IO.Directory.GetDirectories(Native(p), pattern, option));
        }
    }
}
'''
p.write_text(s,encoding='utf-8')
