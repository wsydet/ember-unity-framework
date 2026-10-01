using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Table;
using Game.NovelSave;
using Game.Table.Generated;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>剧情图的测试存档生成入口；输出与正式存档槽位相同的检查点和索引。</summary>
    public sealed class NovelTestSaveWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeStorySO _story;
        [SerializeField] private NarrativeChapterSO _chapter;
        [SerializeField] private NarrativeNodeSO _selected;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private EmberTableEngine _tables;
        private NarrativeTableCatalog _catalog;
        private NovelStory _definition;
        private IReadOnlyList<NovelTestSaveBuilder.Target> _targets = Array.Empty<NovelTestSaveBuilder.Target>();
        private IReadOnlyList<string[]> _chains = Array.Empty<string[]>();
        private NovelCheckpoint _settings;
        private int _targetIndex, _chainIndex, _lineIndex, _slot;
        private string _storyPath, _message;
        private bool _stale, _error;
        private Vector2 _scroll;
        private static string LocalRoot => Path.Combine(Application.persistentDataPath, "VisualNovelSaves");
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnEnable()
        {
            titleContent = new GUIContent("生成测试存档"); minSize = new Vector2(540, 440);
            EditorApplication.projectChanged += MarkStale;
            Undo.undoRedoPerformed += MarkStale;
            if (_story && _selected) RefreshData();
        }
        private void OnDisable()
        {
            EditorApplication.projectChanged -= MarkStale;
            Undo.undoRedoPerformed -= MarkStale;
            _tables?.Dispose(); _tables = null;
        }
        private void MarkStale() { _stale = true; Repaint(); }
        private void Feedback(string message, bool error = false) { _message = message; _error = error; Repaint(); }
        private string NodeLabel(NovelTestSaveBuilder.Target target)
        {
            var chapter = _story.Chapters.FirstOrDefault(c => c && c.ChapterId == target.Chapter.Id);
            var node = chapter?.Nodes.FirstOrDefault(n => n && n.NodeId == target.Node.Id);
            return (chapter ? chapter.name : target.Chapter.Id) + " / " + (node ? node.name : target.Node.Id) + " · " + target.Node.Id;
        }
        private string CallLabel(string[] chain)
        {
            if (chain.Length == 0) return "主线";
            var chapter = _story.Chapters.First(c => c && c.ChapterId == _targets[_targetIndex].Chapter.Id);
            return string.Join(" → ", chain.Select(id => chapter.Nodes.First(n => n && n.NodeId == id).name + " · " + id));
        }
        private void LoadTables()
        {
            _tables?.Dispose(); _tables = new EmberTableEngine(); _catalog = null;
            var catalog = GameTables.CreateCatalog(); var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (!asset) throw new InvalidOperationException("缺少导表产物：" + entry.ResourcePath);
                bytes.Add(entry.TableId, asset.bytes);
            }
            if (!_tables.Load(catalog, bytes).Succeeded) throw new InvalidOperationException("导表产物加载失败，请在配置表中心检查。");
            _catalog = new NarrativeTableCatalog(_tables.Database);
        }
        private void RequireSavedStory()
        {
            if (!_story || !_chapter || !_selected || !_story.Chapters.Contains(_chapter) || !_chapter.Nodes.Contains(_selected))
                throw new InvalidOperationException("请从剧情图选择属于当前剧情的节点。");
            var assets = new List<UnityEngine.Object> { _story };
            foreach (var chapter in _story.Chapters.Where(c => c))
            { assets.Add(chapter); assets.AddRange(chapter.Nodes.Where(n => n)); }
            if (_definition != null) assets.AddRange(_definition.CustomSteps.Values.Where(s => s));
            if (assets.Any(EditorUtility.IsDirty)) throw new InvalidOperationException("剧情存在未保存修改，请先保存剧情和自定义脚本资产，再重新读取节点。");
        }
        private void RefreshData()
        {
            _settings = null; _targets = Array.Empty<NovelTestSaveBuilder.Target>(); _definition = null;
            _targetIndex = _chainIndex = _lineIndex = 0; _stale = true;
            try
            {
                RequireSavedStory(); LoadTables();
                if (!_story.TryReadDefinition(_catalog, out _definition, out var errors))
                    throw new InvalidOperationException(string.Join("\n", errors.Take(5)));
                RequireSavedStory();
                string path = AssetDatabase.GetAssetPath(_story);
                int start = path.IndexOf("/Resources/", StringComparison.Ordinal);
                if (start >= 0) _storyPath = Path.ChangeExtension(path.Substring(start + "/Resources/".Length), null);
                else
                {
                    var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
                    if (!library || library.Find(_story.StoryId) != _story)
                        throw new InvalidOperationException("剧情必须位于 Resources 中或登记到当前小说库，实机才能读档。");
                    _storyPath = NarrativeLibrarySO.CURRENT_STORY;
                }
                _targets = NovelTestSaveBuilder.FindTargets(_definition, _chapter.ChapterId, _selected.NodeId);
                if (_targets.Count == 0) throw new InvalidOperationException("没有可用的前置普通节点。请在特殊节点前连接一个不含自定义步骤的对白或选择节点。");
                _stale = false; SelectTarget(); Feedback(null);
            }
            catch (Exception ex) { Feedback(ex.Message, true); }
        }
        private void SelectTarget()
        {
            _settings = null; _chainIndex = _lineIndex = 0;
            var target = _targets[_targetIndex];
            _chains = NovelTestSaveBuilder.CallChains(target.Chapter, target.Node);
            if (_chains.Count == 0) throw new InvalidOperationException("目标属于没有主线调用入口的流程，请先配置调用点。");
            // 回退到前置对话时默认停在最后一句，推进一次即可继续后面的剧情。
            if (target.Distance > 0) _lineIndex = Math.Max(0, target.Node.Commands.Count(c => c.Kind == NovelCommandKind.Say) - 1);
            ResetVariables();
        }
        private void ResetVariables()
        {
            _settings = NovelTestSaveBuilder.Defaults(_definition, _targets[_targetIndex], _storyPath, _chains[_chainIndex]);
        }
        private static void DrawVariables(string title, List<NovelVariable> variables)
        {
            if (variables.Count == 0) return;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i]; var value = variable.Value;
                var next = value.Type switch
                {
                    NovelValueType.Bool => new NovelValue(EditorGUILayout.Toggle(variable.Id, value.Bool)),
                    NovelValueType.Int => new NovelValue(EditorGUILayout.IntField(variable.Id, value.Int)),
                    _ => new NovelValue(EditorGUILayout.TextField(variable.Id, value.String))
                };
                variables[i] = new NovelVariable(variable.Id, next);
            }
        }
        private void OnGUI()
        {
            if (!NarrativeEditorAvailability.Visible)
            { EditorGUILayout.HelpBox("当前项目未启用视觉小说模块或模板。", MessageType.Info); return; }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("剧情", _story, typeof(NarrativeStorySO), false);
                EditorGUILayout.ObjectField("所选节点", _selected, typeof(NarrativeNodeSO), false);
            }
            EditorGUILayout.HelpBox("生成定位测试档：使用默认变量，可在下方调整；不补跑前置剧情，也不执行小游戏。画面从空舞台开始，所选台词之前的背景、立绘、音乐和赋值不会重放。", MessageType.Info);
            if (GUILayout.Button("重新读取节点并重置变量")) RefreshData();
            if (_stale && _settings != null) EditorGUILayout.HelpBox("资产已变化，请重新读取后生成。", MessageType.Warning);
            if (_targets.Count > 0)
            {
                int next = EditorGUILayout.Popup("实际存档节点", _targetIndex, _targets.Select(NodeLabel).ToArray());
                if (next != _targetIndex)
                {
                    _targetIndex = next;
                    try { SelectTarget(); Feedback(null); } catch (Exception ex) { Feedback(ex.Message, true); }
                }
                var target = _targets[_targetIndex];
                if (target.Distance > 0) EditorGUILayout.HelpBox("所选节点不能直接生成，已回退到前置普通节点。分支结果仍由测试变量与实机选择决定。", MessageType.Info);
                if (_chains.Count > 1)
                {
                    int chain = EditorGUILayout.Popup("从哪个调用点进入", _chainIndex, _chains.Select(CallLabel).ToArray());
                    if (chain != _chainIndex) { _chainIndex = chain; ResetVariables(); }
                }
                var lines = target.Node.Commands.Where(c => c.Kind == NovelCommandKind.Say).ToArray();
                if (lines.Length > 0)
                    _lineIndex = EditorGUILayout.Popup("停留台词", _lineIndex, lines.Select((c, i) => (i + 1) + ". " + c.Text).ToArray());
                if (_settings != null)
                {
                    DrawVariables("全局变量", _settings.Globals); DrawVariables("章节变量", _settings.Locals);
                    foreach (var frame in _settings.CallStack) DrawVariables("流程变量 · " + frame.CallId, frame.Variables);
                }
                _slot = EditorGUILayout.Popup("手动槽位", _slot, Enumerable.Range(1, 6).Select(i => "手动槽 " + i).ToArray());
                using (new EditorGUI.DisabledScope(_settings == null || _stale || EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("生成到本机存档槽")) Generate(false);
                    if (GUILayout.Button("导出实机存档目录…")) Generate(true);
                }
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox("请停止运行后生成，避免与游戏存档同时写入。", MessageType.Info);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _error ? MessageType.Error : MessageType.Info);
            if (GUILayout.Button("打开本机存档目录")) EditorUtility.RevealInFinder(Application.persistentDataPath);
            EditorGUILayout.SelectableLabel(LocalRoot, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(36));
            EditorGUILayout.EndScrollView();
        }
        private void Generate(bool export)
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeEditorAvailability.Visible || _stale)
                    throw new InvalidOperationException("请在编辑模式重新读取节点后生成。");
                RequireSavedStory(); LoadTables();
                if (!_story.TryReadDefinition(_catalog, out var definition, out var errors))
                    throw new InvalidOperationException(string.Join("\n", errors.Take(5)));
                var lines = _targets[_targetIndex].Node.Commands.Where(c => c.Kind == NovelCommandKind.Say).ToArray();
                var checkpoint = NovelTestSaveBuilder.Build(definition, _catalog, _settings, lines.Length == 0 ? null : lines[_lineIndex].CommandId);
                string root = LocalRoot;
                if (export)
                {
                    string folder = EditorUtility.OpenFolderPanel("选择测试存档导出位置", "", "");
                    if (string.IsNullOrEmpty(folder)) return;
                    root = Path.Combine(folder, "NovelTestSave-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6), "VisualNovelSaves");
                }
                var store = new NovelSaveStore(root);
                if (store.IndexError != null) throw new InvalidOperationException(store.IndexError);
                if (!export && store.Slots.Any(s => s.Slot == _slot) && !EditorUtility.DisplayDialog("覆盖手动槽 " + (_slot + 1),
                    "此槽位已有存档。生成测试档会替换该槽位，其他槽位与账号偏好保留。", "覆盖此槽", "取消")) return;
                if (!store.Save(_slot, checkpoint, out string error)) throw new InvalidOperationException(error);
                Feedback("已生成：" + NodeLabel(_targets[_targetIndex]) + "\n手动槽 " + (_slot + 1) + "\n" + root +
                    (export ? "\n在目标设备游戏关闭时，先备份原存档目录，再将导出的 VisualNovelSaves 整体放到该游戏的 persistentDataPath 下。从主菜单读取对应槽位。" : "\n启动游戏，从主菜单读取对应槽位。"));
                if (export) EditorUtility.RevealInFinder(root);
            }
            catch (Exception ex) { Feedback(ex.Message, true); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open(NarrativeChapterSO chapter, NarrativeNodeSO node, NarrativeStorySO story = null)
        {
            if (!NarrativeEditorAvailability.Visible) return;
            var window = GetWindow<NovelTestSaveWindow>();
            window._story = story ? story : chapter ? NarrativeStoryModel.FindStory(chapter) : null;
            window._chapter = chapter; window._selected = node;
            var store = new NovelSaveStore(LocalRoot);
            window._slot = Enumerable.Range(0, 6).FirstOrDefault(slot => !store.Slots.Any(s => s.Slot == slot));
            window.RefreshData(); window.Show();
        }
        #endregion
    }
}
