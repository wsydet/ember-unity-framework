using System;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [Serializable]
    public class HierarchyRule
    {
        public KeyType Type;
        public string Name;
        public GameObject GameObject;

        public int Ordinal;
        public int Priority;

        public HierarchyIcon IconType;
        public Texture2D IconTexture;
        public bool IsIconRecursive;

        public HierarchyBackground BackgroundType;
        public Texture2D BackgroundTexture;
        public bool IsBackgroundRecursive;

        public bool IsHidden;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        public HierarchyRule(KeyType type, GameObject gameObject, string name)
        {
            Type = type;
            GameObject = gameObject;
            Name = name;
        }
        
        public HierarchyRule(HierarchyRule value)
        {
            Type = value.Type;
            Name = value.Name;
            GameObject = value.GameObject;

            Ordinal = value.Ordinal;
            Priority = value.Priority;

            IconType = value.IconType;
            IconTexture = value.IconTexture;
            IsIconRecursive = value.IsIconRecursive;

            BackgroundType = value.BackgroundType;
            BackgroundTexture = value.BackgroundTexture;
            IsBackgroundRecursive = value.IsBackgroundRecursive;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void CopyFrom(HierarchyRule target)
        {
            Type = target.Type;
            Name = target.Name;
            GameObject = target.GameObject;

            Ordinal = target.Ordinal;
            Priority = target.Priority;

            IconType = target.IconType;
            IconTexture = target.IconTexture;
            IsIconRecursive = target.IsIconRecursive;
            
            BackgroundType = target.BackgroundType;
            BackgroundTexture = target.BackgroundTexture;
            IsBackgroundRecursive = target.IsBackgroundRecursive;
        }

        public bool HasIcon()
        {
            return IconType != HierarchyIcon.None  && (!HasCustomIcon() || IconTexture != null);
        }

        public bool HasCustomIcon()
        {
            return IconType == HierarchyIcon.Custom;
        }

        public bool HasBackground()
        {
            return BackgroundType != HierarchyBackground.None  && (!HasCustomBackground() || BackgroundTexture != null);
        }

        public bool HasCustomBackground()
        {
            return BackgroundType == HierarchyBackground.Custom;
        }

        public bool HasAtLeastOneTexture()
        {
            return HasIcon() || HasBackground();
        }

        public bool IsRecursive()
        {
            return IsIconRecursive || IsBackgroundRecursive;
        }
        
        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------
        
        public enum KeyType
        {
            Object,
            Name
        }
    }
}