using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        [UnityTest] public IEnumerator ExtensionDisablePreservesLayoutCandidateAndSerializableDraft()
        {
            string path = EmberExtensionProject.ConfigurationPath;
            byte[] before = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var config = EmberExtensionProject.CreateConfiguration(
                EmberExtensionProject.Load().enabled.Append(EmberExtensionProject.FsmBlueprint));
            string folder = "Assets/__EmberFsmExtensionDraft_" + System.Guid.NewGuid().ToString("N");
            EmberFsmBlueprintWindow window = null;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                // Establish this test's own access instead of borrowing the host template defaults.
                File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(config));
                EmberConditionalMenu.Refresh();
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
                AssetDatabase.CreateAsset(_graph, folder + "/Graph.asset");
                foreach (var definition in _graph.Nodes.Select(n => n.Definition).Distinct()) AssetDatabase.AddObjectToAsset(definition, _graph);
                AssetDatabase.SaveAssetIfDirty(_graph);
                window = IsolatedLayoutWindow(_graph);
                yield return null; window.PreviewLayout("all");
                var candidate = typeof(EmberFsmBlueprintWindow).GetField("_layoutCandidate", flags).GetValue(window);
                Assert.IsNotNull(candidate);
                EmberExtensionProject.Save(new EmberExtensionProject.Configuration { enabled = System.Array.Empty<string>() });
                typeof(EmberFsmBlueprintWindow).GetMethod("RefreshTemplateAccess", flags).Invoke(window, null);
                Assert.AreSame(candidate, typeof(EmberFsmBlueprintWindow).GetField("_layoutCandidate", flags).GetValue(window));
                string draft = (string)typeof(EmberFsmBlueprintWindow).GetField("_extensionLayoutDraft", flags).GetValue(window);
                Assert.IsNotEmpty(draft); Assert.IsFalse(window.ApplyLayoutPreview());
                EmberExtensionProject.Save(config);
                typeof(EmberFsmBlueprintWindow).GetMethod("RefreshTemplateAccess", flags).Invoke(window, null);
                Assert.AreSame(candidate, typeof(EmberFsmBlueprintWindow).GetField("_layoutCandidate", flags).GetValue(window));
                var restored = UnityEngine.ScriptableObject.CreateInstance<EmberFsmBlueprintWindow>();
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(window), restored); restored.Show(); yield return null;
                    Assert.AreSame(_graph, restored.ParameterGraph);
                    Assert.IsNotNull(typeof(EmberFsmBlueprintWindow).GetField("_layoutCandidate", flags).GetValue(restored));
                    Assert.AreEqual(draft, (string)typeof(EmberFsmBlueprintWindow).GetField("_extensionLayoutDraft", flags).GetValue(restored));
                }
                finally { restored.Close(); }
            }
            finally
            {
                window?.Close();
                if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
                if (before == null) File.Delete(path); else File.WriteAllBytes(path, before);
                EmberConditionalMenu.Refresh();
            }
        }
    }
}
