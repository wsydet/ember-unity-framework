using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using Game.Entity;
using Game.LevelAuthoring;
using Game.Module;
using Game.Player;
using Game.Player.Editor;
using Game.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.State.Editor
{
    /// <summary>状态接线位于预定义 Editor 程序集，允许引用现有 Assembly-CSharp 的 GameGameplayState。</summary>
    [InitializeOnLoad]
    public sealed class PlatformerLevelAuthoringAdapter : IEmberLevelAdapter
    {
        #region 内部参数
        public const string ADAPTER_ID = "platformer-2d.level";
        private static readonly PlatformerLevelAuthoringAdapter Instance = new();
        private PlayerEntryRequest _entry;
        private CancellationTokenSource _lifetime;
        private string _status = "编辑", _requestId;
        private bool _ready, _stopping;
        public string Id => ADAPTER_ID;
        internal static bool Available => EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelAuthoring)
            && EmberProjectSetup.IsTemplateActive("platformer-2d") && typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled == true;
        public string UnavailableReason => !EmberProjectSetup.IsTemplateActive("platformer-2d") ? "需要活动 platformer-2d 或合法派生模板。"
            : typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true ? "Player 模块已关闭。"
            : !EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelAuthoring) ? "关卡制作扩展未启用或声明不兼容。" : null;
        public string TrialStatus => _status;
        public bool TrialReady => _ready;
        public bool IsCleaning => _stopping || PlayerDevelopmentEnvironment.IsCleaning;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlatformerLevelAuthoringAdapter()
        {
            EmberLevelAdapters.Register(Instance, "platformer-2d", () => Available);
            EmberLevelAdapters.Register(new PlatformerLevelObjectAdapter(), "platformer-2d", () => Available);
            EmberConditionalMenu.Register("Ember/Level/2D 关卡制作工作台", () => Available, EmberLevelWorkbench.Open);
            PlayerLabEditorBridge.PrepareLevelAuthoring = Instance.Prepare;
            PlayerLabEditorBridge.ReportDevelopmentFailure = message =>
            { if (EmberLevelTrialSession.Request?.adapterId == ADAPTER_ID) EmberLevelTrialSession.ReportFailure(message); };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && EmberLevelTrialSession.Request?.adapterId == ADAPTER_ID) Instance.Enter().Forget();
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    if (EmberLevelTrialSession.Request?.adapterId == ADAPTER_ID && PlayerDataModule.TryGetInstance(out var data) &&
                        data.WhenReady.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    {
                        var result = data.WhenReady.Result;
                        if (result.Status == Game.PlayerData.PrepareStatus.IoFailed || result.Status == Game.PlayerData.PrepareStatus.ConfigInvalid ||
                            result.Status == Game.PlayerData.PrepareStatus.IndexCorrupt || result.Status == Game.PlayerData.PrepareStatus.StorageVersionUnsupported)
                            EmberLevelTrialSession.ReportFailure("测试存储初始化失败：" + result.Status + "；已停止试玩，未回退正式存储。请检查独立测试根与存储配置。");
                    }
                    Instance._lifetime?.Cancel(); Instance._ready = false;
                }
                if (state == PlayModeStateChange.EnteredEditMode) { Instance._entry?.Dispose(); Instance._entry = null; Instance._lifetime?.Dispose(); Instance._lifetime = null; }
            };
        }
        private void Prepare()
        {
            var request = EmberLevelTrialSession.Request;
            if (request?.adapterId != Id) return;
            if (SessionState.GetString("Ember.Level.Consumed", "") == request.requestId) throw new InvalidOperationException("本次关卡请求已消费；请停止后重新启动。");
            if (string.IsNullOrEmpty(PlayerDevelopmentEnvironment.StorageRoot)) throw new InvalidOperationException("隔离存储未准备，禁止使用正式存档。");
            var level = AssetDatabase.LoadAssetAtPath<PlatformerAuthoringLevel>(AssetDatabase.GUIDToAssetPath(request.configurationGuid));
            if (level == null || level.Id != request.levelId) throw new InvalidOperationException("冻结关卡描述已失效。");
            if (AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(level)).ToString() != request.configurationHash)
                throw new InvalidOperationException("保存后的关卡配置发生变化，取消本次启动；请重新启动以捕获新配置。");
            var config = level.Config;
            string path = AssetDatabase.GUIDToAssetPath(request.sceneGuid);
            if (request.mode == EmberLevelStartMode.Checkpoint)
            {
                using var powers = PlayerPowerSession.CreateNewGame(config.Powers);
                var life = config.Scheme?.Life;
                var snapshot = new PlatformerSnapshot(config.Scene, request.position, Vector2.zero, true, powers.Capture(), config.EnableEffects,
                    Array.Empty<EntityTimedEffectSnapshot>(), new PlatformerLifeRecord(life?.Enabled == true, life?.Enabled == true ? life.Initial : 0));
                _entry = PlayerEntryRequest.ForLevel(level.Id, path, config, snapshot);
            }
            else _entry = request.mode == EmberLevelStartMode.Position
                ? PlayerEntryRequest.ForLevelTrial(level.Id, path, config, request.position)
                : PlayerEntryRequest.ForLevel(level.Id, path, config);
            _requestId = request.requestId; _lifetime = new CancellationTokenSource(); _status = "正式初始化中（测试进度）";
        }
        private async UniTaskVoid Enter()
        {
            string ownedRequest = _requestId;
            try
            {
                if (_entry == null || _lifetime == null) throw new InvalidOperationException("关卡入口未在 Init 前冻结。");
                var token = _lifetime.Token; string id = _requestId;
                double until = EditorApplication.timeSinceStartup + 45;
                while (GameLauncher.CurrentLifetime == null || GameLauncher.Instance.Fsm == null || !GameLauncher.Instance.Fsm.Is<MainState>() || GameLauncher.Instance.Fsm.IsTransitioning)
                { if (EditorApplication.timeSinceStartup > until) throw new TimeoutException("等待正式主菜单超时。"); await UniTask.Yield(token); }
                var storage = await PlayerDataModule.Instance.WhenReady.AsUniTask().AttachExternalCancellation(token);
                if (!storage.Succeeded) throw new InvalidOperationException("测试存储初始化失败：" + storage.Status);
                if (EmberLevelTrialSession.Request?.requestId != id) throw new OperationCanceledException();
                SessionState.SetString("Ember.Level.Consumed", id);
                var flow = GameLauncher.Instance.Fsm;
                var result = await ((GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow, _entry, token).Completion;
                if (result.Status != EmberTransitionStatus.Completed) throw new InvalidOperationException("正式关卡进入失败：" + result.Status + " " + result.Error);
                token.ThrowIfCancellationRequested();
                if (EmberLevelTrialSession.Request?.requestId != id) return;
                var player = PlayerModule.Instance;
                if (!player.HasSession || player.Controller == null) throw new InvalidOperationException("转场完成但玩家未就绪。");
                var recovery = player.Controller.GetComponent<PlayerCheckpointRecovery>();
                if (recovery != null) recovery.InitializeEntryBaseline();
                var request = EmberLevelTrialSession.Request;
                if (recovery != null && request?.mode == EmberLevelStartMode.Checkpoint)
                    recovery.SetBaseline(new PlayerCheckpointRecord(request.checkpointId, player.RecoveryTarget, recovery.Checkpoint.CopyWorld()));
                _ready = true; _status = "试玩就绪 · 测试进度 · " + _entry.LevelId;
                _entry.Dispose(); _entry = null;
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (EmberLevelTrialSession.Request?.requestId != ownedRequest) return;
                _status = error.Message; EmberLevelTrialSession.ReportFailure(_status); StopTrial();
            }
        }
        private async UniTaskVoid Stop()
        {
            if (_stopping) return;
            _stopping = true; _ready = false; _lifetime?.Cancel();
            try
            {
                if (GameLauncher.CurrentLifetime != null) await GameLauncher.Instance.ShutdownFrameworkAsync().AsUniTask();
                EditorApplication.isPlaying = false;
            }
            catch (Exception error) { _status = "停止清理失败：" + error.Message; EmberLevelTrialSession.ReportFailure(_status); }
            finally { _stopping = false; }
        }
        private static IEnumerable<T> Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public IEnumerable<EmberLevelDescriptor> Discover() => (Available ? EmberLevelAssetService.Find<PlatformerAuthoringLevel>() : Array.Empty<PlatformerAuthoringLevel>()).Select(level =>
            new EmberLevelDescriptor { id = level.Id, displayName = level.name, sceneGuid = level.SceneGuid, configuration = level, adapterId = Id });

        public IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor level, EmberLevelTrialRequest trial)
        {
            var issues = new List<EmberLevelDiagnostic>();
            void Error(string rule, string message, UnityEngine.Object target = null) => issues.Add(new EmberLevelDiagnostic(rule, message, EmberLevelSeverity.Error, target));
            if (UnavailableReason != null) Error("template", UnavailableReason);
            var asset = level?.configuration as PlatformerAuthoringLevel;
            if (asset == null || asset.Config == null || asset.Config.Scheme == null) { Error("level.config", "缺少关卡配置或完整玩家方案。", asset); return issues; }
            if (Discover().Count(l => l.id == level.id) != 1) Error("level.id", "重复关卡稳定ID：" + level.id, asset);
            if (string.IsNullOrEmpty(level.ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(level.ScenePath) == null) { Error("level.scene", "场景引用失效。", asset); return issues; }
            if (asset.Config.Scene != Path.GetFileNameWithoutExtension(level.ScenePath)) Error("level.scene-config", "配置Scene与关卡场景不一致。", asset.Config);
            Scene scene = SceneManager.GetSceneByPath(level.ScenePath); bool opened = !scene.IsValid();
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(level.ScenePath, OpenSceneMode.Additive);
                foreach (var transform in Components<Transform>(scene))
                {
                    if (PrefabUtility.IsPrefabAssetMissing(transform.gameObject)) Error("prefab.missing", "Prefab源资源缺失。", transform.gameObject);
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) Error("script.missing", "对象存在缺失脚本。", transform.gameObject);
                }
                var players = Components<PlayerController>(scene).ToArray();
                var scopes = Components<GameSceneScope>(scene).Where(s => s.isActiveAndEnabled).ToArray();
                if (players.Length != 1 || scopes.Length != 1) Error("level.assembly", "需要唯一玩家与启用的 GameSceneScope。", asset);
                var checkpoints = Components<PlatformerAuthoringCheckpoint>(scene).ToArray();
                foreach (var group in checkpoints.GroupBy(c => c.Id).Where(g => string.IsNullOrWhiteSpace(g.Key) || g.Count() > 1)) Error("checkpoint.id", "检查点ID为空或重复：" + group.Key, group.First());
                if (trial?.mode == EmberLevelStartMode.Checkpoint)
                {
                    var found = checkpoints.Where(c => c.Id == trial.checkpointId && c.isActiveAndEnabled).ToArray();
                    if (found.Length != 1) Error("checkpoint.missing", "请选择唯一且启用的已登记检查点。", asset);
                    else trial.position = found[0].Spawn;
                }
                if (Components<LaunchZone2D>(scene).Any() && players.Any(p => p.GetComponent<PlayerLaunchTarget2D>() == null))
                    Error("mechanism.launch-target", "关卡存在弹射区，但玩家缺少 PlayerLaunchTarget2D 接收端。", players.First());
                if (trial != null && players.Length == 1)
                {
                    var shape = players[0].GetComponent<BoxCollider2D>();
                    Vector2 point = trial.mode == EmberLevelStartMode.Entrance ? asset.Config.Spawn : trial.position;
                    if (!float.IsFinite(point.x) || !float.IsFinite(point.y)) Error("spawn.finite", "测试位置必须为有限坐标。", asset);
                    if (shape == null) Error("spawn.shape", "缺少玩家碰撞形状。", players[0]);
                    else if (float.IsFinite(point.x) && float.IsFinite(point.y))
                    {
                        var size = Vector2.Scale(shape.size, players[0].transform.lossyScale) * .98f;
                        Physics2D.SyncTransforms();
                        foreach (var collider in Physics2D.OverlapBoxAll(point + shape.offset, size, players[0].transform.eulerAngles.z))
                            if (collider.gameObject.scene == scene && collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger && !collider.transform.IsChildOf(players[0].transform))
                                Error("spawn.blocked", "出生空间与实体碰撞重叠；请移动测试位置。", collider);
                    }
                }
            }
            catch (Exception error) { Error("level.validation", error.Message, asset); }
            finally { if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
            return issues;
        }

        public EmberLevelDescriptor Create(string folder, string name, EmberLevelDescriptor source)
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelAuthoring, true);
            if (!Available) throw new InvalidOperationException(UnavailableReason);
            if (UnavailableReason != null) throw new InvalidOperationException(UnavailableReason);
            if (!AssetDatabase.IsValidFolder(folder) || !folder.StartsWith("Assets/", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\')) throw new ArgumentException("请选择 Assets 下有效目录与文件名。");
            string scenePath = folder + "/" + name + ".unity", configPath = folder + "/" + name + "Config.asset", levelPath = folder + "/" + name + "Level.asset";
            foreach (var path in new[] { scenePath, configPath, levelPath }) if (File.Exists(path)) throw new InvalidOperationException("路径已存在：" + path);
            var sourceLevel = source?.configuration as PlatformerAuthoringLevel;
            string sourceScene = source?.ScenePath ?? PlatformerLevelSamples.SCENE;
            var configSource = sourceLevel?.Config ?? AssetDatabase.LoadAssetAtPath<PlatformerAuthoringLevel>(PlatformerLevelSamples.LEVEL)?.Config;
            if (configSource == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(sourceScene) == null) throw new InvalidOperationException("缺少模板白盒骨架，请先安装关卡样例。");
            var created = new List<string>();
            try
            {
                if (!AssetDatabase.CopyAsset(sourceScene, scenePath)) throw new IOException("复制场景失败。"); created.Add(scenePath);
                var config = UnityEngine.Object.Instantiate(configSource); config.name = name + "Config";
                using (var serialized = new SerializedObject(config)) { serialized.FindProperty("_scene").stringValue = name; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                AssetDatabase.CreateAsset(config, configPath); created.Add(configPath);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    foreach (var checkpoint in Components<PlatformerAuthoringCheckpoint>(scene)) checkpoint.Configure(Guid.NewGuid().ToString("N"), checkpoint.Spawn - (Vector2)checkpoint.transform.position);
                    EditorSceneManager.SaveScene(scene);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
                var level = ScriptableObject.CreateInstance<PlatformerAuthoringLevel>(); level.name = name;
                level.Configure(Guid.NewGuid().ToString("N"), AssetDatabase.AssetPathToGUID(scenePath), config);
                AssetDatabase.CreateAsset(level, levelPath); created.Add(levelPath);
                AssetDatabase.SaveAssetIfDirty(config); AssetDatabase.SaveAssetIfDirty(level);
                var descriptor = Discover().Single(l => l.configuration == level);
                var errors = Validate(descriptor, null).Where(d => d.Severity == EmberLevelSeverity.Error).ToArray();
                if (errors.Length != 0) throw new InvalidOperationException("新关卡装配校验失败：" + string.Join("；", errors.Select(d => d.Message)));
                return descriptor;
            }
            catch { foreach (var path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path); throw; }
        }
        public void BeginTrial(EmberLevelTrialRequest frozen)
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelTrial, true);
            if (UnavailableReason != null || IsCleaning) throw new InvalidOperationException(UnavailableReason ?? "请等待清理结束。");
            _status = "保存后试玩，正在进入正式启动流程"; PlayerDevelopmentEnvironment.Play(true);
        }
        public void StopTrial() => Stop().Forget();
        #endregion
    }

    internal sealed class PlatformerLevelObjectAdapter : IEmberLevelObjectAdapter
    {
        public string Id => "platformer-2d.mechanism";
        public void ValidatePlacement(EmberLevelMaterial material, Scene scene)
        {
            RequireWrite();
            if (!EmberProjectSetup.IsTemplateActive("platformer-2d")) throw new InvalidOperationException("平台模板未激活。");
            if (material.Prefab != null && material.Prefab.GetComponentInChildren<LaunchZone2D>(true) != null &&
                !scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerLaunchTarget2D>(true)).Any())
                throw new InvalidOperationException("弹射机关需要同关卡玩家的 PlayerLaunchTarget2D 接线。");
        }
        public void OnCreated(GameObject instance, bool duplicate)
        {
            RequireWrite();
            foreach (var checkpoint in instance.GetComponentsInChildren<PlatformerAuthoringCheckpoint>(true))
            { Undo.RecordObjects(new UnityEngine.Object[] { checkpoint, checkpoint.GetComponent<PlayerCheckpointZone2D>() }, "检查点身份"); checkpoint.Configure(Guid.NewGuid().ToString("N"), checkpoint.Spawn - (Vector2)checkpoint.transform.position); PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint); PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint.GetComponent<PlayerCheckpointZone2D>()); }
        }
        public void BeforeDelete(GameObject instance) { RequireWrite(); }
        public IEnumerable<EmberLevelDiagnostic> Validate(GameObject instance) { yield break; }
        public void DrawScene(GameObject instance)
        {
            if (!PlatformerLevelAuthoringAdapter.Available) return;
            foreach (var collider in instance.GetComponentsInChildren<BoxCollider2D>(true))
            { Handles.color = collider.isTrigger ? Color.yellow : Color.green; using (new Handles.DrawingScope(collider.transform.localToWorldMatrix)) Handles.DrawWireCube(collider.offset, collider.size); }
        }
        private static void RequireWrite()
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelAuthoring, true);
            if (!PlatformerLevelAuthoringAdapter.Available) throw new InvalidOperationException("平台关卡模块未启用。");
        }
    }
}
