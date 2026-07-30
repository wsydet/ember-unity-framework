////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////#if TEXTMESHPRO
////using System;
////using System.Collections.Generic;
////using UnityEditor;
////using UnityEngine;
////using TMPro;
////
////namespace Burner.UIExtension.Previews
////{
////    public static class TMPMaterialPreview
////    {
////        public static Dictionary<string, Texture2D> m_TMPMaterialPreviewCache = new Dictionary<string, Texture2D>();
////        public static Dictionary<string, TMP_FontAsset> m_TMPAtlasToFontAssetTable = new Dictionary<string, TMP_FontAsset>();
////
////        private static string[] m_ShaderName = new string[]
////        {
////            "TextMeshPro/Distance Field",
////            "TextMeshPro/Distance Field (Surface)",
////            "TextMeshPro/Distance Field Overlay",
////            "TextMeshPro/Distance Field SSD",
////            "TextMeshPro/Distance Field UITOP",
////            "TextMeshPro/Mobile/Distance Field",
////            "TextMeshPro/Mobile/Distance Field - Masking",
////            "TextMeshPro/Mobile/Distance Field (Surface)",
////            "TextMeshPro/Mobile/Distance Field Overlay",
////            "TextMeshPro/Mobile/Distance Field SSD"
////        };
////
////        private static bool CheckMaterialUsingTMPShader(Material material)
////        {
////            bool isTMPMaterial = false;
////            foreach (var shaderName in m_ShaderName)
////            {
////                if (material.shader == Shader.Find(shaderName))
////                    isTMPMaterial = true;
////            }
////            return isTMPMaterial;
////        }
////
////        [InitializeOnLoadMethod]
////        private static void ProjectWindow()
////        {
////            EditorApplication.projectWindowItemOnGUI += DrawTMPMaterialPreview;
////        }
////
////        private static void DrawTMPMaterialPreview(string guid, Rect rect)
////        {
////            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
////            Type assetType = AssetDatabase.GetMainAssetTypeFromGUID(new GUID(guid));
////
////            if (assetType != typeof(Material))
////                return;
////
////            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
////            if (!CheckMaterialUsingTMPShader(material))
////                return;
////
////            if (rect.height < 20)
////                return;
////
////            if (material.mainTexture == null)
////                return;
////
////            if (Event.current.type == EventType.Repaint)
////            {
////                int rectWidth = (int)rect.width;
////                int rectHeight = (int)rect.height - UIPreviewCommon.TitleHeight;
////
////                if (m_TMPMaterialPreviewCache.TryGetValue(guid, out Texture2D cachedTexture))
////                {
////                    Rect newRect = new Rect(rect.x, rect.y, rectWidth, rectHeight);
////                    GUI.DrawTexture(newRect, cachedTexture);
////                }
////                else
////                {
////                    TMP_FontAsset fontAsset;
////                    if (!m_TMPAtlasToFontAssetTable.TryGetValue(material.mainTexture.name, out fontAsset))
////                    {
////                        string[] fontAssetGUIDs = AssetDatabase.FindAssets(material.mainTexture.name);
////                        foreach (var fontAssetGUID in fontAssetGUIDs)
////                        {
////                            string fontAssetPath = AssetDatabase.GUIDToAssetPath(fontAssetGUID);
////                            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
////                            if (fontAsset != null && fontAsset.atlasTexture.imageContentsHash == material.mainTexture.imageContentsHash)
////                            {
////                                m_TMPAtlasToFontAssetTable.Add(material.mainTexture.name, fontAsset);
////                            }
////                        }
////                    }
////                    Texture2D previewTexture = UIPreviewCommon.CaptureTMPMaterialPreview(fontAsset, material, rectWidth, rectHeight);
////
////                    Rect newRect = new Rect(rect.x, rect.y, rectWidth, rectHeight);
////                    GUI.DrawTexture(newRect, previewTexture);
////
////                    m_TMPMaterialPreviewCache.Add(guid, previewTexture);
////                }
////            }
////        }
////    }
////}
////#endif
