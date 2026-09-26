using System.Linq;
using Ember.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    // 自定义节点运行期契约：启动 / 等待 / 完成 / 失败 / 取消，以及通用模块桥接节点。
    // 静态契约（指纹划界、剧情校验、编辑器名称表守卫）见 NovelCustomStepRegistryTests。
    public sealed partial class NovelSessionTests
    {
        #region 内部参数
        private const string GLOBALS_JSON =
            "{\"_globals\":[{\"_id\":\"playerName\",\"_value\":{\"_type\":2,\"_int\":0,\"_string\":\"旅人\"}}," +
            "{\"_id\":\"score\",\"_value\":{\"_type\":1,\"_int\":0,\"_string\":\"\"}}]}";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static NovelCommand StepCommand(string id, string scriptId)
            => new(id, NovelCommandKind.CustomStep, customStepId: scriptId);

        private static NovelCommand SpeakerVariableLine(string id, string lineId, string text, string variableId = "playerName")
            => new(id, NovelCommandKind.Say, text, lineId, characterId: WAN,
                speakerVariableId: variableId, speakerVariableScope: NovelVariableScope.Global);

        private void SetFloat(UnityEngine.Object target, string field, float value)
        { var data = new SerializedObject(target); data.FindProperty(field).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }

        /// <summary>把自定义节点命令装进样例对话节点，返回按正式配表启动的会话。</summary>
        private NovelSession StepSession(View view, NovelCustomStepSO script, params NovelCommand[] commands)
        {
            if (script != null) List(_story, "_customSteps", script);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = commands.ToList() }), _story.Entry.Entry);
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            session.AttachView(view); session.Tick(0, 0); return session;
        }

        private static void ResetCustomStepStatics()
        {
            TestWriteStep.ResetState();
            TestBridgeService.ResetState();
            EmberServiceLocator.Unregister<INovelStepService>();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void CustomStepWritesAVariableAndContinuesToTheNextNode()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            TestWriteStep.CompleteImmediately = true;
            TestWriteStep.WriteVariable = "playerName"; TestWriteStep.WriteValue = "林晚";
            var script = Create<TestWriteStep>();
            using var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);

            Assert.AreEqual(1, TestWriteStep.Begins, "第一步应启动自定义节点");
            Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, "节点完成后应继续走到结局节点");
            Assert.AreEqual(1, TestWriteStep.Ends);
            Assert.AreEqual("林晚", session.Snapshot.GlobalVariables["playerName"].String, "自定义节点必须能写回剧情变量");
        }

        [Test]
        public void CustomStepWaitsForExternalCompletionAndKeepsItsContextAlive()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            TestWriteStep.CompleteImmediately = false;
            var script = Create<TestWriteStep>();
            using var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);

            Assert.AreEqual(1, TestWriteStep.Begins);
            Assert.IsTrue(session.Snapshot.HasActiveSession, "脚本未完成时剧情不能继续");
            Assert.AreEqual(NarrativeWait.CustomStep, session.Snapshot.Wait & NarrativeWait.CustomStep);
            var context = TestWriteStep.Last;
            Assert.IsTrue(context.IsAlive);
            Assert.AreEqual("step", context.Command.CommandId);
            Assert.IsFalse(string.IsNullOrEmpty(context.ExecutionId));

            // 逐帧驱动：会话会调用 OnTick，脚本可以据此计时（关键推进仍应交给业务 Module）。
            session.Tick(.5f, 2);
            Assert.AreEqual(1, TestWriteStep.Ticks);

            Assert.IsTrue(context.SetVariable(NovelVariableScope.Global, "playerName", new NovelValue("小雨"), out string error), error);
            context.Complete();
            Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State);
            Assert.AreEqual("小雨", session.Snapshot.GlobalVariables["playerName"].String);
            Assert.AreEqual(1, TestWriteStep.Ends);
            Assert.AreEqual(0, TestWriteStep.Cancels);
        }

        [Test]
        public void CustomStepVariableWriteRejectsUndeclaredOrMismatchedTargets()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            TestWriteStep.CompleteImmediately = false;
            var script = Create<TestWriteStep>();
            using var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);
            var context = TestWriteStep.Last;

            Assert.IsFalse(context.SetVariable(NovelVariableScope.Global, "notDeclared", new NovelValue("x"), out string missing));
            StringAssert.Contains("未声明", missing);
            Assert.IsFalse(context.SetVariable(NovelVariableScope.Global, "playerName", new NovelValue(3), out string mismatch));
            StringAssert.Contains("类型不符", mismatch);
            // 本章节作用域里没有这个变量：跨作用域不能互相遮蔽。
            Assert.IsFalse(context.SetVariable(NovelVariableScope.Chapter, "playerName", new NovelValue("x"), out string scope));
            StringAssert.Contains("未声明", scope);
            Assert.AreEqual("旅人", session.Snapshot.GlobalVariables["playerName"].String, "失败的写入不得改变原值");
            Assert.IsTrue(context.TryGetVariable(NovelVariableScope.Global, "playerName", out NovelValue current));
            Assert.AreEqual("旅人", current.String);
            context.Fail("用例结束");
        }

        [Test]
        public void FailingCustomStepFaultsWithoutPretendingToEndNormally()
        {
            ResetCustomStepStatics();
            TestWriteStep.FailOnBegin = true;
            var script = Create<TestWriteStep>();
            using var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);

            Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State);
            StringAssert.Contains("测试失败", session.Snapshot.Error.Message);
            Assert.AreEqual(1, TestWriteStep.Ends, "失败也要走正常收尾，而不是取消路径");
            Assert.AreEqual(0, TestWriteStep.Cancels);
        }

        [Test]
        public void DisposingTheSessionCancelsARunningCustomStep()
        {
            ResetCustomStepStatics();
            TestWriteStep.CompleteImmediately = false;
            var script = Create<TestWriteStep>();
            var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);
            var context = TestWriteStep.Last;
            Assert.IsTrue(context.IsAlive);

            session.Dispose();

            Assert.AreEqual(1, TestWriteStep.Cancels, "读档 / 退出必须通知脚本收尾");
            Assert.AreEqual(0, TestWriteStep.Ends);
            Assert.IsFalse(context.IsAlive);
            Assert.IsFalse(context.SetVariable(NovelVariableScope.Global, "playerName", new NovelValue("x")));
            Assert.IsFalse(context.SetVariable(NovelVariableScope.Global, "playerName", new NovelValue("x"), out string dead));
            StringAssert.Contains("已失效", dead);
        }

        [Test]
        public void FireAndForgetStepDoesNotBlockTheStoryWhenTheScriptNeverCompletes()
        {
            ResetCustomStepStatics();
            TestWriteStep.CompleteImmediately = false;
            TestWriteStep.FireAndForget = true;
            var script = Create<TestWriteStep>();
            using var session = StepSession(new View(), script, StepCommand("step", script.ScriptId));
            session.Tick(0, 1);

            Assert.AreEqual(1, TestWriteStep.Begins);
            Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, "WaitForCompletion=false 时剧情不应停在等待里");
        }

        [Test]
        public void BridgeStepWritesTheModuleResultIntoAStoryVariable()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            var bridge = Create<NovelStepServiceBridgeSO>();
            Set(bridge, "_requestKey", "guess_number");
            Set(bridge, "_payload", "target=7");
            Set(bridge, "_resultVariableId", "playerName");
            var service = new TestBridgeService { Result = "猜对了" };
            EmberServiceLocator.Register<INovelStepService>(service);
            try
            {
                using var session = StepSession(new View(), bridge, StepCommand("step", bridge.ScriptId));
                session.Tick(0, 1);

                Assert.AreEqual(1, TestBridgeService.Invokes);
                Assert.AreEqual("guess_number", TestBridgeService.LastRequestKey);
                Assert.AreEqual("target=7", TestBridgeService.LastPayload);
                Assert.IsFalse(string.IsNullOrEmpty(TestBridgeService.LastExecutionId), "请求必须带上本次执行标识，供结果配对");
                Assert.IsTrue(session.Snapshot.HasActiveSession);

                service.Complete();
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State);
                Assert.AreEqual("猜对了", session.Snapshot.GlobalVariables["playerName"].String);
                Assert.AreEqual(0, TestBridgeService.Aborts);
            }
            finally { EmberServiceLocator.Unregister<INovelStepService>(); }
        }

        [Test]
        public void BridgeStepConvertsTheReturnedTextToTheDeclaredVariableType()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            var bridge = Create<NovelStepServiceBridgeSO>();
            Set(bridge, "_requestKey", "roll");
            Set(bridge, "_resultVariableId", "score");
            var service = new TestBridgeService { Result = "42" };
            EmberServiceLocator.Register<INovelStepService>(service);
            try
            {
                using var session = StepSession(new View(), bridge, StepCommand("step", bridge.ScriptId));
                session.Tick(0, 1);
                service.Complete();
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State);
                Assert.AreEqual(42, session.Snapshot.GlobalVariables["score"].Int, "整数变量应解析成数字而不是原样存字符串");
            }
            finally { EmberServiceLocator.Unregister<INovelStepService>(); }
        }

        [Test]
        public void BridgeStepFailsLoudlyWhenNoServiceIsRegistered()
        {
            ResetCustomStepStatics();
            var bridge = Create<NovelStepServiceBridgeSO>();
            Set(bridge, "_requestKey", "guess_number");
            using var session = StepSession(new View(), bridge, StepCommand("step", bridge.ScriptId));
            session.Tick(0, 1);

            Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State, "没有服务时必须报错，不能永久停在等待里");
            StringAssert.Contains("INovelStepService", session.Snapshot.Error.Message);
        }

        [Test]
        public void BridgeStepTimesOutAndAbortsInsteadOfBlockingForever()
        {
            ResetCustomStepStatics();
            var bridge = Create<NovelStepServiceBridgeSO>();
            Set(bridge, "_requestKey", "guess_number");
            SetFloat(bridge, "_timeout", .5f);
            var service = new TestBridgeService();
            EmberServiceLocator.Register<INovelStepService>(service);
            try
            {
                using var session = StepSession(new View(), bridge, StepCommand("step", bridge.ScriptId));
                session.Tick(0, 1); session.Tick(.2f, 2);
                Assert.IsTrue(session.Snapshot.HasActiveSession, "未到超时时必须继续等待");

                session.Tick(.4f, 3);
                Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State);
                StringAssert.Contains("超时", session.Snapshot.Error.Message);
                Assert.AreEqual(1, TestBridgeService.Aborts, "超时后要通知业务模块中止");
            }
            finally { EmberServiceLocator.Unregister<INovelStepService>(); }
        }

        [Test]
        public void SpeakerVariableOverridesTheCharacterNameForTheCurrentLine()
        {
            ResetCustomStepStatics();
            InstallLocalization();
            try
            {
                Assert.IsTrue(NovelLocalization.TryGetCharacterName(WAN, out string realName), "角色名走 character.<角色键>");
                JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
                var view = new View();
                using var session = StepSession(view, null,
                    SpeakerVariableLine("say-name", "line-name", "我回来了。"),
                    new NovelCommand("say-plain", NovelCommandKind.Say, "……你是谁？", "line-plain", characterId: WAN));
                int frame = 0;
                session.Tick(0, ++frame);

                // 填了说话人变量：显示变量当前值，而不是角色名。
                Assert.AreEqual("say-name", session.Snapshot.CommandId);
                Assert.AreEqual("旅人", view.Speaker);
                Assert.AreNotEqual(realName, view.Speaker);

                // 没填的下一句回到原回退链，行为与接入前完全一致。
                session.Tick(10, ++frame);
                Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
                session.Advance(++frame); session.Tick(0, ++frame);
                Assert.AreEqual("say-plain", session.Snapshot.CommandId);
                Assert.AreEqual(realName, view.Speaker);
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void SpeakerVariableFallsBackToTheCharacterNameWhenTheValueIsBlank()
        {
            ResetCustomStepStatics();
            InstallLocalization();
            try
            {
                Assert.IsTrue(NovelLocalization.TryGetCharacterName(WAN, out string realName));
                // 变量存在但值是空白：不走覆盖分支，退回角色名，不显示空白说话人。
                JsonUtility.FromJsonOverwrite(
                    "{\"_globals\":[{\"_id\":\"playerName\",\"_value\":{\"_type\":2,\"_int\":0,\"_string\":\"   \"}}]}", _story);
                var view = new View();
                using var session = StepSession(view, null, SpeakerVariableLine("say-name", "line-name", "我回来了。"));
                session.Tick(0, 1);
                Assert.AreEqual(realName, view.Speaker, "空白值必须退回角色名");
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void HistoryReResolvesTheSpeakerVariableAfterARename()
        {
            ResetCustomStepStatics();
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            var view = new View();
            using var session = StepSession(view, null,
                SpeakerVariableLine("say-before", "line-before", "我回来了。"),
                new NovelCommand("rename", NovelCommandKind.SetVariable, variableId: "playerName",
                    value: new NovelValue("小雨"), scope: NovelVariableScope.Global),
                SpeakerVariableLine("say-after", "line-after", "再说一次。"));
            int frame = 0;
            session.Tick(0, ++frame);
            Assert.AreEqual("旅人", view.Speaker);
            session.Tick(10, ++frame);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
            Assert.AreEqual(1, session.History.Count);

            session.Advance(++frame); session.Tick(0, ++frame);
            Assert.AreEqual("say-after", session.Snapshot.CommandId, "中间的设值步骤应即时执行");
            Assert.AreEqual("小雨", view.Speaker);
            session.Tick(10, ++frame);

            var history = session.History;
            Assert.AreEqual(2, history.Count);
            Assert.AreEqual("playerName", history[0].SpeakerVariableId, "历史条目要带上说话人变量，而不是只存解析结果");
            Assert.AreEqual(NovelVariableScope.Global, history[0].SpeakerVariableScope);
            // 读时解析：改名之后，历史里改名前的旧句也一起变成新名字。
            Assert.AreEqual("小雨", history[0].Speaker, "历史按当前变量值重新解析，旧句不会留下旧名字");
            Assert.AreEqual("小雨", history[1].Speaker);
        }
        #endregion
    }

    /// <summary>测试用自定义节点：行为由静态开关驱动，便于覆盖启动 / 完成 / 失败 / 取消四条路径。</summary>
    public sealed class TestWriteStep : NovelCustomStepSO
    {
        public static int Begins, Ends, Cancels, Ticks;
        public static NovelCustomStepContext Last;
        public static bool CompleteImmediately, FailOnBegin, FireAndForget;
        public static string WriteVariable, WriteValue;
        public override bool WaitForCompletion => !FireAndForget;

        public static void ResetState()
        {
            Begins = Ends = Cancels = Ticks = 0; Last = null;
            CompleteImmediately = FailOnBegin = FireAndForget = false;
            WriteVariable = WriteValue = null;
        }

        public override void OnBegin(NovelCustomStepContext context)
        {
            Begins++; Last = context;
            if (FailOnBegin) { context.Fail("测试失败"); return; }
            if (!string.IsNullOrEmpty(WriteVariable)) context.SetVariable(NovelVariableScope.Global, WriteVariable, new NovelValue(WriteValue ?? string.Empty));
            if (CompleteImmediately) context.Complete();
        }

        public override void OnTick(NovelCustomStepContext context, float delta) => Ticks++;
        public override void OnEnd(NovelCustomStepContext context) => Ends++;
        public override void OnCancel(NovelCustomStepContext context) => Cancels++;
    }

    /// <summary>测试用委托式业务服务：记录请求并允许测试手动完成或中止。</summary>
    public sealed class TestBridgeService : INovelStepService
    {
        public static int Invokes, Aborts;
        public static string LastRequestKey, LastExecutionId, LastPayload;
        private System.Action<string> _complete, _fail;
        public string Result = "";

        public static void ResetState()
        {
            Invokes = Aborts = 0; LastRequestKey = LastExecutionId = LastPayload = null;
        }

        public void Invoke(string requestKey, string executionId, string payload, System.Action<string> complete, System.Action<string> fail)
        {
            Invokes++; LastRequestKey = requestKey; LastExecutionId = executionId; LastPayload = payload;
            _complete = complete; _fail = fail;
        }

        public void Abort(string executionId) { Aborts++; _complete = null; _fail = null; }

        public void Complete() { var complete = _complete; _complete = null; _fail = null; complete?.Invoke(Result); }
        public void Fail(string error) { var fail = _fail; _complete = null; _fail = null; fail?.Invoke(error); }
    }
}
