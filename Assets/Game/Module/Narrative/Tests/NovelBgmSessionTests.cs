using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// BGM 族指令与常驻分段通道的接线：会话只发意图，通道负责播放与跨暂停存活。
    /// 通道用假实现注入，验证的是会话侧的语义（谁被调用、参数是什么、失败与忽略策略）。
    /// </summary>
    public sealed partial class NovelSessionTests
    {
        private sealed class BgmChannelFake : INovelBgmChannel
        {
            public readonly List<NovelBgmTrack> Tracks = new();
            public readonly List<float> Volumes = new();
            public readonly List<float> Fades = new();
            public int Climaxes, Outros, Stops;
            public float LastClimaxFade = -1;
            /// <summary>置 true 后 Play 立刻进入失败态，用于验证剧情层的报错语义。</summary>
            public bool Fail;
            public NovelBgmStatus Status { get; private set; }
            public string Error { get; private set; }
            public string TrackId { get; private set; }
            public NovelBgmSegment Segment { get; private set; }

            public bool Play(NovelBgmTrack track, float volume = 1, float fade = 0, bool restart = false)
            {
                Tracks.Add(track); Volumes.Add(volume); Fades.Add(fade);
                TrackId = track?.Id; Segment = track?.OpeningSegment ?? NovelBgmSegment.None;
                Error = Fail ? "假通道：曲目加载失败" : null;
                Status = Fail ? NovelBgmStatus.Failed : NovelBgmStatus.Playing;
                return true;
            }
            public bool Climax(float fade = NovelBgmRules.CLIMAX_FADE)
            {
                Climaxes++; LastClimaxFade = fade;
                if (Status != NovelBgmStatus.Playing) return false;
                Segment = NovelBgmSegment.Climax; return true;
            }
            public bool FinishWithOutro(float fade = 0)
            {
                Outros++;
                if (Status != NovelBgmStatus.Playing) return false;
                Segment = NovelBgmSegment.Outro; return true;
            }
            public void Stop(float fade = 0)
            {
                Stops++; Segment = NovelBgmSegment.None; TrackId = null; Status = NovelBgmStatus.Idle;
            }
            /// <summary>模拟通道已经在播某曲目的某一段，用于验证「同曲同段不打断」。</summary>
            public void Seed(string trackId, NovelBgmSegment segment)
            {
                TrackId = trackId; Segment = segment;
                Status = segment == NovelBgmSegment.None ? NovelBgmStatus.Idle : NovelBgmStatus.Playing;
            }
        }

        private static NovelCommand Bgm(string id, string key, float volume = 1, float duration = 0) =>
            new(id, NovelCommandKind.BGM, resourceKey: key, volume: volume, duration: duration);
        private static NovelCommand BgmClimax(string id, float duration = 0) =>
            new(id, NovelCommandKind.BGMClimax, duration: duration);
        private static NovelCommand BgmStop(string id, float duration = 0) =>
            new(id, NovelCommandKind.BGMStop, duration: duration);
        private NovelSession BgmSession(out BgmChannelFake bgm, out ReadingAudio audio, params NovelCommand[] commands)
        {
            Commands(commands);
            var resources = new ReadingResources { Story = _story };
            audio = new ReadingAudio(); bgm = new BgmChannelFake();
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, resources, audio, bgm);
            session.AttachView(new View()); session.Tick(0, 0); return session;
        }
        /// <summary>把当前句推到可存档的稳定点。</summary>
        private static void PumpStable(NovelSession session, ref int frame)
        {
            for (int i = 0; i < 30 && session.Snapshot.State != NarrativeState.AwaitingAdvance; i++) session.Tick(10, ++frame);
        }
        /// <summary>用给定的常驻通道读档，走完准备与提交。</summary>
        private NovelSession RestoreBgm(NovelCheckpoint save, BgmChannelFake bgm, out ReadingAudio audio)
        {
            var resources = new ReadingResources { Story = _story };
            audio = new ReadingAudio();
            var restored = new NovelSession(save, () => _tables, resources, audio, bgm);
            int frame = 0;
            for (int i = 0; i < 60 && !restored.RestoreReady && restored.Snapshot.Error == null; i++) restored.Tick(0, ++frame);
            Assert.IsNull(restored.Snapshot.Error, restored.Snapshot.Error?.ToString());
            Assert.IsTrue(restored.RestoreReady, "恢复准备未完成");
            restored.CommitRestore(new View());
            return restored;
        }

        [Test]
        public void BgmNodeStartsTheTrackOnTheResidentChannel()
        {
            using var session = BgmSession(out var bgm, out _, Bgm("music", "theme_main", .6f, 2), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(1, bgm.Tracks.Count);
            Assert.AreEqual("theme_main", bgm.Tracks[0].Id);
            Assert.AreEqual(.6f, bgm.Volumes[0], .001f, "节点的剧情音量要原样交给通道");
            Assert.AreEqual(2f, bgm.Fades[0], .001f, "节点的渐变秒数就是曲目淡入时长");
            Assert.AreEqual(NovelBgmSegment.Intro, bgm.Segment, "有前奏的曲目必须从前奏开始");
            Assert.IsNull(session.Snapshot.Error);
        }

        [Test]
        public void BgmClimaxWithoutAnyTrackIsIgnoredAndTheStoryContinues()
        {
            using var session = BgmSession(out var bgm, out _, BgmClimax("hit"), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(1, bgm.Climaxes, "高潮指令仍会送达通道，由通道判定是否可以切入");
            Assert.AreEqual(NovelBgmSegment.None, bgm.Segment, "没有曲目时不得凭空造出段落");
            Assert.IsNull(session.Snapshot.Error, "没有曲目时高潮指令必须安全忽略，不能中断剧情");
        }

        [TestCase(.5f, .5f)]
        [TestCase(0f, NovelBgmRules.CLIMAX_FADE)]
        public void BgmClimaxUsesAuthoredFadeOrTheDefault(float authored, float expected)
        {
            using var session = BgmSession(out var bgm, out _, Bgm("music", "theme_main"), BgmClimax("hit", authored), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(1, bgm.Climaxes);
            Assert.AreEqual(expected, bgm.LastClimaxFade, .001f);
            Assert.AreEqual(NovelBgmSegment.Climax, bgm.Segment);
        }

        [Test]
        public void UiPauseNeverTouchesTheResidentChannel()
        {
            using var session = BgmSession(out var bgm, out var audio, Bgm("music", "theme_main"), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            var track = bgm.TrackId;
            foreach (var reason in new[] { "ReadingMenu", "History", "Settings", "SavePage", "RestoreTransaction", "SceneLoading" })
            {
                session.Pause(reason);
                session.Tick(5, ++frame);
                Assert.AreEqual(0, bgm.Stops, "暂停源 " + reason + " 不得停止 BGM");
                Assert.AreEqual(0, bgm.Outros, "暂停源 " + reason + " 不得触发尾段");
                Assert.AreEqual(track, bgm.TrackId, "暂停源 " + reason + " 不得换曲或清空曲目");
                Assert.IsTrue(audio.Paused, "对话音与音效仍应随暂停停下");
                session.Resume(reason);
            }
            Assert.AreEqual(1, bgm.Tracks.Count, "暂停与恢复期间不应重新请求播放");
        }

        [Test]
        public void BgmStopStopsTheChannelAndClearsTheSavedKey()
        {
            using var session = BgmSession(out var bgm, out _, Bgm("music", "theme_main"), BgmStop("quiet", 1), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(1, bgm.Stops, "BGMStop 必须落到通道上");
            Assert.IsNull(bgm.TrackId);
            // 保存只在台词稳定显示后才允许，先把当前句推完。
            for (int i = 0; i < 30 && session.Snapshot.State != NarrativeState.AwaitingAdvance; i++) session.Tick(10, ++frame);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            Assert.IsNull(checkpoint.BgmKey, "停止后存档不应再记着曲目键");
            Assert.AreEqual(NovelBgmSegment.None, checkpoint.BgmSegment, "停止后存档不应再记着段落");
        }

        [Test]
        public void BgmLoadFailureFaultsTheStoryInsteadOfSilentlyMuting()
        {
            using var session = BgmSession(out var bgm, out _, Bgm("music", "theme_main"), SayAction("line"));
            bgm.Fail = true;
            int frame = 0;
            session.Tick(0, ++frame); session.Tick(0, ++frame);
            Assert.IsNotNull(session.Snapshot.Error, "曲目加载失败必须报错，不能静默静音");
        }

        [Test]
        public void MissingSegmentRowFallsBackToTheLegacySingleFileLoop()
        {
            // quiet_afternoon 只在 novel_audio 里，是旧剧情的单文件 BGM；
            // 它必须被当成「只有循环段」的曲目，旧剧情零改动。
            using var session = BgmSession(out var bgm, out _, Bgm("music", "quiet_afternoon"), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(1, bgm.Tracks.Count);
            var track = bgm.Tracks[0];
            Assert.AreEqual("quiet_afternoon", track.Id);
            Assert.IsNull(track.IntroPath, "旧曲目没有前奏");
            Assert.AreEqual("Audio/Narrative/M1Sample/quiet_afternoon", track.LoopPath);
            Assert.AreEqual(NovelBgmSegment.Loop, bgm.Segment, "没有前奏的曲目直接进循环");
        }

        [Test]
        public void UnknownTrackKeyIsRejectedByValidationBeforePlayback()
        {
            // 未知曲目键在校验期就被拦下：分段曲目与旧单文件曲目共用同一条资源键判定路径，
            // 所以编排阶段就会报 MissingResourceKey，而不是等到播放时才静默静音。
            Commands(Bgm("music", "no_such_track"));
            Assert.IsFalse(_story.TryReadDefinition(_tables, out _, out var issues));
            Assert.AreEqual("MissingResourceKey", issues[0].Code);
        }

        [Test]
        public void SavedTrackSegmentAndVolumeRoundTrip()
        {
            using var session = BgmSession(out var bgm, out _, Bgm("music", "theme_main", .8f), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); PumpStable(session, ref frame);
            Assert.IsTrue(session.TryCapture(out var save, out var error), error);
            Assert.AreEqual("theme_main", save.BgmKey);
            Assert.AreEqual(NovelBgmSegment.Intro, save.BgmSegment, "存档要记下曲目所在的段落");
            Assert.AreEqual(.8f, save.BgmVolume, .001f, "存档要记下剧情的曲目音量");
            Assert.AreEqual(NovelCheckpoint.CurrentSchemaVersion, save.SchemaVersion);
            Assert.IsEmpty(save.Loops, "BGM 不再进会话循环音列表，存档里只应有环境音");
        }

        [Test]
        public void RestoreKeepsTheMusicWhenTrackAndSegmentAreUnchanged()
        {
            NovelCheckpoint save;
            using (var session = BgmSession(out var live, out _, Bgm("music", "theme_main", .8f), SayAction("line")))
            {
                int frame = 0; PumpAction(session, "line", ref frame); PumpStable(session, ref frame);
                Assert.IsTrue(session.TryCapture(out save, out var error), error);

                // 通道仍在播同一首歌的同一段：读档绝不能把它打断重来。
                live.Seed("theme_main", NovelBgmSegment.Intro);
                int requests = live.Tracks.Count;
                using var restored = RestoreBgm(save, live, out _);
                Assert.AreEqual(requests, live.Tracks.Count, "同曲同段读档必须完全不动音乐");
                Assert.AreEqual(NovelBgmSegment.Intro, live.Segment);
            }
        }

        [Test]
        public void RestoreReentersTheTrackFromItsIntroWhenTheSegmentChanged()
        {
            NovelCheckpoint save;
            using (var session = BgmSession(out var live, out _, Bgm("music", "theme_main"), SayAction("line")))
            {
                int frame = 0; PumpAction(session, "line", ref frame); PumpStable(session, ref frame);
                Assert.IsTrue(session.TryCapture(out save, out var error), error);
                // 同一首歌但停在了别的段落：不能当作「没变」，必须重新进入并从自己的前奏开始。
                live.Seed("theme_main", NovelBgmSegment.Climax);
                int requests = live.Tracks.Count;
                using var restored = RestoreBgm(save, live, out _);
                Assert.AreEqual(requests + 1, live.Tracks.Count, "段落不同就要重新进入曲目");
                Assert.AreEqual("theme_main", live.TrackId);
                Assert.AreEqual(NovelBgmSegment.Intro, live.Segment, "重新进入必须从该曲目的前奏开始");
            }
        }

        [Test]
        public void LegacyCheckpointWithoutSegmentMigratesToTheLoopSegment()
        {
            NovelCheckpoint save;
            using (var session = BgmSession(out var live, out _, Bgm("music", "quiet_afternoon", .5f), SayAction("line")))
            {
                int frame = 0; PumpAction(session, "line", ref frame); PumpStable(session, ref frame);
                Assert.IsTrue(session.TryCapture(out save, out var error), error);
                // 旧档：没有段位字段，BGM 还以循环音项的形式存在。
                save.SchemaVersion = 7; save.BgmSegment = NovelBgmSegment.None;
                save.Loops.Add(new NovelLoopState { Bgm = true, Key = "quiet_afternoon", Volume = .5f });
                int requests = live.Tracks.Count;
                live.Stop(0);
                using var restored = RestoreBgm(save, live, out _);
                Assert.AreEqual("quiet_afternoon", live.TrackId, "旧档的 BGM 循环音项要迁移成曲目");
                Assert.AreEqual(NovelBgmSegment.Loop, live.Segment, "旧曲目没有前奏，迁移成停在循环段");
                Assert.AreEqual(requests + 1, live.Tracks.Count, "旧档迁移后应重新进入曲目一次");
                Assert.AreEqual(.5f, live.Volumes[requests], .001f, "旧档的循环音音量要带到曲目上");
            }
        }

        [Test]
        public void SavedStopStateRestoresWithoutAnyBgm()
        {
            NovelCheckpoint save;
            using (var session = BgmSession(out var live, out _, Bgm("music", "theme_main"), BgmStop("quiet"), SayAction("line")))
            {
                int frame = 0; PumpAction(session, "line", ref frame); PumpStable(session, ref frame);
                Assert.IsTrue(session.TryCapture(out save, out var error), error);
                int requests = live.Tracks.Count;
                using var restored = RestoreBgm(save, live, out _);
                Assert.AreEqual(requests, live.Tracks.Count, "存档里没有 BGM 时读档不得凭空起播");
                Assert.AreEqual(NovelBgmSegment.None, live.Segment);
            }
        }
    }
}
