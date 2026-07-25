using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [CustomPropertyDrawer(typeof(HierarchyRule))]
    public class HierarchyRuleDrawer: PropertyDrawer
    {
        private const float PADDING = 8f;
        private const float SPACING = 2f;
        private const float LINE_HEIGHT = 18f;
        private const float LABELS_WIDTH = 92f;
        private const float ICON_WIDTH = 16f;
        private const float PROPERTY_HEIGHT = 142f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var isHidden = property.FindPropertyRelative("IsHidden").boolValue;
            if (isHidden) return;

            var originalPosition = position;
            var serializedItem = new SerializedItemWrapper(property);
            var serializedObject = property.serializedObject;

            DrawLabels(ref position, serializedItem);
            DrawValues(ref position, originalPosition, serializedItem, serializedObject);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var isHidden = property.FindPropertyRelative("IsHidden").boolValue;
            if (isHidden) return 0;

            var iconType = property.FindPropertyRelative("IconType");
            var backgroundType = property.FindPropertyRelative("BackgroundType");
            var hasCustomIcon = (iconType.intValue == (int) HierarchyIcon.Custom);
            var hasCustomBackground = (backgroundType.intValue == (int) HierarchyBackground.Custom);

            var height = PROPERTY_HEIGHT;
            if (hasCustomIcon) height += LINE_HEIGHT + SPACING;
            if (hasCustomBackground) height += LINE_HEIGHT + SPACING;

            return height;
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void DrawLabels(ref Rect position, SerializedItemWrapper item)
        {
            position.y += PADDING;
            position.width = LABELS_WIDTH;
            position.height = LINE_HEIGHT;
            EditorGUI.PropertyField(position, item.ItemType, GUIContent.none);

            position.y += LINE_HEIGHT + SPACING * 2f;
            EditorGUI.LabelField(position, "Priority");

            position.y += LINE_HEIGHT + SPACING * 3f;
            EditorGUI.LabelField(position, "Icon");

            position.y += LINE_HEIGHT + SPACING;

            if (item.HasIcon)
            {
                position.width = ICON_WIDTH;
                position.height = ICON_WIDTH;

                var texture = (item.HasCustomIcon)
                    ? (Texture2D) item.IconTexture.objectReferenceValue
                    : HierarchyIconsStorage.GetIcon(item.IconType.intValue);

                if (texture != null) GUI.DrawTexture(position, texture);

                position.width = LABELS_WIDTH;
                position.height = LINE_HEIGHT;
            }

            position.y += (item.HasCustomIcon) ? 2f * LINE_HEIGHT + PADDING : LINE_HEIGHT + PADDING - SPACING;
            EditorGUI.LabelField(position, "Background");
            position.y += LINE_HEIGHT + SPACING;

            if (item.HasBackground)
            {
                position.width = ICON_WIDTH * 3f;
                position.height = ICON_WIDTH;

                var texture = (item.HasCustomBackground)
                    ? (Texture2D) item.Background.objectReferenceValue
                    : HierarchyBackgroundsStorage.GetBackground(item.BackgroundType.intValue);

                if (texture != null) GUI.DrawTexture(position, texture);
            }

        }

        private static void DrawValues(ref Rect position, Rect originalPosition, SerializedItemWrapper item, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();

            position.x += LABELS_WIDTH + PADDING;
            position.y = originalPosition.y + PADDING;
            position.width = originalPosition.width - LABELS_WIDTH - PADDING;

            var property = item.ItemType.intValue == 0 ? item.GameObject : item.Name;
            EditorGUI.PropertyField(position, property, GUIContent.none);

            position.y += LINE_HEIGHT + SPACING * 2f;
            EditorGUI.PropertyField(position, item.Priority, GUIContent.none);

            position.y += LINE_HEIGHT + SPACING * 3f;
            DrawIconPopupMenu(position, item.Property, item.IconType.intValue);

            if (item.HasCustomIcon)
            {
                position.y += LINE_HEIGHT + SPACING + 1f;
                EditorGUI.PropertyField(position, item.IconTexture, GUIContent.none);
                position.y -= SPACING;
            }

            position.y += LINE_HEIGHT + SPACING;
            EditorGUI.PropertyField(position, item.IconRecursive, GUIContent.none);
            position.x += ICON_WIDTH;
            EditorGUI.LabelField(position, "Recursive");
            position.x -= ICON_WIDTH;

            position.y += LINE_HEIGHT + PADDING;
            DrawBackgroundPopupMenu(position, item.Property, item.BackgroundType.intValue);

            if (item.HasCustomBackground)
            {
                position.y += LINE_HEIGHT + SPACING + 1f;
                EditorGUI.PropertyField(position, item.Background, GUIContent.none);
                position.y -= SPACING;
            }

            position.y += LINE_HEIGHT + SPACING;
            EditorGUI.PropertyField(position, item.BackgroundRecursive, GUIContent.none);
            position.x += ICON_WIDTH;
            EditorGUI.LabelField(position, "Recursive");
            position.x -= ICON_WIDTH;

            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }

        private static void DrawIconPopupMenu(Rect rect, SerializedProperty property, int iconType)
        {
            var menuName = ((HierarchyIcon) iconType).ToString();
            if (GUI.Button(rect, new GUIContent(menuName), "MiniPopup"))
            {
                var screenPoint = GUIUtility.GUIToScreenPoint(rect.position);
                var window = HierarchyIconsPopup.GetDraggableWindow();
                window.ShowWithParams(screenPoint, property);
            }
        }

        private static void DrawBackgroundPopupMenu(Rect rect, SerializedProperty property, int backgroundType)
        {
            var menuName = ((HierarchyBackground) backgroundType).ToString();
            if (GUI.Button(rect, new GUIContent(menuName), "MiniPopup"))
            {
                var screenPoint = GUIUtility.GUIToScreenPoint(rect.position);
                var window = HierarchyBackgroundsPopup.GetDraggableWindow();
                window.ShowWithParams(screenPoint, property);

            }
        }

        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        private class SerializedItemWrapper
        {
            public readonly SerializedProperty Property;

            public readonly SerializedProperty ItemType;
            public readonly SerializedProperty Name;
            public readonly SerializedProperty GameObject;

            public readonly SerializedProperty Priority;

            public readonly SerializedProperty IconType;
            public readonly SerializedProperty IconTexture;
            public readonly SerializedProperty IconRecursive;

            public readonly SerializedProperty BackgroundType;
            public readonly SerializedProperty Background;
            public readonly SerializedProperty BackgroundRecursive;

            public readonly bool HasIcon;
            public readonly bool HasCustomIcon;
            public readonly bool HasBackground;
            public readonly bool HasCustomBackground;

            public SerializedItemWrapper(SerializedProperty property)
            {
                Property = property;

                ItemType = property.FindPropertyRelative("Type");
                Name = property.FindPropertyRelative("Name");
                GameObject = property.FindPropertyRelative("GameObject");

                Priority = property.FindPropertyRelative("Priority");

                IconType = property.FindPropertyRelative("IconType");
                IconTexture = property.FindPropertyRelative("IconTexture");
                IconRecursive = property.FindPropertyRelative("IsIconRecursive");

                BackgroundType = property.FindPropertyRelative("BackgroundType");
                Background = property.FindPropertyRelative("BackgroundTexture");
                BackgroundRecursive = property.FindPropertyRelative("IsBackgroundRecursive");

                HasIcon = (IconType.intValue != (int) HierarchyIcon.None);
                HasCustomIcon = (IconType.intValue == (int) HierarchyIcon.Custom);
                HasBackground = (BackgroundType.intValue != (int) HierarchyBackground.None);
                HasCustomBackground = (BackgroundType.intValue == (int) HierarchyBackground.Custom);
            }
        }
    }
}