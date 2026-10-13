using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Ember.Core.Editor
{
    /// <summary>按业务提供的条件注册或移除菜单；不推断模板身份。</summary>
    [InitializeOnLoad]
    public static class EmberConditionalMenu
    {
        #region 内部参数
        private sealed class Entry : IDisposable
        {
            public string Path;
            public Func<bool> Visible, Enabled;
            public Action Execute;
            public int Priority;
            public bool Installed;
            public void Dispose()
            {
                if (!Entries.Remove(this)) return;
                if (Installed) Remove.Invoke(null, new object[] { Path });
                Installed = false;
            }
        }
        private static readonly List<Entry> Entries = new();
        // Unity 6 没有公开的动态菜单 API；反射集中在公共适配层，签名不匹配时拒绝注册。
        private static readonly Type Menu = typeof(EditorApplication).Assembly.GetType("UnityEditor.Menu");
        private static readonly MethodInfo Add = Menu?.GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(string), typeof(string), typeof(bool), typeof(int), typeof(Action), typeof(Func<bool>) }, null);
        private static readonly MethodInfo Remove = Menu?.GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(string) }, null);
        private static double _nextCheck;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberConditionalMenu()
        {
            EditorApplication.update += Update;
            EditorApplication.projectChanged += Refresh;
        }
        private static void Update()
        {
            if (EditorApplication.timeSinceStartup < _nextCheck) return;
            _nextCheck = EditorApplication.timeSinceStartup + 1;
            Refresh();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static IDisposable Register(string path, Func<bool> visible, Action execute, Func<bool> enabled = null, int priority = 100)
        {
            if (Add == null || Remove == null) throw new NotSupportedException("当前 Unity 不支持条件菜单适配。");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("菜单路径不能为空。", nameof(path));
            if (Entries.Exists(e => e.Path == path)) throw new InvalidOperationException("菜单已登记：" + path);
            var entry = new Entry { Path = path, Visible = visible ?? throw new ArgumentNullException(nameof(visible)),
                Execute = execute ?? throw new ArgumentNullException(nameof(execute)), Enabled = enabled, Priority = priority };
            Entries.Add(entry);
            // InitializeOnLoad 阶段等待 Unity 完成自己的静态菜单重建。
            EditorApplication.delayCall += Refresh;
            return entry;
        }
        public static void Refresh()
        {
            foreach (var entry in Entries)
            {
                bool visible = entry.Visible();
                if (visible == entry.Installed) continue;
                if (visible)
                {
                    Func<bool> validate = () => entry.Visible() && (entry.Enabled?.Invoke() ?? true);
                    Action execute = () => { if (validate()) entry.Execute(); };
                    Add.Invoke(null, new object[] { entry.Path, "", false, entry.Priority, execute, validate });
                }
                else Remove.Invoke(null, new object[] { entry.Path });
                entry.Installed = visible;
            }
        }
        #endregion
    }
}
