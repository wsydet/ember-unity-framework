using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Borodar.RainbowHierarchy.HierarchyRulesetV2;
using static Borodar.RainbowHierarchy.RHLogger;

namespace Borodar.RainbowHierarchy
{
    public static class HierarchyRulesetExporter
    {
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void Export()
        {
            var dirPath = EditorUtility.SaveFolderPanel("Export Rulesets", "", "");
            if (string.IsNullOrEmpty(dirPath)) return;

            ExportRuleset(dirPath);
            ExportCustomIcons(dirPath);
        }

        public static void ExportRulesetV1(HierarchyRuleset ruleset, string filePath)
        {
            ConvertObjectRefsToPathsV1(ruleset);

            var rulesetJson = EditorJsonUtility.ToJson(ruleset);
            var wrapper = new HierarchyRulesetWrapper {
                Version = HierarchyRuleset.VERSION,
                RulesetJson = rulesetJson
            };
            var wrapperJson = EditorJsonUtility.ToJson(wrapper, true);

            File.WriteAllText(filePath, wrapperJson);

            Log($"Ruleset successfully exported:\n{filePath}");
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void ExportRuleset(string dirPath)
        {
            foreach (var ruleset in Instances)
            {
                ConvertObjectRefsToPaths(ruleset);

                var rulesetJson = EditorJsonUtility.ToJson(ruleset);
                var wrapper = new HierarchyRulesetWrapper { RulesetJson = rulesetJson };
                var wrapperJson = EditorJsonUtility.ToJson(wrapper, true);

                var sceneName = ruleset.gameObject.scene.name;
                var filePath = $"{dirPath}/{sceneName}.{EXTENSION}";

                File.WriteAllText(filePath, wrapperJson);

                Log($"Ruleset successfully exported:\n{filePath}");
            }
        }

        private static void ConvertObjectRefsToPaths(HierarchyRulesetV2 ruleset)
        {
            foreach (var rule in ruleset.Rules)
            {
                if (rule.Type != HierarchyRule.KeyType.Object) continue;
                if (rule.GameObject == null) continue;

                var transform = rule.GameObject.transform;
                var path = HierarchyEditorUtility.GetTransformPath(transform);

                rule.Name = path;
            }
        }

        private static void ConvertObjectRefsToPathsV1(HierarchyRuleset ruleset)
        {
            foreach (var rule in ruleset.Rules)
            {
                if (rule.Type != HierarchyRule.KeyType.Object) continue;
                if (rule.GameObject == null) continue;

                var transform = rule.GameObject.transform;
                var path = HierarchyEditorUtility.GetTransformPath(transform);

                rule.Name = path;
            }
        }

        private static void ExportCustomIcons(string dirPath)
        {
            foreach (var ruleset in Instances)
            {
                var roots = new Object[] { ruleset };
                var allDependencies = EditorUtility.CollectDependencies(roots);

                var textureDependencies = (from dependency in allDependencies
                    where dependency is Texture2D
                    select AssetDatabase.GetAssetPath(dependency))
                    .ToArray();

                var sceneName = ruleset.gameObject.scene.name;

                if (!textureDependencies.Any())
                {
                    Log($"There is no custom icons in ruleset: {sceneName}.\nSkipping package export.");
                    continue;
                }

                var packagePath = $"{dirPath}/{sceneName}.unitypackage";
                AssetDatabase.ExportPackage(textureDependencies, packagePath);

                Log($"Package with custom icons successfully exported:\n{packagePath}");
            }
        }
    }
}