using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    public static class TexturesStorageHelper<T>
    {
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        [SuppressMessage("ReSharper", "InvertIf")]
        public static Texture2D GetTexture(T type, FilterMode filterMode, Dictionary<T, Lazy<string>> strings, Dictionary<T, Texture2D> textures)
        {
            if (!textures.TryGetValue(type, out var texture))
            {
                if (strings.TryGetValue(type, out var lazyString))
                {
                    texture = TextureFromString(lazyString.Value, filterMode);
                }
                else
                {
                    texture = Texture2D.grayTexture;
                    RHLogger.LogWarning($"Cannot find texture with ID: {type}");
                }

                textures.Add(type, texture);
            }

            return texture;
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
        private static Texture2D TextureFromString(string value, FilterMode filterMode)
        {
            var texture = new Texture2D(4, 4, TextureFormat.DXT5, false, false);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.LoadImage(Convert.FromBase64String(value));
            return texture;
        }
    }
}