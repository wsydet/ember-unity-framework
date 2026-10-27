#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Input;
using Game.Module;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.State
{
    /// <summary>显式验收启动场景使用；普通关卡不挂载。结果来自真实Player输入与正式生命周期。</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class PlatformerAuthoringBuildProbe : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private string _scenePath;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private string _root;
        private PlayerDataConfig _storage;
        [Serializable] private sealed class Report
        { public bool passed, editor; public string scene, error, storageRoot; public float moved, jumped; }
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--ember-level-probe-root");
            if (index < 0 || index + 1 >= args.Length || !Path.IsPathFullyQualified(args[index + 1]))
                throw new InvalidOperationException("构建验收需要显式隔离根目录。");
            _root = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(_root);
            _storage = Instantiate(Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH));
            PlayerDataModule.Instance.SetConfigurationForNextInit(_storage, _root);
            Application.runInBackground = true;
        }
        private void Start() { if (!Application.isEditor) Run().Forget(); }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private async UniTaskVoid Run()
        {
            var report = new Report { scene = _scenePath, storageRoot = _root, editor = Application.isEditor };
            Keyboard keyboard = null; InputActionAsset actions = null;
            var behavior = InputSystem.settings.backgroundBehavior;
            UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? devices = null;
            try
            {
                double until = Time.realtimeSinceStartupAsDouble + 60;
                while (!GameLauncher.IsValid || GameLauncher.Instance.Fsm == null || !GameLauncher.Instance.Fsm.Is<MainState>() || GameLauncher.Instance.Fsm.IsTransitioning)
                { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException("正式启动Main超时"); await UniTask.Yield(); }
                var data = await PlayerDataModule.Instance.WhenReady.AsUniTask();
                if (!data.Succeeded) throw new InvalidOperationException("隔离存储失败");
                var flow = GameLauncher.Instance.Fsm;
                using var entry = PlayerEntryRequest.ForLevel("sample.authoring", _scenePath, _config);
                var entered = await ((GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow, entry).Completion;
                if (entered.Status != EmberTransitionStatus.Completed) throw new InvalidOperationException("进入失败：" + entered.Error);
                var player = PlayerModule.Instance;
                if (!player.HasSession || player.Controller.gameObject.scene.path != _scenePath) throw new InvalidOperationException("未进入指定业务关卡");
                await UniTask.Delay(500);
                keyboard = InputSystem.AddDevice<Keyboard>(); actions = EmberInputManager.Instance.ActionAsset; devices = actions.devices;
                actions.devices = new InputDevice[] { keyboard }; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                Vector3 start = player.Controller.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); await UniTask.Delay(250);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await UniTask.Delay(80);
                report.moved = player.Controller.transform.position.x - start.x;
                float ground = player.Controller.transform.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); await UniTask.Delay(180);
                report.jumped = player.Controller.transform.position.y - ground;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                if (report.moved <= .1f || report.jumped <= .1f) throw new InvalidOperationException("合成正常输入未产生移动/跳跃");
                report.passed = true;
            }
            catch (Exception error) { report.error = error.ToString(); }
            finally
            {
                if (actions != null) actions.devices = devices;
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = behavior;
                if (!string.IsNullOrEmpty(_root)) File.WriteAllText(Path.Combine(_root, "build-probe.json"), JsonUtility.ToJson(report, true));
                if (GameLauncher.IsValid) GameLauncher.Instance.Quit(); else Application.Quit(report.passed ? 0 : 1);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(PlayerConfig config, string scenePath) { _config = config; _scenePath = scenePath; }
        #endregion
    }
}
#endif
