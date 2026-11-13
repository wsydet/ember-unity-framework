using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using File = Ember.UPMManager.Editor.EmberTemplateFileSystem.File;

namespace Ember.Core.Editor
{
    /// <summary>仅持有 Editor 元数据。文件通知合并到主线程；稳态不枚举模板或读取源码。</summary>
    [InitializeOnLoad]
    internal static class EmberEditorMetadataCache
    {
        #region 内部参数
        private sealed class Entry
        {
            internal object Value;
            internal Exception Error;
        }
        private readonly struct Stamp : IEquatable<Stamp>
        {
            private readonly long _ticks, _length;
            internal readonly bool Directory;
            internal readonly bool Content;
            internal Stamp(string path, bool directory = false, bool content = false)
            {
                Directory = directory;
                Content = content;
                try { System.IO.File.GetAttributes(path); }
                catch (FileNotFoundException) { _ticks = -1; _length = -1; return; }
                catch (DirectoryNotFoundException) { _ticks = -1; _length = -1; return; }
                if (directory)
                {
                    var folder = new DirectoryInfo(path);
                    _ticks = folder.Exists ? folder.LastWriteTimeUtc.Ticks : -1;
                    _length = -2;
                    return;
                }
                // 不吞掉权限/IO 异常；不能把无法读取的输入当作未变化。
                var info = new FileInfo(path);
                _ticks = info.Exists ? info.LastWriteTimeUtc.Ticks : -1;
                _length = info.Exists ? info.Length : -1;
            }
            public bool Equals(Stamp other) => _ticks == other._ticks && _length == other._length;
        }
        private static readonly Dictionary<string, Entry> Values = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Stamp> Inputs = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<FileSystemWatcher> Watchers = new();
        private static int _dirty, _watcherFault;
        private static long _generation;
        private static double _nextProbe;
        private static string _packageKey;
        private static PackageInfo _package;
        private static Task<bool> _probe;
        private static long _probeGeneration;
        internal static int JsonReads { get; private set; }
        internal static int TemplateScans { get; set; }
        internal static long Generation { get { Check(); return _generation; } }
        internal static PackageInfo Package { get { Check(); return _package; } }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberEditorMetadataCache()
        {
            EditorApplication.projectChanged += Invalidate;
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeWatchers;
            EditorApplication.quitting += DisposeWatchers;
        }

        private static void DisposeWatchers()
        {
            foreach (var watcher in Watchers) watcher.Dispose();
            Watchers.Clear();
        }

        private static void Watch(string root, bool recursive)
        {
            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = recursive,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
                InternalBufferSize = 65536
            };
            watcher.Changed += Changed;
            watcher.Created += Changed;
            watcher.Deleted += Changed;
            watcher.Renamed += (_, args) => { Changed(null, args); MarkDirty(); };
            watcher.Error += (_, _) => { Interlocked.Exchange(ref _watcherFault, 1); MarkDirty(); };
            watcher.EnableRaisingEvents = true;
            Watchers.Add(watcher);
        }

        private static void Changed(object sender, FileSystemEventArgs args)
        {
            if (!args.FullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) MarkDirty();
        }

        private static void MarkDirty() => Interlocked.Exchange(ref _dirty, 1);

        private static void SetupWatchers()
        {
            DisposeWatchers();
            try
            {
                if (_package != null) Watch(_package.resolvedPath, true);
                string root = Path.GetDirectoryName(Application.dataPath);
                Watch(Path.Combine(root, "ProjectSettings"), false);
                // 监视目录替换、默认配置与正式身份；不监视 Library 或生成输出。
                Watch(Application.dataPath, true);
                Interlocked.Exchange(ref _watcherFault, 0);
            }
            catch
            {
                DisposeWatchers();
                Interlocked.Exchange(ref _watcherFault, 1);
                throw;
            }
        }

        private static void Check() => Check(false);

        private static void Update()
        {
            // 查询端会报告同一错误并拒绝资格；后台检测不逐帧刷屏。
            try { Check(); } catch (Exception) { }
        }

