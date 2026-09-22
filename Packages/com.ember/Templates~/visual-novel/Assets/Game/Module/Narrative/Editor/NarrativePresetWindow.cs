using System;
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
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnGUI()
        {
            _node = (NarrativeDialogueSO)EditorGUILayout.ObjectField("插入节点", _node, typeof(NarrativeDialogueSO), false);
            _kind = (NarrativePresetKind)EditorGUILayout.Popup("预设", (int)_kind, new[] { "进场", "受击", "回忆氛围", "镜头 / 遮罩复位" });
            if (_kind == NarrativePresetKind.Entrance || _kind == NarrativePresetKind.Impact) _actor = EditorGUILayout.TextField("人物实例 ID", _actor);
            if (_kind == NarrativePresetKind.Entrance)
            { _portrait = EditorGUILayout.TextField("立绘资源键", _portrait); _slot = (NovelPortraitSlot)EditorGUILayout.EnumPopup("入场位置", _slot); }
            if (_kind == NarrativePresetKind.Impact) _sfx = EditorGUILayout.TextField("SFX 资源键（可空）", _sfx);
            _duration = EditorGUILayout.FloatField("时长（秒）", _duration);
            if (_kind == NarrativePresetKind.Impact || _kind == NarrativePresetKind.Memory)
                _strength = EditorGUILayout.Slider("强度", _strength, .1f, 3);
            EditorGUILayout.HelpBox("在节点末尾插入可逐项编辑的普通指令，并自动创建唯一动作 ID 和等待组；支持撤销。进场要求新实例，受击要求人物已在场。回忆是暖色遮罩 + 镜头推近，不是后处理滤镜；复位将镜头和遮罩归零。插入后可移动指令位置，并用节点校验与播放节点检查资源。", MessageType.Info);
            string error = null; int count = 0;
            try { count = NarrativePresentationPresets.Build(_kind, "preview", _actor, _portrait, _slot, _duration, _strength, _sfx).Count; }
            catch (Exception ex) { error = ex.Message; }
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            using (new EditorGUI.DisabledScope(error != null || !NarrativeGraphModel.CanEdit(_node)))
                if (GUILayout.Button("插入 " + count + " 条指令"))
                {
                    var commands = NarrativePresentationPresets.Build(_kind, "preset-" + Guid.NewGuid().ToString("N"), _actor, _portrait, _slot, _duration, _strength, _sfx);
                    NarrativeGraphModel.AppendCommands(_node, commands); Close();
                }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open(NarrativeDialogueSO node)
        { var window = GetWindow<NarrativePresetWindow>("演出预设"); window._node = node; window.minSize = new Vector2(430, 320); window.Show(); }
        #endregion
    }
}
