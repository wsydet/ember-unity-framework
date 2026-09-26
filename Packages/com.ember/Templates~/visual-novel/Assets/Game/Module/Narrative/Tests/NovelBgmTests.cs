using System.Collections.Generic;
using Game.Table;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 分段 BGM 调度器：段落流转、曲目隔离（前奏只接自己的循环）、音量接缝与降级规则。
    /// 这里用假输出推进时间，验证的是调度语义；真实 AudioSource 的链路在 PlayMode 测试里验证。
    /// </summary>
    public sealed class NovelBgmTests
    {
        private sealed class FakeOutput : INovelBgmOutput
        {
            public AudioClip Clip;
            public bool Loop;
            public float Volume;
            public float Elapsed;
            public bool Stopped, Disposed;
            public float Position => Elapsed;
            public float Length => Clip ? Clip.length : 0;
            public bool IsPlaying => !Stopped && !Disposed && Clip != null;
            public void Play(AudioClip clip, bool loop) { Clip = clip; Loop = loop; Elapsed = 0; Stopped = false; }
            public void Tick(float delta) { if (!Stopped && !Disposed) Elapsed += Mathf.Max(0, delta); }
            public void SetVolume(float volume) { Volume = volume; }
            public void Stop() { Stopped = true; }
            public void Dispose() { Disposed = true; }
        }

        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<FakeOutput> _outputs = new();
        private NovelBgmDirector _director;

        private AudioClip Clip(string name, float seconds)
        {
            var clip = AudioClip.Create(name, Mathf.Max(1, Mathf.RoundToInt(seconds * 44100)), 1, 44100, false);
            _assets.Add(clip);
            return clip;
        }
        private NovelBgmDirector Director()
        {
            _director = new NovelBgmDirector(() => { var output = new FakeOutput(); _outputs.Add(output); return output; });
            return _director;
        }
        private static NovelBgmTrack Track(string id, bool intro = true, bool climax = true, bool outro = true) =>
            new(id, intro ? id + "/intro" : null, id + "/loop", climax ? id + "/climax" : null, outro ? id + "/outro" : null);
        private NovelBgmClips Clips(string id, bool intro = true, bool climax = true, bool outro = true) => new()
        {
            Intro = intro ? Clip(id + " intro", 2) : null,
            Loop = Clip(id + " loop", 3),
            Climax = climax ? Clip(id + " climax", 1) : null,
            Outro = outro ? Clip(id + " outro", 1) : null
        };
        /// <summary>时间只由调度器分发给输出，测试不自己推进时钟——与运行期同一条路径。</summary>
        private void Advance(float delta) => _director.Tick(delta);
        private FakeOutput Live() { for (int i = _outputs.Count - 1; i >= 0; i--) if (!_outputs[i].Disposed) return _outputs[i]; return null; }

        [TearDown]
        public void TearDown()
        {
            _director?.Dispose();
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear(); _outputs.Clear();
        }

        [Test]
        public void IntroPlaysThroughThenLoopsAndClimaxReturnsToLoop()
        {
            var track = Track("theme"); var clips = Clips("theme");
            var director = Director();
            Assert.IsTrue(director.Play(track, clips, .8f, 0));
            Assert.AreEqual(NovelBgmSegment.Intro, director.Segment);
            Assert.AreSame(clips.Intro, Live().Clip);
            Assert.IsFalse(Live().Loop, "前奏不能循环");

            Advance(1.9f);
            Assert.AreEqual(NovelBgmSegment.Intro, director.Segment, "前奏未播完不应切段");
            Advance(.2f);
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment);
            Assert.AreSame(clips.Loop, Live().Clip, "前奏只能接本曲目的循环段");
            Assert.IsTrue(Live().Loop);

            Advance(10);
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment, "循环段不应自行结束");

            Assert.IsTrue(director.Climax());
            Assert.AreEqual(NovelBgmSegment.Climax, director.Segment);
            Assert.AreSame(clips.Climax, Live().Clip, "高潮只能取本曲目的高潮段");
            Assert.IsFalse(Live().Loop);
            Advance(1.1f);
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment, "高潮播完必须回到循环");
            Assert.AreSame(clips.Loop, Live().Clip);
        }

        [Test]
        public void OutroPlaysOnceThenReleasesEverything()
        {
            var director = Director();
            director.Play(Track("theme"), Clips("theme"), 1, 0);
            Advance(2.1f);
            Assert.IsTrue(director.FinishWithOutro());
            Assert.AreEqual(NovelBgmSegment.Outro, director.Segment);
            Advance(1.1f);
            Assert.AreEqual(NovelBgmSegment.None, director.Segment);
            Assert.IsFalse(director.IsPlaying);
            Assert.IsTrue(_outputs.TrueForAll(o => o.Disposed), "尾段播完必须释放音源");
        }

        [Test]
        public void TracksNeverShareSegments()
        {
            var director = Director();
            var first = Clips("first"); var second = Clips("second");
            director.Play(Track("first"), first, 1, 0);
            Advance(2.1f);
            Assert.AreSame(first.Loop, Live().Clip);

            director.Play(Track("second"), second, 1, 0);
            Assert.AreEqual("second", director.TrackId);
            Assert.AreEqual(NovelBgmSegment.Intro, director.Segment, "换曲必须从新曲目的前奏重新开始");
            Assert.AreSame(second.Intro, Live().Clip);
            Assert.AreNotSame(first.Intro, Live().Clip);
            Advance(2.1f);
            Assert.AreSame(second.Loop, Live().Clip, "第二首曲目的前奏只能接第二首自己的循环");
            Assert.AreNotSame(first.Loop, Live().Clip);

            Assert.IsTrue(director.Climax());
            Assert.AreSame(second.Climax, Live().Clip);
            Assert.AreNotSame(first.Climax, Live().Clip);
        }

        [Test]
        public void SameTrackIsIdempotentUnlesRestartIsRequested()
        {
            var director = Director();
            var track = Track("theme"); var clips = Clips("theme");
            director.Play(track, clips, 1, 0);
            Advance(2.1f);
            int created = _outputs.Count;

            Assert.IsTrue(director.Play(track, clips, 1, 0));
            Assert.AreEqual(created, _outputs.Count, "同曲目重复触发不应重建音源");
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment, "同曲目重复触发不应重播前奏");

            Assert.IsTrue(director.Play(track, clips, 1, 0, restart: true));
            Assert.AreEqual(NovelBgmSegment.Intro, director.Segment);
            Assert.AreSame(clips.Intro, Live().Clip);
        }

        [Test]
        public void MissingClimaxOrOutroDegradesSafely()
        {
            var director = Director();
            var track = Track("theme", climax: false, outro: false);
            var clips = Clips("theme", climax: false, outro: false);
            Assert.IsTrue(director.Play(track, clips, 1, 0));
            Advance(2.1f);
            Assert.IsFalse(director.Climax(), "未配高潮段时高潮指令安全忽略");
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment);
            Assert.IsFalse(director.FinishWithOutro(), "未配尾段时收尾退化为淡出停止");
            Assert.IsFalse(director.IsPlaying);
        }

        [Test]
        public void SegmentJointsKeepStoryVolumeWhileClimaxFadesIn()
        {
            var director = Director();
            director.Play(Track("theme"), Clips("theme"), .8f, 0);
            Assert.AreEqual(.8f, Live().Volume, .001f, "进入曲目后按剧情音量出声");
            Advance(1f);
            Assert.AreEqual(.8f, Live().Volume, .001f);
            Advance(1.1f);
            Assert.AreEqual(.8f, Live().Volume, .001f, "前奏接循环是接缝，音量不能凹陷");

            director.Climax();
            Assert.AreEqual(0f, Live().Volume, .001f, "高潮切入必须先归零再淡入");
            Advance(NovelBgmRules.CLIMAX_FADE / 2);
            Assert.AreEqual(.4f, Live().Volume, .01f, "高潮淡入到一半应约为剧情音量的一半");
            Advance(NovelBgmRules.CLIMAX_FADE);
            Assert.AreEqual(.8f, Live().Volume, .001f);
            Advance(1f);
            Assert.AreEqual(.8f, Live().Volume, .001f, "高潮接回循环也不应出现音量凹陷");
        }

        [Test]
        public void CrossFadeKeepsTwoTracksAndReleasesTheOutgoingOne()
        {
            var director = Director();
            var first = Clips("first"); var second = Clips("second");
            director.Play(Track("first"), first, .6f, 0);
            var outgoing = Live();

            director.Play(Track("second"), second, .6f, 1);
            Assert.AreEqual(.6f, outgoing.Volume, .001f, "换曲起点：旧通道保持当前音量");
            Assert.AreEqual(0f, Live().Volume, .001f, "换曲起点：新通道从静音淡入");
            Advance(.5f);
            Assert.AreEqual(.3f, outgoing.Volume, .02f);
            Assert.AreEqual(.3f, Live().Volume, .02f);
            Advance(.6f);
            Assert.IsTrue(outgoing.Disposed, "交叉淡化结束后必须释放旧通道");
            Assert.AreEqual(.6f, Live().Volume, .001f);
        }

        [Test]
        public void TrackWithoutLoopCannotStart()
        {
            var director = Director();
            var track = new NovelBgmTrack("broken", null, null, null, null);
            var clips = new NovelBgmClips { Intro = Clip("intro", 1) };
            Assert.IsFalse(director.Play(track, clips, 1, 0), "缺少循环段的曲目不能开播");
            Assert.AreEqual(NovelBgmSegment.None, director.Segment);
            Assert.AreEqual(0, _outputs.Count);
        }

        [Test]
        public void ClimaxWithoutTrackIsIgnored()
        {
            var director = Director();
            Assert.IsFalse(director.Climax());
            Assert.IsFalse(director.FinishWithOutro());
            Assert.AreEqual(NovelBgmSegment.None, director.Segment);
        }

        [Test]
        public void RulesRequireLoopPathAndRejectBlankSegments()
        {
            Assert.IsNull(NovelBgmRules.Validate(new NovelBgmRow("theme", "a/intro", "a/loop", "a/climax", "a/outro")));
            Assert.IsNull(NovelBgmRules.Validate(new NovelBgmRow("theme", null, "a/loop", null, null)), "只有循环段是合法的最小配置");
            StringAssert.Contains("循环段", NovelBgmRules.Validate(new NovelBgmRow("theme", "a/intro", null, null, null)));
            StringAssert.Contains("只有空白字符", NovelBgmRules.Validate(new NovelBgmRow("theme", null, "a/loop", "  ", null)));
            StringAssert.Contains("id", NovelBgmRules.Validate(new NovelBgmRow(null, null, "a/loop", null, null)));
        }
    }
}
