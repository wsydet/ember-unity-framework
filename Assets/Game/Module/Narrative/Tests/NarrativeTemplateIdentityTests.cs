using System.Reflection;
using Ember.Core.Editor;
using Game.Narrative.Editor;
using Game.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeTemplateIdentityTests
    {
        #region 外部方法

        [Test]
        public void GraphUsesSharedIdentityAndKeepsItsReadCache()
        {
            NarrativeGraphModel.InvalidateTemplateCache();
            int before = NarrativeGraphModel.TemplateReadCount;
            Assert.AreEqual(EmberProjectSetup.IsTemplateActive("visual-novel"), NarrativeGraphModel.IsTemplateActive());
            NarrativeGraphModel.IsTemplateActive();
            Assert.AreEqual(before + 1, NarrativeGraphModel.TemplateReadCount);
            NarrativeGraphModel.IsTemplateActive(true);
            Assert.AreEqual(before + 2, NarrativeGraphModel.TemplateReadCount);
        }

        [Test]
        public void LayoutOpensForActiveNovelWhilePlayEntryStaysDisabledInEditMode()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            Assert.IsTrue(EmberProjectSetup.IsTemplateActive("visual-novel"));
            Assert.IsNull(PrefabStageUtility.GetCurrentPrefabStage(), "请先关闭 Prefab Mode 再运行布局回归。");
            Assert.IsTrue(NovelGameplayLayoutWindow.CanOpen());
            var launcher = typeof(NarrativeGraphModel).Assembly.GetType("Game.Narrative.Editor.NarrativeEntryLauncher");
            Assert.IsFalse((bool)launcher.GetMethod("CanStart", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null));
        }

        [TestCase("EUINovelReaderPage")]
        [TestCase("EUINovelReadingMenuPage")]
        public void LayoutRejectsConflictingPrefabMode(string prefab)
        {
            Assert.IsNull(PrefabStageUtility.GetCurrentPrefabStage(), "请先关闭 Prefab Mode 再运行布局回归。");
            Assert.IsTrue(NovelGameplayLayoutWindow.CanOpen());
            try
            {
                PrefabStageUtility.OpenPrefab("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/" + prefab + ".prefab");
                Assert.IsFalse(NovelGameplayLayoutWindow.CanOpen());
            }
            finally { StageUtility.GoToMainStage(); }
            Assert.IsTrue(NovelGameplayLayoutWindow.CanOpen());
        }

        #endregion
    }
}
