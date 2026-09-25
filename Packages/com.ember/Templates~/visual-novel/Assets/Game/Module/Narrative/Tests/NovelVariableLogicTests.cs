using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed class NovelVariableLogicTests
    {
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => false;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = null; return false; }
        }
        private static readonly Catalog Ready = new();
        private static NovelCommand Op(string id, string target, NovelIntegerOperation op, int value = 0, string source = null)
            => new(id, NovelCommandKind.CalculateVariable, variableId: target, integerOperation: op, integerOperand: value,
                operandVariableId: source, scope: NovelVariableScope.Global, operandScope: NovelVariableScope.Global);
        private static NovelCommand Say(string id) => new(id, NovelCommandKind.Say, "停", id + "-line");
        private static NovelCommand Random(string id, int min = 0, int max = 99) => new(id, NovelCommandKind.RandomVariable,
            variableId: "roll", scope: NovelVariableScope.Global, randomMin: min, randomMax: max);
        private static NovelStory Story(params NovelCommand[] commands)
            => new("logic", 1, "chapter", new[] { new NovelChapter("chapter", 1, "entry", new[] {
                new NovelNode("entry", NovelNodeKind.Dialogue, "end", commands: commands),
                new NovelNode("end", NovelNodeKind.Ending, endingId: "end") }) }, new[] {
                new NovelVariable("money", new NovelValue(100)), new NovelVariable("income", new NovelValue(0)),
                new NovelVariable("clean", new NovelValue(3)), new NovelVariable("plant", new NovelValue(2)),
                new NovelVariable("roll", new NovelValue(0)), new NovelVariable("name", new NovelValue("小雨")) });
        private static void Continue(NarrativeRunner runner, long frame)
        {
            var s = runner.Snapshot;
            if (s.State == NarrativeState.Revealing) runner.CompleteReveal(s.SessionGeneration, s.PositionVersion);
            s = runner.Snapshot; Assert.IsTrue(runner.Advance(s.SessionGeneration, s.PositionVersion, frame));
        }

        [Test]
        public void MonthlySettlementUsesLiveCountersAndDebitAccumulates()
        {
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(Story(Op("a", "income", NovelIntegerOperation.Assign, source: "clean"),
                Op("b", "income", NovelIntegerOperation.Add, source: "plant"), Op("c", "income", NovelIntegerOperation.Multiply, 5),
                Op("d", "income", NovelIntegerOperation.Add, 20), Op("e", "money", NovelIntegerOperation.Add, source: "income"),
                Op("f", "money", NovelIntegerOperation.Subtract, 10), Op("g", "clean", NovelIntegerOperation.Assign), Say("stop")), Ready));
            Assert.AreEqual(45, runner.Snapshot.GlobalVariables["income"].Int);
            Assert.AreEqual(135, runner.Snapshot.GlobalVariables["money"].Int);
            Assert.AreEqual(0, runner.Snapshot.GlobalVariables["clean"].Int);
        }

        [TestCase(NovelIntegerOperation.Divide, 3, 33)]
        [TestCase(NovelIntegerOperation.Modulo, 3, 1)]
        [TestCase(NovelIntegerOperation.Add, 15, 115)]
        public void IntegerOperations(NovelIntegerOperation operation, int operand, int expected)
        {
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(Story(Op("calc", "money", operation, operand), Say("stop")), Ready));
            Assert.AreEqual(expected, runner.Snapshot.GlobalVariables["money"].Int);
        }

        [Test]
        public void OverflowAndRuntimeZeroDivisorFaultWithoutChangingTarget()
        {
            foreach (var command in new[] { Op("overflow", "money", NovelIntegerOperation.Multiply, int.MaxValue),
                Op("zero", "money", NovelIntegerOperation.Divide, source: "income") })
            {
                var runner = new NarrativeRunner(); Assert.IsFalse(runner.StartStory(Story(command), Ready));
                Assert.AreEqual(100, runner.Snapshot.GlobalVariables["money"].Int);
                Assert.AreEqual("VariableArithmetic", runner.Snapshot.Error.Code);
                Assert.AreEqual(command.CommandId, runner.Snapshot.Error.CommandId);
            }
        }

        [Test]
        public void InvalidOperandsAndRandomBoundsRejectBeforeExecution()
        {
            foreach (var command in new[] { Op("string", "name", NovelIntegerOperation.Add, 1),
                Op("source", "money", NovelIntegerOperation.Add, source: "missing"),
                Op("zero", "money", NovelIntegerOperation.Divide), Random("range", 5, 4) })
            {
                var runner = new NarrativeRunner(); Assert.IsFalse(runner.StartStory(Story(command), Ready));
                Assert.AreEqual("BadVariableOperation", runner.Snapshot.Error.Code);
                Assert.IsEmpty(runner.Snapshot.GlobalVariables);
            }
        }

        [Test]
        public void SeedAndCheckpointPreserveNextRandomDrawWithoutReplayingDebit()
        {
            var story = Story(Random("r1"), Op("debit", "money", NovelIntegerOperation.Subtract, 10), Say("first"), Random("r2"), Say("second"));
            var runner = new NarrativeRunner(randomSeed: 42); Assert.IsTrue(runner.StartStory(story, Ready));
            var s = runner.Snapshot; runner.CompleteReveal(s.SessionGeneration, s.PositionVersion);
            Assert.IsTrue(runner.TryCapture(out var saved, out var error), error);
            saved = JsonUtility.FromJson<NovelCheckpoint>(JsonUtility.ToJson(saved));
            var restored = new NarrativeRunner(randomSeed: 999); Assert.IsTrue(restored.TryRestore(story, Ready, saved, out error), error);
            Continue(runner, 1); Continue(restored, 1);
            Assert.AreEqual(runner.Snapshot.GlobalVariables["roll"].Int, restored.Snapshot.GlobalVariables["roll"].Int);
            Assert.AreEqual(90, restored.Snapshot.GlobalVariables["money"].Int);
            saved.RandomState = 0;
            Assert.IsFalse(new NarrativeRunner().TryRestore(story, Ready, saved, out error));
        }

        [TestCase(-3, 3)]
        [TestCase(8, 8)]
        [TestCase(int.MinValue, int.MaxValue)]
        public void RandomBoundsAndRepeatability(int min, int max)
        {
            var commands = Enumerable.Range(0, 100).SelectMany(i => new[] { Random("r" + i, min, max), Say("s" + i) }).ToArray();
            var story = Story(commands); var a = new NarrativeRunner(randomSeed: 7); var b = new NarrativeRunner(randomSeed: 7);
            Assert.IsTrue(a.StartStory(story, Ready)); Assert.IsTrue(b.StartStory(story, Ready));
            for (int i = 0; i < 100; i++)
            {
                int actual = a.Snapshot.GlobalVariables["roll"].Int;
                Assert.That(actual, Is.InRange(min, max)); Assert.AreEqual(actual, b.Snapshot.GlobalVariables["roll"].Int);
                if (i < 99) { Continue(a, i + 1); Continue(b, i + 1); }
            }
        }

        [Test]
        public void CalculationAndRandomConfigurationChangeCompatibility()
        {
            Assert.AreNotEqual(NovelCompatibility.Fingerprint(Story(Op("a", "money", NovelIntegerOperation.Add, 1))),
                NovelCompatibility.Fingerprint(Story(Op("a", "money", NovelIntegerOperation.Add, 2))));
            Assert.AreNotEqual(NovelCompatibility.Fingerprint(Story(Random("r", 0, 10))),
                NovelCompatibility.Fingerprint(Story(Random("r", 0, 11))));
        }

        [Test]
        public void TextBindingsPreserveSourceAndRestoreDisplayedValues()
        {
            var command = new NovelCommand("say", NovelCommandKind.Say, "【主控昵称】有xx元。", "line",
                textBindings: new[] { new NovelTextBinding("【主控昵称】", "name"), new NovelTextBinding("xx", "money") });
            var story = Story(command); var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            Assert.AreEqual("小雨有100元。", runner.CurrentCommand.Text); Assert.AreEqual("【主控昵称】有xx元。", command.Text);
            Assert.AreSame(runner.CurrentCommand, runner.CurrentCommand);
            var s = runner.Snapshot; runner.CompleteReveal(s.SessionGeneration, s.PositionVersion);
            Assert.IsTrue(runner.TryCapture(out var saved, out var error), error);
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, saved, out error), error);
            Assert.AreEqual("小雨有100元。", restored.CurrentCommand.Text);
        }

        [Test]
        public void TextBindingRejectsMissingVariablesAndOverlappingTokens()
        {
            foreach (var bindings in new[] { new[] { new NovelTextBinding("xx", "missing") },
                new[] { new NovelTextBinding("xx", "money"), new NovelTextBinding("x", "money") } })
            {
                var story = Story(new NovelCommand("s", NovelCommandKind.Say, "xx元", "line", textBindings: bindings));
                Assert.IsFalse(new NarrativeRunner().StartStory(story, Ready));
            }
        }
        [Test]
        public void ThirdVisitEventOccursOnceAndUnaffordableChoiceIsHidden()
        {
            NovelPredicate Is(string id, NovelValue value) => new(id, NovelComparison.Equal, value, NovelVariableScope.Global);
            var story = new NovelStory("loop", 1, "ch", new[] { new NovelChapter("ch", 1, "menu", new[] {
                new NovelNode("menu", NovelNodeKind.Choice, routes: new[] {
                    new NovelRoute("clean", "清扫", "count"),
                    new NovelRoute("buy", "购买", "ordinary", new NovelCondition(NovelJunction.All,
                        new NovelPredicate("money", NovelComparison.GreaterOrEqual, new NovelValue(200), NovelVariableScope.Global))) }),
                new NovelNode("count", NovelNodeKind.Dialogue, "gate", commands: new[] { Op("count-up", "visits", NovelIntegerOperation.Add, 1) }),
                new NovelNode("gate", NovelNodeKind.Branch, "ordinary", routes: new[] {
                    new NovelRoute("third", "", "special", new NovelCondition(NovelJunction.All, Is("visits", new NovelValue(3)), Is("seen", new NovelValue(false)))) }),
                new NovelNode("special", NovelNodeKind.Dialogue, "menu", commands: new[] {
                    new NovelCommand("mark", NovelCommandKind.SetVariable, variableId: "seen", value: new NovelValue(true), scope: NovelVariableScope.Global),
                    Op("reward", "money", NovelIntegerOperation.Add, 15), Say("special-line") }),
                new NovelNode("ordinary", NovelNodeKind.Dialogue, "menu", commands: new[] { Say("normal-line") }) }) },
                new[] { new NovelVariable("visits", new NovelValue(0)), new NovelVariable("seen", new NovelValue(false)), new NovelVariable("money", new NovelValue(0)) });
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            for (int i = 1; i <= 5; i++)
            {
                var s = runner.Snapshot; Assert.AreEqual(1, s.Options.Count);
                Assert.IsTrue(runner.Choose(s.SessionGeneration, s.PositionVersion, "clean", i * 2));
                Assert.AreEqual(i == 3 ? "special-line" : "normal-line", runner.CurrentCommand.CommandId);
                Continue(runner, i * 2 + 1);
            }
            Assert.AreEqual(15, runner.Snapshot.GlobalVariables["money"].Int);
            Assert.AreEqual(5, runner.Snapshot.GlobalVariables["visits"].Int);
        }

        [Test]
        public void CommandSerializationPreservesAllLogicParameters()
        {
            var operation = Op("copy", "money", NovelIntegerOperation.Add, 42, "clean");
            var restored = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(operation));
            Assert.AreEqual(operation.IntegerOperation, restored.IntegerOperation);
            Assert.AreEqual(operation.IntegerOperand, restored.IntegerOperand);
            Assert.AreEqual(operation.OperandVariableId, restored.OperandVariableId);
            Assert.AreEqual(operation.OperandScope, restored.OperandScope);
            var random = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(Random("r", -7, 8)));
            Assert.AreEqual(-7, random.RandomMin); Assert.AreEqual(8, random.RandomMax);
        }
        [TestCase(0, "rare")]
        [TestCase(29, "rare")]
        [TestCase(30, "common")]
        [TestCase(99, "common")]
        public void ProbabilityIntervalsIncludeEveryBoundaryAndUseFallback(int roll, string ending)
        {
            var chapter = new NovelChapter("chance", 1, "draw", new[] {
                new NovelNode("draw", NovelNodeKind.Dialogue, "branch", commands: new[] { Random("draw-once", roll, roll) }),
                new NovelNode("branch", NovelNodeKind.Branch, "common", routes: new[] {
                    new NovelRoute("30-percent", "", "rare", new NovelCondition(NovelJunction.All,
                        new NovelPredicate("roll", NovelComparison.Less, new NovelValue(30), NovelVariableScope.Global))) }),
                new NovelNode("rare", NovelNodeKind.Ending, endingId: "rare"),
                new NovelNode("common", NovelNodeKind.Ending, endingId: "common") });
            var story = new NovelStory("chance-story", 1, "chance", new[] { chapter },
                new[] { new NovelVariable("roll", new NovelValue(-1)) });
            var runner = new NarrativeRunner(randomSeed: 1);
            Assert.IsTrue(runner.StartStory(story, Ready));
            Assert.AreEqual(ending, runner.Snapshot.EndingId);
        }
    }
}
