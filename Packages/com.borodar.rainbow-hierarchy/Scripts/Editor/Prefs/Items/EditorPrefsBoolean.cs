using UnityEditor;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public class EditorPrefsBoolean : EditorPrefsItem<bool>
    {
        private readonly float _labelWidth;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        public EditorPrefsBoolean(string key, bool defaultValue, GUIContent label, float labelWidth)
            : base(key, defaultValue, label)
        {
            _labelWidth = labelWidth;
        }

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public override bool Value
        {
            get
            {
                return EditorPrefs.GetBool(Key, DefaultValue);
            }
            set
            {
                if (Value == value) return;
                EditorPrefs.SetBool(Key, value);
                OnValueChanged(value);
            }
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public override void Draw()
        {
            EditorGUIUtility.labelWidth = _labelWidth;
            Value = EditorGUILayout.Toggle(Label, Value);
        }
    }
}