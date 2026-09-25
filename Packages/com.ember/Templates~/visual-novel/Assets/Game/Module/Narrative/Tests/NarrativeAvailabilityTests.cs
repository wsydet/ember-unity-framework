using System;
using System.Collections;
using System.Reflection;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeAvailabilityTests
    {
        private const BindingFlags FLAGS = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Type Availability = typeof(NarrativeGraphWindow).Assembly.GetType("Game.Narrative.Editor.NarrativeEditorAvailability");
        private static readonly string[] Paths =
        {
            "Ember/视觉小说/从所选入口开始新游戏（Play 主菜单）",
            "Ember/视觉小说/流程编辑与运行观察",
            "Ember/视觉小说/Gameplay 主UI布局",
            "Assets/Create/Game/Visual Novel/剧情", "Assets/Create/Game/Visual Novel/章节",
            "Assets/Create/Game/Visual Novel/对话段", "Assets/Create/Game/Visual Novel/选择",
            "Assets/Create/Game/Visual Novel/条件分流", "Assets/Create/Game/Visual Novel/章节出口",
            "Assets/Create/Game/Visual Novel/结局"
        };
        private object _metadata;
        private bool _enabled;

        [SetUp]
        public void SetUp()
        {
            _metadata = Availability.GetField("Module", FLAGS).GetValue(null);
            _enabled = (bool)_metadata.GetType().GetProperty("Enabled").GetValue(_metadata);
        }
        private void SetEnabled(bool enabled)
        {
            // 在测试内改变本域读取的元数据副本；不修改模块源码或模板状态。
            _metadata.GetType().GetProperty("Enabled").SetValue(_metadata, enabled);
            Availability.GetMethod("RefreshMenus", FLAGS).Invoke(null, null);
        }
        [TearDown]
        public void TearDown() => SetEnabled(_enabled);

        [Test]
        public void MenusDisappearAndReturnWithModuleMetadata()
        {
            var exists = typeof(Menu).GetMethod("MenuItemExists", FLAGS | BindingFlags.Public);
            foreach (bool enabled in new[] { true, false, true })
            {
                SetEnabled(enabled);
                foreach (var path in Paths) Assert.AreEqual(enabled, exists.Invoke(null, new object[] { path }), path);
            }
        }

        [Test]
        public void DisabledModuleRejectsOpenAndAssetWrites()
        {
            SetEnabled(true);
            NarrativeGraphWindow.Open();
            Assert.IsNotEmpty(Resources.FindObjectsOfTypeAll<NarrativeGraphWindow>());
            SetEnabled(false);
            var story = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset");
            Assert.IsNotNull(story);
            var before = EditorJsonUtility.ToJson(story);
            NarrativeGraphWindow.Open(); NarrativeGraphWindow.OpenFor(story);
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll<NarrativeGraphWindow>());
            Assert.IsFalse((bool)typeof(NarrativeGraphWindow).GetMethod("OnOpenAsset", FLAGS).Invoke(null, new object[] { EntityId.None, 0 }));
            Assert.IsFalse(NarrativeGraphModel.CanEdit(story));
            Assert.Throws<InvalidOperationException>(() => NarrativeStoryModel.CreateStory("Assets/DisabledNarrative.asset"));
            Assert.Throws<InvalidOperationException>(() => NarrativeStoryModel.CreateChapter(story, "Disabled"));
            Assert.Throws<InvalidOperationException>(() => NarrativeStoryModel.ApplyOrganization(Array.Empty<(string, string)>()));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(story));
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath("Assets/DisabledNarrative.asset"));
        }

        [UnityTest]
        public IEnumerator RestoredWindowClosesWithoutBuildingGraphWhenDisabled()
        {
            SetEnabled(false);
            var window = EditorWindow.GetWindow<NarrativeGraphWindow>();
            try
            {
                Assert.IsNull(window.Graph);
                Assert.IsNull(window.StoryGraph);
                double deadline = EditorApplication.timeSinceStartup + 2;
                while (window && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.IsTrue(!window, "禁用模块恢复出的窗口应在下一次 Editor update 关闭");
                Assert.IsEmpty(Resources.FindObjectsOfTypeAll<NarrativeGraphWindow>());
            }
            finally { if (window) window.Close(); }
        }
    }
}
