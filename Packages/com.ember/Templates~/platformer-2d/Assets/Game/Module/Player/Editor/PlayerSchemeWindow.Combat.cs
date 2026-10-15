using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public sealed partial class PlayerSchemeWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerFeelProfile _combatDraft;
        [SerializeField] private PlayerScheme _combatDraftSource;
        [SerializeField] private string _comboDraftJson, _combatBaseline;
        [SerializeField] private int _combatSegment;
        [SerializeField] private List<string> _combatClipIds = new();
        [SerializeField] private List<AnimationClip> _combatClips = new();
        #endregion
        #region 内部方法
        private void ReadCombatDraft()
        {
            PlayerEditorAccess.Require();
            if (_selected == null) throw new InvalidOperationException("请先选择方案。");
            if (_combatDraft != null) DestroyImmediate(_combatDraft);
            _combatDraft = Instantiate(_selected.Feel); _combatDraft.hideFlags = HideFlags.HideAndDontSave;
            _combatDraftSource = _selected; _combatBaseline = CombatFingerprint(_selected);
            _comboDraftJson = JsonUtility.ToJson(_selected.Combo, true); _combatClipIds.Clear(); _combatClips.Clear();
        }
        private static string CombatFingerprint(PlayerScheme scheme) =>
            JsonUtility.ToJson(scheme.Feel.Attack) + JsonUtility.ToJson(scheme.Combo) + EditorJsonUtility.ToJson(scheme.Graph);
        private void ApplyCombatDraft()
        {
            if (_selected != _combatDraftSource || CombatFingerprint(_selected) != _combatBaseline)
                throw new InvalidOperationException("源方案或图已变化，草稿保留；请先核对并显式重新读取。");
            var clips = new Dictionary<string, AnimationClip>();
            for (int i = 0; i < _combatClipIds.Count; i++)
                if (_combatDraft.Attack.Segments.Any(s => "combat.attack." + s.Id == _combatClipIds[i]) &&
                    !_selected.CombatGraph.Nodes.Any(n => "combat.attack." + n.SegmentId == _combatClipIds[i]))
                    clips.Add(_combatClipIds[i], _combatClips[i]);
            PlayerCombatExampleWorkflow.ApplyConfiguration(_selected, _combatDraft.Attack,
                JsonUtility.FromJson<PlayerCombo>(_comboDraftJson), clips);
            _combatBaseline = CombatFingerprint(_selected);
            _combatClipIds.Clear(); _combatClips.Clear();
        }
        private void DrawCombatPanel()
        {
            EditorGUILayout.HelpBox("候选草稿独立于方案。参数、连接、图、动画一起校验后提交；应用不保存。新增攻击复用现有状态类，必须选择动画。", MessageType.Info);
            if (_selected == null) return;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerSchemeOwnership.IsReadOnly(_selected)))
            {
                if (GUILayout.Button(_combatDraft == null ? "读取当前方案为攻击候选" : "重新读取当前方案（替换此攻击候选）")) Run(ReadCombatDraft);
                if (_combatDraft != null)
                {
                    EditorGUILayout.ObjectField("候选来源", _combatDraftSource, typeof(PlayerScheme), false);
                    using (var data = new SerializedObject(_combatDraft))
                    {
                        EditorGUILayout.PropertyField(data.FindProperty("_attack"), new GUIContent("攻击配置候选"), true);
                        data.ApplyModifiedProperties();
                    }
                    EditorGUILayout.LabelField("连招连接候选（Combo JSON）", EditorStyles.boldLabel);
                    _comboDraftJson = EditorGUILayout.TextArea(_comboDraftJson, GUILayout.MinHeight(90));
                    foreach (var segment in _combatDraft.Attack.Segments)
                    {
                        string node = "combat.attack." + segment.Id;
                        if (_selected.CombatGraph.Nodes.Any(n => n.SegmentId == segment.Id)) continue;
                        int index = _combatClipIds.IndexOf(node);
                        if (index < 0) { index = _combatClipIds.Count; _combatClipIds.Add(node); _combatClips.Add(null); }
                        _combatClips[index] = (AnimationClip)EditorGUILayout.ObjectField(node + " 动画", _combatClips[index], typeof(AnimationClip), false);
                    }
                    if (GUILayout.Button("联合校验并应用候选（Undo，不保存）")) Run(ApplyCombatDraft);
                }
            }
            var segments = _selected.Feel.Attack.Segments;
            if (segments.Count == 0) return;
            _combatSegment = EditorGUILayout.Popup("已应用攻击时间轴", Mathf.Clamp(_combatSegment, 0, segments.Count - 1), segments.Select(s => s.Id).ToArray());
            var selected = segments[_combatSegment];
            Rect bar = GUILayoutUtility.GetRect(100, 24, GUILayout.ExpandWidth(true));
            float duration = Mathf.Max(.001f, selected.Duration);
            EditorGUI.DrawRect(bar, new Color(.25f, .3f, .4f));
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * selected.WindupEnd / duration, bar.height), new Color(.7f, .5f, .2f));
            EditorGUI.DrawRect(new Rect(bar.x + bar.width * selected.WindupEnd / duration, bar.y,
                bar.width * (selected.ActiveEnd - selected.WindupEnd) / duration, bar.height), new Color(.75f, .25f, .2f));
            GUI.Label(bar, $"前摇 {selected.WindupEnd:0.###} / 有效结束 {selected.ActiveEnd:0.###} / 本体 {selected.Duration:0.###}s · 速度×{selected.Speed:0.##}");
            EditorGUILayout.LabelField($"取消开放：冲刺 {selected.Dash.OpensAt:0.###} / 跳跃 {selected.Jump.OpensAt:0.###} / 接续 {selected.Attack.OpensAt:0.###} / 移动 {selected.Move.OpensAt:0.###}");
            for (int w = 0; w < selected.Windows.Count; w++)
                for (int f = 0; f < selected.Windows[w].Keyframes.Count; f++)
                    for (int s = 0; s < selected.Windows[w].Keyframes[f].Shapes.Count; s++)
                    {
                        int window = w, frame = f, shape = s;
                        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerSchemeOwnership.IsReadOnly(_selected.Feel)))
                            if (GUILayout.Button($"原生命中编辑 · 窗口 {w} / 关键帧 {f} / 形状 {s}"))
                                Run(() => PlayerHitShapePreview.Open(_selected, _combatSegment, window, frame, shape));
                    }
            if (EditorApplication.isPlaying)
                foreach (var player in Resources.FindObjectsOfTypeAll<PlayerController>().Where(p => p.IsPrepared && p.Context != null))
                    EditorGUILayout.LabelField($"{player.name} · {player.CurrentStateId} · HP {player.Context.Health?.Current.ToString() ?? "未装配"}", EditorStyles.wordWrappedLabel);
        }
        #endregion
    }
}
