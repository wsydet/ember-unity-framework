using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ember.UI
{
    /// <summary>单次动画时钟。默认非缩放时间；用户暂停独立于业务 timeScale 暂停。</summary>
    public sealed class EUITransitionPlayback
    {
        public bool IsPaused { get; set; }
        public bool UseScaledTime { get; }
        public CancellationToken Cancellation { get; }
        public float DeltaTime => IsPaused ? 0f : UseScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;

        public EUITransitionPlayback(bool useScaledTime = false, CancellationToken cancellation = default)
        { UseScaledTime = useScaledTime; Cancellation = cancellation; }
    }

    /// <summary>可由遮幕会话驱动的动画能力；不包含场景或业务事务。</summary>
    public interface IEUIControlledTransitionEffect : IEUITransitionEffect
    {
        UniTask PlayEnterAsync(EUITransitionPlayback playback, float duration = -1f);
        UniTask PlayExitAsync(EUITransitionPlayback playback, float duration = -1f);
    }

    /// <summary>会话选项。负时长沿用组件配置；创建会话时复制。</summary>
    public sealed class EUICurtainOptions
    {
        public float EnterDuration { get; set; } = -1f;
        public float ExitDuration { get; set; } = -1f;
        public bool UseScaledTime { get; set; }
        public bool InitiallyPaused { get; set; }
        /// <summary>在页面 OnInit/OnShow 前跳过进度显示及其模拟计时。</summary>
        public bool SkipFakeProgress { get; set; }
        /// <summary>跳过页面自定义 Enter/Exit 槽（例如进度组淡入淡出），只播放方块。</summary>
        public bool SkipCustomTransition { get; set; }
    }
}
