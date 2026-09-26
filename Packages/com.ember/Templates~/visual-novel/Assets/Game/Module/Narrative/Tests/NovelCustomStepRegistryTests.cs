using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 自定义节点扩展点的静态契约：存档指纹划界、剧情校验、编辑器步骤名称表守卫。
    /// 运行期调度行为见 NovelCustomStepTests（NovelSessionTests 分部）。
    /// </summary>
    public sealed class NovelCustomStepRegistryTests
    {
        #region 内部参数
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => false;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = null; return false; }
        }
        private static readonly Catalog Ready = new();
        private readonly List<UnityEngine.Object> _assets = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private T Create<T>() where T : ScriptableObject { var asset = ScriptableObject.CreateInstance<T>(); _assets.Add(asset); return asset; }

        private static NovelCommand Step(string id, string scriptId) => new(id, NovelCommandKind.CustomStep, customStepId: scriptId);

        private static NovelCommand SpeakerLine(string id, string variableId)
            => new(id, NovelCommandKind.Say, "我回来了。", id + "-line", speakerVariableId: variableId,
                speakerVariableScope: NovelVariableScope.Global);

        private static NovelChapter Chapter(params NovelCommand[] commands) => new("chapter", 1, "talk", new[]
        {
            new NovelNode("talk", NovelNodeKind.Dialogue, "end", commands: commands),
            new NovelNode("end", NovelNodeKind.Ending, endingId: "done")
        });

        private static NovelStory Story(IList<NovelChapter> chapters, IDictionary<string, NovelCustomStepSO> registry)
            => new("custom", 1, "chapter", chapters, customSteps: registry);

        private static NovelStory StepStory(string scriptId, IDictionary<string, NovelCustomStepSO> registry)
            => Story(new[] { Chapter(Step("step", scriptId)) }, registry);

        private static IDictionary<string, NovelCustomStepSO> Register(params NovelCustomStepSO[] scripts)
        {
            var map = new Dictionary<string, NovelCustomStepSO>(StringComparer.Ordinal);
            foreach (var script in scripts) map[script.ScriptId] = script;
            return map;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void CustomStepScriptParametersParticipateInTheStoryFingerprint()
        {
            // 语义参数住在脚本资产里，所以改脚本参数必须让指纹变化；否则两份不同剧情会共用同一个存档。
            var first = Create<TestFingerprintStep>(); first.Value = 1;
            var second = Create<TestFingerprintStep>(); second.Value = 2;
            string fingerprint = NovelCompatibility.Fingerprint(StepStory(first.ScriptId, Register(first)));
            string changed = NovelCompatibility.Fingerprint(StepStory(second.ScriptId, Register(second)));
            Assert.AreNotEqual(fingerprint, changed);
            // 同一份内容重复计算必须稳定：指纹里写进了脚本资产的规范化 JSON。
            Assert.AreEqual(fingerprint, NovelCompatibility.Fingerprint(StepStory(first.ScriptId, Register(first))));
        }

        [Test]
        public void OptingOutOfTheFingerprintKeepsOlderSavesCompatible()
        {
            var first = Create<TestFingerprintStep>(); first.Value = 1; first.OptOut = true;
            var second = Create<TestFingerprintStep>(); second.Value = 2; second.OptOut = true;
            Assert.AreEqual(NovelCompatibility.Fingerprint(StepStory(first.ScriptId, Register(first))),
                NovelCompatibility.Fingerprint(StepStory(second.ScriptId, Register(second))),
                "IncludeInFingerprint=false 时脚本参数不进指纹，作者自行承担旧档配新参数的风险");
        }

        [Test]
        public void SpeakerVariableBindingNeverChangesTheStoryFingerprint()
        {
            // 说话人变量与称呼 Key 同属表现层：只改它不能让旧档被判成「剧情语义已变化」。
            var without = Story(new[] { Chapter(SpeakerLine("say", null)) }, null);
            var with = Story(new[] { Chapter(SpeakerLine("say", "playerName")) }, null);
            Assert.AreEqual(NovelCompatibility.Fingerprint(without), NovelCompatibility.Fingerprint(with));
        }

        [Test]
        public void AnEmptyCustomStepRegistryKeepsTheFingerprintOfExistingStories()
        {
            // 新增的剧情级脚本清单不得影响没有任何自定义节点的既有故事。
            var legacy = Story(new[] { Chapter(SpeakerLine("say", null)) }, null);
            var withEmptyRegistry = Story(new[] { Chapter(SpeakerLine("say", null)) }, new Dictionary<string, NovelCustomStepSO>());
            Assert.AreEqual(NovelCompatibility.Fingerprint(legacy), NovelCompatibility.Fingerprint(withEmptyRegistry));
        }

        [Test]
        public void RegisteredCustomStepPassesStoryValidation()
        {
            var script = Create<TestFingerprintStep>();
            var errors = NarrativeStoryValidator.Validate(StepStory(script.ScriptId, Register(script)), Ready);
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void CustomStepWithoutAScriptIdIsRejected()
        {
            var errors = NarrativeStoryValidator.Validate(StepStory(null, new Dictionary<string, NovelCustomStepSO>()), Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "BadCustomStep"), string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void CustomStepWithAnUnregisteredScriptIsRejectedBeforeTheStoryStarts()
        {
            // 未登记的脚本必须在剧情校验阶段失败，而不是运行到该步骤才崩。
            var errors = NarrativeStoryValidator.Validate(StepStory("Some.Missing.Step", new Dictionary<string, NovelCustomStepSO>()), Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "BadCustomStep" && e.Message.Contains("未在本剧情登记")),
                string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void CustomStepCannotRunInAStandaloneChapter()
        {
            // 与章节出口同一口径：没有剧情清单就没有脚本解析权威，独立 Start 不支持自定义节点。
            var errors = NarrativeValidator.Validate(Chapter(Step("step", "Some.Step")), Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "MissingStory"), string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void BridgeStepRequiresARequestKeyAndAPositiveTimeout()
        {
            var bridge = Create<NovelStepServiceBridgeSO>();
            var errors = NarrativeStoryValidator.Validate(StepStory(bridge.ScriptId, Register(bridge)), Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "BadCustomStep" && e.Message.Contains("请求键")),
                string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void BridgeStepRequiresADeclaredResultVariableOfAConvertibleType()
        {
            var bridge = Create<NovelStepServiceBridgeSO>();
            var data = new SerializedObject(bridge);
            data.FindProperty("_requestKey").stringValue = "guess_number";
            data.FindProperty("_resultVariableId").stringValue = "score";
            data.ApplyModifiedPropertiesWithoutUndo();
            var story = new NovelStory("custom", 1, "chapter", new[] { Chapter(Step("step", bridge.ScriptId)) },
                new[] { new NovelVariable("score", new NovelValue(0)) }, null, Register(bridge));
            CollectionAssert.IsEmpty(NarrativeStoryValidator.Validate(story, Ready),
                "整数结果变量是支持的：模块返回的文本会按声明类型解析");
        }

        [Test]
        public void SpeakerVariableMustBeADeclaredStringVariable()
        {
            var chapter = Chapter(SpeakerLine("say", "playerName"));
            Assert.IsTrue(NarrativeValidator.Validate(chapter, Ready).Any(e => e.Code == "BadSpeakerVariable"),
                "未声明的说话人变量必须报错");
            var wrongType = new Dictionary<string, NovelValue> { ["playerName"] = new NovelValue(1) };
            Assert.IsTrue(NarrativeValidator.Validate(chapter, Ready, wrongType).Any(e => e.Code == "BadSpeakerVariable"),
                "非字符串的说话人变量必须报错");
            var declared = new Dictionary<string, NovelValue> { ["playerName"] = new NovelValue("旅人") };
            Assert.IsFalse(NarrativeValidator.Validate(chapter, Ready, declared).Any(e => e.Code == "BadSpeakerVariable"));
        }

        [Test]
        public void EditorCommandNamesStayAlignedWithTheCommandEnum()
        {
            // 内容表单按 (int)kind 直接索引这张表：长度或顺序不一致会显示错名字甚至越界。
            var type = typeof(NarrativePresetWindow).Assembly.GetType("Game.Narrative.Editor.NarrativeContentGUI");
            Assert.IsNotNull(type, "找不到编辑器内容表单类型");
            var field = type.GetField("CommandNames", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "找不到步骤名称表");
            var names = (string[])field.GetValue(null);
            Assert.AreEqual(Enum.GetValues(typeof(NovelCommandKind)).Length, names.Length,
                "步骤名称表必须与 NovelCommandKind 逐一对应");
            CollectionAssert.DoesNotContain(names, null);
            CollectionAssert.AllItemsAreUnique(names);
        }
        #endregion
    }

    /// <summary>测试用可配参数脚本：验证脚本资产内容如何进入剧情指纹。</summary>
    public sealed class TestFingerprintStep : NovelCustomStepSO
    {
        [SerializeField] private int _value;
        [SerializeField] private bool _optOut;
        public int Value { get => _value; set => _value = value; }
        public bool OptOut { get => _optOut; set => _optOut = value; }
        public override bool IncludeInFingerprint => !_optOut;
    }
}
