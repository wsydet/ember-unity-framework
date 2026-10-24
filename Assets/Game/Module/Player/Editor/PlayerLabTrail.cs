using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player.Editor
{
    /// <summary>白盒专属轨迹视图，只读取正式物理步记录；不采样、不驱动玩家。</summary>
    [InitializeOnLoad]
    internal static class PlayerLabTrail
    {
        #region 内部参数
        private const string KEY = "Game.Player.LabTrail.";
        private static bool _enabled = SessionState.GetBool(KEY + "Enabled", true);
        private static float _duration = SessionState.GetFloat(KEY + "Duration", 5);
        private static float _width = SessionState.GetFloat(KEY + "Width", .035f);
        private static PlayerMotionTrace _trace;
        private static long _session;
        private static double _visibleAfter = double.NegativeInfinity;
        private static LineRenderer _line;
        private static Material _material;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerLabTrail()
        {
            EditorApplication.update += Refresh;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) Release();
            };
        }

        private static void Refresh()
        {
            if (!_enabled || !EditorApplication.isPlaying || !PlayerLabEditorBridge.Active || !PlayerEditorAccess.Available ||
                !PlayerModule.TryGetInstance(out var module) || !module.IsActive || module.Controller == null ||
                !module.Controller.IsPrepared || module.Controller.gameObject.scene.path != PlayerDevelopmentEnvironment.LAB_PATH)
            { Release(); return; }
            var player = module.Controller;
            var trace = player.Context.MotionTrace;
            if (_trace != trace || _session != module.SessionVersion)
            {
                Release(); _trace = trace; _session = module.SessionVersion;
            }
            if (_line == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                var root = new GameObject("Player Lab Motion Trail") { hideFlags = HideFlags.HideAndDontSave, layer = player.gameObject.layer };
                SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
                _line = root.AddComponent<LineRenderer>();
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _line.sharedMaterial = _material;
                _line.useWorldSpace = true; _line.positionCount = 0;
                _line.numCornerVertices = 3; _line.numCapVertices = 3;
                _line.sortingOrder = 100;
                _line.startColor = new Color(.15f, .85f, 1, .15f);
                _line.endColor = new Color(.15f, .85f, 1, 1);
                _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _line.receiveShadows = false;
            }
            DrawTrace(_line, trace, _duration, _visibleAfter, player.transform.position.z - .02f);
            _line.widthMultiplier = _width;
        }

        private static void DrawTrace(LineRenderer line, PlayerMotionTrace trace, float duration, double after, float z)
        {
            int first = 0;
            double cutoff = trace.Time - duration;
            while (first < trace.Count && (trace[first].Time < cutoff || trace[first].Time <= after)) first++;
            line.positionCount = trace.Count - first;
            for (int i = first; i < trace.Count; i++)
            {
                var p = trace[i].Position;
                line.SetPosition(i - first, new Vector3(p.x, p.y, z));
            }
        }

        private static void Release()
        {
            if (_line != null) Object.DestroyImmediate(_line.gameObject);
            if (_material != null) Object.DestroyImmediate(_material);
            _line = null; _material = null; _trace = null;
            _visibleAfter = double.NegativeInfinity;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void DrawControls()
        {
            PlayerEditorAccess.Require();
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            _enabled = EditorGUILayout.Toggle("白盒玩家运动尾迹", _enabled);
            using (new EditorGUI.DisabledScope(!_enabled))
            {
                _duration = EditorGUILayout.Slider("尾迹保留时间 (游戏秒)", _duration, .1f, 10);
                _width = EditorGUILayout.Slider("尾迹宽度 (u)", _width, .01f, .15f);
            }
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetBool(KEY + "Enabled", _enabled);
                SessionState.SetFloat(KEY + "Duration", _duration);
                SessionState.SetFloat(KEY + "Width", _width);
                Refresh();
            }
            using (new EditorGUI.DisabledScope(_trace == null))
                if (GUILayout.Button("清除已显示尾迹"))
                {
                    _visibleAfter = _trace.Count > 0 ? _trace[_trace.Count - 1].Time : double.NegativeInfinity;
                    if (_line != null) _line.positionCount = 0;
                }
            EditorGUILayout.LabelField("Game / Scene 中显示真实位置路径，最多512个物理步；暂停保留，重生／读档／换会话清空。清除尾迹不清除运动曲线记录。", EditorStyles.wordWrappedMiniLabel);
        }
        #endregion
    }
}
