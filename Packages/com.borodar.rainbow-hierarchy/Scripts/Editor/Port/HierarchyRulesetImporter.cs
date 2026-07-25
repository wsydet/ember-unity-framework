using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Borodar.RainbowHierarchy.HierarchyRulesetV2;
using static Borodar.RainbowHierarchy.RHLogger;

namespace Borodar.RainbowHierarchy
{
    public static class HierarchyRulesetImporter
    {
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void Import()
        {
            var path = EditorUtility.OpenFilePanel("Import Ruleset", "", EXTENSION);
            if (string.IsNullOrEmpty(path)) return;

            ImportCustomIcons(path);
            ImportRuleset(path);
        }

        public static void ImportRuleset(string path)
        {
            var wrapperJson = File.ReadAllText(path);
            var wrapper = new HierarchyRulesetWrapper();
            EditorJsonUtility.FromJsonOverwrite(wrapperJson, wrapper);

            if (wrapper.Version < VERSION)
            {
                UpdateFromOlderVersions(wrapper);
            }

            var activeScene = SceneManager.GetActiveScene();
            var activeRuleset = GetRulesetByScene(activeScene, true);

            Undo.RecordObject(activeRuleset, "Hierarchy Ruleset Import");
            EditorJsonUtility.FromJsonOverwrite(wrapper.RulesetJson, activeRuleset);

            ConvertPathsToObjectRefs(activeRuleset);

            Log("Ruleset successfully imported.");
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void UpdateFromOlderVersions(HierarchyRulesetWrapper wrapper)
        {
            if (wrapper.Version <= 1)
            {
                // Convert all icon IDs to xxx00 (e.g 5432 -> 543200)
                wrapper.RulesetJson = Regex.Replace(
                    wrapper.RulesetJson,
                    "\"IconType\":[1-9]\\d*",
                    match => $"{match}00");

                // Convert all background IDs to xxx00 (e.g 5432 -> 543200)
                wrapper.RulesetJson = Regex.Replace(
                    wrapper.RulesetJson,
                    "\"BackgroundType\":[1-9]\\d*",
                    match => $"{match}00");

                // Replace particular values
                wrapper.RulesetJson = wrapper.RulesetJson
                    // General
                    .Replace("\"IconType\":100000,", "\"IconType\":125650,") // CmpAssemblyDefinitionAsset
                    .Replace("\"IconType\":101000,", "\"IconType\":125250,") // CmpCsScript
                    .Replace("\"IconType\":102000,", "\"IconType\":125000,") // CmpDefaultAsset
                    .Replace("\"IconType\":111000,", "\"IconType\":125750,") // CmpShader
                    .Replace("\"IconType\":112000,", "\"IconType\":125150,") // CmpTextAsset
                    .Replace("\"IconType\":115000,", "\"IconType\":125550,") // CmpVisualTreeAsset
                    // Miscellaneous
                    .Replace("\"IconType\":181000,", "\"IconType\":117000,") // Animation
                    .Replace("\"IconType\":182000,", "\"IconType\":117350,") // Animator
                    // Rendering
                    .Replace("\"IconType\":293000,", "\"IconType\":169500,") // Light
                    .Replace("\"IconType\":294000,", "\"IconType\":169550,") // LightProbeGroup
                    .Replace("\"IconType\":295000,", "\"IconType\":169560,") // LightProbeProxyVolume
                    ;
            }
        }

        private static void ConvertPathsToObjectRefs(HierarchyRulesetV2 ruleset)
        {
            for (var i = ruleset.Rules.Count - 1; i >= 0; i--)
            {
                var rule = ruleset.Rules[i];
                if (rule.Type != HierarchyRule.KeyType.Object) continue;

                if (string.IsNullOrEmpty(rule.Name))
                {
                    ruleset.Rules.RemoveAt(i);
                    continue;
                }

                var gameObject = GameObject.Find(rule.Name);
                if (gameObject != null)
                {
                    // Replace
                    rule.GameObject = gameObject;
                    rule.Name = string.Empty;
                }
                else
                {
                    // Delete
                    ruleset.Rules.RemoveAt(i);
                }
            }
        }

        private static void ImportCustomIcons(string path)
        {
            var importDir = Path.GetDirectoryName(path);
            var importFileName = Path.GetFileNameWithoutExtension(path);
            var packagePath = $"{importDir}/{importFileName}.unitypackage";

            if (!File.Exists(packagePath))
            {
                Log("There is no package with custom icons. Skipping package import.");
                return;
            }

            AssetDatabase.ImportPackage(packagePath, true);
        }
    }
}