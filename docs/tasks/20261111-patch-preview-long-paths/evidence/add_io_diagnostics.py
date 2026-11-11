from pathlib import Path
import re
p=Path('Packages/com.ember/UPMManager/Editor/EmberTemplateFileSystem.cs')
s=p.read_text(encoding='utf-8')
anchor='        private static bool Exists(string path, bool directory)'
s=s.replace(anchor, '''        private static T Access<T>(string operation, string path, Func<T> action)
        {
            try { return action(); }
            catch (Exception error) { error.Data["EmberIOOperation"] = operation; error.Data["EmberIOPath"] = path; throw; }
        }

        private static void Access(string operation, string path, Action action)
        {
            Access(operation, path, () => { action(); return true; });
        }

        private static IEnumerable<string> ReadLines(string path)
        {
            using var iterator = Access("ReadLines", path, () => System.IO.File.ReadLines(NativePath(path)).GetEnumerator());
            while (Access("ReadLines", path, () => iterator.MoveNext())) yield return iterator.Current;
        }

'''+anchor)
# Preserve exception objects; diagnostics always name ordinary logical paths, including both copy/move endpoints.
pat=r'(public static [^\n]+? (\w+)\([^\n]*?\) => )([^;\n]+);'
def wrap(m):
    name=m[2];body=m[3]
    if name=='ReadLines': body='EmberTemplateFileSystem.ReadLines(path)'
    if name=='GetParent': return m[0]
    params=m[1].split('(')[1]
    paths='source + " → " + destination' if 'string source' in params else 'path'
    return m[1]+'Access("'+name+'", '+paths+', () => '+body+');'
s=re.sub(pat,wrap,s)
p.write_text(s,encoding='utf-8')
