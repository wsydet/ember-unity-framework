using System;
using System.IO;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.LevelAuthoring.Editor.Tests
{
    public sealed class TopdownExtensionTrialTests
    {
        #region 内部参数
        private string _path, _before;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        { _path = EmberExtensionProject.ConfigurationPath; _before = File.Exists(_path) ? File.ReadAllText(_path) : null; }
        [TearDown] public void Teardown()
        {
            EmberExtensionProject.Save(JsonUtility.FromJson<EmberExtensionProject.Configuration>(_before ?? File.ReadAllText(EmberExtensionProject.DefaultsPath)));
            if (_before == null) File.Delete(_path); else File.WriteAllText(_path, _before);
            EmberConditionalMenu.Refresh();
        }
        [Test] public void DirectTrialEntrypointCannotBypassDisabledTrialWhileAuthoringRemainsEnabled()
        {
            EmberExtensionProject.Save(EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.LevelAuthoring }));
            Assert.IsNotNull(EmberLevelAdapters.Get("topdown.level"));
            Assert.Throws<InvalidOperationException>(() => new TopdownLevelAdapter().BeginTrial(null));
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            Assert.DoesNotThrow(() => new TopdownLevelAdapter().StopTrial());
        }
        [Test] public void FormalTrialAdapterRemainsAvailableWithoutBlueprint()
        {
            EmberExtensionProject.Save(EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.LevelTrial }));
            Assert.IsFalse(EmberFsmBlueprintAccess.IsAvailable);
            Assert.IsTrue(EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelTrial));
            Assert.IsNotNull(EmberLevelAdapters.Get("topdown.level"));
            Assert.IsNull(new TopdownLevelAdapter().UnavailableReason);
        }
        #endregion
    }
}
