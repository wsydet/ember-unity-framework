using System;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>开场段落的两种用法：开始进入自动播放并锁住玩家推进；结束恢复手动阅读并解锁。</summary>
    public enum NovelOpeningSegmentMode { Begin, End }

    /// <summary>
    /// 脚本化开场段落的中性节点：成对使用（一个 Begin、一个 End），中间夹着要自动播放的台词。
    ///
    /// <b>它解决什么：</b>普通 <c>Say</c> 一定要等玩家点击才能继续，玩家也能随时点击抢跳。
    /// 本节点在 Begin 时给会话加一路「推进锁」并切到自动阅读，于是这一段：
    /// 台词自己按玩家设置的自动间隔往前走，点击与空格一律不生效（演出、计时、语音都不受影响）。
    /// End 再切回手动并解锁。锁只挡玩家输入，不是暂停——暂停会连自动播放一起冻住。
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/视觉小说/开场段落（自动播放）", fileName = "OpeningSegment")]
    public sealed class NovelOpeningSegmentSO : NovelCustomStepSO
    {
        #region 编辑器面板参数
        [SerializeField] private NovelOpeningSegmentMode _mode = NovelOpeningSegmentMode.Begin;
        /// <summary>锁的标识：Begin 与 End 必须一致；不同段落用不同 reason，避免互相解锁。</summary>
        [SerializeField] private string _lockReason = "NovelOpening";
        /// <summary>进入自动播放失败时的最长等待秒数；超时后放弃自动并解锁，避免把玩家锁死。</summary>
        [SerializeField, Range(0f, 30f)] private float _waitLimit = 8f;
        /// <summary>本段落的固定自动间隔（秒），覆盖玩家的自动间隔偏好。</summary>
        [SerializeField, Range(.2f, 10f)] private float _autoInterval = 1.6f;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private sealed class WaitState { internal float Elapsed; }
        public NovelOpeningSegmentMode Mode => _mode;

        /// <summary>
        /// Begin 与 End 是同一个脚本类型的两个资产，默认的「类型全名」不够用——那会让两份资产拿到
        /// 同一个 ScriptId，被剧情清单的重复校验挡下。这里用模式区分：模式是结构选择而不是可调参数，
        /// 所以写进 ID 是稳定的。消费端若也遇到「一个脚本类型配多个资产」，必须同样覆写 ScriptId。
        /// </summary>
        public override string ScriptId => GetType().FullName + "." + _mode;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override string DisplayName => _mode == NovelOpeningSegmentMode.Begin ? "开场段落 · 开始自动播放" : "开场段落 · 结束并解锁";

        public override string Summary() => _mode == NovelOpeningSegmentMode.Begin
            ? "锁定玩家推进 + 切自动播放（锁 " + _lockReason + "）"
            : "恢复手动阅读 + 解锁 " + _lockReason;

        public override string Validate(NovelCustomStepValidation validation)
            => string.IsNullOrWhiteSpace(_lockReason) ? "必须填写锁标识" : null;

        public override void OnBegin(NovelCustomStepContext context)
        {
            if (_mode == NovelOpeningSegmentMode.End)
            {
                context.SetReadMode(NarrativeReadMode.Manual);
                context.SetAutoIntervalOverride(null);
                context.SetInputLock(_lockReason, false);
                context.Complete();
                return;
            }
            context.State = new WaitState();
            context.SetInputLock(_lockReason, true);
            // 固定本段落的节奏，玩家把自动间隔设成 60 秒也不会拖慢开场。
            context.SetAutoIntervalOverride(_autoInterval);
            if (context.SetReadMode(NarrativeReadMode.Auto)) context.Complete();
            // 若此刻被页面/场景暂停挡下（暂停期间不允许切自动），保持本节点存活逐帧重试。
        }

        public override void OnTick(NovelCustomStepContext context, float delta)
        {
            if (_mode != NovelOpeningSegmentMode.Begin) return;
            var state = context.State as WaitState;
            if (state == null) return;
            state.Elapsed += delta;
            if (context.SetReadMode(NarrativeReadMode.Auto)) { context.Complete(); return; }
            if (state.Elapsed < _waitLimit) return;
            // 放弃自动就一并解锁并还原间隔：宁可让玩家自己点，也不能把推进彻底锁死。
            context.SetAutoIntervalOverride(null);
            context.SetInputLock(_lockReason, false);
            context.Complete();
        }

        /// <summary>被打断（读档 / 退出 / 故障）时兜底解锁并还原间隔，避免残留给下一次开局。</summary>
        public override void OnCancel(NovelCustomStepContext context)
        {
            context.SetAutoIntervalOverride(null);
            context.SetInputLock(_lockReason, false);
        }
        #endregion
    }
}
