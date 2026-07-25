using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [CustomEditor(typeof(HierarchyRuleset), true)]
    public class HierarchyRulesetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox
            (
                "This ruleset created from the older Rainbow Hierarchy version. Do you want to update it now?",
                MessageType.Warning
            );

            if (GUILayout.Button("Update Ruleset"))
            {
                HierarchyEditorProxy.UpdateOldRuleset?.Invoke(((HierarchyRuleset) target).gameObject);
            }
        }
    }
}