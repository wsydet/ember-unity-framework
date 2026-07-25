using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Borodar.RainbowHierarchy.RList;
using UnityEditor;
using UnityEngine;
using static Borodar.RainbowHierarchy.HierarchyRule;
using static UnityEngine.SendMessageOptions;

namespace Borodar.RainbowHierarchy
{
    [CustomEditor(typeof(HierarchyRulesetV2), true)]
    public class HierarchyRulesetEditorV2 : Editor
    {
        private const string SEARCH_RESULTS_TITLE = "Search Results";
        private const string PROP_NAME_ITEMS = "Rules";
        private const string NEGATIVE_LOOKAHEAD = "(?!.*)"; // Regex that matches nothing

        private static readonly Dictionary<HierarchyRulesetV2, List<HierarchyRulesetEditorV2>> EDITORS_BY_CONF =
            new Dictionary<HierarchyRulesetV2, List<HierarchyRulesetEditorV2>>();

        private SerializedProperty _itemsProperty;
        private ReorderableList _reorderableList;

        private string _query = string.Empty;
        private Enum _filter = Filter.All;
        private bool _matchCase;
        private bool _useRegex;

        private string _warningMessage;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public bool ForceUpdate { get; set; }

        public int SearchTab { get; set; }

        public GameObject GameObject { get; set; }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        protected void OnEnable()
        {
            var ruleset = (HierarchyRulesetV2) target;
            if (EDITORS_BY_CONF.TryGetValue(ruleset, out var editors))
            {
                editors.Add(this);
            }
            else
            {
                var list = new List<HierarchyRulesetEditorV2> {this};
                EDITORS_BY_CONF.Add(ruleset, list);
            }

            _itemsProperty = serializedObject.FindProperty(PROP_NAME_ITEMS);

            _reorderableList = new ReorderableList(_itemsProperty)
            {
                label = new GUIContent(string.Empty),
                elementDisplayType = ReorderableList.ElementDisplayType.SingleLine,
                headerHeight = 0f,
                expandable = false,
                paginate = true,
                pageSize = 10,
            };

            _reorderableList.onChangedCallback += (list) => OnRulesetChange();

            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= OnRulesetChange;
            Undo.undoRedoPerformed += OnRulesetChange;
        }

