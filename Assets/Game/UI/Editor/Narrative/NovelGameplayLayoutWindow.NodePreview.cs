using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Table;
using Game.Narrative;
using Game.Table.Generated;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    public sealed partial class NovelGameplayLayoutWindow
    {
        #region 编辑器面板参数
        [SerializeField] private bool _nodePreview, _followNode = true, _usePreviewContext;
        [SerializeField] private NarrativeNodeSO _previewNode;
        [SerializeField] private int _previewLine;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private EmberTableEngine _tableEngine;
        private NarrativeTableCatalog _catalog;
        private string _nodeJson, _nodeNotice;
        private bool _tablesDirty = true;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnNodeSelection()
        {
            if (_followNode && Selection.activeObject is NarrativeNodeSO node) SetPreviewNode(node);
        }
        private void OnPreviewProjectChanged() { _tablesDirty = true; _nodeJson = null; }
        private void OnInspectorUpdate()
        {
            if (!_contents || !_nodePreview || !CanEdit(out _)) return;
            string json = _previewNode ? EditorJsonUtility.ToJson(_previewNode) : string.Empty;
            if (_nodeJson == json && !_tablesDirty) return;
            _nodeJson = json; RebuildPreview(); Repaint();
        }
        private void SetPreviewNode(NarrativeNodeSO node)
        {
            if (!CanEdit(out _)) return;
            if (_previewNode != node) _previewLine = 0;
            _previewNode = node; _nodePreview = true;
            _nodeJson = node ? EditorJsonUtility.ToJson(node) : string.Empty;
            if (_contents) RebuildPreview();
            Repaint();
        }
        private void DrawNodeControls()
        {
            EditorGUI.BeginChangeCheck();
            _nodePreview = EditorGUILayout.Toggle("预览剧情节点", _nodePreview);
            if (_nodePreview)
            {
                _followNode = EditorGUILayout.Toggle("跟随图编辑器 / Project 选择", _followNode);
                var node = (NarrativeNodeSO)EditorGUILayout.ObjectField("节点 SO", _previewNode, typeof(NarrativeNodeSO), false);
                if (node != _previewNode) { _previewNode = node; _previewLine = 0; }
                var lines = (_previewNode as NarrativeDialogueSO)?.Commands.Where(c => c?.Kind == NovelCommandKind.Say).ToArray();
                if (lines?.Length > 0)
                {
                    _previewLine = Mathf.Clamp(_previewLine, 0, lines.Length - 1);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(_previewLine == 0))
                            if (GUILayout.Button("上一句", GUILayout.Width(60))) { _previewLine--; GUI.changed = true; }
                        _previewLine = EditorGUILayout.Popup(_previewLine, lines.Select((c, i) => (i + 1) + "/" + lines.Length + "  " + (c.Text ?? "").Replace('\n', ' ')).ToArray());
                        using (new EditorGUI.DisabledScope(_previewLine == lines.Length - 1))
                            if (GUILayout.Button("下一句", GUILayout.Width(60))) { _previewLine++; GUI.changed = true; }
                    }
                }
                _usePreviewContext = EditorGUILayout.Toggle("使用右侧图片作为起始画面", _usePreviewContext);
                EditorGUILayout.HelpBox("仅计算本节点到当前句的画面；不推断前置分支。选项展示全部文案，不判断条件。", MessageType.None);
            }
            if (EditorGUI.EndChangeCheck()) RebuildPreview();
            if (_nodePreview && !string.IsNullOrEmpty(_nodeNotice)) EditorGUILayout.HelpBox(_nodeNotice, MessageType.Warning);
        }
        private void EnsurePreviewTables()
        {
            if (!_tablesDirty) return;
            _tablesDirty = false; _catalog = null;
            _tableEngine?.Dispose(); _tableEngine = new EmberTableEngine();
            var catalog = GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (!asset) throw new InvalidOperationException("缺少导表产物：" + entry.ResourcePath);
                bytes.Add(entry.TableId, asset.bytes);
            }
            if (!_tableEngine.Load(catalog, bytes).Succeeded) throw new InvalidOperationException("导表产物加载失败，请检查配置表中心。");
            _catalog = new NarrativeTableCatalog(_tableEngine.Database);
            // 预览与运行期保持一致：装上多语言与皮肤，预览才会显示当前语言的文本与皮肤图。
            NovelLocalization.Install(_catalog); NovelSkin.Install(_catalog);
        }
        private void ApplyNodePreview()
        {
            _nodeNotice = null; _previewTextMode = NovelTextMode.Dialogue;
            var speaker = _previewTargets["Speaker"].GetComponent<TMP_Text>();
            var body = _previewTargets["Body"].GetComponent<TMP_Text>();
            speaker.text = body.text = string.Empty;
            _previewTargets["Status"].GetComponent<TMP_Text>().text = "节点静态预览";
            if (!_previewNode) { _nodeNotice = "在图编辑器选中节点，或拖入节点 SO。"; return; }
            try { EnsurePreviewTables(); }
            catch (Exception ex) { _nodeNotice = ex.Message; }
            if (_previewNode is NarrativeDialogueSO dialogue)
            {
                int count = dialogue.Commands.Count(c => c?.Kind == NovelCommandKind.Say);
                _previewLine = Mathf.Clamp(_previewLine, 0, Mathf.Max(0, count - 1));
                int line = -1;
                foreach (var command in dialogue.Commands)
                {
                    if (command == null) continue;
                    if (command.Kind == NovelCommandKind.Say)
                    {
                        line++;
                        body.text = command.Text;
                        // 与运行期同一条解析链：称呼 Key → character.〈角色键〉→ 角色表 displayName。
                        string fallback = string.IsNullOrEmpty(command.CharacterId) ? string.Empty
                            : _catalog != null && _catalog.TryGetCharacter(command.CharacterId, out var character)
                                ? character.DisplayName : command.CharacterId;
                        speaker.text = NovelLocalization.SpeakerName(command.CharacterId, command.SpeakerNameKey, fallback);
                        if (line == _previewLine)
                        {
                            _previewTextMode = command.TextMode;
                            if (command.TextBeats.Count > 0)
                                _nodeNotice = "当前为布局静态预览；本句的分页与文字节奏请使用“播放节点”检查。";
                            break;
                        }
                    }
                    else if (command.Kind == NovelCommandKind.Background || command.Kind == NovelCommandKind.Character)
                    {
                        string target = command.Kind == NovelCommandKind.Background ? "Background" : command.Slot.ToString();
                        Sprite sprite = null;
                        if (command.VisualAction != NovelVisualAction.Hide)
                        {
                            if (_catalog != null && _catalog.TryResolve(command.Kind, command.ResourceKey, out string path))
                                sprite = path.StartsWith("Assets/", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : Resources.Load<Sprite>(path);
                            if (!sprite) _nodeNotice = "资源无法预览：" + command.ResourceKey + "。请检查配表与 Sprite 导入设置。";
                        }
                        SetPicture(target, sprite);
                    }
                    // Deliberately do not run the session: no variables, branches, waits, audio or persistence.
                }
                if (count == 0 && string.IsNullOrEmpty(_nodeNotice)) _nodeNotice = "此节点没有对白，展示节点内全部静态演出命令的最终画面。";
            }
            else if (_previewNode is NarrativeChoiceSO choice)
            {
                body.text = choice.Prompt;
                var prototype = Array.Find(_previewRoot.GetComponent<Ember.UIExtension.EUIBinding>().Bindings, b => b.Name == "ChoiceTemplate").GameObject;
                foreach (var option in choice.Options)
                {
                    if (option == null) continue;
                    var item = Instantiate(prototype, _previewTargets["Choices"]); item.SetActive(true);
                    item.GetComponentInChildren<TMP_Text>(true).text = option.Text;
                }
            }
            else _nodeNotice = "此节点为流程控制节点，没有可独立预览的对白；不会执行分支或章节跳转。";
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>图编辑器调用的单向桥接，不引入 UI Editor 到 Narrative Editor 的循环依赖。</summary>
        public static void FollowNodeSelection(NarrativeNodeSO node)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<NovelGameplayLayoutWindow>())
                if (window._followNode) window.SetPreviewNode(node);
        }
        public static void OpenNodePreview(NarrativeNodeSO node)
        {
            if (!CanEdit(out _)) return;
            var window = GetWindow<NovelGameplayLayoutWindow>(); window.SetPreviewNode(node); window.Show();
        }
        #endregion
    }
}
