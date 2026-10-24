using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entity
{
    [Flags] public enum WorldResetReason2D { Death = 1, Load = 2, NewRun = 4, Leave = 8 }
    public enum WorldResetPolicy2D { Retain, Initial, Checkpoint }
    public readonly struct WorldStateRecord2D
    {
        #region 内部参数

        public string Id { get; }
        public string State { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public WorldStateRecord2D(string id, string state)
        {
            if (string.IsNullOrWhiteSpace(id) || state == null) throw new ArgumentException("世界记录需要稳定ID和状态。");
            Id = id; State = state;
        }

        #endregion
    }

    public abstract class WorldResetParticipant2D : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField] private string _stableId;
        [SerializeField] private WorldResetPolicy2D _policy = WorldResetPolicy2D.Initial;
        [SerializeField] private WorldResetReason2D _reasons = WorldResetReason2D.Death | WorldResetReason2D.Load | WorldResetReason2D.NewRun;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public string StableId => _stableId;
        public WorldResetPolicy2D Policy => _policy;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public bool Handles(WorldResetReason2D reason) => (_reasons & reason) != 0;
        public abstract string CaptureState();
        public abstract void RestoreState(string state, WorldResetReason2D reason);

        #endregion
    }

    /// <summary>显式有序世界参与者；不在位置解析接口中产生突变，不自动重置未登记对象。</summary>
    public sealed class WorldResetScope2D : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField] private WorldResetParticipant2D[] _participants = Array.Empty<WorldResetParticipant2D>();

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private readonly Dictionary<string, string> _initial = new();
        private bool _initialized;
        public string LastError { get; private set; }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake() => Initialize();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void Initialize()
        {
            if (_initialized) return;
            var initial = new Dictionary<string, string>();
            foreach (var participant in _participants)
            {
                if (participant == null || participant.gameObject.scene != gameObject.scene ||
                    string.IsNullOrWhiteSpace(participant.StableId) || initial.ContainsKey(participant.StableId))
                    throw new InvalidOperationException("世界复位参与者缺失、跨场景或稳定ID重复。");
                initial.Add(participant.StableId, participant.CaptureState() ?? throw new InvalidOperationException("世界初始状态不能为null。"));
            }
            foreach (var pair in initial) _initial.Add(pair.Key, pair.Value);
            _initialized = true;
        }
        public WorldStateRecord2D[] Capture()
        {
            Initialize(); var result = new WorldStateRecord2D[_participants.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new WorldStateRecord2D(_participants[i].StableId, _participants[i].CaptureState());
            return result;
        }
        public void Restore(WorldStateRecord2D[] records, WorldResetReason2D reason)
        {
            Initialize(); LastError = null;
            try
            {
                var states = new Dictionary<string, string>();
                foreach (var record in records ?? throw new ArgumentNullException(nameof(records))) states.Add(record.Id, record.State);
                var planned = new List<(WorldResetParticipant2D participant, string state)>();
                foreach (var participant in _participants)
                {
                    if (participant == null) throw new InvalidOperationException("复位参与者已销毁。");
                    if (!participant.Handles(reason) || participant.Policy == WorldResetPolicy2D.Retain) continue;
                    var source = participant.Policy == WorldResetPolicy2D.Initial ? _initial : states;
                    if (!source.TryGetValue(participant.StableId, out var state)) throw new InvalidOperationException("缺少世界记录：" + participant.StableId);
                    planned.Add((participant, state));
                }
                foreach (var item in planned) item.participant.RestoreState(item.state, reason);
            }
            catch (Exception error) { LastError = error.ToString(); throw; }
        }

        #endregion
    }
}
