using System;
using System.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Ember.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>base 的可选 Global 模块。登记、活动与 Ready 分别查询，不在构造时访问 Manager。</summary>
    [EmberModule(ModulePhase.Global, Enabled = true)]
    public sealed class GameInputModule : EmberSingleton<GameInputModule>, IEmberModule
    {
        #region 内部参数
        public const string CONFIG_PATH = "Config/GameInput/GameInputConfig";
        private GameInputConfig _configuration;
        private InputActionAsset _preparedActions;
        private IDisposable _shutdownSubscription;
        private IDisposable _lifetimeAbort;
        private GameTransitionInputGate _transitionGate;
        private bool _hasOverride;
        public GameInputServiceState State { get; private set; }
        public long Epoch { get; private set; }
        public GameInputService Service { get; private set; }
        public Task<GameInputPrepareResult> WhenReady { get; private set; }
            = Task.FromResult(new GameInputPrepareResult(0, GameInputPrepareStatus.NotInitialized));
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Complete(GameInputPrepareStatus status, string error = null)
        {
            State = status == GameInputPrepareStatus.Ready ? GameInputServiceState.Ready : GameInputServiceState.Faulted;
            WhenReady = Task.FromResult(new GameInputPrepareResult(Epoch, status, error));
        }

        private void Stop()
        {
            if (State == GameInputServiceState.Stopped) return;
            State = GameInputServiceState.Stopped;
            Epoch++;
            var service = Service;
            Service = null;
            service?.Dispose();
            _transitionGate?.Dispose();
            _transitionGate = null;
            _shutdownSubscription?.Dispose();
            _shutdownSubscription = null;
            _lifetimeAbort?.Dispose();
            _lifetimeAbort = null;
            WhenReady = Task.FromResult(new GameInputPrepareResult(Epoch, GameInputPrepareStatus.Stopped));
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>由 Global 激活完成后的项目入口显式接线；不隐式创建 FSM 或激活模块。</summary>
        public void BindTransitions(EmberStateMachine fsm, string channel)
        {
            if (State != GameInputServiceState.Ready || Service == null || Service.IsStopped)
                throw new InvalidOperationException("输入服务尚未 Ready，不能接管转场输入。");
            if (_transitionGate != null)
                throw new InvalidOperationException("本输入服务世代已接线转场。");
            _transitionGate = new GameTransitionInputGate(fsm, Service, channel);
        }

        /// <summary>
        /// 初始化前显式注入。preparedActions 用于测试/特殊启动，由调用方持有，替代 Manager 的已准备输入。
        /// 注入不激活服务；普通启动由 OnInit 同步加载 Resources 配置。
        /// </summary>
        public void SetConfigurationForNextInit(GameInputConfig config, InputActionAsset preparedActions = null)
        {
            if (State == GameInputServiceState.Ready || State == GameInputServiceState.Preparing)
                throw new InvalidOperationException("运行期间不能替换输入配置，请先结束模块生命周期。");
            _configuration = config;
            _preparedActions = preparedActions;
            _hasOverride = true;
        }

        void IEmberModule.OnInit()
        {
            if (State == GameInputServiceState.Ready || State == GameInputServiceState.Preparing) return;
            _lifetimeAbort = GameLauncher.CurrentLifetime?.RegisterAbort(Stop);
            State = GameInputServiceState.Preparing;
            Epoch++;
            var config = _hasOverride ? _configuration : Resources.Load<GameInputConfig>(CONFIG_PATH);
            if (config == null || config.Actions == null)
            {
                Complete(GameInputPrepareStatus.ConfigInvalid, "缺少 GameInputConfig 或 InputActionAsset：" + CONFIG_PATH);
                return;
            }
            try
            {
                var actions = _preparedActions;
                if (actions == null)
                {
                    var manager = EmberInputManager.Instance;
                    if (manager.IsInitialized && manager.ActionAsset != config.Actions)
                    {
                        Complete(GameInputPrepareStatus.InputUnavailable, "InputManager 已由其他输入资产初始化。");
                        return;
                    }
                    manager.Init(config.Actions);
                    if (!manager.IsInitialized || manager.ActionAsset == null)
                    {
                        Complete(GameInputPrepareStatus.InputUnavailable, "InputManager 尚未准备完成，请检查 InputHost。");
                        return;
                    }
                    actions = manager.ActionAsset;
                }
                Service = new GameInputService(config, actions);
                _shutdownSubscription = EmberEventBus.Subscribe(EmberBroadcastEvent.InputShutdown, Stop);
                Complete(GameInputPrepareStatus.Ready);
            }
            catch (ArgumentException exception)
            {
                Service?.Dispose();
                Service = null;
                Complete(GameInputPrepareStatus.ConfigInvalid, exception.Message);
            }
            catch (Exception exception)
            {
                Service?.Dispose();
                Service = null;
                Complete(GameInputPrepareStatus.InputUnavailable, exception.ToString());
            }
        }

        void IEmberModule.OnDestroy() => Stop();
        void IEmberModule.ResetModuleData() => Stop();
        protected override void OnDestroy() => Stop();
        #endregion
    }
}
