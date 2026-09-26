using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 真实链路验证：Runner 自 Update 推进段落，音源是挂在宿主下的真实 AudioSource。
    /// 这里不接剧情层，只证明「前奏播完自动接自己的循环、高潮回循环、尾段播完收摊」在运行期成立。
    /// 与仓库既有运行期用例一致：由 EditMode 发现，用例内部自己进 FrameworkScene 的 Play Mode。
    /// </summary>
    public sealed class NovelBgmPlayModeTests
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<GameObject> _hosts = new();
        private bool _background;

        /// <summary>
        /// 运行期等待必须基于真实时间：Editor 失去焦点时 Time 会停，WaitForSeconds 会一直不返回。
        /// 仓库既有运行期用例同样显式打开 runInBackground。
        /// </summary>
        private void KeepRunningInBackground()
        {
            _background = Application.runInBackground;
            Application.runInBackground = true;
        }

        private AudioClip Clip(string name, float seconds)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * 44100));
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Sin(i * .01f) * .01f;
            clip.SetData(data, 0);
            _assets.Add(clip);
            return clip;
        }
        private NovelBgmRunner Runner()
        {
            var host = new GameObject("novel bgm playmode host");
            _hosts.Add(host);
            return host.AddComponent<NovelBgmRunner>();
        }
        private static AudioSource[] Sources(NovelBgmRunner runner) =>
            runner.GetComponentsInChildren<AudioSource>(true);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Application.runInBackground = _background;
            foreach (var host in _hosts) if (host) UnityEngine.Object.Destroy(host);
            _hosts.Clear();
            yield return null;
            foreach (var asset in _assets) if (asset) UnityEngine.Object.Destroy(asset);
            _assets.Clear();
            NovelPlayModeScenes.DiscardUnsavedScenes();
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            NovelPlayModeScenes.DiscardUnsavedScenes();
        }

        [UnityTest]
        public IEnumerator RunnerAdvancesIntroToLoopAndEndsOnOutro()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            KeepRunningInBackground();
            var runner = Runner();
            yield return null; // 让 Awake 建立 Director 与音源
            var director = runner.Director;
            Assert.IsNotNull(director, "Runner 必须自建 Director");

            var track = new NovelBgmTrack("theme", "theme/intro", "theme/loop", "theme/climax", "theme/outro");
            var clips = new NovelBgmClips
            {
                Intro = Clip("theme intro", .25f),
                Loop = Clip("theme loop", .5f),
                Climax = Clip("theme climax", .25f),
                Outro = Clip("theme outro", .25f)
            };

            Assert.IsTrue(director.Play(track, clips, .5f, 0));
            Assert.AreEqual(NovelBgmSegment.Intro, director.Segment);
            var introSource = Sources(runner);
            Assert.AreEqual(1, introSource.Length, "进入曲目只应占用一条音源");
            Assert.AreSame(clips.Intro, introSource[0].clip);
            Assert.IsTrue(introSource[0].isPlaying);
            Assert.IsFalse(introSource[0].loop, "前奏不能循环");

            // 段落推进用显式 tick，不用等待真实时间：
            // Editor 在用例运行期帧率不可控（实测 0.4 秒墙钟只跑了 12 帧 ≈ 22ms 游戏时间），
            // 靠 WaitForSeconds 反推段落已经推进会得到假失败。这里验的是真实 AudioSource 上的换段。
            runner.Tick(.26f);
            var loopSource = Sources(runner);
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment,
                "前奏播完必须自动接循环：clip=" + (loopSource[0].clip ? loopSource[0].clip.name : "null") +
                " isPlaying=" + loopSource[0].isPlaying);
            Assert.AreSame(clips.Loop, loopSource[0].clip, "接上的必须是本曲目的循环段");
            Assert.IsTrue(loopSource[0].loop);

            Assert.IsTrue(director.Climax());
            Assert.AreEqual(NovelBgmSegment.Climax, director.Segment);
            Assert.AreSame(clips.Climax, Sources(runner)[0].clip);
            runner.Tick(.26f);
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment, "高潮播完必须回到循环");
            Assert.AreSame(clips.Loop, Sources(runner)[0].clip);

            Assert.IsTrue(director.FinishWithOutro());
            Assert.AreEqual(NovelBgmSegment.Outro, director.Segment);
            Assert.AreSame(clips.Outro, Sources(runner)[0].clip);
            runner.Tick(.26f);
            Assert.AreEqual(NovelBgmSegment.None, director.Segment, "尾段播完必须停止");
            yield return null;
            Assert.AreEqual(0, Sources(runner).Length, "尾段播完必须释放音源对象");
        }

        [UnityTest]
        public IEnumerator RunnerKeepsPlayingWhileNothingDrivesTheStory()
        {
            // 关键约定：分段推进只由 Runner 自己驱动，不依赖阅读会话的 Tick。
            // 这里全程没有会话、没有 Pause/Resume，音乐仍必须走到循环段并保持播放。
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            KeepRunningInBackground();
            var runner = Runner();
            yield return null;
            var director = runner.Director;
            runner.PlayerVolume = .7f;
            var track = new NovelBgmTrack("theme", "theme/intro", "theme/loop", null, null);
            var clips = new NovelBgmClips { Intro = Clip("theme intro", .2f), Loop = Clip("theme loop", .5f) };

            director.Play(track, clips, .4f, 0);
            runner.Tick(.21f);
            var first = Sources(runner)[0];
            Assert.AreEqual(NovelBgmSegment.Loop, director.Segment,
                "无会话驱动时也必须走到循环段：clip=" + (first.clip ? first.clip.name : "null") +
                " isPlaying=" + first.isPlaying);
            runner.Tick(30f);
            Assert.IsTrue(director.IsPlaying, "循环段必须一直播下去，直到有人显式停止");
            var source = Sources(runner)[0];
            Assert.IsTrue(source.isPlaying);
            Assert.AreSame(clips.Loop, source.clip, "循环段走过任意长时间都必须还在循环段");
            // 接了 Mixer 分组时玩家音量由分组承担；没有分组才在音源上叠乘玩家音量。
            float expected = source.outputAudioMixerGroup ? .4f : .28f;
            Assert.AreEqual(expected, source.volume, .02f);
        }
    }
}
