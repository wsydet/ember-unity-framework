using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using static Borodar.RainbowHierarchy.HierarchyEditorUtility;

namespace Borodar.RainbowHierarchy
{
    public static class HierarchyPreferences
    {
        private const float PREF_LABEL_WIDTH = 150f;

        private const string EDIT_MODIFIER_PKEY = "Borodar.RainbowHierarchy.EditMod.";
        private const string EDIT_MODIFIER_HINT = "Modifier key that is used to show configuration dialogue when clicking on an object in the hierarchy.";
        private const EventModifiers EDIT_MODIFIER_DEFAULT = EventModifiers.Alt;

        private const string DRAG_MODIFIER_PKEY = "Borodar.RainbowHierarchy.DragMod.";
        private const string DRAG_MODIFIER_HINT = "Modifier key that is used to drag configuration dialogue.";
        private const EventModifiers DRAG_MODIFIER_DEFAULT = EventModifiers.Shift;

        private const string HIDE_CONFIG_PKEY = "Borodar.RainbowHierarchy.HideConfig.";
        private const string HIDE_CONFIG_HINT = "Change this setting to show/hide the RainbowHierarchyConf object in the hierarchy window.";
        private const bool HIDE_CONFIG_DEFAULT = false;

        private const string HIERARCHY_TREE_PKEY = "Borodar.RainbowHierarchy.ShowHierarchyTree.";
        private const string HIERARCHY_TREE_HINT = "Change this setting to show/hide the \"branches\" in the hierarchy window.";
        private const bool HIERARCHY_TREE_DEFAULT = true;

        private const string ROW_SHADING_PKEY = "Borodar.RainbowHierarchy.RowShading.";
        private const string ROW_SHADING_HINT = "Change this setting to enable/disable row shading in the hierarchy window.";
        private const bool ROW_SHADING_DEFAULT = true;

        private static readonly EditorPrefsModifierKey EDIT_MODIFIER_PREF;
        private static readonly EditorPrefsModifierKey DRAG_MODIFIER_PREF;
        private static readonly EditorPrefsBoolean HIDE_CONFIG_PREF;

        private static readonly EditorPrefsBoolean HIERARCHY_TREE_PREF;
        private static readonly EditorPrefsBoolean ROW_SHADING_PREF;

        private static EventModifiers _editModifier;
        private static EventModifiers _dragModifier;
        public static bool HideConfig;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public static bool ShowHierarchyTree { get; private set; }
        public static bool DrawRowShading { get; private set; }

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static HierarchyPreferences()
        {
            // General

            var editModifierLabel = new GUIContent("Edit Modifier", EDIT_MODIFIER_HINT);
            EDIT_MODIFIER_PREF = new EditorPrefsModifierKey(EDIT_MODIFIER_PKEY + ProjectName, EDIT_MODIFIER_DEFAULT, editModifierLabel);
            EDIT_MODIFIER_PREF.Changed += OnEditModifierChanged;
            _editModifier = EDIT_MODIFIER_PREF.Value;

            var dragModifierLabel = new GUIContent("Drag Modifier", DRAG_MODIFIER_HINT);
            DRAG_MODIFIER_PREF = new EditorPrefsModifierKey(DRAG_MODIFIER_PKEY + ProjectName, DRAG_MODIFIER_DEFAULT, dragModifierLabel);
            DRAG_MODIFIER_PREF.Changed += OnDragModifierChanged;
            _dragModifier = DRAG_MODIFIER_PREF.Value;

            var hideConfigLabel = new GUIContent("Hide Config", HIDE_CONFIG_HINT);
            HIDE_CONFIG_PREF = new EditorPrefsBoolean(HIDE_CONFIG_PKEY + ProjectName, HIDE_CONFIG_DEFAULT, hideConfigLabel, PREF_LABEL_WIDTH);
            HIDE_CONFIG_PREF.Changed += OnHideConfigChanged;
            HideConfig = HIDE_CONFIG_PREF.Value;

            // Enhancements

            var hierarchyTreeLabel = new GUIContent("Hierarchy Tree", HIERARCHY_TREE_HINT);
            HIERARCHY_TREE_PREF = new EditorPrefsBoolean(HIERARCHY_TREE_PKEY + ProjectName, HIERARCHY_TREE_DEFAULT, hierarchyTreeLabel, PREF_LABEL_WIDTH);
            HIERARCHY_TREE_PREF.Changed += OnHierarchyTreeChanged;
            ShowHierarchyTree = HIERARCHY_TREE_PREF.Value;

            var rowShadingLabel = new GUIContent("Row Shading", ROW_SHADING_HINT);
            ROW_SHADING_PREF = new EditorPrefsBoolean(ROW_SHADING_PKEY + ProjectName, ROW_SHADING_DEFAULT, rowShadingLabel, PREF_LABEL_WIDTH);
            ROW_SHADING_PREF.Changed += OnRowShadingChanged;
            DrawRowShading = ROW_SHADING_PREF.Value;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        [SettingsProvider]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static SettingsProvider CreateSettingProvider()
        {
            return new SettingsProvider("Borodar/RainbowHierarchy", SettingsScope.Project)
            {
                label = AssetInfo.NAME,
                guiHandler = OnGUI
            };
        }

        public static bool IsEditModifierPressed(Event e)
        {
            return (e.modifiers & _editModifier) == _editModifier;
        }

        public static bool IsDragModifierPressed(Event e)
        {
            return (e.modifiers & _dragModifier) == _dragModifier;
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        private static void OnGUI(string searchContext)
        {
            DrawGeneralSection();
            DrawEnhancementsSection();
            DrawImportExportSection();

            DrawVersionAtBottom();
        }

        //---------------------------------------------------------------------
        // OnChange
        //---------------------------------------------------------------------

        private static void OnEditModifierChanged(EventModifiers value)
        {
            _editModifier = value;
        }

        private static void OnDragModifierChanged(EventModifiers value)
        {
            _dragModifier = value;
        }

        private static void OnHideConfigChanged(bool value)
        {
            HideConfig = value;
            UpdateSceneConfigVisibility(!value);
        }

        private static void OnHierarchyTreeChanged(bool value)
        {
            ShowHierarchyTree = value;
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnRowShadingChanged(bool value)
        {
            DrawRowShading = value;
            EditorApplication.RepaintHierarchyWindow();
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void DrawGeneralSection()
        {
            DrawHeader("General");

            EDIT_MODIFIER_PREF.Draw();
            DRAG_MODIFIER_PREF.Draw();
            TinySeparator();
            HIDE_CONFIG_PREF.Draw();
        }

        private static void DrawEnhancementsSection()
        {
            DrawHeader("Enhancements");

            HIERARCHY_TREE_PREF.Draw();
            TinySeparator();
            ROW_SHADING_PREF.Draw();
        }

        private static void DrawImportExportSection()
        {
            DrawHeader("Import / Export Ruleset (Current Scene)");

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Import")) HierarchyRulesetImporter.Import();;
                if (GUILayout.Button("Export")) HierarchyRulesetExporter.Export();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawVersionAtBottom()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Version " + AssetInfo.VERSION, EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawHeader(string label)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.Separator();
        }

        private static void TinySeparator()
        {
            GUILayoutUtility.GetRect(0f, 0f);
        }
    }
}