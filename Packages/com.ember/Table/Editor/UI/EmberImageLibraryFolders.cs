using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>仅编辑器使用的逻辑目录；身份同时包含来源、自动分组和 Key。</summary>
    public sealed class EmberImageLibraryFolders : ScriptableObject
    {
        #region 编辑器面板参数
        public List<Folder> Folders = new();
        public List<Assignment> Assignments = new();
        [Serializable] public sealed class Folder { public string Source, Group, Path; }
        [Serializable] public sealed class Assignment { public string Source, Group, Key, Path; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string GetFolder(string source, string group, string key)
            => Assignments.FirstOrDefault(a => a.Source == source && a.Group == group && a.Key == key)?.Path ?? "";
        public void Assign(string source, string group, string key, string path)
        {
            ValidatePath(path);
            Assignments.RemoveAll(a => a.Source == source && a.Group == group && a.Key == key);
            if (string.IsNullOrEmpty(path)) return;
            AddFolder(source, group, path);
            Assignments.Add(new Assignment { Source = source, Group = group, Key = key, Path = path });
        }
        public void AddFolder(string source, string group, string path)
        {
            ValidatePath(path);
            if (string.IsNullOrEmpty(path)) return;
            if (!Folders.Any(f => f.Source == source && f.Group == group && f.Path == path))
                Folders.Add(new Folder { Source = source, Group = group, Path = path });
        }
        public static void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (path.Split('/').Any(p => string.IsNullOrWhiteSpace(p) || p != p.Trim() || p == "." || p == ".." || p.Contains('\\')))
                throw new InvalidOperationException("文件夹请使用非空名称，多级目录用 / 分隔，不能包含 .、.. 或反斜杠。");
        }
        #endregion
    }
}
