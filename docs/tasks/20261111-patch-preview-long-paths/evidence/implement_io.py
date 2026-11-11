from pathlib import Path
root = Path('Packages/com.ember')
p = root/'UPMManager/Editor/EmberTemplateFileSystem.cs'
s = p.read_text(encoding='utf-8-sig')
s = s.replace('internal static class File', 'public static class File').replace('internal static class Directory', 'public static class Directory')
s = s.replace('internal static ', 'public static ')
s = s.replace('public static string NativePath', 'internal static string NativePath')
s = s.replace('System.IO.File.Exists(NativePath(path))', 'EmberTemplateFileSystem.Exists(path, false)')
s = s.replace('System.IO.Directory.Exists(NativePath(path))', 'EmberTemplateFileSystem.Exists(path, true)')
s = s.replace('bool overwrite) =>', 'bool overwrite = false) =>')
s = s.replace('public static void WriteAllLines(string path, IEnumerable<string> lines) =>', 'public static void WriteAllLines(string path, IEnumerable<string> lines, Encoding encoding) => System.IO.File.WriteAllLines(NativePath(path), lines, encoding);\n            public static void WriteAllLines(string path, IEnumerable<string> lines) =>')
s = s.replace('public static FileAttributes GetAttributes', 'public static FileStream OpenRead(string path) => System.IO.File.OpenRead(NativePath(path));\n            public static FileStream CreateNewLease(string path) => new FileStream(NativePath(path), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);\n            public static long Length(string path) => new FileInfo(NativePath(path)).Length;\n            public static DateTime GetLastWriteTimeUtc(string path) => System.IO.File.GetLastWriteTimeUtc(NativePath(path));\n            public static FileAttributes GetAttributes')
p.write_text(s, encoding='utf-8')
for name in ['Core/Editor/EmberProjectSetup.cs','Core/Editor/EmberProjectSetup.Patch.cs','Core/Editor/EmberTemplateInheritanceEngine.cs','Core/Editor/EmberTemplateTransaction.cs','Tests/EditMode/EmberTemplatePatchEditTests.cs']:
    p=root/name; s=p.read_text(encoding='utf-8-sig').replace('Ember.Core.Editor.EmberTemplateFileSystem', 'Ember.UPMManager.Editor.EmberTemplateFileSystem')
    s=s.replace('CryptographyUtils.GetMD5File(EmberTemplateFileSystem.NativePath(stagedScenePath))', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.RawMD5(stagedScenePath)')
    s=s.replace('CryptographyUtils.GetMD5File(EmberTemplateFileSystem.NativePath(stagedMetaPath))', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.RawMD5(stagedMetaPath)')
    s=s.replace('CryptographyUtils.GetMD5File(EmberTemplateFileSystem.NativePath(path))', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.RawMD5(path)')
    p.write_text(s,encoding='utf-8')
p=root/'Core/Editor/EmberProjectSetup.AISkills.cs';s=p.read_text(encoding='utf-8-sig').replace('using System.IO;', 'using System.IO;\nusing File = Ember.UPMManager.Editor.EmberTemplateFileSystem.File;\nusing Directory = Ember.UPMManager.Editor.EmberTemplateFileSystem.Directory;');p.write_text(s,encoding='utf-8')
# Explicit calls in the shared template preparation/read helpers; leave unrelated Install untouched.
p=root/'UPMManager/Editor/EmberAISkillInstaller.cs';s=p.read_text(encoding='utf-8-sig');i=s.index('        private static string Within(');j=s.index('        internal static string Incompatibility',i)
part=s[i:j].replace('File.', 'EmberTemplateFileSystem.File.').replace('Directory.', 'EmberTemplateFileSystem.Directory.')
part=part.replace('new FileInfo(file).Length','EmberTemplateFileSystem.File.Length(file)').replace('new FileInfo(catalogPath).Length','EmberTemplateFileSystem.File.Length(catalogPath)')
s=s[:i]+part+s[j:];p.write_text(s,encoding='utf-8')
p=root/'UPMManager/Editor/EmberAISkillInstaller.Templates.cs';s=p.read_text(encoding='utf-8-sig').replace('File.', 'EmberTemplateFileSystem.File.').replace('Directory.', 'EmberTemplateFileSystem.Directory.');p.write_text(s,encoding='utf-8')
for name in ['EmberExtensionProject.cs','EmberExtensionProject.Identity.cs']:
    p=root/'Core/Editor'/name;s=p.read_text(encoding='utf-8-sig')
    if name.endswith('.Identity.cs'):
        s=s.replace('File.Exists(config)', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.File.Exists(config)').replace('File.Exists(defaults)', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.File.Exists(defaults)')
    else:
        s=s.replace('File.GetLastWriteTimeUtc(path)', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.File.GetLastWriteTimeUtc(path)').replace('File.ReadAllText(path)', 'Ember.UPMManager.Editor.EmberTemplateFileSystem.File.ReadAllText(path)')
    p.write_text(s,encoding='utf-8')
# Keep best-effort marker callers, with a strict entry only for patch staging.
p=root/'Core/Editor/EmberProjectSetup.cs';s=p.read_text(encoding='utf-8-sig');anchor='        private static bool RewriteVersionMarker(string fullPath, string version, string frameworkVersion)\n        {'
s=s.replace(anchor, '''        private static bool RewriteVersionMarker(string fullPath, string version, string frameworkVersion)
        {
            try { return RewriteVersionMarkerStrict(fullPath, version, frameworkVersion); }
            catch (Exception error)
            {
                EmberDebug.LogWarning(LogTags.CoreEditor, $"版本头未更新 [{fullPath}]：{error}");
                return false;
            }
        }

        private static bool RewriteVersionMarkerStrict(string fullPath, string version, string frameworkVersion)
        {''');p.write_text(s,encoding='utf-8')
p=root/'Core/Editor/EmberProjectSetup.Patch.cs';s=p.read_text(encoding='utf-8-sig').replace('RewriteVersionMarker(', 'RewriteVersionMarkerStrict(');p.write_text(s,encoding='utf-8')
