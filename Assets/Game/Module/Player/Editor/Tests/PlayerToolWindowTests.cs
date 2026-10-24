using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerToolWindowTests
    {
        [Test]
        public void LegacyEntrypointsShareOneWindowAndKeepSelectionAcrossTabs()
        {
            var existing = Resources.FindObjectsOfTypeAll<PlayerSchemeWindow>().FirstOrDefault();
            string before = existing != null ? EditorJsonUtility.ToJson(existing) : null;
            var scheme = PlayerSchemeOwnership.Builtins[0];
            try
            {
                PlayerSchemeWindow.Open(scheme);
                var window = Resources.FindObjectsOfTypeAll<PlayerSchemeWindow>().Single();
                PlayerFeelLabWindow.ShowWindow();
                using (var data = new SerializedObject(window))
                {
                    Assert.That(data.FindProperty("_tab").intValue, Is.Zero);
                    Assert.That(data.FindProperty("_selected").objectReferenceValue, Is.SameAs(scheme));
                }
                PlayerAppearanceWindow.Open();
                using (var data = new SerializedObject(window))
                {
                    Assert.That(data.FindProperty("_tab").intValue, Is.EqualTo(1));
                    Assert.That(data.FindProperty("_selected").objectReferenceValue, Is.SameAs(scheme));
                }
                Assert.That(Resources.FindObjectsOfTypeAll<PlayerSchemeWindow>().Length, Is.EqualTo(1));
                Assert.That(Resources.FindObjectsOfTypeAll<PlayerFeelLabWindow>(), Is.Empty);
                Assert.That(Resources.FindObjectsOfTypeAll<PlayerAppearanceWindow>(), Is.Empty);
            }
            finally
            {
                var window = Resources.FindObjectsOfTypeAll<PlayerSchemeWindow>().FirstOrDefault();
                if (window != null) { if (before != null) EditorJsonUtility.FromJsonOverwrite(before, window); else window.Close(); }
            }
        }
    }
}