        protected void OnDisable()
        {
            var ruleset = (HierarchyRulesetV2) target;
            if (EDITORS_BY_CONF.TryGetValue(ruleset, out var editors))
            {
                editors.Remove(this);
                if (editors.Count == 0) EDITORS_BY_CONF.Remove(ruleset);
            }

            ClearHiddenFlags();
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= OnRulesetChange;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(4f);

            var searchTabBefore = SearchTab;
            SearchTab = GUILayout.Toolbar(SearchTab, new [] {"Filter by object", "Filter by name"});
            ForceUpdate |= SearchTab != searchTabBefore;

            EditorGUILayout.BeginVertical("AvatarMappingBox");
            {
                GUILayout.Space(6f);

                switch (SearchTab)
                {
                    case 0: // Object
                    {
                        DrawSearchByFolderPanel(ForceUpdate);
                        break;
                    }
                    case 1: // Name
                    {
                        DrawSearchByKeyPanel(ForceUpdate);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SearchTab), SearchTab, null);
                }

                if (!string.IsNullOrEmpty(_warningMessage))
                {
                    EditorGUILayout.HelpBox(_warningMessage, MessageType.Warning);
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(2f);

            serializedObject.Update();
            DrawHierarchyList();
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static void ShowInspector(HierarchyRulesetV2 ruleset, GameObject currentObject = null)
        {
            Selection.activeObject = ruleset;

            // Workaround with double delay to make sure all Inspectors already enabled
            EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
            {
                if (!EDITORS_BY_CONF.TryGetValue(ruleset, out var editors)) return;

                foreach (var editor in editors)
                {
                    editor.GameObject = currentObject;
                    editor.ForceUpdate = true;
                    editor.SearchTab = 0;
                    editor.Repaint();
                }
            };
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void DrawSearchByFolderPanel(bool forceUpdate)
        {
            var assetBefore = GameObject;
            GameObject = (GameObject) EditorGUILayout.ObjectField(GameObject, typeof(GameObject), true);

            if (!forceUpdate && GameObject == assetBefore) return;

            if (GameObject == null)
            {
                ClearHiddenFlags();
            }
            else
            {
                ApplyHiddenFlagsByAsset();
            }
        }

        private void DrawSearchByKeyPanel(bool forceUpdate)
        {
            EditorGUILayout.BeginHorizontal();
            var queryChanged = HierarchyEditorUtility.SearchField(ref _query, ref _filter, Filter.All);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (!Equals(_filter, Filter.All))
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, "MiniLabel");
                rect.x += 2f;
                rect.y += 1f;
                rect.width = 55f;
                GUI.Label(rect, $"➔ {_filter}", "MiniLabel");
            }

            GUILayout.FlexibleSpace();

            var matchCaseBefore = _matchCase;
            _matchCase = EditorGUILayout.ToggleLeft("Match case", _matchCase, "MiniLabel", GUILayout.Width(83f));
            var matchCaseChanged = _matchCase != matchCaseBefore;

            var useRegexBefore = _useRegex;
            _useRegex = EditorGUILayout.ToggleLeft("Regex", _useRegex, "MiniLabel", GUILayout.Width(58f));
            var useRegexChanged = _useRegex != useRegexBefore;

            EditorGUILayout.EndHorizontal();

            if (!forceUpdate && !queryChanged && !matchCaseChanged && !useRegexChanged) return;

            _warningMessage = string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var isDefaultFilter = Equals(Filter.All, _filter);
            if (string.IsNullOrEmpty(_query) && isDefaultFilter)
            {
                ClearHiddenFlags();
            }
            else
            {
                ApplyHiddenFlagsByKey();
            }
        }

        private void ClearHiddenFlags()
        {
            if (_itemsProperty == null) return;

            for (var i = 0; i < _itemsProperty.arraySize; i++)
            {
                var item = _itemsProperty.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("IsHidden").boolValue = false;
            }

            serializedObject.ApplyModifiedProperties();

            _reorderableList.canAdd = true;
            _reorderableList.headerHeight = 0f;
            _reorderableList.label.text = string.Empty;
            _reorderableList.paginate = true;
        }

        private void ApplyHiddenFlagsByAsset()
        {
            foreach (var rule in ((HierarchyRulesetV2) target).Rules)
            {
                var match = false;
                switch (rule.Type)
                {
                    case KeyType.Object:
                        match |= rule.GameObject == GameObject;
                        match |= rule.IsRecursive() && GameObject.transform.IsChildOf(rule.GameObject.transform);
                        break;
                    case KeyType.Name:
                        match |= rule.Name == GameObject.name;
                        if (rule.IsRecursive())
                        {
                            var parent = GameObject.transform.parent;
                            while (parent != null)
                            {
                                match |= rule.Name == parent.name;
                                if (match) break;
                                parent = parent.parent;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                rule.IsHidden = !match;
            }

            _reorderableList.canAdd = false;
            _reorderableList.headerHeight = 18f;
            _reorderableList.label.text = SEARCH_RESULTS_TITLE;
            _reorderableList.paginate = false;
        }

        private void ApplyHiddenFlagsByKey()
        {
            var regex = (_useRegex) ? MakeRegexFromQuery() : null;

            for (var i = 0; i < _itemsProperty.arraySize; i++)
            {
                var item = _itemsProperty.GetArrayElementAtIndex(i);
                var isHidden = item.FindPropertyRelative("IsHidden");

                switch (_filter)
                {
                    case Filter.All:
                        var isObject = KeyHasSameType(item, KeyType.Object);
                        isHidden.boolValue = !NameContainsQuery(item, regex, isObject);
                        break;
                    case Filter.Object:
                        isHidden.boolValue = !KeyHasSameType(item, KeyType.Object) || !NameContainsQuery(item, regex, true);
                        break;
                    case Filter.Name:
                        isHidden.boolValue = !KeyHasSameType(item, KeyType.Name) || !NameContainsQuery(item, regex, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_filter), _filter, null);
                }
            }

            _itemsProperty.serializedObject.ApplyModifiedProperties();

            _reorderableList.canAdd = false;
            _reorderableList.headerHeight = 18f;
            _reorderableList.label = new GUIContent(SEARCH_RESULTS_TITLE);
            _reorderableList.paginate = false;
        }

        private Regex MakeRegexFromQuery()
        {
            var options = _matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            try
            {
                return new Regex(_query, options);
            }
            catch (ArgumentException ex)
            {
                _warningMessage = ex.Message;
                return new Regex(NEGATIVE_LOOKAHEAD);
            }
        }

        private static bool KeyHasSameType(SerializedProperty item, KeyType keyType)
        {
            var propType = item.FindPropertyRelative("Type").enumValueIndex;
            return propType == (int) keyType;
        }

        private bool NameContainsQuery(SerializedProperty item, Regex regex, bool isObject)
        {
            var key = KeyToString(item, isObject);

            // Regex search
            if (_useRegex)
            {
                var match = regex.Match(key);
                return match.Success;
            }

            // Simple search
            var comparison = _matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return key.IndexOf(_query, comparison) >= 0;
        }

        private string KeyToString(SerializedProperty item, bool isObject)
        {
            if (isObject)
            {
                var reference = item.FindPropertyRelative("GameObject").objectReferenceValue;
                return reference != null ? reference.name : string.Empty;
            }
            else
            {
                return item.FindPropertyRelative("Name").stringValue;
            }
        }

        private void DrawHierarchyList()
        {
            EditorGUI.BeginChangeCheck();

            _reorderableList.DoLayoutList();

            // Track changes in reorderable list
            if (EditorGUI.EndChangeCheck())
            {
                OnRulesetChange();
            }
        }

        private void OnRulesetChange()
        {
            ((HierarchyRulesetV2) target).SendMessage("OnRulesetChange", DontRequireReceiver);
            serializedObject.ApplyModifiedProperties();
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private enum Filter
        {
            All, Name, Object
        }
    }
}