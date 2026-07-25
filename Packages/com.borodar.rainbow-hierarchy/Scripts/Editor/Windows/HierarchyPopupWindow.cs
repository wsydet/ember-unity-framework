using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Borodar.RainbowHierarchy.ColorHelper;
using KeyType = Borodar.RainbowHierarchy.HierarchyRule.KeyType;

namespace Borodar.RainbowHierarchy
{
    public class HierarchyPopupWindow : CoreDraggablePopupWindow
    {
        private const float PADDING = 8f;
        private const float SPACING = 2f;
        private const float LINE_HEIGHT = 18f;
        private const float LABELS_WIDTH = 80f;
        private const float BUTTON_WIDTH = 55f;
        private const float ICON_WIDTH_SMALL = 16f;

        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 176f;

        private static readonly Vector2 WINDOW_SIZE = new(WINDOW_WIDTH, WINDOW_HEIGHT);

        private Rect _windowRect;
        private Rect _backgroundRect;

        private List<GameObject> _selectedObjects;
        private GameObject _currentObject;
        private HierarchyRulesetV2 _ruleset;
        private HierarchyRule _currentRule;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static HierarchyPopupWindow GetDraggableWindow()
        {
            return GetDraggableWindow<HierarchyPopupWindow>();
        }

        public void ShowWithParams(Vector2 inPosition, List<GameObject> selectedObjects, int firstSelected)
        {
            _selectedObjects = selectedObjects;

            var scene = selectedObjects[0].scene;
            _ruleset = HierarchyRulesetV2.GetRulesetByScene(scene, true);

            _currentObject = selectedObjects[firstSelected];
            
            var rule = _ruleset.GetRule(_currentObject);

            _currentRule = rule == null ?
                new HierarchyRule(KeyType.Object, _currentObject, _currentObject.name) :
                new HierarchyRule(rule);
            
            _currentRule.GameObject = _currentObject;

            // Resize
                        
            var customIconHeight = (_currentRule.HasCustomIcon()) ? LINE_HEIGHT : 0f;
            var customBackgroundHeight = (_currentRule.HasCustomBackground()) ? LINE_HEIGHT : 0f;

            var rect = new Rect(inPosition, WINDOW_SIZE)
            {
                height = WINDOW_HEIGHT + customIconHeight + customBackgroundHeight
            };

            _windowRect = new Rect(Vector2.zero, rect.size);
            _backgroundRect = new Rect(Vector2.one, rect.size - new Vector2(2f, 2f));

            Show<HierarchyPopupWindow>(rect);
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        protected override void OnGUI()
        {
            if (_currentRule == null) { Close(); return; }

            base.OnGUI();

            ChangeWindowSize(_currentRule.HasCustomIcon(), _currentRule.HasCustomBackground());
            var rect = _windowRect;

            // Window Background

            var borderColor = EditorGUIUtility.isProSkin ? POPUP_BORDER_CLR_PRO : POPUP_BORDER_CLR_FREE;
            EditorGUI.DrawRect(_windowRect, borderColor);

            var backgroundColor = EditorGUIUtility.isProSkin ? POPUP_BACKGROUND_CLR_PRO : POPUP_BACKGROUND_CLR_FREE;
            EditorGUI.DrawRect(_backgroundRect, backgroundColor);
            
            // Body
            
            DrawTypeAndKey(ref rect);
            DrawPriority(ref rect);
            DrawIcon(ref rect);
            DrawBackground(ref rect);
            DrawButtons(ref rect);
            DrawSeparators(ref rect);
        }

        //---------------------------------------------------------------------
        // Protected
        //---------------------------------------------------------------------

        protected override bool IsDragModifierPressed(Event e)
        {
            return HierarchyPreferences.IsDragModifierPressed(e);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void DrawTypeAndKey(ref Rect rect)
        {
            rect.x += 0.5f * PADDING;
            rect.y += PADDING;
            rect.width = LABELS_WIDTH - PADDING;
            rect.height = LINE_HEIGHT;

            _currentRule.Type = (KeyType) EditorGUI.EnumPopup(rect, _currentRule.Type);

            rect.x += LABELS_WIDTH;
            rect.y = _windowRect.y + PADDING;
            rect.width = _windowRect.width - LABELS_WIDTH - PADDING;

            GUI.enabled = false;
            if (_selectedObjects.Count == 1)
                if (_currentRule.Type == KeyType.Object)
                {
                    EditorGUI.ObjectField(rect, _currentObject, typeof(GameObject), true);
                }
                else
                {
                    EditorGUI.TextField(rect, _currentObject.name);
                }
            else
            {
                EditorGUI.TextField(rect, GUIContent.none, _selectedObjects.Count + " selected");
            }
            GUI.enabled = true;
        }

        private void DrawPriority(ref Rect rect)
        {
            rect.x = 0.5f * PADDING;
            rect.y += LINE_HEIGHT + 3f;
            EditorGUI.LabelField(rect, "Priority");

            rect.width = _windowRect.width - LABELS_WIDTH - PADDING;
            rect.x += LABELS_WIDTH;
            _currentRule.Priority = EditorGUI.IntField(rect, GUIContent.none, _currentRule.Priority);
        }

        private void DrawIcon(ref Rect rect)
        {
            rect.x = 0.5f * PADDING;
            rect.y += LINE_HEIGHT + 1.5f * PADDING + 1f;
            EditorGUI.LabelField(rect, "Icon");

            rect.y += LINE_HEIGHT + SPACING;
            rect.width = ICON_WIDTH_SMALL;
            rect.height = ICON_WIDTH_SMALL;
            if (_currentRule.HasIcon())
            {
                var texture = (_currentRule.HasCustomIcon())
                    ? _currentRule.IconTexture
                    : HierarchyIconsStorage.GetIcon(_currentRule.IconType);

                GUI.DrawTexture(rect, texture);
            }

            rect.x += LABELS_WIDTH;
            rect.y -= LINE_HEIGHT + 1.5f * SPACING;
            rect.width = _windowRect.width - LABELS_WIDTH - PADDING;
            rect.height = LINE_HEIGHT;
            DrawIconsPopup(rect, _currentRule);

            // Custom Icon Field
            if (_currentRule.HasCustomIcon())
            {
                rect.y += LINE_HEIGHT + 2f * SPACING;
                _currentRule.IconTexture = (Texture2D) EditorGUI.ObjectField(rect, _currentRule.IconTexture, typeof(Texture2D), false);
            }

            rect.y += _currentRule.HasCustomIcon() ? LINE_HEIGHT + 0.2f * SPACING : LINE_HEIGHT + SPACING;
            _currentRule.IsIconRecursive = EditorGUI.Toggle(rect, _currentRule.IsIconRecursive);

            rect.x += ICON_WIDTH_SMALL;
            EditorGUI.LabelField(rect, "Recursive");
        }

        private void DrawBackground(ref Rect rect)
        {
            rect.x = 0.5f * PADDING;
            rect.y += LINE_HEIGHT + SPACING * 3;
            EditorGUI.LabelField(rect, "Background");

            rect.y += LINE_HEIGHT + SPACING;
            rect.width = ICON_WIDTH_SMALL * 3f;
            rect.height = ICON_WIDTH_SMALL;
            if (_currentRule.HasBackground())
            {
                var texture = (_currentRule.HasCustomBackground())
                    ? _currentRule.BackgroundTexture
                    : HierarchyBackgroundsStorage.GetBackground(_currentRule.BackgroundType);

                GUI.DrawTexture(rect, texture);
            }

            rect.x += LABELS_WIDTH;
            rect.y -= LINE_HEIGHT + 1.5f * SPACING;
            rect.width = _windowRect.width - LABELS_WIDTH - PADDING;
            rect.height = LINE_HEIGHT;
            DrawBackgroundsPopup(rect, _currentRule);

            // Custom Icon Field
            if (_currentRule.HasCustomBackground())
            {
                rect.y += LINE_HEIGHT + 2f * SPACING;
                _currentRule.BackgroundTexture =
                    (Texture2D) EditorGUI.ObjectField(rect, _currentRule.BackgroundTexture, typeof(Texture2D), false);
            }

            rect.y += _currentRule.HasCustomBackground() ? LINE_HEIGHT + 0.2f * SPACING : LINE_HEIGHT + SPACING;
            _currentRule.IsBackgroundRecursive = EditorGUI.Toggle(rect, _currentRule.IsBackgroundRecursive);

            rect.x += ICON_WIDTH_SMALL;
            EditorGUI.LabelField(rect, "Recursive");
        }

        private void DrawButtons(ref Rect rect)
        {
            rect.x = PADDING;
            rect.y = position.height - LINE_HEIGHT - 0.75f * PADDING;
            rect.width = ICON_WIDTH_SMALL;
            ButtonSettings(rect);

            rect.x += ICON_WIDTH_SMALL + 0.75f * PADDING;
            ButtonFilter(rect);

            rect.x += ICON_WIDTH_SMALL + 0.75f * PADDING;
            ButtonDefault(rect);

            rect.x = WINDOW_WIDTH - 2f * (BUTTON_WIDTH + PADDING);
            rect.width = BUTTON_WIDTH;
            ButtonCancel(rect);

            rect.x = WINDOW_WIDTH - BUTTON_WIDTH - PADDING;
            ButtonApply(rect);
        }

        private void DrawSeparators(ref Rect rect)
        {
            var color1 = (EditorGUIUtility.isProSkin) ? SEPARATOR_CLR_1_PRO : SEPARATOR_CLR_1_FREE;
            var color2 = (EditorGUIUtility.isProSkin) ? SEPARATOR_CLR_2_PRO : SEPARATOR_CLR_2_FREE;

            // First separator
            rect.x = 0.5f * PADDING;
            rect.y = 2f * LINE_HEIGHT + 16f;
            rect.width = WINDOW_WIDTH - PADDING;
            rect.height = 1f;
            EditorGUI.DrawRect(rect, color1);
            rect.y += 1;
            EditorGUI.DrawRect(rect, color2);

            // Second separator
            rect.y = position.height - LINE_HEIGHT - 11f;
            EditorGUI.DrawRect(rect, color1);
            rect.y += 1;
            EditorGUI.DrawRect(rect, color2);
        }
        
        private void ButtonSettings(Rect rect)
        {
            var icon = HierarchyEditorUtility.GetSettingsButtonIcon();
            if (!GUI.Button(rect, new GUIContent(icon, "All Rules"), GUIStyle.none)) return;
            Selection.activeObject = _ruleset;
            Close();
        }

        private void ButtonFilter(Rect rect)
        {
            var icon = HierarchyEditorUtility.GetFilterButtonIcon();
            if (!GUI.Button(rect, new GUIContent(icon, "Object Rules"), GUIStyle.none)) return;

            HierarchyRulesetEditorV2.ShowInspector(_ruleset, _currentObject);
            Close();
        }
        
        private void ButtonDefault(Rect rect)
        {
            var icon = HierarchyEditorUtility.GetDeleteButtonIcon();
            if (!GUI.Button(rect, new GUIContent(icon, "Revert to Default"), GUIStyle.none)) return;

            _currentRule.Priority = 0;

            _currentRule.IconType = HierarchyIcon.None;
            _currentRule.IconTexture = null;
            _currentRule.IsIconRecursive = false;

            _currentRule.BackgroundType = HierarchyBackground.None;
            _currentRule.BackgroundTexture = null;
            _currentRule.IsBackgroundRecursive = false;
        }

        private void ButtonCancel(Rect rect)
        {
            if (!GUI.Button(rect, "Cancel")) return;
            Close();
        }

        private void ButtonApply(Rect rect)
        {
            if (!GUI.Button(rect, "Apply")) return;
            
            var currentScene = _currentObject.scene;
            var ruleset = HierarchyRulesetV2.GetRulesetByScene(currentScene, true);

            foreach (var selectedObject in _selectedObjects)
            {
                // objects can be selected across multiple scenes
                if (currentScene != selectedObject.scene)
                {
                    currentScene = selectedObject.scene;
                    ruleset = HierarchyRulesetV2.GetRulesetByScene(currentScene, true);
                }

                _currentRule.GameObject = _currentRule.Type == KeyType.Name ? null : selectedObject;
                _currentRule.Name = _currentRule.Type == KeyType.Name ? selectedObject.name : null;

                Undo.RecordObject(ruleset, "Rainbow Hierarchy Ruleset Changes");
                ruleset.UpdateRule(selectedObject, _currentRule);
            }   

            Close();
        }
        
        private void ChangeWindowSize(bool hasCustomIcon, bool hasCustomBackground)
        {
            var rect = position;
            var customIconHeight = (hasCustomIcon) ? LINE_HEIGHT + 1f : 0f;
            var customBackgroundHeight = (hasCustomBackground) ? LINE_HEIGHT + 1f : 0f;

            var resizeHeight = WINDOW_HEIGHT + customIconHeight + customBackgroundHeight;
            if (resizeHeight == rect.height) return;
            
            rect.height = resizeHeight;            
            position = rect;

            _windowRect.height = rect.height;
            _backgroundRect.height = rect.height - 2f;
        }
        
        private void DrawIconsPopup(Rect rect, HierarchyRule rule)
        {
            var menuName = (_currentRule.HasCustomIcon()) ? "Custom" : _currentRule.IconType.ToString();
            if (!GUI.Button(rect, new GUIContent(menuName), "MiniPopup")) return;

            var window = HierarchyIconsPopup.GetDraggableWindow();
            window.ShowWithParams(position.position + rect.position, rule, this);
        }        
        
        private void DrawBackgroundsPopup(Rect rect, HierarchyRule rule)
        {
            var  menuName = _currentRule.HasCustomBackground() ? "Custom" : _currentRule.BackgroundType.ToString();
            if (!GUI.Button(rect, new GUIContent(menuName), "MiniPopup")) return;

            var window = HierarchyBackgroundsPopup.GetDraggableWindow();
            window.ShowWithParams(position.position + rect.position, rule, this);
        }
        
    }
}