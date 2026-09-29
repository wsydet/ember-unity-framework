using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.Utilities.Editor;
using Game.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    public sealed class NovelUISkinWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private NovelUISkin _skin, _source;
        [SerializeField] private NarrativeStorySO _story;
        [SerializeField] private string _id = "new_skin", _displayName = "新皮肤";
        [SerializeField] private bool _pendingOnly, _before, _create;
        [SerializeField] private int _resolution, _pageIndex;
        [SerializeField] private string _imageSearch = "";
        [SerializeField] private bool _showReplacement = true, _showAppearance = true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private NovelUISkinPreview _preview;
        private string[] _prefabs = Array.Empty<string>();
        private Dictionary<string, string> _legacy = new();
        private string _legacyId, _selectedKey, _message;
        private Vector2 _listScroll, _detailScroll;
        private Color _color;
        private Image.Type _type;
        private bool _aspect;
        private float _ppu;
        private Vector4 _border;
        private readonly Dictionary<string, string> _batch = new();
        private static readonly Vector2Int[] Sizes = { new(1920, 1080), new(1440, 1080), new(1280, 720) };
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            titleContent = new GUIContent("视觉小说皮肤"); minSize = new Vector2(1000, 650);
            Undo.undoRedoPerformed += Changed; EditorApplication.playModeStateChanged += PlayModeChanged;
            Run(() => { _legacy = NovelUISkinEditorService.LegacySkins(); RefreshPages(); });
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Changed; EditorApplication.playModeStateChanged -= PlayModeChanged;
            _preview?.Dispose(); _preview = null;
        }
        private void PlayModeChanged(PlayModeStateChange state)
        { _preview?.Dispose(); _preview = null; Repaint(); }
        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorGUILayout.HelpBox("请退出 Play Mode 后编辑皮肤。", MessageType.Info); return; }
            DrawHeader();
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            if (!_skin) { EditorGUILayout.HelpBox("选择现有皮肤，或展开创建面板，从基础外观或已有皮肤创建独立副本。", MessageType.Info); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawImages();
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawPreview(); DrawAssignment();
                }
                DrawDetails();
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { _message = ex.Message; }
            Repaint();
        }
        private void Changed() { Select(_skin ? _skin.Images.FirstOrDefault(i => i.Key == _selectedKey) : null); Repaint(); }
        private void RefreshPages()
        {
            _prefabs = NovelUISkinEditorService.Prefabs(); _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, _prefabs.Length - 1));
            RebuildPreview();
        }
        private void RebuildPreview()
        {
            _preview?.Dispose(); _preview = null;
            if (_prefabs.Length == 0) return;
            var size = Sizes[_resolution]; _preview = new NovelUISkinPreview(_prefabs[_pageIndex], size.x, size.y);
        }
        private void DrawHeader()
        {
            SirenixEditorGUI.Title("UI 皮肤编辑器", _skin ? _skin.DisplayName + " · 选择素材 → 调整效果 → 关联剧情" : "选择或创建皮肤", TextAlignment.Left, true);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var selected = (NovelUISkin)EditorGUILayout.ObjectField(_skin, typeof(NovelUISkin), false, GUILayout.Width(260));
                if (selected != _skin) { _skin = selected; _batch.Clear(); Select(null); }
                _create = GUILayout.Toggle(_create, "创建新皮肤", EditorStyles.toolbarButton);
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton)) Run(() => { _legacy = NovelUISkinEditorService.LegacySkins(); RefreshPages(); });
                using (new EditorGUI.DisabledScope(!_skin))
                    if (GUILayout.Button("检查皮肤", EditorStyles.toolbarButton)) Run(() =>
                    {
                        var issues = NovelUISkinEditorService.Validate(_skin);
                        _message = issues.Length == 0 ? "目标与资源检查通过。" : string.Join("\n", issues);
                    });
            }
            if (!_create) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _id = EditorGUILayout.TextField("皮肤标识", _id); _displayName = EditorGUILayout.TextField("显示名称", _displayName);
                _source = (NovelUISkin)EditorGUILayout.ObjectField("复制已有皮肤", _source, typeof(NovelUISkin), false);
                using (new EditorGUI.DisabledScope(_source))
                {
                    var ids = new[] { "" }.Concat(_legacy.Keys).ToArray();
                    int selected = Mathf.Max(0, Array.IndexOf(ids, _legacyId));
                    selected = EditorGUILayout.Popup("基础来源", selected, new[] { "基础外观（当前正式 UI）" }.Concat(_legacy.Values).ToArray());
                    _legacyId = ids[selected];
                }
                EditorGUILayout.HelpBox("复制配置和图片，生成独立占位资源。创建不会改变任何剧情当前使用的皮肤。", MessageType.None);
                if (GUILayout.Button("复制并创建")) Run(() =>
                {
                    _skin = NovelUISkinEditorService.Create(_id, _displayName, _source, _source ? null : _legacyId);
                    _create = false; _batch.Clear(); Select(null); _message = "皮肤已创建，图片已标记为待替换。";
                    EditorGUIUtility.PingObject(_skin);
                });
            }
        }
        private void DrawImages()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(250)))
            {
                EditorGUILayout.LabelField(_skin.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"待替换 {_skin.Images.Count(e => e.Pending)} / {_skin.Images.Count}");
                _pendingOnly = EditorGUILayout.Toggle("只看待替换", _pendingOnly);
                _imageSearch = EditorGUILayout.TextField(_imageSearch, EditorStyles.toolbarSearchField);
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                string page = _prefabs.Length > 0 ? Path.GetFileNameWithoutExtension(_prefabs[_pageIndex]) : "";
                foreach (var entry in _skin.Images.Where(e => e.Page == page && (!_pendingOnly || e.Pending)))
                {
                    if (!string.IsNullOrEmpty(_imageSearch) && entry.Key.IndexOf(_imageSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string label = (entry.Pending ? "○ " : "● ") + (string.IsNullOrEmpty(entry.Control) ? "页面" : entry.Control) + "/" + entry.Node;
                    if (GUILayout.Toggle(_selectedKey == entry.Key, new GUIContent(label, entry.Key), "Button"))
                        if (_selectedKey != entry.Key) Select(entry);
                }
                EditorGUILayout.EndScrollView();
                using (new EditorGUI.DisabledScope(_batch.Count == 0))
                    if (GUILayout.Button($"应用批量替换（{_batch.Count}）")) Run(() =>
                    {
                        NovelUISkinEditorService.ReplaceImages(_skin, _batch.Select(p => new NovelUISkinEditorService.Replacement { Key = p.Key, File = p.Value }).ToArray());
                        _batch.Clear(); Changed(); _message = "素材已替换。请预览并调整显示参数。";
                    });
            }
        }
        private void DrawPreview()
        {
            if (_prefabs.Length == 0) return;
            EditorGUI.BeginChangeCheck();
            _pageIndex = EditorGUILayout.Popup("页面", _pageIndex, _prefabs.Select(Path.GetFileNameWithoutExtension).ToArray());
            _resolution = EditorGUILayout.Popup("预览尺寸", _resolution, Sizes.Select(s => s.x + " × " + s.y).ToArray());
            if (EditorGUI.EndChangeCheck()) Run(() => { Select(null); RebuildPreview(); });
            _before = EditorGUILayout.Toggle("对比基础外观", _before);
            var available = GUILayoutUtility.GetRect(300, 280, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var size = Sizes[_resolution]; float fit = Mathf.Min(available.width / size.x, available.height / size.y);
            var area = new Rect(available.center - new Vector2(size.x, size.y) * fit / 2, new Vector2(size.x, size.y) * fit);
            if (Event.current.type == EventType.Repaint && _preview != null)
            {
                try { GUI.DrawTexture(area, _preview.Render(area, _before ? null : _skin), ScaleMode.StretchToFill, false); }
                catch (Exception ex) { _message = "预览失败：" + ex.Message; _preview.Dispose(); _preview = null; }
            }
            if (GUILayout.Button("导出当前预览 PNG")) Run(() =>
            {
                string path = EditorUtility.SaveFilePanel("保存皮肤预览", ".utmp", _skin.Id + "-" + size.x, "png");
                if (!string.IsNullOrEmpty(path)) { _preview.SavePng(path, _before ? null : _skin); _message = "预览已保存：" + path; }
            });
        }
        private void Select(NovelUISkinImage entry)
        {
            _selectedKey = entry?.Key;
            if (entry == null) return;
            _color = entry.Color; _type = entry.Type; _aspect = entry.PreserveAspect;
            _ppu = entry.PixelsPerUnitMultiplier; _border = entry.Sprite ? entry.Sprite.border : Vector4.zero;
        }
        private void DrawDetails()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(275)))
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                var entry = _skin.Images.FirstOrDefault(e => e.Key == _selectedKey);
                if (entry != null)
                {
                    SirenixEditorGUI.Title("当前素材", entry.Pending ? "待替换" : "已完成", TextAlignment.Left, true);
                    EditorGUILayout.SelectableLabel(entry.Key, EditorStyles.wordWrappedLabel, GUILayout.Height(48));
                    _showReplacement = SirenixEditorGUI.Foldout(_showReplacement, "素材替换 · 加入待应用列表");
                    if (_showReplacement)
                    {
                        var sprite = (Sprite)EditorGUILayout.ObjectField("替换图片", entry.Sprite, typeof(Sprite), false);
                        if (sprite && sprite != entry.Sprite) Run(() =>
                        {
                            string path = AssetDatabase.GetAssetPath(sprite);
                            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            if (!importer || importer.spriteImportMode != SpriteImportMode.Single)
                                throw new ArgumentException("请选择独立图片；图集子图请先导出为单独图片再替换。");
                            Queue(entry, path);
                        });
                        var drop = GUILayoutUtility.GetRect(120, 64, GUILayout.ExpandWidth(true)); GUI.Box(drop, "将图片文件拖到这里");
                        var ev = Event.current;
                        if (drop.Contains(ev.mousePosition) && (ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (ev.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                if (DragAndDrop.paths.Length == 1) Run(() => Queue(entry, DragAndDrop.paths[0]));
                                else _message = "一个目标对应一张图片；请逐项指定后批量应用。";
                            }
                            ev.Use();
                        }
                        if (GUILayout.Button("选择图片文件"))
                        {
                            string path = EditorUtility.OpenFilePanel("选择替换图片", "", "png,jpg,jpeg,tga,psd");
                            if (!string.IsNullOrEmpty(path)) Queue(entry, path);
                        }
                        if (_batch.TryGetValue(entry.Key, out string queued)) EditorGUILayout.HelpBox("待应用：" + queued, MessageType.Info);
                    }
                    _showAppearance = SirenixEditorGUI.Foldout(_showAppearance, "显示效果 · 保存到当前皮肤");
                    if (_showAppearance)
                    {
                        SirenixEditorGUI.BeginBox();
                        _type = (Image.Type)EditorGUILayout.EnumPopup("图片显示方式", _type);
                        _aspect = EditorGUILayout.Toggle("保持比例", _aspect);
                        _color = EditorGUILayout.ColorField("颜色与透明度", _color);
                        _ppu = EditorGUILayout.FloatField("九宫格比例", _ppu);
                        _border = EditorGUILayout.Vector4Field("边距（左 下 右 上）", _border);
                        if (GUILayout.Button("保存显示效果")) Run(() =>
                        {
                            NovelUISkinEditorService.SetAppearance(_skin, entry.Key, _color, _type, _aspect, _ppu, _border);
                            _message = "显示参数已保存。";
                        });
                        SirenixEditorGUI.EndBox();
                    }
                    if (GUILayout.Button(entry.Pending ? "保留此图，标记完成" : "重新标记为待替换"))
                    { Undo.RecordObject(_skin, "标记皮肤素材"); entry.Pending = !entry.Pending; EditorUtility.SetDirty(_skin); AssetDatabase.SaveAssets(); }
                }
                else EditorGUILayout.HelpBox("在左侧选择需要替换的图片。", MessageType.None);
                EditorGUILayout.EndScrollView();
            }
        }
        private void Queue(NovelUISkinImage entry, string path)
        { _batch[entry.Key] = Path.GetFullPath(path); _message = "已加入替换列表；指定其他目标后可一起应用。"; }
        private void DrawAssignment()
        {
            SirenixEditorGUI.BeginBox("剧情关联");
            _story = (NarrativeStorySO)EditorGUILayout.ObjectField("目标剧情", _story, typeof(NarrativeStorySO), false);
            using (new EditorGUI.DisabledScope(!_story))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用此皮肤")) Run(() => { NovelUISkinEditorService.Assign(_story, _skin); _message = "剧情关联已保存，下次打开页面生效。"; });
                if (GUILayout.Button("恢复基础外观")) Run(() => { NovelUISkinEditorService.Assign(_story, null); _message = "已恢复基础外观，下次打开页面生效。"; });
            }
            SirenixEditorGUI.EndBox();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [MenuItem("Ember/视觉小说/UI 皮肤编辑器")]
        public static void Open() => GetWindow<NovelUISkinWindow>().Show();
        #endregion
    }
}
