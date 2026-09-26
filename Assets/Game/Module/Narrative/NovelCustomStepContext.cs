using System;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 自定义节点本次执行的唯一凭据。会话代次与位置版本封装在内部，
    /// 脚本拿不到 <see cref="NarrativeRunner"/>，也不能直接改剧情位置。
    /// </summary>
    public sealed class NovelCustomStepContext
    {
        #region 内部参数
        private readonly NovelSession _session;
        private readonly long _generation;
        private readonly long _positionVersion;
        private bool _settled, _cancelled;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        /// <summary>本步骤的参数（只读）。</summary>
        public NovelCommand Command { get; }
        /// <summary>本次执行的唯一标识。跨 Module 请求 / 结果配对时原样带回。</summary>
        public string ExecutionId { get; }
        public string ChapterId { get; }
        public string NodeId { get; }
        /// <summary>
        /// 本次执行私有的状态槽。脚本资产是共享的，运行状态必须放这里。
        /// </summary>
        public object State { get; set; }
        /// <summary>
        /// 会话是否仍然有效。读档、退出 Gameplay、故障之后为 false；
        /// 业务 Module 的迟到回调必须先检查它，再写变量或完成。
        /// </summary>
        public bool IsAlive => !_cancelled && _session != null && _session.IsGenerationAlive(_generation);
        internal long Generation => _generation;
        internal long PositionVersion => _positionVersion;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        internal NovelCustomStepContext(NovelSession session, NovelCommand command, long generation, long positionVersion)
        {
            _session = session; _generation = generation; _positionVersion = positionVersion;
            Command = command; ChapterId = session?.Snapshot.ChapterId; NodeId = session?.Snapshot.NodeId;
            ExecutionId = (command?.CommandId ?? "step") + "@" + generation + "#" + positionVersion;
        }

        internal void MarkCancelled() => _cancelled = true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>正常完成，剧情继续。重复调用是幂等的。</summary>
        public void Complete()
        {
            if (_settled || !IsAlive) return;
            _settled = true; _session.CompleteCustomStep(_generation, _positionVersion, this);
        }

        /// <summary>故障结束。不伪造成正常结局；不清空变量、不静默继续。</summary>
        public void Fail(string message)
        {
            if (_settled || !IsAlive) return;
            _settled = true;
            _session.FailCustomStep(_generation, _positionVersion, this,
                string.IsNullOrWhiteSpace(message) ? "自定义节点失败" : message);
        }

        /// <summary>追加等待原因（资源、配音、动作等）。不改变完成与取消的语义。</summary>
        public void SetWait(NarrativeWait reason)
        {
            if (_settled || !IsAlive) return;
            _session.SetCustomStepWait(_generation, _positionVersion, reason);
        }

        /// <summary>写入剧情变量。目标必须已声明且类型一致。</summary>
        public bool SetVariable(NovelVariableScope scope, string id, NovelValue value)
            => SetVariable(scope, id, value, out _);

        /// <summary>写入剧情变量，失败时给出可定位的原因。</summary>
        public bool SetVariable(NovelVariableScope scope, string id, NovelValue value, out string error)
        {
            error = null;
            if (!IsAlive) { error = "会话已失效"; return false; }
            return _session.TrySetStoryVariable(scope, id, value, out error);
        }

        /// <summary>读取剧情变量（当前章节或全局）。</summary>
        public bool TryGetVariable(NovelVariableScope scope, string id, out NovelValue value)
        {
            value = default;
            return IsAlive && _session.TryGetStoryVariable(scope, id, out value);
        }

        /// <summary>
        /// 取得一个暂停凭据：在 Dispose 之前剧情推进冻结，但会话的逐帧 Tick 会提前返回，
        /// 因此本节点的 <see cref="NovelCustomStepSO.OnTick"/> 也会停止。
        /// 需要持续推进的逻辑请交给业务 Module 自己的 Update。
        /// </summary>
        public IDisposable AcquirePause() => IsAlive ? _session.AcquirePause("CustomStep:" + ExecutionId) : null;

        /// <summary>
        /// 打开/关闭一路玩家推进锁：锁定期间点击与空格都不推进剧情，但演出、计时与自动播放照常。
        /// 与暂停的区别正在这里——暂停冻结一切，锁只挡玩家输入，适合自动播放的脚本化段落。
        /// 同一 <paramref name="reason"/> 幂等；必须由同名的后续节点解锁，并在
        /// <see cref="NovelCustomStepSO.OnCancel"/> 里兜底解锁（那时 <see cref="IsAlive"/> 已是 false，本方法仍然生效）。
        /// </summary>
        public void SetInputLock(string reason, bool locked)
        {
            if (_session == null || string.IsNullOrWhiteSpace(reason)) return;
            _session.SetStoryInputLock(reason, locked);
        }

        /// <summary>切换阅读模式。自动播放片段用它进入 <see cref="NarrativeReadMode.Auto"/>，结束再切回手动。</summary>
        public bool SetReadMode(NarrativeReadMode mode) => IsAlive && _session.SetStoryReadMode(mode);

        /// <summary>
        /// 临时覆盖自动播放间隔（秒）；传 null 恢复玩家设置里的间隔。
        /// 让脚本化段落有固定节奏，不受玩家把自动间隔调得很长的影响。
        /// </summary>
        public void SetAutoIntervalOverride(float? seconds)
        {
            if (_session == null) return;
            _session.SetStoryAutoIntervalOverride(seconds);
        }
        #endregion
    }
}
