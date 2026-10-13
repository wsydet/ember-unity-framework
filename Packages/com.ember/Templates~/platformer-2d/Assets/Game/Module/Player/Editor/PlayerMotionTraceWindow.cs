using System;
using System.Linq;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public sealed class PlayerMotionTraceWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerController _player;
        [SerializeField] private string _instance;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Vector2 _scroll;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            var selected = (PlayerController)EditorGUILayout.ObjectField("观察宿主", _player, typeof(PlayerController), true);
            if (selected != _player) { _player = selected; _instance = selected?.FsmInstanceId?.ToString(); }
            using (new EditorGUI.DisabledScope(_player == null || !_player.IsPrepared))
                if (GUILayout.Button("显式选择该宿主当前会话")) _instance = _player.FsmInstanceId?.ToString();
            EditorGUILayout.HelpBox("物理步记录 · 最近512步 · 时间为已执行物理步的游戏秒。速度/重力为本步提交值，位置为本步开始时观测值（上步模拟后的结果）。暂停不采样；窗口刷新不驱动模拟。", MessageType.Info);
            if (!EditorApplication.isPlaying || _player == null || !_player.IsPrepared || _player.FsmInstanceId?.ToString() != _instance)
            { EditorGUILayout.HelpBox("未选择有效会话，或所选会话已结束。不会自动跟随其他实例。", MessageType.None); return; }
            var trace = _player.Context.MotionTrace;
            EditorGUILayout.LabelField($"{_player.name} · {_instance} · {_player.FsmStatus} · {trace.Count} 样本", EditorStyles.wordWrappedLabel);
            var feel = _player.Context.Feel;
            EditorGUILayout.LabelField($"准备时基础估算：{feel.EstimatedHeight:F3}u / {feel.EstimatedApexTime:F3}s（不含修正及碰撞）", EditorStyles.wordWrappedLabel);
            if (trace.HasJump)
                EditorGUILayout.HelpBox($"最近普通 Jump 状态片段：观测上升 {trace.JumpRise:F3}u，峰值时刻 +{trace.JumpPeakTime:F3}s，片段持续 {trace.JumpDuration:F3}s；" +
                    (trace.JumpComplete ? "已切至 " + trace.JumpEndState : "记录中") + "。这是片段观测，碰顶/短跳/取消会截断，不能视为完整自由跳跃高度。", MessageType.None);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCurve(trace, "水平提交速度 u/s", x => x.Velocity.x, Color.cyan);
            DrawCurve(trace, "竖直提交速度 u/s", x => x.Velocity.y, Color.green);
            DrawCurve(trace, "提交重力倍率", x => x.Gravity, new Color(1, .7f, .2f));
            DrawCurve(trace, "观测位置 Y / u", x => x.Position.y, new Color(.8f, .6f, 1));
            DrawCurve(trace, "跳跃缓冲剩余 / s", x => x.Buffer, Color.cyan);
            DrawCurve(trace, "土狼资格剩余 / s", x => x.Coyote, Color.green);
            EditorGUILayout.EndScrollView();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void DrawCurve(PlayerMotionTrace trace, string title, Func<PlayerMotionTrace.Sample, float> value, Color color)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(100, 120, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(.09f, .10f, .12f));
            if (trace.Count < 2) { GUI.Label(rect, "等待物理样本"); return; }
            float min = 0, max = 0;
            for (int i = 0; i < trace.Count; i++) { min = Mathf.Min(min, value(trace[i])); max = Mathf.Max(max, value(trace[i])); }
            if (max - min < .01f) max = min + 1;
            var plot = new Rect(rect.x + 52, rect.y + 6, Mathf.Max(1, rect.width - 62), rect.height - 28);
            var start = trace[0].Time; var duration = Math.Max(.001, trace[trace.Count - 1].Time - start);
            GUI.Label(new Rect(rect.x + 3, rect.y, 50, 20), max.ToString("F2"));
            GUI.Label(new Rect(rect.x + 3, plot.yMax - 16, 50, 20), min.ToString("F2"));
            GUI.Label(new Rect(plot.x, plot.yMax, plot.width, 22), $"{start:F2}s → {trace[trace.Count - 1].Time:F2}s · 最新 {value(trace[trace.Count - 1]):F3}");
            if (Event.current.type != EventType.Repaint) return;
            Handles.BeginGUI(); var old = Handles.color; Handles.color = color;
            Vector3 previous = default;
            for (int i = 0; i < trace.Count; i++)
            {
                var point = new Vector3(plot.x + (float)((trace[i].Time - start) / duration) * plot.width,
                    plot.yMax - (value(trace[i]) - min) / (max - min) * plot.height);
                if (i > 0) Handles.DrawLine(previous, point); previous = point;
            }
            Handles.color = old; Handles.EndGUI();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void ShowWindow() { PlayerEditorAccess.Require(); GetWindow<PlayerMotionTraceWindow>("运动曲线与实测").Show(); }
        public static void Open(EmberFsmBlueprintWindow blueprint)
        {
            PlayerEditorAccess.Require();
            var window = GetWindow<PlayerMotionTraceWindow>("运动曲线与实测");
            var id = blueprint.ObservedSnapshot?.InstanceId;
            window._player = id.HasValue ? Resources.FindObjectsOfTypeAll<PlayerController>().FirstOrDefault(x => x.IsPrepared && x.FsmInstanceId == id) : null;
            window._instance = id?.ToString(); window.minSize = new Vector2(500, 400); window.Show();
        }
        #endregion
    }
}
