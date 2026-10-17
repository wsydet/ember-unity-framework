using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Ember.Camera;
using Ember.Core;
using Ember.Core.Editor;
using Ember.UI;
using Game.Entity;
using Game.Input;
using Game.Module;
using Game.Player;
using Game.Player.Editor;
using Game.PlayerCamera;
using Game.PlayerData;
using Game.SceneFlow;
using Game.UI;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.State.Editor
{
    /// <summary>通过真实启动器、状态、UI和加载器执行两关验收；结果保存在本机Library，存档使用正式验收隔离库。</summary>
    [InitializeOnLoad]
    public static class PlayerLevelAcceptance
    {
        #region 内部参数
        private const string ROOT = "Assets/Game/Development/LevelAcceptance";
        private const string KEY = "Game.Player.LevelAcceptance.Armed";
        private static readonly List<string> Evidence = new();
        public static string Status { get; private set; } = "Idle";
        private static string ScenePath(string id) => ROOT + "/Level" + id + ".unity";
        private static string ConfigPath(string id) => ROOT + "/Config" + id + ".asset";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerLevelAcceptance()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KEY, false))
                { SessionState.EraseBool(KEY); Run().Forget(); }
            };
        }
        private static void Require()
        {
            if (!EmberProjectSetup.IsTemplateActive("platformer-2d") ||
                typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true)
                throw new InvalidOperationException("仅限活动platformer-2d模板。");
        }
        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            using var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Text(UnityEngine.Object target, string name, string value)
        {
            using var data = new SerializedObject(target);
            data.FindProperty(name).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Build(string id)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath(id)) != null) return;
            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath(id)) != null)
                throw new InvalidOperationException("部分白盒资产已存在，禁止覆盖。");
            var config = UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH));
            Text(config, "_scene", "Level" + id); config.Configure(config.Graph, new Vector2(id == "A" ? -4 : 4, 1));
            AssetDatabase.CreateAsset(config, ConfigPath(id));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/PlayerBlock.prefab"), scene);
                player.transform.position = config.Spawn;
                var participant = player.GetComponent<PlayerSceneParticipant>() ?? player.AddComponent<PlayerSceneParticipant>();
                var recovery = player.AddComponent<PlayerCheckpointRecovery>();
                var scopeObject = new GameObject("LevelScope"); SceneManager.MoveGameObjectToScene(scopeObject, scene);
                var world = scopeObject.AddComponent<WorldResetScope2D>(); Set(recovery, "_world", world);
                var progress = scopeObject.AddComponent<PlayerCheckpointAcceptanceParticipant>();
                Set(progress, "_world", world); Set(progress, "_recovery", recovery);
                var ground = new GameObject("Ground"); SceneManager.MoveGameObjectToScene(ground, scene);
                ground.transform.position = new Vector3(0, -.5f, 0);
                ground.AddComponent<BoxCollider2D>().size = new Vector2(40, 1);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Block.png");
                var visual = new GameObject("GroundVisual"); visual.transform.SetParent(ground.transform, false);
                visual.transform.localScale = new Vector3(40, 1, 1);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = Color.gray;
                var carrierObject = new GameObject("ResetPlatform"); SceneManager.MoveGameObjectToScene(carrierObject, scene);
                carrierObject.transform.position = new Vector3(10, 2, 0);
                carrierObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                carrierObject.AddComponent<BoxCollider2D>().size = new Vector2(3, .5f);
                carrierObject.AddComponent<EntityTranslationSupport2D>();
                var reset = carrierObject.AddComponent<TranslationSupportReset2D>(); Text(reset, "_stableId", "platform");
                using (var data = new SerializedObject(reset))
                { data.FindProperty("_policy").enumValueIndex = (int)WorldResetPolicy2D.Checkpoint; data.ApplyModifiedPropertiesWithoutUndo(); }
                using (var data = new SerializedObject(world))
                { var list = data.FindProperty("_participants"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = reset; data.ApplyModifiedPropertiesWithoutUndo(); }
                var cameraObject = new GameObject("LevelCamera" + id); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<CinemachineCamera>();
                var lens = camera.Lens; lens.OrthographicSize = 6; camera.Lens = lens;
                var follow = cameraObject.AddComponent<CinemachineFollow>(); follow.FollowOffset = new Vector3(0, 1, -10);
                var settings = follow.TrackerSettings; settings.BindingMode = BindingMode.WorldSpace; follow.TrackerSettings = settings;
                var registration = cameraObject.AddComponent<EmberCameraRegistration>(); Text(registration, "_cameraKey", "acceptance-" + id);
                var cameraParticipant = scopeObject.AddComponent<PlayerCameraParticipant>();
                Set(cameraParticipant, "_player", participant); Set(cameraParticipant, "_camera", camera); Set(cameraParticipant, "_registration", registration);
                var scope = scopeObject.AddComponent<GameSceneScope>();
                using (var data = new SerializedObject(scope))
                {
                    var list = data.FindProperty("_participants"); list.arraySize = 3;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = progress;
                    list.GetArrayElementAtIndex(1).objectReferenceValue = cameraParticipant;
                    list.GetArrayElementAtIndex(2).objectReferenceValue = participant; data.ApplyModifiedPropertiesWithoutUndo();
                }
                var light = new GameObject("WhiteboxLight"); SceneManager.MoveGameObjectToScene(light, scene);
                light.AddComponent<Light>().type = LightType.Directional;
                EditorSceneManager.SaveScene(scene, ScenePath(id));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Evidence.Add("PASS " + message); Status = message;
        }
        private static async UniTask Wait(Func<bool> predicate, string description)
        {
            double until = Time.realtimeSinceStartupAsDouble + 45;
            while (!predicate())
            {
                if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException(description);
                await UniTask.Yield();
            }
        }
        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            { var found = root.GetComponentInChildren<T>(true); if (found != null) return found; }
            throw new InvalidOperationException("缺少 " + typeof(T).Name);
        }
        private static async UniTask Enter(string id, PlatformerSnapshot snapshot = null)
        {
            var flow = GameLauncher.Instance.Fsm;
            using var entry = PlayerEntryRequest.ForLevel(id, ScenePath(id), AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath(id)), snapshot);
            var result = await ((GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow, entry).Completion;
            Check(result.Status == EmberTransitionStatus.Completed, "正式入口 " + id + ": " + result.Status + " " + result.Error);
        }
        private static async UniTask RenderCheck(PlayerModule player, string id, string phase)
        {
            await Wait(() => EUIViewEngine.Instance.PageContext.CurrentMainPage is { IsOpened: true, Logic: EUIGamePlayPage },
                phase + " 正式玩法UI未就绪");
            await UniTask.DelayFrame(2, PlayerLoopTiming.LastPostLateUpdate);
            var manager = EmberCameraManager.Instance;
            Check(Vector2.Distance(manager.MainCamera.transform.position, manager.ActiveCamera.State.GetFinalPosition()) < .15f,
                phase + " Brain实际输出匹配活动镜头");
            Check(EUIViewEngine.Instance.PageContext.CurrentMainPage?.Logic is EUIGamePlayPage, phase + " 正式玩法UI已打开");
            Directory.CreateDirectory("Library/PlayerLevelAcceptance");
            ScreenCapture.CaptureScreenshot("Library/PlayerLevelAcceptance/" + phase + ".png");
        }
        private static async UniTask RejectBrokenLevel(PlayerModule player, bool missingScope)
        {
            var previous = player.Controller;
            void Break(Scene scene, LoadSceneMode mode)
            {
                if (scene.path != ScenePath("B")) return;
                if (missingScope) Find<GameSceneScope>(scene).enabled = false;
                else Find<PlayerSceneParticipant>(scene).enabled = false;
            }
            SceneManager.sceneLoaded += Break;
            try
            {
                var flow = GameLauncher.Instance.Fsm;
                using var entry = PlayerEntryRequest.ForLevel("B", ScenePath("B"), AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath("B")));
                var result = await ((GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow, entry).Completion;
                Check(result.Status == EmberTransitionStatus.PreparationFailed && player.Controller == previous && previous.IsRunning,
                    (missingScope ? "缺少Scope" : "缺少玩家参与者") + " 拒绝换关且保留旧玩家");
                Check(!SceneManager.GetSceneByPath(ScenePath("B")).isLoaded, "失败候选B卸载");
                Check(EmberCameraManager.Instance.GetCamera("acceptance-B") == null, "失败候选相机无注册残留");
            }
            finally { SceneManager.sceneLoaded -= Break; }
        }
        private static void CameraCheck(PlayerModule player, string id, string phase)
        {
            var manager = EmberCameraManager.Instance; var camera = manager.GetCamera("acceptance-" + id);
            Check(camera != null && manager.ActiveCamera == camera && camera.Follow == player.Controller.transform, phase + " 相机绑定当前玩家");
            Check(Vector2.Distance(camera.State.RawPosition, player.Controller.transform.position + Vector3.up) < .15f, phase + " 虚拟相机无旧位置阻尼残留");
        }
        private sealed class TextCodec : IProgressCodec<string>
        {
            public string DataTypeId => "whitebox.text";
            public EncodedProgress Encode(string value) => new(Encoding.UTF8.GetBytes(value), 1);
            public DecodeResult<string> Decode(ReadOnlyMemory<byte> bytes, int version)
                => version == 1 ? new(DecodeStatus.Decoded, Encoding.UTF8.GetString(bytes.Span)) : new(DecodeStatus.DataVersionUnsupported);
        }
        private static async UniTaskVoid Run()
        {
            Evidence.Clear(); Status = "Running";
            try
            {
                await Wait(() => GameLauncher.IsValid && GameLauncher.Instance.Fsm != null &&
                    GameLauncher.Instance.Fsm.Is<MainState>() && !GameLauncher.Instance.Fsm.IsTransitioning, "启动到Main超时");
                var flow = GameLauncher.Instance.Fsm;
                Check(!string.IsNullOrEmpty(PlayerFormalAcceptance.StorageRoot), "使用独立正式验收存档库");
                Check(EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player), "正式Player模块存在");
                Check(EmberModuleCollector.Instance.TryGetModule<PlayerDataModule>(out var data), "正式存档模块存在");
                await Enter("A"); CameraCheck(player, "A", "A加载");
                await RenderCheck(player, "A", "A加载");
                await RejectBrokenLevel(player, true);
                await RejectBrokenLevel(player, false);
                var oldPlayer = player.Controller; var oldCamera = EmberCameraManager.Instance.GetCamera("acceptance-A");
                await Enter("B"); CameraCheck(player, "B", "B加载");
                await RenderCheck(player, "B", "B加载");
                Check(oldPlayer == null && oldCamera == null && !SceneManager.GetSceneByPath(ScenePath("A")).isLoaded, "换关卸载A玩家与相机");
                Check(EmberCameraManager.Instance.GetCamera("acceptance-A") == null, "A相机注册释放");
                await Wait(() => player.Controller.Context.JumpWindow.HasGroundSupport, "玩家落地超时");
                var controller = player.Controller;
                var recovery = controller.GetComponent<PlayerCheckpointRecovery>();
                var scene = controller.gameObject.scene;
                var platform = Find<EntityTranslationSupport2D>(scene);
                platform.Teleport(new Vector2(12, 3));
                var baseline = recovery.Capture("B-checkpoint");
                platform.Teleport(new Vector2(17, 5));
                controller.Context.Motor.Body.position += new Vector2(5, 6);
                var owner = controller.gameObject.AddComponent<PlayerConstantVelocitySource>();
                using var lease = controller.Context.ExternalMotion.Register(owner,
                    new PlayerExternalMotionRequest("whitebox-air", PlayerExternalMotionMode.ForcedRise, Vector2.up * 8, 5));
                var codec = new CheckpointProgressCodec<string>("ember.platformer2d.progress", new TextCodec());
                var writes = new CheckpointSaveSession<string>(data, codec, "acceptance-run");
                var failed = await writes.SaveAndExitAsync(writes.RunId, 999, "B", baseline, "confirmed-progress", new SaveSummary(), player, flow);
                Check(failed.Status == PlayerExitStatus.SaveFailed && player.Controller == controller && scene.isLoaded, "保存失败保留当前场景玩家");
                data.AcknowledgeResults(new[] { failed.Save.Context.RequestId });
                var saved = await writes.SaveAndExitAsync(writes.RunId, 0, "B", baseline, "confirmed-progress", new SaveSummary(), player, flow);
                Check(saved.Status == PlayerExitStatus.Completed && flow.Is<MainState>() && !player.HasSession, "组合保存成功后正式退出Main");
                await Wait(() => EUIViewEngine.Instance.PageContext.CurrentMainPage is { IsOpened: true, Logic: EUIMainPage }, "保存退出后主菜单UI未打开");
                Check(true, "保存退出后正式主菜单UI已打开");
                Check(!scene.isLoaded && EmberCameraManager.Instance.GetCamera("acceptance-B") == null, "退出卸载B及相机注册");
                var read = await data.ReadAsync(0, codec).Completion;
                Check(read.Succeeded && read.Progress.Project == "confirmed-progress", "磁盘组合记录保留最新已提交数据");
                PlayerCheckpointAcceptanceParticipant.Pending = read.Progress;
                await Enter("B", read.Progress.Checkpoint.Player);
                PlayerCheckpointAcceptanceParticipant.Pending = null;
                Check(Vector2.Distance(player.Controller.Context.Motor.Position, baseline.Player.Position) < .1f, "续玩回合法检查点而非空中保存位置");
                scene = player.Controller.gameObject.scene;
                Check(Find<PlayerCheckpointAcceptanceParticipant>(scene).Applied == "confirmed-progress", "项目数据在玩家启动前准备完成");
                Check(Find<EntityTranslationSupport2D>(scene).Body.position == new Vector2(12, 3), "续玩恢复检查点世界平台");
                CameraCheck(player, "B", "B续玩");
                await RenderCheck(player, "B", "B续玩");
                recovery = player.Controller.GetComponent<PlayerCheckpointRecovery>();
                Find<EntityTranslationSupport2D>(scene).Teleport(new Vector2(18, 5));
                player.Controller.Context.Motor.Body.position += new Vector2(8, 5);
                int commits = 0; Action<PlayerController> committed = _ => commits++; player.SessionCommitted += committed;
                bool restored = await recovery.RecoverAsync(player, flow, WorldResetReason2D.Death);
                player.SessionCommitted -= committed;
                Check(restored && commits == 1, "死亡恢复只提交一次: " + recovery.LastError);
                Check(Vector2.Distance(player.Controller.Context.Motor.Position, baseline.Player.Position) < .1f, "死亡恢复玩家位置");
                Check(Find<EntityTranslationSupport2D>(scene).Body.position == new Vector2(12, 3), "死亡恢复世界平台");
                CameraCheck(player, "B", "B死亡恢复");
                await RenderCheck(player, "B", "B死亡恢复");
                var exit = await player.ExitToMenuAsync(flow, null);
                Check(exit.Status == PlayerExitStatus.Completed && !scene.isLoaded && EmberCameraManager.Instance.GetCamera("acceptance-B") == null, "最终卸载无相机注册残留");
                Status = "PASSED " + Evidence.Count;
            }
            catch (Exception error) { Status = "FAILED " + error; Evidence.Add(Status); }
            finally
            {
                PlayerCheckpointAcceptanceParticipant.Pending = null;
                Directory.CreateDirectory("Library/PlayerLevelAcceptance");
                File.WriteAllText("Library/PlayerLevelAcceptance/latest.txt", DateTime.Now + "\n" + Status + "\n" + string.Join("\n", Evidence));
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string CreateScenes()
        {
            Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出Play。");
            var active = SceneManager.GetActiveScene();
            Directory.CreateDirectory(ROOT); AssetDatabase.Refresh();
            try { Build("A"); Build("B"); AssetDatabase.SaveAssets(); }
            finally { if (active.IsValid()) SceneManager.SetActiveScene(active); }
            return "Created/retained LevelA and LevelB";
        }
        public static void Launch()
        {
            Require();
            if (SceneManager.GetActiveScene().path != "Assets/Game/Scenes/FrameworkScene.unity")
                throw new InvalidOperationException("请从正式FrameworkScene启动。");
            SessionState.SetBool(KEY, true); PlayerFormalAcceptance.Launch();
        }
        #endregion
    }
}
