using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StorageHelper = Borodar.RainbowHierarchy.TexturesStorageHelper<Borodar.RainbowHierarchy.HierarchyIcon>;

namespace Borodar.RainbowHierarchy
{
    public static class HierarchyIconsStorage
    {
        private static readonly Dictionary<HierarchyIcon, Texture2D> ICON_TEXTURES;
        private static readonly Dictionary<HierarchyIcon, Lazy<string>> ICON_STRINGS;

        //---------------------------------------------------------------------
        // Ctors
        //---------------------------------------------------------------------

        static HierarchyIconsStorage()
        {
            ICON_TEXTURES = new Dictionary<HierarchyIcon, Texture2D>();
            ICON_STRINGS = (EditorGUIUtility.isProSkin) ? HierarchyIconsArchivePro.GetDict()
                                                        : HierarchyIconsArchiveFree.GetDict();
        }

        public static Texture2D GetIcon(int type)
        {
            return GetIcon((HierarchyIcon) type);
        }

        public static Texture2D GetIcon(HierarchyIcon type)
        {
            return StorageHelper.GetTexture(type, FilterMode.Bilinear, ICON_STRINGS, ICON_TEXTURES);
        }
    }
}