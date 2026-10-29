using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class TopdownExtensionAccessTests
    {
        #region 内部参数
        private string _path, _before;
        private const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        {
            _path = EmberExtensionProject.ConfigurationPath; _before = File.Exists(_path) ? File.ReadAllText(_path) : null;
            EmberExtensionProject.Save(EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint, EmberExtensionProject.LevelTrial }));
        }
        [TearDown] public void Teardown()
        {
            var restore = JsonUtility.FromJson<EmberExtensionProject.Configuration>(_before ?? File.ReadAllText(EmberExtensionProject.DefaultsPath));
            EmberExtensionProject.Save(restore);
            if (_before == null) File.Delete(_path); else File.WriteAllText(_path, _before);
            EmberConditionalMenu.Refresh();
        }
        [Test] public void DisabledExtensionRejectsPublicToolsAndDocumentsWithoutChangingWindowDraft()
        {
            var window = ScriptableObject.CreateInstance<PlayerDevelopmentWindow>();
            try
            {
                using (var data = new SerializedObject(window))
                {
                    data.FindProperty("_scheme").objectReferenceValue = Resources.Load<PlayerScheme>("Config/Player/PlayerScheme");
                    data.FindProperty("_copyFolder").stringValue = "Assets/ProtectedTopdownDraft";
                    data.FindProperty("_angle").floatValue = 123;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                string draft = EditorJsonUtility.ToJson(window);
                Assert.IsNotEmpty(EmberExtensionDocuments.Read("topdown.player.tools"));
                byte[] source = File.ReadAllBytes("Assets/Game/Documentation/topdown-2d/EditorTools.md");
                EmberExtensionProject.Save(EmberExtensionProject.CreateConfiguration(Array.Empty<string>()));
                Assert.IsFalse((bool)typeof(PlayerDevelopmentWindow).GetProperty("HasAccess", INSTANCE).GetValue(window));
                Assert.Throws<InvalidOperationException>(() => PlayerDevelopmentWindow.Open());
                Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Copy(null, "Assets/ForbiddenCopy"));
                Assert.IsFalse(EmberExtensionDocuments.Available().Any(entry => entry.id.StartsWith("topdown.", StringComparison.Ordinal)));
                Assert.Throws<InvalidOperationException>(() => EmberExtensionDocuments.Read("topdown.player.tools"));
                Assert.AreEqual(draft, EditorJsonUtility.ToJson(window));
                CollectionAssert.AreEqual(source, File.ReadAllBytes("Assets/Game/Documentation/topdown-2d/EditorTools.md"));
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
        [Test] public void OldVersionWindowCannotBeAdoptedByCurrentOpenEntrypoint()
        {
            var existing = Resources.FindObjectsOfTypeAll<PlayerDevelopmentWindow>();
            var originals = existing.ToDictionary(window => window, EditorJsonUtility.ToJson);
            var stale = ScriptableObject.CreateInstance<PlayerDevelopmentWindow>();
            stale.name = "EmberTopdownExtensionTestStale";
            try
            {
                typeof(PlayerDevelopmentWindow).GetField("_extensionScope", INSTANCE).SetValue(stale, "topdown-2d@old-version:old-hash");
                string draft = EditorJsonUtility.ToJson(stale);
                PlayerDevelopmentWindow.Open();
                Assert.IsFalse((bool)typeof(PlayerDevelopmentWindow).GetProperty("HasAccess", INSTANCE).GetValue(stale));
                Assert.AreEqual(draft, EditorJsonUtility.ToJson(stale));
                Assert.IsTrue(Resources.FindObjectsOfTypeAll<PlayerDevelopmentWindow>().Any(window => !ReferenceEquals(window, stale)
                    && (bool)typeof(PlayerDevelopmentWindow).GetProperty("HasAccess", INSTANCE).GetValue(window)));
            }
            finally
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<PlayerDevelopmentWindow>().Where(window => !ReferenceEquals(window, stale) && !existing.Contains(window)).ToArray()) window.Close();
                if (stale != null) UnityEngine.Object.DestroyImmediate(stale);
                foreach (var pair in originals) if (pair.Key != null) EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
            }
        }
        #endregion
    }
}
