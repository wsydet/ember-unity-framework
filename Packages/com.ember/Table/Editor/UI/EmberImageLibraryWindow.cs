using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Table;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>源表驱动的三栏图片管理。Odin 负责分类树，图片预览保持等比显示。</summary>
    public abstract class EmberImageLibraryWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeReference] private List<Session> _sessions = new();
        [SerializeField] private List<PendingImage> _pending = new();
        [SerializeField] private int _source;
        [SerializeField] private string _branch = "", _key = "", _search = "", _newKey = "", _folderName = "";
        [SerializeField] private float _thumbnailSize = 120;
        [SerializeField] private float _previewZoom = 1;
        [SerializeField] private bool _missingOnly;
        [SerializeField] private EmberImageLibraryFolders _folders;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable] private sealed class Session
        {
            public EmberImageSource Source;
            public EmberTableDefinition Definition;
            [SerializeReference] public EmberTableSourceDocument Document;
        }
        [Serializable] private sealed class PendingImage
        {
            public string Source, Key, File, Destination;
        }
        private sealed class Branch { public int Source; public string Path; }
        private OdinMenuTree _tree;
        private Branch _pendingBranch;
        private bool _rebuild = true;
        private string _message;
        private MessageType _messageType;
        private Vector2 _gridScroll, _detailScroll;
        private readonly HashSet<string> _selected = new();
        private readonly Dictionary<string, Texture2D> _previews = new();
        private readonly Dictionary<string, string[]> _choices = new();
        private List<EmberTableSourceDocument.Row> _visible = new();
        private Session Active => _source >= 0 && _source < _sessions.Count ? _sessions[_source] : null;
        private EmberTableSourceDocument.Row Current => Active?.Document.Rows.FirstOrDefault(r => Key(Active, r) == _key);
        protected abstract string FolderAssetPath { get; }
        protected abstract IEnumerable<EmberImageSource> BuiltinSources();
        protected virtual bool EditingEnabled => true;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        protected virtual void OnEnable()
        {
            titleContent = new GUIContent("图片资源管理");
            minSize = new Vector2(1040, 620);
            saveChangesMessage = "图片源表还有草稿，是否保存？";
            _folders = AssetDatabase.LoadAssetAtPath<EmberImageLibraryFolders>(FolderAssetPath);
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += OnProjectChanged;
            if (_sessions.Count == 0) Run(LoadSources);
            _rebuild = true;
        }
        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= OnProjectChanged;
            foreach (var texture in _previews.Values) if (texture) DestroyImmediate(texture);
            _previews.Clear();
        }
        private void OnGUI()
        {
            if (!EditingEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorGUILayout.HelpBox("请启用所属模块并退出运行模式后编辑图片。", MessageType.Info); return; }
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingBranch != null)
                {
                    _source = _pendingBranch.Source; _branch = _pendingBranch.Path;
                    _key = ""; _selected.Clear(); _gridScroll = Vector2.zero; _pendingBranch = null;
                }
                if (_rebuild) RebuildTree();
                _visible = Active == null ? new() : Active.Document.Rows.Where(Matches).ToList();
            }
            DrawToolbar();
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(230), GUILayout.ExpandHeight(true)))
                {
                    EditorGUILayout.LabelField("分类与文件夹", EditorStyles.boldLabel);
                    _tree?.DrawMenuTree();
                    var branch = _tree?.Selection.FirstOrDefault()?.Value as Branch;
                    if (branch != null && (branch.Source != _source || branch.Path != _branch))
                    {
                        _pendingBranch = branch; Repaint();
                    }
                    GUILayout.FlexibleSpace();
                    _folderName = EditorGUILayout.TextField(_folderName);
                    if (GUILayout.Button("在当前分类新建文件夹")) Run(CreateFolder);
                    EditorGUILayout.HelpBox("角色／皮肤自动分组。自建文件夹只整理图片，不改变 Key 和加载路径。", MessageType.None);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) DrawGrid();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(330))) DrawDetails();
            }
            hasUnsavedChanges = _sessions.Any(s => s.Document.Dirty) || _pending.Count > 0;
            EditorGUILayout.LabelField(Active == null ? "暂无图片来源" :
                $"{Active.Source.Label} · {_visible.Count} 张 · 已选 {_selected.Count} 张 · {(hasUnsavedChanges ? "有未保存草稿" : "已保存")}", EditorStyles.miniLabel);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string Key(Session s, EmberTableSourceDocument.Row row) => s.Document.Get(row, s.Source.KeyColumn);
        private string RowBranch(Session s, EmberTableSourceDocument.Row row)
        {
            string group = s.Source.AutoGroup(s.Document, row);
            string folder = _folders ? _folders.GetFolder(s.Source.Identity, group, Key(s, row)) : "";
            return Join(group, folder);
        }
        private static string Join(string first, string second) => string.IsNullOrEmpty(first) ? second :
            string.IsNullOrEmpty(second) ? first : first + "/" + second;
        private bool Matches(EmberTableSourceDocument.Row row)
        {
            string path = RowBranch(Active, row);
            return (string.IsNullOrEmpty(_branch) || path == _branch || path.StartsWith(_branch + "/", StringComparison.Ordinal))
                && (string.IsNullOrEmpty(_search) || row.Values.Any(v => (v ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0))
                && (!_missingOnly || !Image(Active, row));
        }
        private void LoadSources()
        {
            var definitions = EmberTablePipeline.FindAllDefinitions().ToList();
            var sources = BuiltinSources().Concat(AssetDatabase.FindAssets("t:EmberImageSourceAsset")
                .Select(g => AssetDatabase.LoadAssetAtPath<EmberImageSourceAsset>(AssetDatabase.GUIDToAssetPath(g)).Source));
            var sessions = new List<Session>();
            var warnings = new List<string>();
            foreach (var source in sources.Where(s => s != null).GroupBy(s => s.Identity).Select(g => g.First()))
            {
                var definition = definitions.FirstOrDefault(d => d.TableId == source.TableId);
                if (!definition || !definition.Source) { warnings.Add("未找到图片表：" + source.TableId); continue; }
                try
                {
                    var document = sessions.FirstOrDefault(s => s.Definition == definition)?.Document
                        ?? EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(definition.Source));
                    source.Validate(document);
                    sessions.Add(new Session { Source = source, Definition = definition, Document = document });
                }
                catch (Exception ex) { warnings.Add(source.TableId + "：" + ex.Message); }
            }
            _sessions = sessions; _source = Mathf.Clamp(_source, 0, Math.Max(0, sessions.Count - 1));
            _choices.Clear(); _selected.Clear(); _rebuild = true;
            if (warnings.Count > 0) { _message = string.Join("\n", warnings); _messageType = MessageType.Warning; }
        }
        private void RebuildTree()
        {
            _tree = new OdinMenuTree(false);
            var added = new HashSet<string>(StringComparer.Ordinal);
            var roots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _sessions.Count; i++)
            {
                var session = _sessions[i];
                string root = EmberImageSource.GroupSegment(session.Source.Label);
                if (!roots.Add(root)) root += " (" + EmberImageSource.GroupSegment(session.Source.Identity) + ")";
                _tree.Add(root, new Branch { Source = i, Path = "" });
                var paths = session.Document.Rows.Select(r => RowBranch(session, r)).ToList();
                if (session.Source.GroupColumns.Length == 1)
                    paths.AddRange(Choices(session, session.Source.GroupColumns[0]).Select(EmberImageSource.GroupSegment));
                if (_folders) paths.AddRange(_folders.Folders.Where(f => f.Source == session.Source.Identity).Select(f => Join(f.Group, f.Path)));
                foreach (string path in paths.Where(p => !string.IsNullOrEmpty(p)).Distinct().OrderBy(p => p, StringComparer.Ordinal))
                {
                    string parent = "";
                    foreach (string segment in path.Split('/'))
                    {
                        parent = Join(parent, segment);
                        if (added.Add(root + "/" + parent))
                            _tree.Add(root + "/" + parent, new Branch { Source = i, Path = parent });
                    }
                }
            }
            foreach (var item in _tree.EnumerateTree())
            {
                item.Name = Uri.UnescapeDataString(item.Name);
                if (item.Value is not Branch branch) continue;
                if (branch.Path == "") item.Toggled = true;
                if (branch.Source == _source && branch.Path == _branch) item.Select();
            }
            _rebuild = false;
        }
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("图片资源管理", EditorStyles.boldLabel, GUILayout.Width(110));
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                _missingOnly = GUILayout.Toggle(_missingOnly, "仅缺失", EditorStyles.toolbarButton, GUILayout.Width(65));
                GUILayout.Label("缩略图", GUILayout.Width(45));
                _thumbnailSize = GUILayout.HorizontalSlider(_thumbnailSize, 80, 180, GUILayout.Width(90));
                if (GUILayout.Button("重新加载", EditorStyles.toolbarButton, GUILayout.Width(80))) Run(() =>
                {
                    if (hasUnsavedChanges && !EditorUtility.DisplayDialog("重新加载", "丢弃所有未保存的源表和导入草稿？", "丢弃并重新加载", "取消")) return;
                    _pending.Clear(); LoadSources();
                });
                if (GUILayout.Button("保存当前表", EditorStyles.toolbarButton, GUILayout.Width(90))) Run(() => SaveTable(Active));
            }
        }
        private void DrawGrid()
        {
            if (Active == null) { EditorGUILayout.HelpBox("通过 Create → Ember → 图片资源来源登记图片表。", MessageType.Info); return; }
            EditorGUILayout.LabelField(Active.Source.Label + (string.IsNullOrEmpty(_branch) ? "" : " / " + Uri.UnescapeDataString(_branch)), EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField("新 Key", _newKey);
                if (GUILayout.Button("新增图片", GUILayout.Width(90))) Run(AddRow);
            }
            EditorGUILayout.LabelField("点击选择；Ctrl / Cmd 多选。将图片拖到右侧绑定区可替换。", EditorStyles.miniLabel);
            _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll);
            int columns = Math.Max(1, Mathf.FloorToInt((position.width - 605) / (_thumbnailSize + 12)));
            foreach (var group in _visible.GroupBy(r => RowBranch(Active, r)).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                EditorGUILayout.LabelField(string.IsNullOrEmpty(group.Key) ? "未分组" : Uri.UnescapeDataString(group.Key), EditorStyles.boldLabel);
                var rows = group.ToList();
                for (int offset = 0; offset < rows.Count; offset += columns)
                {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var row in rows.Skip(offset).Take(columns))
                    {
                        // 切换分类后保持本事件的布局快照，下次 Layout 再更新列表。
                        if (!Active.Document.Rows.Contains(row)) continue;
                        string key = Key(Active, row);
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_thumbnailSize)))
                        {
                            Rect rect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);
                            if (_selected.Contains(key)) EditorGUI.DrawRect(rect, new Color(.2f, .45f, .65f));
                            var image = Image(Active, row);
                            if (image) GUI.DrawTexture(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), image, ScaleMode.ScaleToFit);
                            else GUI.Label(rect, "图片缺失", EditorStyles.centeredGreyMiniLabel);
                            if (GUI.Button(rect, new GUIContent("", key), GUIStyle.none))
                            {
                                bool multi = Event.current.control || Event.current.command;
                                if (!multi) _selected.Clear();
                                if (!_selected.Add(key) && multi) _selected.Remove(key);
                                _key = key; GUI.FocusControl(null);
                            }
                            GUILayout.Label(new GUIContent(key, key), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(32));
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                }
            }
            if (_visible.Count == 0) EditorGUILayout.HelpBox("当前分类没有匹配的图片，可新增 Key 或调整筛选。", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        private void AddRow()
        {
            Undo.RecordObject(this, "新增图片 Key");
            var row = Active.Document.Add(_newKey, Active.Source.KeyColumn);
            string[] segments = _branch.Split('/');
            for (int i = 0; i < Active.Source.GroupColumns.Length; i++)
            {
                string column = Active.Source.GroupColumns[i];
                string[] choices = Choices(Active, column);
                row.Values[Active.Document.Column(column)] = i < segments.Length && !string.IsNullOrEmpty(segments[i]) && segments[i] != "未分类"
                    ? Uri.UnescapeDataString(segments[i]) : choices.FirstOrDefault() ?? "";
            }
            int expression = Active.Document.Column("expression");
            if (expression >= 0) row.Values[expression] = "neutral";
            string folder = string.Join("/", segments.Skip(Active.Source.GroupColumns.Length));
            _key = _newKey; _newKey = ""; _search = ""; _missingOnly = false;
            _selected.Clear(); _selected.Add(_key);
            if (!string.IsNullOrEmpty(folder)) AssignSelected(folder);
            _branch = RowBranch(Active, row); _rebuild = true;
        }
        private void DrawDetails()
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var row = Current;
            if (row == null) EditorGUILayout.HelpBox("选择图片以编辑，或填写新 Key 后新增。", MessageType.Info);
            else
            {
                EditorGUILayout.LabelField("预览与编辑", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(_key, EditorStyles.boldLabel, GUILayout.Height(22));
                if (GUILayout.Button("复制 Key")) EditorGUIUtility.systemCopyBuffer = _key;
                var image = Image(Active, row);
                _previewZoom = EditorGUILayout.Slider("预览缩放", _previewZoom, .5f, 2f);
                Rect preview = GUILayoutUtility.GetRect(290, 220 * _previewZoom, GUILayout.ExpandWidth(true));
                DrawChecker(preview);
                if (image) GUI.DrawTexture(preview, image, ScaleMode.ScaleToFit, true);
                else GUI.Label(preview, "尚未绑定图片", EditorStyles.centeredGreyMiniLabel);
                if (image) EditorGUILayout.LabelField($"{image.width} × {image.height}", EditorStyles.centeredGreyMiniLabel);
                foreach (string column in Active.Document.Columns.Where(c => c != Active.Source.KeyColumn && c != Active.Source.ImageColumn))
                {
                    string value = Active.Document.Get(row, column);
                    string[] choices = Choices(Active, column);
                    if (choices.Length == 0) Set(row, column, EditorGUILayout.TextField(FieldLabel(column), value));
                    else
                    {
                        string[] options = new[] { value }.Concat(choices).Distinct().ToArray();
                        Set(row, column, options[EditorGUILayout.Popup(FieldLabel(column), 0, options)]);
                    }
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("绑定图片", EditorStyles.boldLabel);
                var bound = Resources.Load<Texture2D>(Active.Document.Get(row, Active.Source.ImageColumn));
                var picked = EditorGUILayout.ObjectField("项目图片", bound, typeof(Texture2D), false) as Texture2D;
                if (picked && picked != bound) Run(() => BindFile(AssetDatabase.GetAssetPath(picked)));
                if (GUILayout.Button("从本地导入 / 替换图片…")) Run(() =>
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("选择图片", "", new[] { "图片", "png,jpg,jpeg" });
                    if (!string.IsNullOrEmpty(path)) BindFile(path);
                });
                Rect drop = GUILayoutUtility.GetRect(200, 35, GUILayout.ExpandWidth(true));
                GUI.Box(drop, "拖入一张图片，绑定到当前 Key");
                HandleDrop(drop);
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("资源路径", Active.Document.Get(row, Active.Source.ImageColumn));
                if (GUILayout.Button("定位已保存图片") && bound) EditorGUIUtility.PingObject(bound);
                if (_pending.Any(p => p.Source == Active.Source.Identity && p.Key == _key))
                    EditorGUILayout.HelpBox("待导入：保存当前表时写入项目，失败会撤回本次导入。", MessageType.Info);
                DrawFolders(row);
                EditorGUILayout.Space();
                if (GUILayout.Button("保存当前表并更新运行数据", GUILayout.Height(30))) Run(() => SaveTable(Active));
                if (GUILayout.Button("定位来源表")) EditorGUIUtility.PingObject(Active.Definition.Source);
            }
            EditorGUILayout.EndScrollView();
        }
        private static string FieldLabel(string column) => column switch
        {
            "characterId" => "角色", "expression" => "表情", "skinId" => "皮肤",
            "page" => "页面", "control" => "控件", "node" => "相对节点", _ => column
        };
        private string[] Choices(Session session, string column)
        {
            string id = session.Source.TableId + "/" + column;
            if (_choices.TryGetValue(id, out var cached)) return cached;
            var member = session.Definition.RowType?.GetMembers().FirstOrDefault(m => m.GetCustomAttribute<EmberTableColumnAttribute>()?.Name == column);
            string target = member?.GetCustomAttribute<EmberTableReferenceAttribute>()?.TargetTableId;
            var definition = string.IsNullOrEmpty(target) ? null : EmberTablePipeline.FindAllDefinitions().FirstOrDefault(d => d.TableId == target);
            string[] values = Array.Empty<string>();
            if (definition && definition.Source)
            {
                var keyMember = definition.RowType?.GetMembers().FirstOrDefault(m => m.IsDefined(typeof(EmberTableKeyAttribute), false));
                string keyColumn = keyMember?.GetCustomAttribute<EmberTableColumnAttribute>()?.Name ?? "id";
                var document = EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(definition.Source));
                values = document.Rows.Select(r => document.Get(r, keyColumn)).ToArray();
            }
            return _choices[id] = values;
        }
        private void Set(EmberTableSourceDocument.Row row, string column, string value)
        {
            int index = Active.Document.Column(column);
            if (row.Values[index] == value) return;
            Undo.RecordObject(this, "编辑图片源表"); row.Values[index] = value;
            if (Active.Source.GroupColumns.Contains(column)) _rebuild = true;
        }
        private void DrawFolders(EmberTableSourceDocument.Row row)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("文件夹 · 支持批量整理", EditorStyles.boldLabel);
            string group = Active.Source.AutoGroup(Active.Document, row);
            EditorGUILayout.LabelField("自动分类", Uri.UnescapeDataString(group));
            var paths = new[] { "" }.Concat(_folders ? _folders.Folders.Where(f => f.Source == Active.Source.Identity && f.Group == group).Select(f => f.Path) : Array.Empty<string>()).Distinct().ToArray();
            string current = _folders ? _folders.GetFolder(Active.Source.Identity, group, _key) : "";
            int index = Math.Max(0, Array.IndexOf(paths, current));
            int next = EditorGUILayout.Popup("移动所选图片到", index, paths.Select(p => p == "" ? "（自动分类根目录）" : p).ToArray());
            if (next != index) Run(() => AssignSelected(paths[next]));
            _folderName = EditorGUILayout.TextField("新文件夹", _folderName);
            if (GUILayout.Button("创建文件夹并移入所选图片")) Run(() =>
            {
                if (string.IsNullOrWhiteSpace(_folderName)) throw new InvalidOperationException("请填写文件夹名，可用 / 创建多层目录。");
                AssignSelected(_folderName); _folderName = "";
            });
            EditorGUILayout.HelpBox("多选图片会分别整理到各自角色／皮肤下的同名文件夹；目录立即保存，可撤销。", MessageType.None);
        }
        private void AssignSelected(string path)
        {
            EmberImageLibraryFolders.ValidatePath(path);
            EnsureFolders();
            Undo.RecordObject(_folders, "整理图片文件夹");
            foreach (var row in Active.Document.Rows.Where(r => _selected.Contains(Key(Active, r)) || (_selected.Count == 0 && Key(Active, r) == _key)))
                _folders.Assign(Active.Source.Identity, Active.Source.AutoGroup(Active.Document, row), Key(Active, row), path);
            EditorUtility.SetDirty(_folders); AssetDatabase.SaveAssetIfDirty(_folders); _rebuild = true;
        }
        private void EnsureFolders()
        {
            if (_folders) return;
            EnsureAssetFolder(Path.GetDirectoryName(FolderAssetPath).Replace('\\', '/'));
            _folders = CreateInstance<EmberImageLibraryFolders>();
            AssetDatabase.CreateAsset(_folders, FolderAssetPath);
        }
        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            if (!folder.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("只能创建项目 Assets 下的目录。");
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureAssetFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder))))
                throw new IOException("不能创建目录：" + folder);
        }
        private void CreateFolder()
        {
            if (Active == null) return;
            EmberImageLibraryFolders.ValidatePath(_folderName);
            if (string.IsNullOrWhiteSpace(_folderName)) throw new InvalidOperationException("请填写新文件夹名称。");
            int count = Active.Source.GroupColumns.Length;
            string[] segments = string.IsNullOrEmpty(_branch) ? Array.Empty<string>() : _branch.Split('/');
            if (segments.Length < count) throw new InvalidOperationException("请先选择一个角色，或皮肤下的具体页面。");
            string group = string.Join("/", segments.Take(count));
            string path = Join(string.Join("/", segments.Skip(count)), _folderName);
            EnsureFolders(); Undo.RecordObject(_folders, "新建图片文件夹");
            _folders.AddFolder(Active.Source.Identity, group, path);
            EditorUtility.SetDirty(_folders); AssetDatabase.SaveAssetIfDirty(_folders);
            _branch = Join(group, path); _folderName = ""; _rebuild = true;
        }
        private Texture2D Image(Session session, EmberTableSourceDocument.Row row)
        {
            var pending = _pending.FirstOrDefault(p => p.Source == session.Source.Identity && p.Key == Key(session, row));
            string path = pending?.File ?? session.Document.Get(row, session.Source.ImageColumn);
            if (string.IsNullOrEmpty(path)) return null;
            if (pending == null)
            {
                if (!session.Source.Sprite) return Resources.Load<Texture2D>(path);
                var sprite = Resources.Load<Sprite>(path);
                return sprite ? sprite.texture : null;
            }
            if (_previews.TryGetValue(path, out var cached)) return cached;
            var texture = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (texture.LoadImage(File.ReadAllBytes(path))) return _previews[path] = texture;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
            DestroyImmediate(texture); return _previews[path] = null;
        }
        private void BindFile(string file)
        {
            string full = Path.GetFullPath(file);
            string root = Path.GetFullPath(".").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string asset = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length).Replace('\\', '/') : "";
            if (asset.StartsWith("Assets/", StringComparison.Ordinal) && asset.Contains("/Resources/"))
            {
                string resource = ResourcePath(asset);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
                if (!texture || Resources.Load<Texture2D>(resource) != texture)
                    throw new InvalidOperationException("图片路径无法唯一加载，请检查重复的 Resources 路径。");
                if (Active.Source.Sprite && !Resources.Load<Sprite>(resource))
                    throw new InvalidOperationException("此来源需要 Sprite，请先在图片导入设置中选择 Sprite (2D and UI) / Single。");
                Undo.RecordObject(this, "绑定图片");
                _pending.RemoveAll(p => p.Source == Active.Source.Identity && p.Key == _key);
                Set(Current, Active.Source.ImageColumn, resource); return;
            }
            string extension = Path.GetExtension(full).ToLowerInvariant();
            if (!new[] { ".png", ".jpg", ".jpeg" }.Contains(extension) || !File.Exists(full))
                throw new InvalidOperationException("本地导入支持 PNG / JPG 图片。");
            string destination = Active.Source.ImportFolder.TrimEnd('/') + "/" + Guid.NewGuid().ToString("N") + extension;
            if (destination.Split('/').Any(p => p == "." || p == ".." || p.Contains('\\')))
                throw new InvalidOperationException("导入目录不能包含 .、.. 或反斜杠。");
            string destinationFull = Path.GetFullPath(destination);
            if (!destination.StartsWith("Assets/", StringComparison.Ordinal) || !destination.Contains("/Resources/")
                || !destinationFull.StartsWith(Path.GetFullPath("Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("导入目录必须位于项目 Assets 的 Resources 内。");
            Undo.RecordObject(this, "准备导入图片");
            _pending.RemoveAll(p => p.Source == Active.Source.Identity && p.Key == _key);
            _pending.Add(new PendingImage { Source = Active.Source.Identity, Key = _key, File = full, Destination = destination });
            Set(Current, Active.Source.ImageColumn, ResourcePath(destination));
        }
        private static string ResourcePath(string asset)
        {
            int start = asset.IndexOf("/Resources/", StringComparison.Ordinal) + "/Resources/".Length;
            return asset.Substring(start, asset.Length - start - Path.GetExtension(asset).Length);
        }
        private void HandleDrop(Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition) || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)) return;
            DragAndDrop.visualMode = DragAndDrop.paths.Length == 1 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (evt.type == EventType.DragPerform && DragAndDrop.paths.Length == 1)
            { DragAndDrop.AcceptDrag(); Run(() => BindFile(DragAndDrop.paths[0])); }
            evt.Use();
        }
        private void SaveTable(Session session)
        {
            if (session == null) return;
            var related = _sessions.Where(s => s.Document == session.Document).ToArray();
            var imports = _pending.Where(p => related.Any(s => s.Source.Identity == p.Source)).ToArray();
            var created = new List<string>();
            try
            {
                foreach (var pending in imports)
                {
                    if (File.Exists(pending.Destination)) throw new IOException("导入目标已存在，请重新选择图片。");
                    EnsureAssetFolder(Path.GetDirectoryName(pending.Destination).Replace('\\', '/'));
                    File.Copy(pending.File, pending.Destination, false); created.Add(pending.Destination);
                    AssetDatabase.ImportAsset(pending.Destination, ImportAssetOptions.ForceSynchronousImport);
                    if (related.First(s => s.Source.Identity == pending.Source).Source.Sprite)
                    {
                        var importer = AssetImporter.GetAtPath(pending.Destination) as TextureImporter;
                        if (!importer) throw new InvalidOperationException("不能识别图片导入器。");
                        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                        importer.SaveAndReimport();
                    }
                }
                foreach (var entry in related)
                    foreach (var row in entry.Document.Rows)
                    {
                        string path = entry.Document.Get(row, entry.Source.ImageColumn);
                        bool exists = !string.IsNullOrWhiteSpace(path) && (entry.Source.Sprite ? Resources.Load<Sprite>(path) != null : Resources.Load<Texture2D>(path) != null);
                        if (!exists) throw new InvalidOperationException("图片缺失或类型不匹配：" + Key(entry, row));
                    }
                session.Document.Save(session.Definition);
                _pending.RemoveAll(p => imports.Contains(p));
                // 保存是草稿事务边界，不能撤销到已导入但已被外部加载的旧草稿状态。
                Undo.ClearUndo(this);
                _message = "源表已保存，运行数据已更新。"; _messageType = MessageType.Info;
            }
            catch
            {
                foreach (string path in created) AssetDatabase.DeleteAsset(path);
                throw;
            }
        }
        private static void DrawChecker(Rect rect)
        {
            for (int y = 0; y < Mathf.CeilToInt(rect.height / 16); y++)
                for (int x = 0; x < Mathf.CeilToInt(rect.width / 16); x++)
                    EditorGUI.DrawRect(new Rect(rect.x + x * 16, rect.y + y * 16, Mathf.Min(16, rect.width - x * 16), Mathf.Min(16, rect.height - y * 16)),
                        (x + y) % 2 == 0 ? new Color(.22f, .22f, .22f) : new Color(.3f, .3f, .3f));
        }
        private void OnUndo()
        {
            if (_folders) { EditorUtility.SetDirty(_folders); AssetDatabase.SaveAssetIfDirty(_folders); }
            _rebuild = true; Repaint();
        }
        private void OnProjectChanged() { _choices.Clear(); _rebuild = true; Repaint(); }
        private void Run(Action action)
        {
            try { _message = null; action(); }
            catch (Exception ex) { _message = ex.Message; _messageType = MessageType.Error; }
            Repaint();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void SaveChanges()
        {
            foreach (var session in _sessions.GroupBy(s => s.Document).Select(g => g.First()))
                if (session.Document.Dirty || _pending.Any(p => _sessions.Any(s => s.Document == session.Document && s.Source.Identity == p.Source))) SaveTable(session);
            base.SaveChanges();
        }
        public override void DiscardChanges()
        {
            _pending.Clear(); _sessions.Clear(); base.DiscardChanges();
        }
        #endregion
    }
}
