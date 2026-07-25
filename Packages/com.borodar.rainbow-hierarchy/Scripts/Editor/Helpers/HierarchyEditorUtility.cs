using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingExpression")]
    public static class HierarchyEditorUtility
    {
        private static Texture2D _editIcon;
        private static Texture2D _settingsIcon;
        private static Texture2D _filterIcon;
        private static Texture2D _deleteIcon;
        
        private static Texture2D _foldoutFirstIcon;
        private static Texture2D _foldoutMiddleIcon;
        private static Texture2D _foldoutLastIcon;
        private static Texture2D _foldoutLevelsIcon;

        private static string _projectName;
        private static string _projectPath;
        private static string _tempDir;

        //---------------------------------------------------------------------
        // Project
        //---------------------------------------------------------------------

        [SuppressMessage("ReSharper", "InvertIf")]
        public static string ProjectName
        {
            get
            {
                if (_projectName == null)
                {
                    var path = Application.dataPath.Split('/');
                    _projectName = path[path.Length - 2];
                }

                return _projectName;
            }
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        public static string ProjectPath
        {
            get
            {
                if (_projectPath == null)
                {
                    _projectPath = Path.GetDirectoryName(Application.dataPath);
                }

                return _projectPath;
            }
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        public static string TempDir
        {
            get
            {
                if (_tempDir == null)
                {
                    _tempDir = $"{ProjectPath}/Temp";
                }

                return _tempDir;
            }
        }

        //---------------------------------------------------------------------
        // Windows
        //---------------------------------------------------------------------

        public static IEnumerable<EditorWindow> GetAllWindowsByType(string type)
        {
            var objectList = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            var windows = from obj in objectList where obj.GetType().ToString() == type select (EditorWindow) obj;
            return windows;
        }

        //---------------------------------------------------------------------
        // Scene
        //---------------------------------------------------------------------

        public static void UpdateSceneConfigVisibility(bool isVisible)
        {
            var rulesetInstances = HierarchyRulesetV2.Instances;
            if (rulesetInstances == null || rulesetInstances.Count < 1) return;

            foreach (var ruleset in rulesetInstances)
            {
                if (ruleset == null) return;
                ruleset.transform.hideFlags = isVisible
                    ? HideFlags.None
                    : HideFlags.HideInHierarchy;
            }

            EditorApplication.DirtyHierarchyWindowSorting();
        }

        public static string GetTransformPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return $"/{path}";
        }

        //---------------------------------------------------------------------
        // GUI
        //---------------------------------------------------------------------

        public static bool SearchField(ref string query, ref Enum filter, Enum defaultFilter, params GUILayoutOption[] options)
        {
            var queryBefore = query;
            var filterBefore = filter;
            var changed = false;

            GUILayout.BeginHorizontal();

            var queryRect = GUILayoutUtility.GetRect(GUIContent.none, "ToolbarSearchTextFieldPopup", options);
            queryRect.x += 2f;
            queryRect.width -= 13f;

            var filterRect = queryRect;
            filterRect.width = 20f;

            filter = EditorGUI.EnumPopup(filterRect, filter, "label");
            if (!Equals(filter, filterBefore)) changed = true;

            query = EditorGUI.TextField(queryRect, GUIContent.none, query, "ToolbarSearchTextField");
            if (query != null && !query.Equals(queryBefore)) changed = true;

            var cancelRect = queryRect;
            cancelRect.x += queryRect.width;
            cancelRect.width = 12f;
            if (GUI.Button(cancelRect, GUIContent.none, "ToolbarSearchCancelButton"))
            {
                query = string.Empty;
                filter = defaultFilter;
                changed = true;
                // workaround for bug with selected text
                GUIUtility.keyboardControl = 0;
            }

            GUILayout.EndHorizontal();

            return changed;
        }
        
        //---------------------------------------------------------------------
        // Icons
        //---------------------------------------------------------------------
        
        public static Texture2D GetEditIcon()
        {
            return GetTexture(ref _editIcon, HierarchyEditorTexture.IcnEdit);
        }

        public static Texture2D GetSettingsButtonIcon()
        {
            return GetTexture(ref _settingsIcon, HierarchyEditorTexture.IcnSettings);
        }

        public static Texture2D GetFilterButtonIcon()
        {
            return GetTexture(ref _filterIcon, HierarchyEditorTexture.IcnFilter);
        }
        
        public static Texture2D GetDeleteButtonIcon()
        {
            return GetTexture(ref _deleteIcon, HierarchyEditorTexture.IcnDelete);
        }

        public static Texture2D GetFoldoutIcon(SiblingIndex index)
        {
            switch (index)
            {
                case SiblingIndex.First:
                    return GetTexture(ref _foldoutFirstIcon, HierarchyEditorTexture.IcnFoldoutFirst);
                case SiblingIndex.Middle:
                    return GetTexture(ref _foldoutMiddleIcon, HierarchyEditorTexture.IcnFoldoutMiddle);
                case SiblingIndex.Last:
                    return GetTexture(ref _foldoutLastIcon, HierarchyEditorTexture.IcnFoldoutLast);
                default:
                    throw new ArgumentOutOfRangeException("index", index, null);
            }
        }

        public static Texture2D GetFoldoutLevelsIcon()
        {
            return GetTexture(ref _foldoutLevelsIcon, HierarchyEditorTexture.IcnFoldoutLevels);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static Texture2D GetTexture(ref Texture2D texture, HierarchyEditorTexture type)
        {
            if (texture == null)
                texture = HierarchyEditorTexturesStorage.GetTexture(type);

            return texture;
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        public enum SiblingIndex
        {
            First,
            Middle,
            Last
        }

    }
}