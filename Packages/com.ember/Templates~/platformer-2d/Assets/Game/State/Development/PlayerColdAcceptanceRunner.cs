#if UNITY_EDITOR || EMBER_PLAYER_ACCEPTANCE
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Ember.Camera;
using Ember.Core;
using Ember.UI;
using Game.Entity;
using Game.Module;
using Game.Player;
using Game.PlayerData;
using Game.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.State
{
    /// <summary>专用冷启动验收场景；普通发行不编译，不定义产品存档业务。</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class PlayerColdAcceptanceRunner : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerConfig _configA;
        [SerializeField] private PlayerConfig _configB;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private const string ROOT = "Assets/Game/Development/LevelAcceptance/";
        private string _root, _phase;
        private readonly string _boot = Guid.NewGuid().ToString("N");
        private readonly List<string> _checks = new(), _errors = new();
        private PlayerDataConfig _config;
        [Serializable] private sealed class Expected { public string boot, project; public Vector2 position; }
        [Serializable] private sealed class Report
        {
            public string phase, boot, buildGuid, root, result, failure, time;
            public int processId;
            public bool editor, development;
            public string[] checks, errors;
        }
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake()
        {
            Application.runInBackground = true;
#if UNITY_EDITOR
            _root = UnityEditor.SessionState.GetString("PlayerCold.Root", "");
            _phase = UnityEditor.SessionState.GetString("PlayerCold.Phase", "");
            var previous = PlayerLabEditorBridge.Prepare;
            PlayerLabEditorBridge.Prepare = () => { previous?.Invoke(); PrepareStorage(); };
#else
            _root = Argument("--ember-acceptance-root");
            _phase = Argument("--ember-acceptance-phase");
            PrepareStorage();
#endif
            Application.logMessageReceived += OnLog;
        }
        private void Start() => Run().Forget();
        private void OnDestroy() { Application.logMessageReceived -= OnLog; if (_config != null) Destroy(_config); }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return "";
        }
        private void PrepareStorage()
        {
            if (!Path.IsPathFullyQualified(_root) || (_phase != "save" && _phase != "resume"))
                throw new InvalidOperationException("必须显式指定验收隔离库及save/resume阶段。");
            Directory.CreateDirectory(_root);
            _config = Instantiate(Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH));
            _config.hideFlags = HideFlags.DontSave;
            PlayerDataModule.Instance.SetConfigurationForNextInit(_config, _root);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type is LogType.Error or LogType.Exception or LogType.Assert) _errors.Add(message + "\n" + stack); }
        private void Check(bool value, string description)
        { if (!value) throw new InvalidOperationException(description); _checks.Add(description); }
        private static async UniTask Wait(Func<bool> condition, string description)
        {
            double until = Time.realtimeSinceStartupAsDouble + 60;
            while (!condition()) { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException(description); await UniTask.Yield(); }
        }
        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<T>(true); if (value != null) return value; }
            throw new InvalidOperationException("缺少 " + typeof(T).Name);
        }
        private async UniTask Enter(string id, PlatformerSnapshot snapshot = null)
        {
            var flow = GameLauncher.Instance.Fsm;
            using var entry = PlayerEntryRequest.ForLevel(id, ROOT + "Level" + id + ".unity", id == "A" ? _configA : _configB, snapshot);
            var result = await ((GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow, entry).Completion;
            Check(result.Status == EmberTransitionStatus.Completed, "正式入口 " + id + ": " + result.Status + " " + result.Error);
        }
        private async UniTask CameraCheck(PlayerModule player, string id, string stage)
        {
            var manager = EmberCameraManager.Instance; var camera = manager.GetCamera("acceptance-" + id);
            Check(camera != null && manager.ActiveCamera == camera && camera.Follow == player.Controller.transform, stage + " camera binding");
            Check(Vector2.Distance(camera.State.RawPosition, player.Controller.transform.position + Vector3.up) < .15f, stage + " camera warp");
            await Wait(() => EUIViewEngine.Instance.PageContext.CurrentMainPage is { IsOpened: true, Logic: EUIGamePlayPage }, "Gameplay UI");
            await UniTask.DelayFrame(2, PlayerLoopTiming.LastPostLateUpdate);
            Check(Vector2.Distance(manager.MainCamera.transform.position, camera.State.GetFinalPosition()) < .15f, stage + " Brain output");
            // 隐藏Player窗口没有可靠的屏幕backbuffer；以实际Brain驱动的主相机输出留证。
            var main = manager.MainCamera;
            var previousTarget = main.targetTexture; var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            var screenshot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                main.targetTexture = target; main.Render(); RenderTexture.active = target;
                screenshot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); screenshot.Apply();
                File.WriteAllBytes(Path.Combine(_root, _phase + "-" + stage + ".png"), screenshot.EncodeToPNG());
            }
            finally
            {
                main.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Destroy(screenshot);
            }
        }
        private sealed class TextCodec : IProgressCodec<string>
        {
            public string DataTypeId => "whitebox.text";
            public EncodedProgress Encode(string value) => new(Encoding.UTF8.GetBytes(value), 1);
            public DecodeResult<string> Decode(ReadOnlyMemory<byte> bytes, int version)
                => version == 1 ? new(DecodeStatus.Decoded, Encoding.UTF8.GetString(bytes.Span)) : new(DecodeStatus.DataVersionUnsupported);
        }
        private async UniTaskVoid Run()
        {
            string failure = null;
            try
            {
                await Wait(() => GameLauncher.IsValid && GameLauncher.Instance.Fsm != null && GameLauncher.Instance.Fsm.Is<MainState>() && !GameLauncher.Instance.Fsm.IsTransitioning, "Boot Main");
                var flow = GameLauncher.Instance.Fsm;
                var player = PlayerModule.Instance; var data = PlayerDataModule.Instance;
                Check(!player.HasSession && PlayerCheckpointAcceptanceParticipant.Pending == null, "fresh boot without player or pending snapshot");
                var codec = new CheckpointProgressCodec<string>("ember.platformer2d.progress", new TextCodec());
                if (_phase == "save")
                {
                    Check(!File.Exists(Path.Combine(_root, "expected.json")), "fresh isolated save run");
                    await Enter("A"); await CameraCheck(player, "A", "A");
                    var oldPlayer = player.Controller; var oldCamera = EmberCameraManager.Instance.GetCamera("acceptance-A");
                    await Enter("B"); await CameraCheck(player, "B", "B");
                    Check(oldPlayer == null && oldCamera == null && !SceneManager.GetSceneByPath(ROOT + "LevelA.unity").isLoaded && EmberCameraManager.Instance.GetCamera("acceptance-A") == null, "A unloaded and unregistered");
                    await Wait(() => player.Controller.Context.JumpWindow.HasGroundSupport, "ground support");
                    var controller = player.Controller; var scene = controller.gameObject.scene;
                    var platform = Find<EntityTranslationSupport2D>(scene); platform.Teleport(new Vector2(12, 3));
                    var baseline = controller.GetComponent<PlayerCheckpointRecovery>().Capture("B-checkpoint");
                    var expected = new Expected { boot = _boot, project = "committed:" + _boot, position = baseline.Player.Position };
                    platform.Teleport(new Vector2(18, 5)); controller.Context.Motor.Body.position += new Vector2(6, 5);
                    var owner = controller.gameObject.AddComponent<PlayerConstantVelocitySource>();
                    using var lease = controller.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("cold-air", PlayerExternalMotionMode.ForcedRise, Vector2.up * 8, 5));
                    var writes = new CheckpointSaveSession<string>(data, codec, _boot);
                    var saved = await writes.SaveAndExitAsync(writes.RunId, 0, "B", baseline, expected.project, new SaveSummary(), player, flow);
                    Check(saved.Status == PlayerExitStatus.Completed && flow.Is<MainState>() && !player.HasSession, "composite save and exit Main");
                    Check(!scene.isLoaded && EmberCameraManager.Instance.GetCamera("acceptance-B") == null, "saved B unloaded and camera released");
                    File.WriteAllText(Path.Combine(_root, "expected.json"), JsonUtility.ToJson(expected, true));
                }
                else
                {
                    var expected = JsonUtility.FromJson<Expected>(File.ReadAllText(Path.Combine(_root, "expected.json")));
                    Check(expected.boot != _boot, "different boot identity");
                    var read = await data.ReadAsync(0, codec).Completion;
                    Check(read.Succeeded && read.Progress.Project == expected.project && read.Progress.LevelId == "B", "disk composite restores committed project and B");
                    PlayerCheckpointAcceptanceParticipant.Pending = read.Progress;
                    await Enter("B", read.Progress.Checkpoint.Player);
                    PlayerCheckpointAcceptanceParticipant.Pending = null;
                    var scene = player.Controller.gameObject.scene;
                    Check(Vector2.Distance(player.Controller.Context.Motor.Position, expected.position) < .1f, "cold resume at legal checkpoint, not air position");
                    Check(Find<PlayerCheckpointAcceptanceParticipant>(scene).Applied == expected.project, "project restored before player start");
                    Check(Find<EntityTranslationSupport2D>(scene).Body.position == new Vector2(12, 3), "world checkpoint restored");
                    await CameraCheck(player, "B", "resume");
                    var recovery = player.Controller.GetComponent<PlayerCheckpointRecovery>();
                    Find<EntityTranslationSupport2D>(scene).Teleport(new Vector2(18, 5));
                    player.Controller.Context.Motor.Body.position += new Vector2(8, 5);
                    int commits = 0; Action<PlayerController> handler = _ => commits++; player.SessionCommitted += handler;
                    bool restored;
                    try { restored = await recovery.RecoverAsync(player, flow, WorldResetReason2D.Death); }
                    finally { player.SessionCommitted -= handler; }
                    Check(restored && commits == 1, "recovery commits once: " + recovery.LastError);
                    Check(Vector2.Distance(player.Controller.Context.Motor.Position, expected.position) < .1f && Find<EntityTranslationSupport2D>(scene).Body.position == new Vector2(12, 3), "death restores player and world");
                    await CameraCheck(player, "B", "death");
                    var exit = await player.ExitToMenuAsync(flow, null);
                    Check(exit.Status == PlayerExitStatus.Completed && !scene.isLoaded && EmberCameraManager.Instance.GetCamera("acceptance-B") == null, "resume B exit releases scene and camera");
                }
                await Wait(() => EUIViewEngine.Instance.PageContext.CurrentMainPage is { IsOpened: true, Logic: EUIMainPage }, "Main UI");
                Check(true, "Main UI opened");
                await data.StopAsync();
                Check(data.State == SaveServiceState.Stopped, "storage drained before shutdown");
                Check(_errors.Count == 0, "no runtime errors");
            }
            catch (Exception error) { failure = error.ToString(); }
            finally
            {
                PlayerCheckpointAcceptanceParticipant.Pending = null;
                var report = new Report { phase = _phase, boot = _boot, buildGuid = Application.buildGUID, root = _root,
                    result = failure == null ? "PASSED" : "FAILED", failure = failure, checks = _checks.ToArray(), errors = _errors.ToArray(),
                    processId = System.Diagnostics.Process.GetCurrentProcess().Id, editor = Application.isEditor, development = Debug.isDebugBuild, time = DateTime.Now.ToString("O") };
                File.WriteAllText(Path.Combine(_root, "report-" + _phase + ".json"), JsonUtility.ToJson(report, true));
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(failure == null ? 0 : 1);
#endif
            }
        }
        #endregion
    }
}
#endif
