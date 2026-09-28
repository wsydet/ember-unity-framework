using System;
using Ember.Basic;
using UnityEditor;

namespace Ember.UIExtension.Editor
{
    internal static class EmberLocalizationEditorBootstrap
    {
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Install;
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { EmberLocalization.Reload(); }
            catch (Exception exception) { EmberDebug.LogWarning("Localization", exception.Message); }
        }
    }
}
