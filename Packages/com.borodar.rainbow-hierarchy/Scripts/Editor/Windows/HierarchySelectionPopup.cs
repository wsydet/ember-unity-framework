using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Borodar.RainbowHierarchy.ColorHelper;

namespace Borodar.RainbowHierarchy
{
    public abstract class HierarchySelectionPopup<T> : CoreDraggablePopupWindow where T : System.Enum
    {
        protected const float PADDING = 4f;
        protected const float SPACING = 1f;
        protected const float LINE_HEIGHT = 18f;
        protected const float LABELS_WIDTH = 90f;
        protected const float PREVIEW_SIZE_SMALL = 16f;
        protected const float PREVIEW_SIZE_LARGE = 64f;

        protected Vector2 ScrollPos;
        protected Vector2 ScrollPos2;
        protected dynamic HierarchyRule;
        protected bool IsRuleSerialized;

        private EditorWindow _parent;
        private Rect _windowRect;
        private Rect _backgroundRect;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        protected abstract Vector2 WindowSize { get; }
        protected abstract int GridColumns { get; }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void ShowWithParams(Vector2 inPosition, dynamic hierarchyRule, EditorWindow parent = null)
        {
            var rect = new Rect(inPosition, WindowSize);
            _windowRect = new Rect(Vector2.zero, rect.size);
            _backgroundRect = new Rect(Vector2.one, rect.size - new Vector2(2f, 2f));

            HierarchyRule = hierarchyRule;
            _parent = parent;
            IsRuleSerialized = parent == null;

            Show<HierarchyPopupWindow>(rect);
        }

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        protected override void OnGUI()
        {
            base.OnGUI();

            if (focusedWindow != this) Close();

            var rect = _windowRect;

            DrawBackground();

            rect.x = PADDING;
            rect.y = PADDING;
            rect.width = WindowSize.x - LABELS_WIDTH - 2f * PADDING;
            rect.height = WindowSize.y - 2f * PADDING;

            DrawIcons(rect);

            rect.x += rect.width + 1f;
            rect.width = 2f;

            DrawSeparators(rect);

            rect.x += rect.width + PADDING  * 1.5f;
            rect.y += 1f;
            rect.width = LABELS_WIDTH - PADDING * 2.5f;
            rect.height = LINE_HEIGHT;

            DrawButtons(rect);
        }

        //---------------------------------------------------------------------
        // Protected
        //---------------------------------------------------------------------

        protected void CloseAndRepaintParent()
        {
            _parent.Repaint();
            Close();
        }

        protected void ApplyPropertyChangesAndClose(SerializedProperty projectItem)
        {
            var serializedObject = projectItem.serializedObject;
            serializedObject.ApplyModifiedProperties();
            HierarchyRulesetV2.OnRulesetChangeCallback();
            Close();
        }

        protected static Vector2 BeginScrollView(Vector2 scrollPos)
        {
            return EditorGUILayout.BeginScrollView(scrollPos, false, true,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
        }

        protected override bool IsDragModifierPressed(Event e)
        {
            return HierarchyPreferences.IsDragModifierPressed(e);
        }

        protected void DrawIconsGrid(IReadOnlyList<T> icons)
        {
            var count = icons.Count();
            var rows = Mathf.Ceil((float) count / GridColumns);

            for (var i = 0; i < rows; i++)
            {
                GUILayout.BeginHorizontal();
                for (var j = 0; j < GridColumns; j++)
                {
                    var id = i * GridColumns + j;
                    if (id >= count)
                    {
                        GUILayout.FlexibleSpace();
                        break;
                    }

                    DrawIconButton(icons[id]);
                }
                GUILayout.EndHorizontal();
            }
        }

        protected abstract void DrawIconButton(T icon);

        protected abstract void DrawButtons(Rect rect);

        protected abstract void DrawIcons(Rect rect);

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void DrawBackground()
        {
            var isProSkin = EditorGUIUtility.isProSkin;

            var borderColor = isProSkin ? POPUP_BORDER_CLR_PRO : POPUP_BORDER_CLR_FREE;
            EditorGUI.DrawRect(_windowRect, borderColor);

            var backgroundColor = isProSkin ? POPUP_BACKGROUND_CLR_PRO : POPUP_BACKGROUND_CLR_FREE;
            EditorGUI.DrawRect(_backgroundRect, backgroundColor);
        }

        private void DrawSeparators(Rect rect)
        {
            var color1 = (EditorGUIUtility.isProSkin) ? SEPARATOR_CLR_1_PRO : SEPARATOR_CLR_1_FREE;
            var color2 = (EditorGUIUtility.isProSkin) ? SEPARATOR_CLR_2_PRO : SEPARATOR_CLR_2_FREE;

            rect.x += 0.5f * PADDING;
            rect.y = 1f;
            rect.width = 1f;
            rect.height = WindowSize.y - 2f;
            EditorGUI.DrawRect(rect, color1);
            rect.x += 1f;
            EditorGUI.DrawRect(rect, color2);
        }
    }
}