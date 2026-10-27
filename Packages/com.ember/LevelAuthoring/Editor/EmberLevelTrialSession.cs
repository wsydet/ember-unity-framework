using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>跨域保存描述符和编辑现场；实际初始化/隔离由明确的适配器拥有。</summary>
    [InitializeOnLoad]
    public static class EmberLevelTrialSession
    {
        #region 内部参数
        private const string KEY = "Ember.LevelAuthoring.Trial";
        private const string RESTORE = "Ember.LevelAuthoring.OwnsSceneRestore";
        [Serializable] private sealed class Snapshot
        {
            public SceneSetup[] scenes;
            public string selection;
            public Vector3 pivot;
            public Quaternion rotation;
            public float size;
            public bool mode2D, orthographic;
            public EmberLevelTrialRequest request;
            public bool entered;
            public bool returning;
        }
        private static Snapshot _state;
        public static EmberLevelTrialRequest Request => _state == null ? null : JsonUtility.FromJson<EmberLevelTrialRequest>(JsonUtility.ToJson(_state.request));
        public static bool Active => _state != null;
        public static string LastError { get; private set; }
        public static string Status => _state == null ? LastError ?? "编辑" : EmberLevelAdapters.Get(_state.request.adapterId)?.TrialStatus ?? "缺少试玩适配器；可停止并恢复编辑现场。";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberLevelTrialSession()
        {
            string json = SessionState.GetString(KEY, "");
            if (!string.IsNullOrEmpty(json)) _state = JsonUtility.FromJson<Snapshot>(json);
            string error = SessionState.GetString(KEY + ".Error", ""); LastError = string.IsNullOrEmpty(error) ? null : error;
            EditorApplication.playModeStateChanged += OnPlay;
            EditorApplication.update += Tick;
        }
        private static void OnPlay(PlayModeStateChange state)
        {
            if (_state == null) return;
            if (state == PlayModeStateChange.EnteredPlayMode) { _state.entered = true; Save(); }
            if (state == PlayModeStateChange.EnteredEditMode) { _state.returning = true; Save(); EditorApplication.delayCall += TryRestore; }
        }
        private static void Save() => SessionState.SetString(KEY, JsonUtility.ToJson(_state));
        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (_state != null) { if (_state.returning) TryRestore(); return; }
            string retry = SessionState.GetString(KEY + ".Retry", "");
            if (string.IsNullOrEmpty(retry)) return;
            var request = JsonUtility.FromJson<EmberLevelTrialRequest>(retry);
            var adapter = EmberLevelAdapters.Get(request.adapterId);
            if (adapter?.IsCleaning == true) return;
            SessionState.EraseString(KEY + ".Retry");
            try
            {
                if (AssetDatabase.GetAssetDependencyHash(AssetDatabase.GUIDToAssetPath(request.configurationGuid)).ToString() != request.configurationHash)
                    throw new InvalidOperationException("冻结配置已变化，请重新选择启动；重试不会静默捕获新配置。");
                var level = adapter.Discover().Single(l => l.id == request.levelId && l.sceneGuid == request.sceneGuid);
                Start(level, request.mode, request.position, request.checkpointId);
            }
            catch (Exception error) { LastError = error.Message; }
        }
        private static void TryRestore()
        {
            if (_state == null || !_state.returning || EditorApplication.isPlayingOrWillChangePlaymode || EmberLevelAdapters.Get(_state.request.adapterId)?.IsCleaning == true) return;
            try
            {
                EditorSceneManager.RestoreSceneManagerSetup(_state.scenes);
                if (GlobalObjectId.TryParse(_state.selection, out var selected)) Selection.activeObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(selected);
                var view = SceneView.lastActiveSceneView;
                if (view != null) { view.in2DMode = _state.mode2D; view.LookAt(_state.pivot, _state.rotation, _state.size, _state.orthographic, true); }
                _state = null; SessionState.EraseString(KEY); SessionState.EraseBool(RESTORE);
            }
            catch (Exception error) { LastError = "编辑现场恢复失败；已保存内容保留：" + error.Message; EditorApplication.update -= Tick; }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Start(EmberLevelDescriptor level, EmberLevelStartMode mode, Vector2 position, string checkpoint = "")
        {
            if (Active || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("请等待上一轮试玩、导入或编译结束，并修复编译错误。");
            if (EditorSettings.enterPlayModeOptionsEnabled && (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0)
                throw new InvalidOperationException("本版试玩要求启用 Domain Reload；请在项目设置中恢复域重载。");
            var adapter = EmberLevelAdapters.Get(level?.adapterId) ?? throw new InvalidOperationException("缺少关卡适配器。");
            if (!string.IsNullOrEmpty(adapter.UnavailableReason)) throw new InvalidOperationException(adapter.UnavailableReason);
            var request = new EmberLevelTrialRequest { requestId = Guid.NewGuid().ToString("N"), adapterId = level.adapterId,
                levelId = level.id, sceneGuid = level.sceneGuid, configurationGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level.configuration)),
                mode = mode, position = position, checkpointId = checkpoint };
            var errors = adapter.Validate(level, request).Where(d => d.Severity == EmberLevelSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(d => d.Message)));
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(EditorSceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("试玩前必须保存所有编辑场景；选择不保存时不会启动。");
            var dependencies = AssetDatabase.GetDependencies(new[] { level.ScenePath, AssetDatabase.GetAssetPath(level.configuration) }, true)
                .Select(AssetDatabase.LoadMainAssetAtPath).Where(asset => asset != null && EditorUtility.IsDirty(asset)).ToArray();
            if (dependencies.Length > 0 && !EditorUtility.DisplayDialog("保存试玩依赖", "本次试玩需要保存以下依赖：\n" +
                string.Join("\n", dependencies.Select(AssetDatabase.GetAssetPath)), "保存并试玩", "取消")) return;
            foreach (var asset in dependencies) AssetDatabase.SaveAssetIfDirty(asset);
            request.configurationHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(level.configuration)).ToString();
            var view = SceneView.lastActiveSceneView;
            _state = new Snapshot { scenes = EditorSceneManager.GetSceneManagerSetup(), request = request,
                selection = Selection.activeObject != null ? GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeObject).ToString() : "",
                pivot = view != null ? view.pivot : Vector3.zero, rotation = view != null ? view.rotation : Quaternion.identity,
                size = view != null ? view.size : 10, mode2D = view != null && view.in2DMode, orthographic = view != null && view.orthographic };
            SessionState.SetBool(RESTORE, true); Save(); LastError = null; SessionState.EraseString(KEY + ".Error");
            try
            {
                EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity", OpenSceneMode.Single);
                adapter.BeginTrial(JsonUtility.FromJson<EmberLevelTrialRequest>(JsonUtility.ToJson(request)));
            }
            catch (Exception error)
            {
                ReportFailure("试玩准备失败：" + error.Message); _state.returning = true; Save();
                try { adapter.StopTrial(); }
                catch (Exception cleanup) { ReportFailure(LastError + "；清理失败：" + cleanup.Message); }
                TryRestore(); throw;
            }
        }
        public static void Stop()
        { SessionState.EraseString(KEY + ".Retry"); StopCurrent(); }
        private static void StopCurrent()
        {
            if (_state == null) return;
            // 进入Play前取消可能没有EnteredEditMode事件；停止请求本身即进入恢复阶段。
            _state.returning = true; Save();
            var adapter = EmberLevelAdapters.Get(_state.request.adapterId);
            if (adapter != null) adapter.StopTrial(); else EditorApplication.isPlaying = false;
        }
        /// <summary>适配器报告自己的启动/清理错误；恢复编辑现场后仍保留可行动诊断。</summary>
        public static void ReportFailure(string message)
        { LastError = message; SessionState.SetString(KEY + ".Error", message ?? ""); }
        public static void Retry()
        {
            if (_state == null) throw new InvalidOperationException("当前没有试玩会话。");
            SessionState.SetString(KEY + ".Retry", JsonUtility.ToJson(_state.request)); StopCurrent();
        }
        #endregion
    }
}
