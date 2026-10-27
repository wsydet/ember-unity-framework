using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.Module;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>本机开发环境：播放前冻结，Init 前注入，Play 结束释放；不拥有场景或玩家运行会话。</summary>
    [InitializeOnLoad]
    public static class PlayerDevelopmentEnvironment
    {
        #region 内部参数
        public const string LAB_PATH = "Assets/Game/Development/Scenes/PlayerFeelLab.unity";
        private static string Key => "Game.Player.Development." + Application.dataPath + ".";
        private static readonly List<UnityEngine.Object> Owned = new();
        private static PlayerDataModule _data;
        private static PlayerModule _player;
        private static CancellationTokenSource _lifetime;
        private static bool _prepared, _cleaning;
        private static long _generation;
        public static bool UseWhitebox
        {
            get => EditorPrefs.GetBool(Key + "Whitebox", false);
            set { PlayerEditorAccess.Require(); EditorPrefs.SetBool(Key + "Whitebox", value); }
        }
        public static string StorageRoot => SessionState.GetString(Key + "Root", "");
        public static bool IsCleaning => _cleaning;
        public static string Status { get; private set; } = "选择本次 Gameplay 场景；播放后照常从主菜单进入。";
        [Serializable]
        private sealed class Request
        {
            public bool whitebox, acceptance;
            public string schemeGuid;
            public Vector2 position, velocity;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerDevelopmentEnvironment()
        {
            InstallBridge();
            EditorApplication.playModeStateChanged += OnPlayChanged;
            EditorApplication.delayCall += QueueRestart;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallBridge()
        {
            PlayerLabEditorBridge.Prepare = Prepare;
            PlayerLabEditorBridge.ObserveStorage = ObserveStorage;
            PlayerLabEditorBridge.GameplayScenePath = null;
        }
        private static T Own<T>(T item) where T : UnityEngine.Object
        { item.hideFlags = HideFlags.DontSave; Owned.Add(item); return item; }
        private static void Freeze()
        {
            if (_cleaning) throw new InvalidOperationException("上一轮开发环境尚未清理完成。");
            _generation++; // 任何新一次 Play 都使旧环境回调失效。
            SessionState.EraseBool(Key + "Restart");
            bool acceptance = PlayerEditorAccess.Available && SessionState.GetBool(Key + "AcceptanceNext", false);
            SessionState.EraseBool(Key + "AcceptanceNext");
            string acceptanceScheme = SessionState.GetString(Key + "AcceptanceSchemeNext", "");
            SessionState.EraseString(Key + "AcceptanceSchemeNext");
            var request = new Request { whitebox = PlayerEditorAccess.Available && UseWhitebox && !acceptance, acceptance = acceptance };
            if (acceptance && !string.IsNullOrEmpty(acceptanceScheme))
            {
                var scheme = AssetDatabase.LoadAssetAtPath<PlayerScheme>(AssetDatabase.GUIDToAssetPath(acceptanceScheme));
                PlayerSchemeWorkflow.Validate(scheme);
                request.schemeGuid = acceptanceScheme;
            }
            if (request.whitebox)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LAB_PATH) == null) throw new InvalidOperationException("缺少白盒场景。");
                var scheme = SelectedScheme;
                if (scheme == null) throw new InvalidOperationException("请选择本次完整玩家方案。");
                var feel = scheme.Feel;
                if (scheme.Graph == null || feel == null) throw new InvalidOperationException("方案缺少图或手感配置。");
                _ = feel.Capture(Physics2D.gravity);
                request.schemeGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scheme));
                if (string.IsNullOrEmpty(request.schemeGuid)) throw new InvalidOperationException("本次方案必须是已保存的项目资产。");
                request.position = SelectedPosition; request.velocity = SelectedVelocity;
                _ = new PlayerTrialStart(PlayerLabEditorBridge.SceneName, request.position, request.velocity, feel);
            }
            SessionState.SetString(Key + "Frozen", JsonUtility.ToJson(request));
            SessionState.SetString(Key + "Root", request.whitebox || request.acceptance
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Library",
                    request.whitebox ? "PlayerFeelLab" : "PlayerFormalAcceptance", Guid.NewGuid().ToString("N"))) : "");
        }
        private static void Prepare()
        {
            if (_prepared) return; // 同一 Play 的 Reset 不换库，返回 Main 不重新装配。
            try
            {
                var json = SessionState.GetString(Key + "Frozen", "");
                if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("本次 Play 缺少已冻结的开发环境设置。");
                var request = JsonUtility.FromJson<Request>(json);
                if (!request.whitebox && !request.acceptance) { _prepared = true; return; }
                var modules = EmberModuleCollector.Instance;
                modules.DiscoverModules();
                if (!modules.TryGetModule<PlayerDataModule>(out _data)) throw new InvalidOperationException("未装配 Global 存档模块。");
                var config = Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH);
                if (config == null) throw new InvalidOperationException("缺少存档配置。");
                var root = StorageRoot;
                string parent = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library",
                    request.whitebox ? "PlayerFeelLab" : "PlayerFormalAcceptance")) + Path.DirectorySeparatorChar;
                if (string.IsNullOrEmpty(root) || !Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("开发库根目录无效，禁止回退真实库。");
                _data.SetConfigurationForNextInit(Own(UnityEngine.Object.Instantiate(config)), root);
                if (request.whitebox || request.acceptance && !string.IsNullOrEmpty(request.schemeGuid))
                {
                    if (!modules.TryGetModule<PlayerModule>(out _player)) throw new InvalidOperationException("未装配玩家模块。");
                    var source = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
                    if (source == null) throw new InvalidOperationException("缺少 PlayerConfig。");
                    var scheme = AssetDatabase.LoadAssetAtPath<PlayerScheme>(AssetDatabase.GUIDToAssetPath(request.schemeGuid));
                    if (scheme == null) throw new InvalidOperationException("本次选定方案已失效，禁止回退项目默认。");
                    var runtime = Own(UnityEngine.Object.Instantiate(source));
                    PlayerSchemeConversion.SelectScheme(runtime, scheme, false);
                    if (request.whitebox)
                    {
                        runtime.Configure(runtime.Graph, request.position);
                        using (var serialized = new SerializedObject(runtime))
                        {
                            serialized.FindProperty("_scene").stringValue = PlayerLabEditorBridge.SceneName;
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                        }
                        PlayerLabEditorBridge.GameplayScenePath = LAB_PATH;
                    }
                    _player.SetConfigurationForNextInit(runtime, request.velocity);
                }
                _lifetime = new CancellationTokenSource(); _generation++; _prepared = true;
                Status = "环境已冻结；正常 Main → Gameplay 流程，存取位于本次独立开发库。";
            }
            catch (Exception error)
            {
                Status = "开发环境初始化失败：" + error.Message;
                PlayerLabEditorBridge.ReportDevelopmentFailure?.Invoke(Status);
                EditorApplication.isPlaying = false;
                throw;
            }
        }
        private static void ObserveStorage()
        {
            if (_data != null && _lifetime != null) ObserveStorageAsync(_data, _generation, _lifetime.Token).Forget();
        }
        private static async UniTaskVoid ObserveStorageAsync(PlayerDataModule data, long generation, CancellationToken token)
        {
            try
            {
                var result = await data.WhenReady.AsUniTask().AttachExternalCancellation(token);
                if (token.IsCancellationRequested || generation != _generation || !EditorApplication.isPlaying) return;
                if (!result.Succeeded)
                {
                    Status = "开发库存储初始化失败，已中止播放：" + result.Status;
                    PlayerLabEditorBridge.ReportDevelopmentFailure?.Invoke(Status);
                    EmberDebug.LogError("Game.Player.Development", Status);
                    EditorApplication.isPlaying = false;
                }
            }
            catch (OperationCanceledException) { }
        }
        private static void OnPlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try { Freeze(); InstallBridge(); }
                catch (Exception error)
                {
                    SessionState.EraseBool(Key + "Restart"); Status = error.Message; EditorApplication.isPlaying = false;
                    EmberDebug.LogError("Game.Player.Development", Status);
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) { _generation++; _lifetime?.Cancel(); }
            else if (state == PlayModeStateChange.EnteredEditMode) ReleaseAsync().Forget();
        }
        private static async UniTaskVoid ReleaseAsync()
        {
            if (_cleaning) return;
            _cleaning = true; _generation++; PlayerLabEditorBridge.GameplayScenePath = null;
            try
            {
                _lifetime?.Cancel(); _lifetime?.Dispose(); _lifetime = null;
                if (_data != null) { await _data.StopAsync(); _data.UseDefaultConfigurationForNextInit(); }
                _player?.SetConfigurationForNextInit(null);
                foreach (var item in Owned) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                Owned.Clear(); _data = null; _player = null; _prepared = false;
                SessionState.EraseString(Key + "Frozen");
                Status = "播放已停止；开发配置和存储服务已释放。";
            }
            finally { _cleaning = false; }
            QueueRestart();
        }
        private static void QueueRestart()
        {
            if (!SessionState.GetBool(Key + "Restart", false)) return;
            long generation = _generation;
            EditorApplication.CallbackFunction resume = null;
            resume = () =>
            {
                if (!PlayerEditorAccess.Available || generation != _generation || !SessionState.GetBool(Key + "Restart", false))
                { EditorApplication.update -= resume; return; }
                // EnteredEditMode 的当帧仍可能处于播放切换；域重载后也需保留重启意图。
                if (EditorApplication.isPlayingOrWillChangePlaymode || _cleaning) return;
                EditorApplication.update -= resume;
                SessionState.EraseBool(Key + "Restart");
                EditorApplication.isPlaying = true;
            };
            EditorApplication.update += resume;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static PlayerScheme SelectedScheme
        {
            get
            {
                string guid = EditorPrefs.GetString(Key + "Scheme", "");
                return string.IsNullOrEmpty(guid) ? Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)?.Scheme
                    : AssetDatabase.LoadAssetAtPath<PlayerScheme>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
        public static Vector2 SelectedPosition => new(EditorPrefs.GetFloat(Key + "X", 0), EditorPrefs.GetFloat(Key + "Y", 1.05f));
        public static PlayerFeelProfile SelectedFeel => SelectedScheme?.Feel;
        public static Vector2 SelectedVelocity => new(EditorPrefs.GetFloat(Key + "VX", 0), EditorPrefs.GetFloat(Key + "VY", 0));
        public static void SetParameters(PlayerFeelProfile feel, Vector2 position, Vector2 velocity, bool effectPreview = false)
        {
            PlayerEditorAccess.Require();
            if (effectPreview || SelectedScheme == null || SelectedScheme.Feel != feel)
                throw new InvalidOperationException("请在 Gameplay 开发设置显式选择包含所需手感与效果的完整方案；专项入口不再替换装配。");
            SetSchemeParameters(SelectedScheme, position, velocity);
        }
        public static void SetSchemeParameters(PlayerScheme scheme, Vector2 position, Vector2 velocity)
        {
            PlayerEditorAccess.Require();
            PlayerSchemeWorkflow.Validate(scheme);
            if (scheme == null || scheme.Graph == null) throw new InvalidOperationException("请选择完整方案。");
            if (scheme.ActionEffects == null) throw new InvalidOperationException("请先显式迁移动作效果接线。");
            scheme.ActionEffects.Validate(scheme.Graph);
            var feel = scheme.Feel;
            _ = new PlayerTrialStart(PlayerLabEditorBridge.SceneName, position, velocity, feel);
            var path = AssetDatabase.GetAssetPath(scheme);
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("请选择已保存的完整方案资产。");
            EditorPrefs.SetString(Key + "Scheme", AssetDatabase.AssetPathToGUID(path));
            EditorPrefs.SetFloat(Key + "X", position.x); EditorPrefs.SetFloat(Key + "Y", position.y);
            EditorPrefs.SetFloat(Key + "VX", velocity.x); EditorPrefs.SetFloat(Key + "VY", velocity.y);
        }
        public static void Play(bool formalAcceptance = false)
        {
            PlayerEditorAccess.Require();
            if (_cleaning || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请等待本次播放及清理完成。");
            SessionState.EraseString(Key + "AcceptanceSchemeNext");
            SessionState.SetBool(Key + "AcceptanceNext", formalAcceptance);
            EditorApplication.isPlaying = true;
        }
        /// <summary>本次正式场景验收使用指定方案与隔离存档，不修改默认方案或白盒偏好。</summary>
        public static void PlayFormalWithScheme(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme);
            if (_cleaning || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请等待本次播放及清理完成。");
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scheme));
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("正式验收方案必须为已保存资产。");
            SessionState.SetString(Key + "AcceptanceSchemeNext", guid);
            SessionState.SetBool(Key + "AcceptanceNext", true);
            EditorApplication.isPlaying = true;
        }
        public static void Restart()
        {
            PlayerEditorAccess.Require();
            if (_cleaning || !EditorApplication.isPlaying) throw new InvalidOperationException("当前没有可重启的播放。");
            SessionState.SetBool(Key + "Restart", true); EditorApplication.isPlaying = false;
        }
        #endregion
    }
}
