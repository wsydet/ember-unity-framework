using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    public sealed class NarrativePresetWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeDialogueSO _node;
        [SerializeField] private NarrativePresetKind _kind;
        [SerializeField] private string _actor = "wan", _portrait = "lastlight_alice", _sfx = "";
        [SerializeField] private NovelPortraitSlot _slot = NovelPortraitSlot.Left;
        [SerializeField] private float _duration = .8f, _strength = 1;
        [SerializeField] private int _source, _insert = 1, _start = 1, _count = 1;
        [SerializeField] private string _label = "我的步骤";
        [SerializeField] private Color _color = new(.5f, .75f, 1, 1);
        [SerializeField] private NarrativeStepPresetSO _custom;
        private string _error;
        private Vector2 _scroll;
        private static readonly string[] Names = { "进场", "受击", "回忆氛围", "镜头 / 遮罩复位", "隐藏所有立绘", "恢复舞台状态", "场景收尾（立绘 + 舞台）" };
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _node = (NarrativeDialogueSO)EditorGUILayout.ObjectField("目标剧情节点", _node, typeof(NarrativeDialogueSO), false);
            _source = GUILayout.Toolbar(_source, new[] { "内置二级步骤", "自定义步骤" });
            if (_source == 0)
            {
                _kind = (NarrativePresetKind)EditorGUILayout.Popup("步骤", (int)_kind, Names);
                if (_kind == NarrativePresetKind.Entrance || _kind == NarrativePresetKind.Impact) _actor = EditorGUILayout.TextField("人物实例 ID", _actor);
                if (_kind == NarrativePresetKind.Entrance)
                { _portrait = EditorGUILayout.TextField("立绘资源键", _portrait); _slot = (NovelPortraitSlot)EditorGUILayout.EnumPopup("入场位置", _slot); }
                if (_kind == NarrativePresetKind.Impact) _sfx = EditorGUILayout.TextField("音效资源键（可空）", _sfx);
                _duration = EditorGUILayout.FloatField("时长（秒）", _duration);
                if (_kind == NarrativePresetKind.Impact || _kind == NarrativePresetKind.Memory) _strength = EditorGUILayout.Slider("强度", _strength, .1f, 3);
            }
            else
            {
                _custom = (NarrativeStepPresetSO)EditorGUILayout.ObjectField("自定义步骤资产", _custom, typeof(NarrativeStepPresetSO), false);
                if (GUILayout.Button("从项目选择…"))
                {
                    var menu = new GenericMenu();
                    foreach (string guid in AssetDatabase.FindAssets("t:NarrativeStepPresetSO"))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<NarrativeStepPresetSO>(AssetDatabase.GUIDToAssetPath(guid));
                        menu.AddItem(new GUIContent(asset.DisplayName + " (" + asset.name + ")"), _custom == asset, () => { _custom = asset; Repaint(); });
                    }
                    if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("先将下方步骤范围保存成预设"));
                    menu.ShowAsContext();
                }
            }
            _insert = EditorGUILayout.IntField("插入到第几步之前", _insert);
            _color = EditorGUILayout.ColorField("二级步骤颜色", _color);
            EditorGUILayout.HelpBox("保留基础指令，可展开、解包和逐项修改。复制时重建命令/台词/动作 ID，并同步等待引用；人物实例和资源键保留。插入后为独立副本。场景收尾恢复镜头、遮罩、舞台透明度和强调，不清空剧情变量或停止音乐。", MessageType.Info);
            using (new EditorGUI.DisabledScope(!NarrativeGraphModel.CanEdit(_node)))
            {
                if (GUILayout.Button("插入二级步骤")) Try(() =>
                {
                    var commands = _source == 0 ? NarrativePresentationPresets.Build(_kind, "preset-" + Guid.NewGuid().ToString("N"), _actor, _portrait, _slot, _duration, _strength, _sfx)
                        : _custom ? _custom.Commands : throw new InvalidOperationException("选择自定义步骤资产");
                    var wrapped = NarrativeStepGroups.Wrap(commands, _source == 0 ? Names[(int)_kind] : _custom.DisplayName, _source == 0 ? _color : _custom.Color);
                    NarrativeStepGroups.Insert(_node, _insert - 1, wrapped); _insert += wrapped.Count;
                });
                GUILayout.Space(16); GUILayout.Label("把现有基础步骤包装成二级步骤", EditorStyles.boldLabel);
                _start = EditorGUILayout.IntField("起始步骤（从 1 起）", _start);
                _count = EditorGUILayout.IntField("包含步骤数", _count);
                _label = EditorGUILayout.TextField("名称", _label);
                if (GUILayout.Button("仅包装当前节点中的这个范围")) Try(() => NarrativeStepGroups.GroupRange(_node, _start - 1, _count, _label, _color));
                if (GUILayout.Button("保存这个范围为可复用的自定义步骤…")) Try(() =>
                {
                    if (_start < 1 || _count <= 0 || _start - 1 + _count > _node.Commands.Count) throw new ArgumentException("步骤范围超出节点");
                    var commands = NarrativeStepGroups.Wrap(_node.Commands.Skip(_start - 1).Take(_count).ToArray(), _label, _color);
                    string path = EditorUtility.SaveFilePanelInProject("保存自定义二级步骤", "CustomStep", "asset", "建议放入 GameResource/Authoring/Narrative/StepPresets，供其他剧情复用。");
                    if (string.IsNullOrEmpty(path)) return;
                    if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("目标文件已存在，请使用新名称");
                    var asset = CreateInstance<NarrativeStepPresetSO>(); asset.Initialize(_label, _color, commands);
                    AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssetIfDirty(asset); _custom = asset;
                    EditorGUIUtility.PingObject(asset);
                });
            }
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            EditorGUILayout.EndScrollView();
        }
        private void Try(Action action)
        { try { action(); _error = null; } catch (Exception error) { _error = error.Message; } }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open(NarrativeDialogueSO node)
        { var window = GetWindow<NarrativePresetWindow>("二级步骤与预设"); window._node = node; window._insert = (node?.Commands.Count ?? 0) + 1; window.minSize = new Vector2(480, 540); window.Show(); }
        #endregion
    }
}
