using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerExtensionAccessTests
    {
        private string _path, _before;
        private const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            _path = EmberExtensionProject.ConfigurationPath; _before = File.Exists(_path) ? File.ReadAllText(_path) : null;
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = new[] { EmberExtensionProject.FsmBlueprint, EmberExtensionProject.LevelTrial } });
        }
        [TearDown] public void Teardown()
        {
            var restore = _before == null ? EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint, EmberExtensionProject.LevelTrial })
                : JsonUtility.FromJson<EmberExtensionProject.Configuration>(_before);
            EmberExtensionProject.Save(restore);
            if (_before == null) File.Delete(_path); else File.WriteAllText(_path, _before);
            EmberConditionalMenu.Refresh();
        }
        [Test] public void DisabledExtensionRejectsPublicEntrypointsAndRetainsOldWindowDraft()
        {
            var window = ScriptableObject.CreateInstance<PlayerSchemeWindow>();
            try
            {
                using (var data = new SerializedObject(window)) { data.FindProperty("_selected").objectReferenceValue = PlayerSchemeOwnership.Builtins[0]; data.ApplyModifiedPropertiesWithoutUndo(); }
                string draft = EditorJsonUtility.ToJson(window);
                EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
                Assert.IsFalse((bool)window.GetType().GetProperty("HasAccess", INSTANCE).GetValue(window));
                Assert.Throws<InvalidOperationException>(() => PlayerSchemeWindow.Open());
                Assert.Throws<InvalidOperationException>(() => PlayerMotionTraceWindow.ShowWindow());
                Assert.Throws<InvalidOperationException>(() => PlayerDevelopmentEnvironment.SetSchemeParameters(null, Vector2.zero, Vector2.zero));
                Assert.AreEqual(draft, EditorJsonUtility.ToJson(window));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
        [Test] public void FormalLevelTrialDoesNotRequireBlueprintAndOldVersionWindowIsRejected()
        {
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = new[] { EmberExtensionProject.LevelTrial } });
            var access = typeof(PlayerSchemeWindow).Assembly.GetType("Game.Player.Editor.PlayerEditorAccess");
            Assert.DoesNotThrow(() => access.GetMethod("RequireSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { true }));
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWindow.Open());
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = new[] { EmberExtensionProject.FsmBlueprint } });
            var window = ScriptableObject.CreateInstance<PlayerSchemeWindow>();
            try
            {
                window.GetType().GetField("_ownerVersion", INSTANCE).SetValue(window, "999.0.0");
                Assert.IsFalse((bool)window.GetType().GetProperty("HasAccess", INSTANCE).GetValue(window));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
        [Test] public void DisabledExtensionRemovesDocumentDiscoveryAndBlocksOldDocumentIdWithoutDeletingSource()
        {
            string path = "Assets/Game/Documentation/platformer-2d/PlayerWorkflow.md";
            byte[] before = File.ReadAllBytes(path);
            Assert.IsTrue(EmberExtensionDocuments.Available().Any(entry => entry.id == "platformer.player.workflow"));
            Assert.IsNotEmpty(EmberExtensionDocuments.Read("platformer.player.workflow"));
            EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = Array.Empty<string>() });
            Assert.IsFalse(EmberExtensionDocuments.Available().Any(entry => entry.id == "platformer.player.workflow"));
            Assert.IsFalse(Directory.Exists(".agents/skills/ember-platformer-add-state"));
            Assert.IsNotNull(EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-platformer-add-state"));
            Assert.Throws<InvalidOperationException>(() => EmberExtensionDocuments.Read("platformer.player.workflow"));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
        }

        [Test] public void OldWindowExtensionHashIsRejectedWithoutMutatingSerializedDraft()
        {
            var window = ScriptableObject.CreateInstance<PlayerSchemeWindow>();
            try
            {
                window.GetType().GetField("_extensionScope", INSTANCE).SetValue(window, "previous-extension-content");
                string before = EditorJsonUtility.ToJson(window);
                Assert.IsFalse((bool)window.GetType().GetProperty("HasAccess", INSTANCE).GetValue(window));
                Assert.AreEqual(before, EditorJsonUtility.ToJson(window));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
    }
}
