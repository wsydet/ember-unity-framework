// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

#if UNITY_EDITOR

using UnityEditor;

namespace Ember.Editor
{
    /// <summary>
    /// Automatically sets PNG textures imported into <c>Assets/Art/Icons/</c> to Sprite mode.
    /// SVG files are handled by the <c>com.unity.vectorgraphics</c> package once installed.
    /// </summary>
    public class IconImportProcessor : AssetPostprocessor
    {
        private const string ICON_DIR = "Assets/Art/Icons/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ICON_DIR))
                return;

            var importer = (TextureImporter)assetImporter;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.streamingMipmaps = false;
            }
        }
    }
}
#endif
