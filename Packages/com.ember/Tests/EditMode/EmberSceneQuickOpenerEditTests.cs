using System;
using System.Collections.Generic;
using System.Reflection;
using Ember.Core;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ember.UI.Tests
{
    public class EmberSceneQuickOpenerEditTests
    {
        #region 内部参数

        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private string _folder;
        private string _scenePath;
        private EmberSceneMapping _mapping;
        private EmberSceneQuickOpener _window;

        #endregion

        #region 内部方法

        private static EmberSceneField AssetOnlyField(string path)
        {
            // Reproduce a loaded SO: serialized asset reference exists, nonserialized name is empty.
            object field = new EmberSceneField();
            typeof(EmberSceneField).GetField("_sceneAsset", PRIVATE_INSTANCE)
                .SetValue(field, AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            return (EmberSceneField)field;
        }

        private object Invoke(string name, params object[] arguments)
            => typeof(EmberSceneQuickOpener).GetMethod(name, PRIVATE_INSTANCE).Invoke(_window, arguments);

        private string Resolve(EmberSceneField field)
            => (string)typeof(EmberSceneQuickOpener)
                .GetMethod("ResolveScenePath", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { field });

        private T Get<T>(string name)
            => (T)typeof(EmberSceneQuickOpener).GetField(name, PRIVATE_INSTANCE).GetValue(_window);

        #endregion

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            string name = "__EmberSceneQuickOpenerTests_" + Guid.NewGuid().ToString("N");
            _folder = "Assets/" + name;
            AssetDatabase.CreateFolder("Assets", name);
            // Copy only an asset; never open or replace the user's loaded scenes.
            const string source = "Assets/Game/Scenes/FrameworkScene.unity";
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(source));
            _scenePath = _folder + "/" + name + ".unity";
            Assert.IsTrue(AssetDatabase.CopyAsset(source, _scenePath));
            _mapping = ScriptableObject.CreateInstance<EmberSceneMapping>();
            _mapping.frameworkScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);
            _mapping.entries.Add(new StateSceneEntry("MainState", AssetOnlyField(_scenePath)));
            _window = ScriptableObject.CreateInstance<EmberSceneQuickOpener>();
            Invoke("UpdateMapping", _mapping);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window) Object.DestroyImmediate(_window);
            if (_mapping) Object.DestroyImmediate(_mapping);
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void Refresh_AssetReferenceWithEmptyNameCanOpenWithoutInspectingSO()
        {
            Assert.IsFalse(_mapping.entries[0].sceneField.HasValue, "Reproduce the missing name cache.");
            Invoke("UpdateMapping", _mapping);
            Assert.AreEqual(_scenePath, Resolve(_mapping.entries[0].sceneField));
            Assert.IsTrue((bool)Invoke("CanOpen"));
            Assert.IsFalse(_mapping.entries[0].sceneField.HasValue,
                "Opening eligibility must not rely on warming the name cache.");
        }

        [Test]
        public void Resolution_UsesBoundAssetAfterRenameAndRejectsAmbiguousNames()
        {
            var field = AssetOnlyField(_scenePath);
            string originalName = System.IO.Path.GetFileNameWithoutExtension(_scenePath);
            AssetDatabase.CreateFolder(_folder, "Duplicate");
            Assert.IsTrue(AssetDatabase.CopyAsset(_scenePath, _folder + "/Duplicate/" + originalName + ".unity"));
            Assert.AreEqual(_scenePath, Resolve(field));
            Assert.IsNull(Resolve(new EmberSceneField(originalName)), "Name-only references must not pick an arbitrary duplicate.");
            Assert.IsEmpty(AssetDatabase.RenameAsset(_scenePath, "Renamed"));
            Assert.AreEqual(_folder + "/Renamed.unity", Resolve(field));
            Assert.AreEqual(_folder + "/Duplicate/" + originalName + ".unity", Resolve(new EmberSceneField(originalName)));
        }

        [Test]
        public void Refresh_ReorderPreservesSelectionAndMissingMappingClearsLists()
        {
            _mapping.entries.Add(new StateSceneEntry("GameplayState", AssetOnlyField(_scenePath)));
            _mapping.entries.Add(new StateSceneEntry("SettingsState", AssetOnlyField(_scenePath)));
            Invoke("UpdateMapping", _mapping);
            typeof(EmberSceneQuickOpener).GetField("_mainIndex", PRIVATE_INSTANCE).SetValue(_window, 1);
            Get<List<bool>>("_overlayToggles")[0] = true;
            _mapping.entries.Reverse();
            Invoke("UpdateMapping", _mapping);
            Assert.AreEqual("GameplayState", Get<List<StateSceneEntry>>("_mainStates")[Get<int>("_mainIndex")].stateName);
            Assert.IsTrue(Get<List<bool>>("_overlayToggles")[0]);
            _mapping.entries.RemoveAll(e => e.stateName == "GameplayState");
            Invoke("UpdateMapping", _mapping);
            Assert.AreEqual(0, Get<int>("_mainIndex"));
            Assert.IsTrue((bool)Invoke("CanOpen"));
            Invoke("UpdateMapping", new object[] { null });
            Assert.IsEmpty(Get<List<StateSceneEntry>>("_mainStates"));
            Assert.IsEmpty(Get<List<bool>>("_overlayToggles"));
            Assert.IsFalse((bool)Invoke("CanOpen"));
        }

        [Test]
        public void MissingSelectedOverlayBlocksOpeningBeforeSceneSwitch()
        {
            _mapping.entries.Add(new StateSceneEntry("SettingsState", EmberSceneField.None));
            Invoke("UpdateMapping", _mapping);
            Assert.IsTrue((bool)Invoke("CanOpen"));
            Get<List<bool>>("_overlayToggles")[0] = true;
            Assert.IsFalse((bool)Invoke("CanOpen"));
        }

        #endregion
    }
}
