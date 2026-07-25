using System;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public abstract class EditorPrefsItem<T>
    {
        protected readonly string Key;
        protected readonly GUIContent Label;
        protected readonly T DefaultValue;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public abstract T Value { get; set; }

        //---------------------------------------------------------------------
        // Events
        //---------------------------------------------------------------------

        public event Action<T> Changed;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        protected EditorPrefsItem(string key, T defaultValue, GUIContent label)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            DefaultValue = defaultValue;
            Label = label;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public abstract void Draw();

        public static implicit operator T(EditorPrefsItem<T> s)
        {
            return s.Value;
        }

        //---------------------------------------------------------------------
        // Protected
        //---------------------------------------------------------------------

        protected void OnValueChanged(T value)
        {
            Changed?.Invoke(value);
        }
    }
}