        private static bool InputsChanged(KeyValuePair<string, Stamp>[] inputs)
        {
            foreach (var input in inputs)
                if (!input.Value.Equals(new Stamp(input.Key, input.Value.Directory, input.Value.Content))) return true;
            return false;
        }

        private static void Check(bool force, bool content = true)
        {
            try
            {
                if (_probe?.IsCompleted == true)
                {
                    var probe = _probe; _probe = null;
                    bool changed = probe.GetAwaiter().GetResult();
                    if (_probeGeneration == _generation && changed) Invalidate();
                }
                if (force || EditorApplication.timeSinceStartup >= _nextProbe)
                {
                    if (!force) _nextProbe = EditorApplication.timeSinceStartup + .5;
                    var package = PackageInfo.FindForAssembly(typeof(EmberProjectSetup).Assembly);
                    string key = Application.dataPath + "|" + package?.resolvedPath + "|" + package?.version + "|" + package?.source;
                    if (key != _packageKey || Volatile.Read(ref _watcherFault) != 0)
                    {
                        _package = package; _packageKey = key;
                        Invalidate(); SetupWatchers();
                    }
                    var inputs = new List<KeyValuePair<string, Stamp>>();
                    foreach (var input in Inputs) if (!force || content || !input.Value.Content) inputs.Add(input);
                    if (force)
                    {
                        if (InputsChanged(inputs.ToArray())) Invalidate();
                    }
                    else if (_probe == null)
                    {
                        // Unity 的文件 stat 可能很慢。后台只访问已冻结的路径/时间戳，不调用 Unity API。
                        var snapshot = inputs.ToArray();
                        _probeGeneration = _generation;
                        _probe = Task.Run(() => InputsChanged(snapshot));
                    }
                    if (_package != null) Track(Path.Combine(_package.resolvedPath, "package.json"));
                    string root = Path.GetDirectoryName(Application.dataPath);
                    Track(Path.Combine(root, "Packages/manifest.json"));
                    Track(Path.Combine(root, "Packages/packages-lock.json"));
                }
                if (Interlocked.Exchange(ref _dirty, 0) != 0) Invalidate();
                if (Volatile.Read(ref _watcherFault) != 0)
                    throw new IOException("Editor 元数据文件监视不可用；资格已拒绝，等待重新建立监视。");
            }
            catch
            {
                Invalidate();
                throw;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Invalidate()
        {
            _generation++;
            Values.Clear(); Inputs.Clear();
        }

        /// <summary>实际操作边界强制复查轻量输入。动态回调由本次检查重新求值。</summary>
        internal static void ValidateNow(bool content = true) => Check(true, content);

        /// <summary>冷重建登记已知输入；周期仅 stat，不再枚举目录、读取内容或计算 hash。</summary>
        internal static void Track(string path, bool directory = false, bool content = false)
        {
            string full = Path.GetFullPath(path);
            if (!Inputs.ContainsKey(full)) Inputs.Add(full, new Stamp(full, directory, content));
        }

        internal static T Get<T>(string key, Func<T> create)
        {
            Check();
            if (!Values.TryGetValue(key, out var entry))
            {
                long generation = _generation;
                entry = new Entry();
                try { entry.Value = create(); }
                catch (Exception error) { entry.Error = error; }
                if (generation != _generation)
                    throw new IOException("Editor 元数据在构建期间变化，请在输入稳定后重试。");
                Values[key] = entry;
            }
            if (entry.Error != null) throw new InvalidDataException("Editor 元数据不可用：" + key, entry.Error);
            return (T)entry.Value;
        }

        internal static T ReadJson<T>(string path) where T : class
        {
            string full = Path.GetFullPath(path);
            return Get("json:" + typeof(T).FullName + ":" + full, () =>
            {
                Track(full);
                if (!File.Exists(full)) return null;
                JsonReads++;
                return JsonUtility.FromJson<T>(File.ReadAllText(full))
                    ?? throw new InvalidDataException("JSON 内容为空：" + path);
            });
        }
        #endregion
    }
}
