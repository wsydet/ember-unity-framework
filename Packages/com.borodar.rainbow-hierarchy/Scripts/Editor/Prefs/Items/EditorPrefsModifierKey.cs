using System;
using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public class EditorPrefsModifierKey : EditorPrefsItem<EventModifiers> {

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        public EditorPrefsModifierKey(string key, EventModifiers defaultValue, GUIContent label)
            : base(key, defaultValue, label) { }

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public override EventModifiers Value {
            get
            {
                var index = EditorPrefs.GetInt(Key, (int) DefaultValue);
                return (Enum.IsDefined(typeof(EventModifiers), index)) ? (EventModifiers) index : DefaultValue;
            }
            set
            {
                if (Value == value) return;
                EditorPrefs.SetInt(Key, (int) value);
                OnValueChanged(value);
            }
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public override void Draw() {
            Value = (EventModifiers) EditorGUILayout.EnumPopup(Label, Value);
        }
    }
}