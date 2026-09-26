using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 背景声明的重设语义与样例的「章节卡先于背景」约定。
    ///
    /// 官方推荐在对话节点开头重新声明一次场景背景（LastLight 就是这么写的），
    /// 于是「同一个键被反复声明」是常规写法而不是异常。它必须是幂等重设，
    /// 不能把画面先淡到全透明再淡回来。
    /// </summary>
    public sealed partial class NovelSessionTests
    {
        #region 内部参数
        private const string SAMPLE_STORY_PATH = "Config/Narrative/LastLight/Story";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static NovelCommand BackgroundAction(string id, string key, float duration = 0,
            NovelVisualAction action = NovelVisualAction.Show) =>
            new(id, NovelCommandKind.Background, resourceKey: key, duration: duration, visualAction: action);
        private static NovelCommand BackgroundFade(string id, float opacity) =>
            new(id, NovelCommandKind.Opacity, targetKind: NovelTargetKind.Background, duration: 0, opacity: opacity);
        /// <summary>推进到指定指令后再走两帧，让资源准备与过渡起点都已经写进视图。</summary>
        private static void PumpTransition(NovelSession session, string id, ref int frame)
        {
            for (int i = 0; i < 30 && session.Snapshot.CommandId != id && session.Snapshot.Error == null; i++) session.Tick(0, ++frame);
            Assert.IsNull(session.Snapshot.Error, session.Snapshot.Error?.ToString());
            Assert.AreEqual(id, session.Snapshot.CommandId);
            for (int i = 0; i < 4 && (session.Snapshot.Wait & NarrativeWait.Transition) == 0; i++) session.Tick(0, ++frame);
        }
        /// <summary>离开当前对白：第一次推进补全本页，第二次推进才进入下一条指令。</summary>
        private static void AdvancePastSay(NovelSession session, ref int frame)
        {
            for (int i = 0; i < 4 && session.Snapshot.State != NarrativeState.AwaitingAdvance; i++) session.Advance(++frame);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State, "对白没有进入可推进状态");
            session.Advance(++frame);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void E6RedeclaringTheSameBackgroundIsIdempotentAndStillConsumesItsDuration()
        {
            using var view = new ActionView();
            using var session = ActionSession(view,
                BackgroundAction("bg1", "campus"),
                SayAction("line"),
                BackgroundAction("bg2", "campus", .3f),
                SayAction("next"));
            int frame = 0;
            PumpTransition(session, "line", ref frame);
            Assert.IsNotNull(view.Picture.sprite, "首个背景应该已经应用");
            Assert.AreEqual(1f, view.Picture.color.a, .001f);

            AdvancePastSay(session, ref frame);
            PumpTransition(session, "bg2", ref frame);
            Assert.IsNotNull(view.Picture.sprite);
            Assert.AreEqual(1f, view.Picture.color.a, .001f,
                "同键重设必须保持不透明；从 0 淡入会让每个节点开头都整屏闪一下");
            Assert.IsTrue((session.Snapshot.Wait & NarrativeWait.Transition) != 0,
                "同键重设仍然占用指令时长，不能被当成零耗时指令跳过剧情节奏");
        }

        [Test]
        public void E6BackgroundChangeAndFadedOutBackgroundStillFadeInFromTransparent()
        {
            using var changedView = new ActionView();
            using var changed = ActionSession(changedView,
                BackgroundAction("bg1", "campus"),
                SayAction("line"),
                BackgroundAction("bg2", "lastlight_rooftop", .3f),
                SayAction("next"));
            int frame = 0;
            PumpTransition(changed, "line", ref frame);
            AdvancePastSay(changed, ref frame);
            PumpTransition(changed, "bg2", ref frame);
            Assert.AreEqual(0f, changedView.Picture.color.a, .001f, "换键仍然是换景，必须从透明淡入");

            // 背景被显式淡到全透明之后，同键的 Show 是真的要把它显示出来，
            // 不能因为「键相同」就保持 0 —— 那会变成一条静默失效的指令。
            using var revivedView = new ActionView();
            using var revived = ActionSession(revivedView,
                BackgroundAction("bg1", "campus"),
                BackgroundFade("fade", 0),
                SayAction("line"),
                BackgroundAction("bg2", "campus", .3f),
                SayAction("next"));
            frame = 0;
            PumpTransition(revived, "line", ref frame);
            Assert.AreEqual(0f, revivedView.Picture.color.a, .001f, "背景应已被淡到全透明");
            AdvancePastSay(revived, ref frame);
            PumpTransition(revived, "bg2", ref frame);
            Assert.AreEqual(0f, revivedView.Picture.color.a, .001f,
                "当前背景不可见时不能算幂等重设，否则这条 Show 什么也做不了");
        }

        [Test]
        public void E6SampleDeclaresTheSceneBackgroundBeforeItsChapterCard()
        {
            var story = Resources.Load<NarrativeStorySO>(SAMPLE_STORY_PATH);
            Assert.IsNotNull(story, "缺少内置样例小说：" + SAMPLE_STORY_PATH);
            Assert.IsTrue(story.TryReadDefinition(_tables, out var definition, out var issues),
                issues.Count == 0 ? "样例小说读取失败" : issues[0].ToString());
            int cards = 0;
            foreach (var chapter in definition.Chapters)
                foreach (var node in chapter.Nodes)
                {
                    bool backgroundDeclared = false;
                    foreach (var command in node.Commands)
                    {
                        if (command.Kind == NovelCommandKind.Background && command.VisualAction != NovelVisualAction.Hide)
                            backgroundDeclared = true;
                        if (command.Kind != NovelCommandKind.Say || command.TextMode != NovelTextMode.Title) continue;
                        cards++;
                        // 模板自带的验收样例必须满足这条约定：章节卡之前先声明背景。
                        // 运行期另有不透明底板兜底（见 NovelReaderBackdropTests），
                        // 但样例本身要保持「章节卡不会落在空白底色上」的示范写法。
                        Assert.IsTrue(backgroundDeclared,
                            chapter.Id + "/" + node.Id + " 的章节卡 " + command.CommandId + " 之前没有声明背景");
                    }
                }
            Assert.Greater(cards, 0, "样例里应该至少有一张章节卡，否则这条约定没有被验证到");
        }
        #endregion
    }
}
