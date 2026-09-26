using System;
using Ember.Basic;
using Game.Table;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>一首分段 BGM 当前所处的段落。None 表示没有曲目在播。</summary>
    public enum NovelBgmSegment { None, Intro, Loop, Climax, Outro }

    /// <summary>分段 BGM 的请求状态；剧情层据此决定等待还是报错。</summary>
    public enum NovelBgmStatus { Idle, Loading, Playing, Failed }

    /// <summary>
    /// 会话与分段 BGM 播放器之间的唯一通道契约。
    ///
    /// 正式运行期由常驻的 <c>NovelBgmModule</c> 实现：它跨会话存活，因此菜单、读档事务、
    /// 场景加载都不会打断音乐，返回主界面后的尾段也能播完。
    /// 会话侧只发意图，不认识具体播放器；测试与编辑器试播可以注入自己的实现。
    /// </summary>
    public interface INovelBgmChannel
    {
        NovelBgmStatus Status { get; }
        string Error { get; }
        /// <summary>当前曲目键；没有曲目时为 null。</summary>
        string TrackId { get; }
        /// <summary>当前曲目所在的段落。</summary>
        NovelBgmSegment Segment { get; }
        /// <summary>进入一首曲目：先播前奏（未配前奏则直接进循环）。同曲目已在播时幂等，restart 强制重播。</summary>
        bool Play(NovelBgmTrack track, float volume = 1, float fade = 0, bool restart = false);
        /// <summary>立即切入高潮段；没有曲目或未配高潮段时返回 false 且不改变现状。</summary>
        bool Climax(float fade = NovelBgmRules.CLIMAX_FADE);
        /// <summary>收尾：切到尾段并让它播完自停；未配尾段时退化为淡出停止。</summary>
        bool FinishWithOutro(float fade = 0);
        void Stop(float fade = 0);
    }

    /// <summary>
    /// 一条分段 BGM 的播放输出。
    /// 由宿主提供：正式运行期是挂在 AudioHost 上的 AudioSource，测试里是可控的假实现。
    /// 输出自己持有播放时钟与片段长度，Director 只做段落调度与音量斜坡，不关心音频后端。
    /// </summary>
    public interface INovelBgmOutput : IDisposable
    {
        /// <summary>
        /// 当前片段的已播放时长（秒）。
        /// <b>必须由 <see cref="Tick"/> 累计的未缩放时间推进，不要读 AudioSource.time：</b>
        /// Editor 静音、无音频设备或音频时钟被挂起时真实播放位置会停滞，
        /// 段落就会卡在前奏永远接不到循环。既有循环音与编辑器试播用的也都是自累计时钟。
        /// </summary>
        float Position { get; }
        /// <summary>当前片段的长度（秒）；没有片段时为 0。</summary>
        float Length { get; }
        /// <summary>当前片段是否仍在推进。</summary>
        bool IsPlaying { get; }
        /// <summary>推进播放时钟；由调度器每帧调用，换段时归零。</summary>
        void Tick(float delta);
        /// <summary>换到指定片段并从头播放。</summary>
        void Play(AudioClip clip, bool loop);
        /// <summary>写剧情音量；玩家音量由 BGM Mixer 分组承担，不要在这里叠乘。</summary>
        void SetVolume(float volume);
        void Stop();
    }

    /// <summary>
    /// 一首曲目的四段资源路径。loopPath 必需，其余段可留空并按规则降级。
    /// 曲目视图与配表行解耦，便于测试直接构造。
    /// </summary>
    public sealed class NovelBgmTrack
    {
        #region 内部参数
        public string Id { get; }
        public string IntroPath { get; }
        public string LoopPath { get; }
        public string ClimaxPath { get; }
        public string OutroPath { get; }
        public bool HasIntro => !string.IsNullOrWhiteSpace(IntroPath);
        public bool HasClimax => !string.IsNullOrWhiteSpace(ClimaxPath);
        public bool HasOutro => !string.IsNullOrWhiteSpace(OutroPath);
        /// <summary>进入曲目时首播的段落：有前奏播前奏，否则直接进循环。</summary>
        public NovelBgmSegment OpeningSegment => HasIntro ? NovelBgmSegment.Intro : NovelBgmSegment.Loop;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelBgmTrack(string id, string introPath, string loopPath, string climaxPath, string outroPath)
        {
            Id = id; IntroPath = introPath; LoopPath = loopPath; ClimaxPath = climaxPath; OutroPath = outroPath;
        }
        /// <summary>该段配置的资源路径；未配置或段位非法时返回 null。</summary>
        public string PathFor(NovelBgmSegment segment) => segment switch
        {
            NovelBgmSegment.Intro => IntroPath,
            NovelBgmSegment.Loop => LoopPath,
            NovelBgmSegment.Climax => ClimaxPath,
            NovelBgmSegment.Outro => OutroPath,
            _ => null
        };
        #endregion
    }

    /// <summary>一首曲目已加载完成的分段音源；未配置的段为 null。</summary>
    public sealed class NovelBgmClips
    {
        #region 内部参数
        public AudioClip Intro, Loop, Climax, Outro;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public AudioClip For(NovelBgmSegment segment) => segment switch
        {
            NovelBgmSegment.Intro => Intro,
            NovelBgmSegment.Loop => Loop,
            NovelBgmSegment.Climax => Climax,
            NovelBgmSegment.Outro => Outro,
            _ => null
        };
        #endregion
    }

    public static class NovelBgmRules
    {
        #region 内部参数
        /// <summary>篇幅阈值：短于此长度的片段播放位置判定为已结束的容差。</summary>
        public const float END_EPSILON = .02f;
        /// <summary>高潮切入的默认淡入秒数；不淡变会有明显爆音。</summary>
        public const float CLIMAX_FADE = .15f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>把配表行转成运行期曲目视图；行为空时返回 null。</summary>
        [HasGC]
        public static NovelBgmTrack ToTrack(NovelBgmRow row) => row == null
            ? null
            : new NovelBgmTrack(row.Id, row.IntroPath, row.LoopPath, row.ClimaxPath, row.OutroPath);

        /// <summary>校验曲目配置；返回 null 表示合法。</summary>
        [HasGC]
        public static string Validate(NovelBgmRow row)
        {
            if (row == null) return "BGM 曲目行为空";
            if (string.IsNullOrWhiteSpace(row.Id)) return "BGM 曲目缺少 id";
            if (string.IsNullOrWhiteSpace(row.LoopPath)) return "BGM 曲目缺少循环段路径：" + row.Id;
            foreach (var pair in new[]
            {
                ("前奏", row.IntroPath), ("循环", row.LoopPath), ("高潮", row.ClimaxPath), ("尾段", row.OutroPath)
            })
                if (pair.Item2 != null && string.IsNullOrWhiteSpace(pair.Item2))
                    return "BGM 曲目的" + pair.Item1 + "段路径只有空白字符：" + row.Id;
            return null;
        }
        #endregion
    }
}
