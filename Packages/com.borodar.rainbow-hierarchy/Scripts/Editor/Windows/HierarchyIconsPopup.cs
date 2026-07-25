using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public class HierarchyIconsPopup : HierarchySelectionPopup<HierarchyIcon>
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(325f, 214f);

        private HierarchyIconCategory _category;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        protected override Vector2 WindowSize => WINDOW_SIZE;
        protected override int GridColumns => 6;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static HierarchyIconsPopup GetDraggableWindow()
        {
            return GetDraggableWindow<HierarchyIconsPopup>();
        }

        //---------------------------------------------------------------------
        // Protected
        //---------------------------------------------------------------------

        protected override void DrawButtons(Rect rect)
        {
            rect.height = WindowSize.y - 60f;
            GUILayout.BeginArea(rect);
            ScrollPos2 = BeginScrollView(ScrollPos2);

            GUILayout.BeginVertical();
            DrawCategoryButton("All", HierarchyIconCategory.All);
            DrawCategoryButton("Colors", HierarchyIconCategory.Colors);
            DrawCategoryButton("General", HierarchyIconCategory.General);
            DrawCategoryButton("Animation", HierarchyIconCategory.Animation);
            DrawCategoryButton("AR", HierarchyIconCategory.AR);
            DrawCategoryButton("Assets", HierarchyIconCategory.Assets);
            DrawCategoryButton("Audio", HierarchyIconCategory.Audio);
            DrawCategoryButton("Collab", HierarchyIconCategory.Collab);
            DrawCategoryButton("Console", HierarchyIconCategory.Console);
            DrawCategoryButton("Effects", HierarchyIconCategory.Effects);
            DrawCategoryButton("Events", HierarchyIconCategory.Events);
            DrawCategoryButton("Folders", HierarchyIconCategory.Folders);
            DrawCategoryButton("Layouts", HierarchyIconCategory.Layouts);
            DrawCategoryButton("Light", HierarchyIconCategory.Light);
            DrawCategoryButton("Meshes", HierarchyIconCategory.Meshes);
            DrawCategoryButton("Misc", HierarchyIconCategory.Miscellaneous);
            DrawCategoryButton("Navigation", HierarchyIconCategory.Navigation);
            DrawCategoryButton("Network", HierarchyIconCategory.Network);
            DrawCategoryButton("Physics2D", HierarchyIconCategory.Physics2D);
            DrawCategoryButton("Physics", HierarchyIconCategory.Physics);
            DrawCategoryButton("Playables", HierarchyIconCategory.Playables);
            DrawCategoryButton("Rendering", HierarchyIconCategory.Rendering);
            DrawCategoryButton("Tilemap", HierarchyIconCategory.Tilemap);
            DrawCategoryButton("UI", HierarchyIconCategory.UI);
            DrawCategoryButton("Video", HierarchyIconCategory.Video);
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            rect.y = WindowSize.y - LINE_HEIGHT * 2f - 5f;
            rect.height = LINE_HEIGHT;
            DrawCustomButton(rect);
            rect.y += LINE_HEIGHT + SPACING;
            DrawNoneButton(rect);
        }

        protected override void DrawIcons(Rect rect)
        {
            GUILayout.BeginArea(rect);
            ScrollPos = BeginScrollView(ScrollPos);

            var predicate = GetCategoryPredicate(_category);
            var icons = Enum.GetValues(typeof(HierarchyIcon))
                .Cast<HierarchyIcon>()
                .Where(predicate)
                .ToList();

            GUILayout.BeginVertical();
            DrawIconsGrid(icons);
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        protected override void DrawIconButton(HierarchyIcon iconType)
        {
            var rect = EditorGUILayout.GetControlRect(
                GUILayout.Width(PREVIEW_SIZE_SMALL + PADDING * 4f),
                GUILayout.Height(PREVIEW_SIZE_SMALL + PADDING * 4f));

            if (GUI.Button(rect, GUIContent.none, "grey_border"))
            {
                AssignIconByType(HierarchyRule, iconType);
            }

            var iconTex = HierarchyIconsStorage.GetIcon(iconType);
            DrawPreview(rect, iconTex);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private void DrawCategoryButton(string text, HierarchyIconCategory category)
        {
            GUILayout.BeginHorizontal();
            var isButtonPressed = GUILayout.Button(text, "MiniToolbarButtonLeft");
            GUILayout.Space(2f);
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);

            if (!isButtonPressed) return;
            ApplyIconsCategory(category);
        }


        private void DrawNoneButton(Rect rect)
        {
            if (!GUI.Button(rect, "None", "minibutton")) return;

            ResetIconToDefault(HierarchyRule);
        }

        private void DrawCustomButton(Rect rect)
        {
            if (!GUI.Button(rect, "Custom", "minibutton")) return;
            AssignIconByType(HierarchyRule, HierarchyIcon.Custom);
        }

        private static Func<HierarchyIcon, bool> GetCategoryPredicate(HierarchyIconCategory category)
        {
            switch (category)
            {
                case HierarchyIconCategory.All:
                    return icon => icon != HierarchyIcon.None && icon != HierarchyIcon.Custom;
                case HierarchyIconCategory.Colors:
                    return icon => (int) icon >= 1000 && (int) icon < 50000;
                case HierarchyIconCategory.General:
                    return icon => (int) icon >= 100000 && (int) icon < 117000;
                case HierarchyIconCategory.Animation:
                    return icon => (int) icon >= 117000 && (int) icon < 120000;
                case HierarchyIconCategory.AR:
                    return icon => (int) icon >= 120000 && (int) icon < 125000;
                case HierarchyIconCategory.Assets:
                    return icon => (int) icon >= 125000 && (int) icon < 130000;
                case HierarchyIconCategory.Audio:
                    return icon => (int) icon >= 130000 && (int) icon < 138500;
                case HierarchyIconCategory.Collab:
                    return icon => (int) icon >= 138500 && (int) icon < 139100;
                case HierarchyIconCategory.Console:
                    return icon => (int) icon >= 139100 && (int) icon < 139500;
                case HierarchyIconCategory.Effects:
                    return icon => (int) icon >= 139500 && (int) icon < 150000;
                case HierarchyIconCategory.Events:
                    return icon => (int) icon >= 150000 && (int) icon < 158000;
                case HierarchyIconCategory.Folders:
                    return icon => (int) icon >= 158000 && (int) icon < 160000;
                case HierarchyIconCategory.Layouts:
                    return icon => (int) icon >= 160000 && (int) icon < 169500;
                case HierarchyIconCategory.Light:
                    return icon => (int) icon >= 169500 && (int) icon < 170000;
                case HierarchyIconCategory.Meshes:
                    return icon => (int) icon >= 170000 && (int) icon < 180000;
                case HierarchyIconCategory.Miscellaneous:
                    return icon => (int) icon >= 180000 && (int) icon < 200000;
                case HierarchyIconCategory.Navigation:
                    return icon => (int) icon >= 200000 && (int) icon < 210000;
                case HierarchyIconCategory.Network:
                    return icon => (int) icon >= 210000 && (int) icon < 230000;
                case HierarchyIconCategory.Physics2D:
                    return icon => (int) icon >= 230000 && (int) icon < 260000;
                case HierarchyIconCategory.Physics:
                    return icon => (int) icon >= 260000 && (int) icon < 280000;
                case HierarchyIconCategory.Playables:
                    return icon => (int) icon >= 280000 && (int) icon < 290000;
                case HierarchyIconCategory.Rendering:
                    return icon => (int) icon >= 290000 && (int) icon < 310000;
                case HierarchyIconCategory.Tilemap:
                    return icon => (int) icon >= 310000 && (int) icon < 320000;
                case HierarchyIconCategory.UI:
                    return icon => (int) icon >= 320000 && (int) icon < 340000;
                case HierarchyIconCategory.Video:
                    return icon => (int) icon >= 340000 && (int) icon < 350000;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DrawPreview(Rect rect, Texture icon)
        {
            rect.x += PADDING * 2f;
            rect.y += PADDING * 2f;
            rect.width = rect.height = PREVIEW_SIZE_SMALL;

            GUI.DrawTexture(rect, icon);
        }

        private void ApplyIconsCategory(HierarchyIconCategory category)
        {
            _category = category;
            ScrollPos = Vector2.zero;
        }

        private void AssignIconByType(dynamic rule, HierarchyIcon type)
        {
            if (IsRuleSerialized)
            {
                rule.FindPropertyRelative("IconType").intValue = (int) type;
                rule.FindPropertyRelative("IconTexture").objectReferenceValue = null;
                ApplyPropertyChangesAndClose(rule);
            }
            else
            {
                rule.IconType = type;
                rule.IconTexture = null;
                CloseAndRepaintParent();
            }
        }

        private void ResetIconToDefault(dynamic rule)
        {
            if (IsRuleSerialized)
            {
                rule.FindPropertyRelative("IconType").intValue = (int) HierarchyIcon.None;
                rule.FindPropertyRelative("IconTexture").objectReferenceValue = null;
                rule.FindPropertyRelative("IsIconRecursive").boolValue = false;
                ApplyPropertyChangesAndClose(rule);
            }
            else
            {
                rule.IconType = HierarchyIcon.None;
                rule.IconTexture = null;
                rule.IsIconRecursive = false;
                CloseAndRepaintParent();
            }
        }
    }
}