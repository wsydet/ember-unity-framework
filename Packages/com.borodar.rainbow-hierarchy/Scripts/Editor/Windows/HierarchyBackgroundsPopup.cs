using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public class HierarchyBackgroundsPopup : HierarchySelectionPopup<HierarchyBackground>
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(390f, 214f);

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        protected override Vector2 WindowSize => WINDOW_SIZE;
        protected override int GridColumns => 4;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static HierarchyBackgroundsPopup GetDraggableWindow()
        {
            return GetDraggableWindow<HierarchyBackgroundsPopup>();
        }

        //---------------------------------------------------------------------
        // Protected
        //---------------------------------------------------------------------

        protected override void DrawButtons(Rect rect)
        {
            DrawCategoryButton(rect, "All");

            rect.y += LINE_HEIGHT * 9f + SPACING * 6f;
            DrawCustomButton(rect);
            rect.y += LINE_HEIGHT + SPACING;
            DrawNoneButton(rect);
        }

        protected override void DrawIcons(Rect rect)
        {
            GUILayout.BeginArea(rect);
            ScrollPos = BeginScrollView(ScrollPos);

            var predicate = GetCategoryPredicate();
            var icons = Enum.GetValues(typeof(HierarchyBackground))
                .Cast<HierarchyBackground>()
                .Where(predicate)
                .ToList();

            GUILayout.BeginVertical();
            DrawIconsGrid(icons);
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        protected override void DrawIconButton(HierarchyBackground backgroundType)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(66), GUILayout.Height(22));
            if (GUI.Button(rect, GUIContent.none, "grey_border"))
            {
                AssignBackgroundByType(HierarchyRule, backgroundType);
            }

            var backgroundTex = HierarchyBackgroundsStorage.GetBackground(backgroundType);
            DrawPreview(rect, backgroundTex);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void DrawCategoryButton(Rect rect, string text)
        {
            if (!GUI.Button(rect, text, "MiniToolbarButtonLeft")) return;
            ApplyIconsCategory();
        }

        private void DrawCustomButton(Rect rect)
        {
            if (!GUI.Button(rect, "Custom", "minibutton")) return;
            AssignBackgroundByType(HierarchyRule, HierarchyBackground.Custom);
        }

        private void DrawNoneButton(Rect rect)
        {
            if (!GUI.Button(rect, "None", "minibutton")) return;
            ResetBackgroundToDefault(HierarchyRule);
        }

        private static Func<HierarchyBackground, bool> GetCategoryPredicate()
        {
            return icon => (int) icon >= 1000 && (int) icon < 50000;
        }

        private static void DrawPreview(Rect rect, Texture icon)
        {
            rect.x += 1f;
            rect.y += 1f;
            rect.width = PREVIEW_SIZE_LARGE;
            rect.height = PREVIEW_SIZE_SMALL + 4f;

            GUI.Label(rect, "Folder", "CenteredLabel");
            GUI.DrawTexture(rect, icon);
        }

        private void ApplyIconsCategory()
        {
            ScrollPos = Vector2.zero;
        }

        private void AssignBackgroundByType(dynamic rule, HierarchyBackground type)
        {
            if (IsRuleSerialized)
            {
                rule.FindPropertyRelative("BackgroundType").intValue = (int) type;
                rule.FindPropertyRelative("BackgroundTexture").objectReferenceValue = null;
                ApplyPropertyChangesAndClose(rule);
            }
            else
            {
                rule.BackgroundType = type;
                rule.BackgroundTexture = null;
                CloseAndRepaintParent();
            }
        }

        private void ResetBackgroundToDefault(dynamic rule)
        {
            if (IsRuleSerialized)
            {
                rule.FindPropertyRelative("BackgroundType").intValue = (int) HierarchyBackground.None;
                rule.FindPropertyRelative("BackgroundTexture").objectReferenceValue = null;
                rule.FindPropertyRelative("IsBackgroundRecursive").boolValue = false;
                ApplyPropertyChangesAndClose(rule);
            }
            else
            {
                rule.BackgroundType = HierarchyBackground.None;
                rule.BackgroundTexture = null;
                rule.IsBackgroundRecursive = false;
                CloseAndRepaintParent();
            }
        }
    }
}