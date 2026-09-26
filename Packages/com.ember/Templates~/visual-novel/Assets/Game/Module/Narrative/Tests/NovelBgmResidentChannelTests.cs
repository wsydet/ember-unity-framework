using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 常驻分段 BGM 通道的运行期验收：在真实框架场景里确认
    /// 「Global 阶段装配的通道能用真实资源起播 → 从本曲目的前奏开始 → 收尾转入本曲目的尾段」。
    /// 这里不依赖真实时间推进（Editor 帧率不可控），所以只等状态，不用 WaitForSeconds 反推段落。
    /// </summary>
    public sealed class NovelBgmResidentChannelTests
    {
        private const string CLIP = "Audio/Narrative/M1Sample/quiet_afternoon";

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            NovelPlayModeScenes.DiscardUnsavedScenes();
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            NovelPlayModeScenes.DiscardUnsavedScenes();
        }

        [UnityTest]
        public IEnumerator ResidentChannelPlaysIntroAndTurnsToOutroOnLeave()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();

            INovelBgmChannel channel = null;
            for (int i = 0; i < 300 && channel == null; i++) { yield return null; channel = NovelBgmModule.Active; }
            Assert.IsNotNull(channel, "Global 阶段必须装配分段 BGM 模块，否则正式运行期会退回会被暂停的单文件循环");

            // 占位配表里 theme_main 的四段都指向同一段音频，等价于一段可用的分段曲目。
            var track = new NovelBgmTrack("theme_main", CLIP, CLIP, CLIP, CLIP);
            Assert.IsTrue(channel.Play(track, 1, 0), channel.Error);
            for (int i = 0; i < 300 && channel.Status == NovelBgmStatus.Loading; i++) yield return null;
            Assert.AreEqual(NovelBgmStatus.Playing, channel.Status, channel.Error);
            Assert.AreEqual("theme_main", channel.TrackId);
            Assert.AreEqual(NovelBgmSegment.Intro, channel.Segment, "有前奏的曲目必须从前奏开始");

            // 退出阅读时走的就是这条调用（NarrativeModule.EndSession 在会话销毁前发尾段）。
            Assert.IsTrue(channel.FinishWithOutro(), "配了尾段的曲目收尾必须真的切到尾段");
            Assert.AreEqual(NovelBgmSegment.Outro, channel.Segment);
            Assert.AreEqual("theme_main", channel.TrackId, "尾段期间曲目身份必须保持，尾段才不会被别的曲目顶掉");

            channel.Stop(0);
            Assert.AreEqual(NovelBgmSegment.None, channel.Segment);
            Assert.IsNull(channel.TrackId);
        }
    }
}
