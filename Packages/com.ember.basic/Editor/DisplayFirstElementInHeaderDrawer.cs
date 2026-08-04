// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

#if UNITY_EDITOR

using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    [CustomPropertyDrawer(typeof(DisplayFirstElementInHeaderAttribute))]
    public class DisplayFirstElementInHeaderDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 尝试读取第一个子字段的值作为标题
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                SerializedProperty first = property.Copy();
                if (first.NextVisible(true))
                {
                    string header = GetValueAsString(first);
                    if (!string.IsNullOrEmpty(header))
                        label.text = header;
                }
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private static string GetValueAsString(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.intValue.ToString(),
                SerializedPropertyType.Boolean => prop.boolValue.ToString(),
                SerializedPropertyType.Float => prop.floatValue.ToString("F2"),
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.Enum => prop.enumNames[prop.enumValueIndex],
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue != null
                    ? prop.objectReferenceValue.name
                    : "Null",
                SerializedPropertyType.Vector2 => prop.vector2Value.ToString(),
                SerializedPropertyType.Vector3 => prop.vector3Value.ToString(),
                SerializedPropertyType.Color => prop.colorValue.ToString(),
                _ => null // 不支持的类型 → 保持原标题
            };
        }
    }
}

#endif
