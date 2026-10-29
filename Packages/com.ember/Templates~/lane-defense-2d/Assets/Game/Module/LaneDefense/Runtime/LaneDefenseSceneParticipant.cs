using System;
using Cysharp.Threading.Tasks;
using Game.Input;
using Game.SceneFlow;
using UnityEngine;

namespace Game.LaneDefense
{
    public interface ILaneDefensePolicyProvider { LaneDefensePolicies CreatePolicies(LaneLevelSnapshot level); }

    public sealed class LaneDefenseSceneParticipant : GameSceneParticipant
    {
        #region 编辑器面板参数
        [SerializeField] private LaneDefenseLevel _level;
        [SerializeField] private LaneDefenseBoardView _view;
        [SerializeField] private MonoBehaviour _policyProvider;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if(_level==null||_view==null)throw new InvalidOperationException("LaneDefense level/view missing.");
            if(GameInputModule.Instance.State!=GameInputServiceState.Ready)throw new InvalidOperationException("GameInput must be explicitly enabled and ready.");
            if(_policyProvider!=null&&_policyProvider is not ILaneDefensePolicyProvider)throw new InvalidOperationException("Invalid policy provider.");
            var level=(context.Args as LaneDefenseLevel??_level).Freeze();
            _view.ValidateResources();
            var lease=new LaneDefenseSceneLease(level,_view,_policyProvider as ILaneDefensePolicyProvider);
            return UniTask.FromResult<IGameScenePreparedParticipant>(lease);
        }
        #endregion
    }

    internal sealed class LaneDefenseSceneLease : IGameScenePreparedParticipant
    {
        #region 内部参数
        private readonly LaneLevelSnapshot _level;
        private readonly LaneDefenseBoardView _view;
        private readonly ILaneDefensePolicyProvider _provider;
        private bool _started,_disposed;
        public BattleSession Battle {get;private set;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal LaneDefenseSceneLease(LaneLevelSnapshot level,LaneDefenseBoardView view,ILaneDefensePolicyProvider provider)
        {_level=level;_view=view;_provider=provider;Battle=new BattleSession(level,provider?.CreatePolicies(level));}
        public void Start()
        {
            if(_disposed)throw new ObjectDisposedException(nameof(LaneDefenseSceneLease));if(_started)return;
            try{_view.Bind(Battle);Battle.Start();LaneDefenseModule.Instance.Attach(this);_started=true;}
            catch{Dispose();throw;}
        }
        internal void Tick(float delta,bool input)
        {
            if(!_started||_disposed)return;
            _view.ReadInput(input);Battle.Advance(delta);_view.Render();
        }
        internal void Restart()
        {
            if(_disposed)return;
            var candidate=new BattleSession(_level,_provider?.CreatePolicies(_level));
            try{_view.Bind(candidate);candidate.Start();}
            catch{candidate.Dispose();throw;}
            var old=Battle;Battle=candidate;old.Dispose(RemovalReason.Restart);
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            LaneDefenseModule.Instance.Detach(this);
            _view.Release();Battle.Dispose();
        }
        #endregion
    }
}
