using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Borodar.RainbowHierarchy.HierarchyRulesetV2;
using static Borodar.RainbowHierarchy.RHLogger;

namespace Borodar.RainbowHierarchy
{
    [InitializeOnLoad]
    public static class EditorProxyInitializer
    {
        static EditorProxyInitializer()
        {
            HierarchyEditorProxy.UpdateOldRuleset = (gameObject) =>
            {
                var dialogResult = EditorUtility.DisplayDialog
                (
                    "Rainbow Hierarchy Update",
                    "This scene contains a ruleset from the older Rainbow Hierarchy version. Do you want to update it now?",
                    "Yes",
                    "No"
                );

                if (!dialogResult) return;

                var scene = gameObject.scene;
                var sceneName = scene.name;
                var filePath = $"{HierarchyEditorUtility.TempDir}/{sceneName}.{EXTENSION}";

                // Export old ruleset and override version
                if (gameObject.TryGetComponent<HierarchyRuleset>(out var rulesetV1))
                {
                    HierarchyRulesetExporter.ExportRulesetV1(rulesetV1, filePath);
                    Object.DestroyImmediate(rulesetV1);
                }
                else
                {
                    LogError($"Cannot find ruleset component on: {gameObject.name}");
                    return;
                }

                gameObject.AddComponent<HierarchyRulesetV2>();

                // Import ruleset data (all upgrade steps will be done automatically)
                HierarchyRulesetImporter.ImportRuleset(filePath);
                EditorSceneManager.SaveScene(scene);
                File.Delete(filePath);

                Log($"Ruleset successfully updated to latest version: {sceneName}");
            };
        }
    }
}