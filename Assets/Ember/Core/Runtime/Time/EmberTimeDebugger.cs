using UnityEngine;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// EmberTimeManager 运行时调试器。
    ///
    /// 挂到场景中的任意 GameObject 上即可在 Game 视图中看到实时时间数据，
    /// 并用键盘快捷键测试 Pause/Resume/TimeScale。
    ///
    /// <b>快捷键：</b>
    /// <list type="bullet">
    ///   <item>Space — 切换 Pause/Resume</item>
    ///   <item>1 — TimeScale = 0.25（1/4 速）</item>
    ///   <item>2 — TimeScale = 0.5（半速）</item>
    ///   <item>3 — TimeScale = 1（正常）</item>
    ///   <item>4 — TimeScale = 2（双倍速）</item>
    ///   <item>5 — TimeScale = 5（五倍速）</item>
    /// </list>
    ///
    /// 正式发布时删除此组件即可。
    /// </summary>
    [ForDebug]
    public class EmberTimeDebugger : MonoBehaviour
    {
        private const float FONT_SIZE = 16f;
        private const float LINE_HEIGHT = 22f;
        private const float PADDING_X = 16f;
        private const float PADDING_Y = 16f;

        private GUIStyle _style;
        private bool _styleReady;

        private void Start()
        {
            EmberDebug.Log(LogTags.CoreTimeManager,
                "[EmberTimeDebugger] Ready. Press Space to toggle Pause, 1-5 to change TimeScale.");
        }

        private void OnGUI()
        {
            InitStyle();

            // 通过 Event.current 处理键盘输入（不依赖任何 Input System）
            HandleKeyboardInput();

            var tm = EmberTimeManager.Instance;
            if (tm == null) return;

            float y = PADDING_Y;

            // 标题
            DrawLine(ref y, "<b>EmberTimeManager 运行时状态</b>", tm.IsPaused ? Color.red : Color.green);
            y += 4f;

            // 分隔线
            DrawLine(ref y, "─────────────────────────", Color.gray);

            // 帧时间
            DrawLine(ref y, $"DeltaTime:           {tm.DeltaTime:F6}", Color.white);
            DrawLine(ref y, $"UnscaledDeltaTime:   {tm.UnscaledDeltaTime:F6}", Color.white);

            // 累计时间
            DrawLine(ref y, $"Time:                {tm.Time:F2} s", new Color(0.7f, 1f, 0.7f));
            DrawLine(ref y, $"UnscaledTime:        {tm.UnscaledTime:F2} s", Color.white);

            // 状态
            DrawLine(ref y, $"TimeScale:           {tm.TimeScale:F2}", Color.yellow);
            DrawLine(ref y, $"IsPaused:            {(tm.IsPaused ? "PAUSED" : "running")}",
                tm.IsPaused ? Color.red : Color.green);

            y += 4f;
            DrawLine(ref y, "─────────────────────────", Color.gray);
            DrawLine(ref y, "Space=暂停  1=0.25x  2=0.5x  3=1x  4=2x  5=5x", new Color(0.5f, 0.5f, 0.5f));
        }

        private void DrawLine(ref float y, string text, Color color)
        {
            _style.normal.textColor = color;
            GUI.Label(new Rect(PADDING_X, y, 400, LINE_HEIGHT), text, _style);
            y += LINE_HEIGHT;
        }

        private void InitStyle()
        {
            if (_styleReady) return;

            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(FONT_SIZE),
                richText = true,
                fontStyle = FontStyle.Bold,
            };
            _styleReady = true;
        }

        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            var tm = EmberTimeManager.Instance;
            if (tm == null) return;

            switch (e.keyCode)
            {
                case KeyCode.Space:
                    if (tm.IsPaused) tm.Resume();
                    else tm.Pause();
                    e.Use();
                    break;
                case KeyCode.Alpha1: tm.TimeScale = 0.25f; e.Use(); break;
                case KeyCode.Alpha2: tm.TimeScale = 0.5f;  e.Use(); break;
                case KeyCode.Alpha3: tm.TimeScale = 1f;    e.Use(); break;
                case KeyCode.Alpha4: tm.TimeScale = 2f;    e.Use(); break;
                case KeyCode.Alpha5: tm.TimeScale = 5f;    e.Use(); break;
            }
        }
    }
}
