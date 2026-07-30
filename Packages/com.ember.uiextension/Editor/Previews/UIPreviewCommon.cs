////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System;
////using Unity.Collections;
////using UnityEditor;
////using UnityEngine;
////using UnityEngine.Rendering;
////using UnityEditor.SceneManagement;
////using UnityEngine.Experimental.Rendering;
////using UnityEngine.SceneManagement;
////#if TEXTMESHPRO
////using TMPro;
////#endif
////
////namespace Burner.UIExtension.Previews
////{
////    public static class UIPreviewCommon
////    {
////        private static ComputeShader m_UIPreviewCS;
////        private static ComputeShader UIPreviewCS
////        {
////            get
////            {
////                if (m_UIPreviewCS == null)
////                    m_UIPreviewCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.burner.uiextension/Editor/Previews/UIPreviewCS.compute");
////
////                return m_UIPreviewCS;
////            }
////        }
////
////        public static NativeArray<Vector2Int> m_MaxCoordsNativeArray;
////        public static NativeArray<Vector2Int> m_MinCoordsNativeArray;
////
////        public static int TitleHeight = 15;
////
////        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture, Vector2Int size)
////        {
////            Texture2D texture2D = new Texture2D(size.x, size.y);
////
////            RenderTexture active = RenderTexture.active;
////            RenderTexture.active = renderTexture;
////            texture2D.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
////            texture2D.Apply();
////            RenderTexture.active = active;
////
////            return texture2D;
////        }
////
////        public static (Vector2Int, Vector2Int) CalculateUIItemBoundingBox(RenderTexture renderTexture, Vector2Int size)
////        {
////            int numTileX = Mathf.CeilToInt(size.x / 8.0f);
////            int numTileY = Mathf.CeilToInt(size.y / 8.0f);
////            int groupCount = numTileX * numTileY;
////
////            ComputeBuffer maxCoordsBuffer = new ComputeBuffer(groupCount, sizeof(int) * 2, ComputeBufferType.Structured);
////            ComputeBuffer minCoordsBuffer = new ComputeBuffer(groupCount, sizeof(int) * 2, ComputeBufferType.Structured);
////
////            CommandBuffer cmd = new CommandBuffer { name = "CalcBoundingBox" };
////
////            cmd.SetComputeTextureParam(UIPreviewCS, 0, "FullSizeRT", renderTexture);
////            cmd.SetComputeBufferParam(UIPreviewCS, 0, "MaxCoords", maxCoordsBuffer);
////            cmd.SetComputeBufferParam(UIPreviewCS, 0, "MinCoords", minCoordsBuffer);
////            cmd.SetComputeVectorParam(UIPreviewCS, "FullSize", new Vector4(size.x, size.y));
////            cmd.SetComputeVectorParam(UIPreviewCS, "ClearColor", new Vector4(0.05f, 0.05f, 0.05f, 1.0f));
////            cmd.SetComputeVectorParam(UIPreviewCS, "DispatchSize", new Vector4(numTileX, numTileY));
////            cmd.DispatchCompute(UIPreviewCS, 0, numTileX, numTileY, 1);
////
////            cmd.RequestAsyncReadbackIntoNativeArray(ref m_MaxCoordsNativeArray, maxCoordsBuffer, _ => { });
////            cmd.RequestAsyncReadbackIntoNativeArray(ref m_MinCoordsNativeArray, minCoordsBuffer, _ => { });
////            cmd.WaitAllAsyncReadbackRequests();
////
////            Graphics.ExecuteCommandBuffer(cmd);
////            cmd.Clear();
////
////            Vector2Int maxCoord = new Vector2Int(-1, -1);
////            Vector2Int minCoord = new Vector2Int(9999, 9999);
////            for (int i = 0; i < m_MaxCoordsNativeArray.Length; i++)
////            {
////                maxCoord = Vector2Int.Max(maxCoord, m_MaxCoordsNativeArray[i]);
////                minCoord = Vector2Int.Min(minCoord, m_MinCoordsNativeArray[i]);
////            }
////
////            return (minCoord, maxCoord);
////        }
////
////        public static void ScaledBlit(RenderTexture source, RenderTexture destination, Vector2Int rectSize, Vector2Int minCoord, Vector2Int maxCoord)
////        {
////            Vector2Int itemSize = new Vector2Int(maxCoord.x - minCoord.x, maxCoord.y - minCoord.y);
////
////            float aspectRatioOuter = (float)rectSize.x / rectSize.y;
////            float aspectRatioInner = (float)itemSize.x / itemSize.y;
////
////            int width, height;
////            Vector2Int min;
////            if (aspectRatioOuter > aspectRatioInner)
////            {
////                height = rectSize.y;
////                width = Mathf.CeilToInt(height * aspectRatioInner);
////                min = new Vector2Int(rectSize.x / 2 - width / 2, 0);
////            }
////            else
////            {
////                width = rectSize.x;
////                height = Mathf.CeilToInt(width / aspectRatioInner);
////                min = new Vector2Int(0, rectSize.y / 2 - height / 2);
////            }
////
////            int numTileX2 = Mathf.CeilToInt(rectSize.x / 8.0f);
////            int numTileY2 = Mathf.CeilToInt(rectSize.y / 8.0f);
////
////            UIPreviewCS.SetTexture(1, "SourceTexture", source);
////            UIPreviewCS.SetTexture(1, "DestinationTexture", destination);
////            UIPreviewCS.SetVector("SourceOffsetAndSize", new Vector4(minCoord.x, minCoord.y, itemSize.x, itemSize.y));
////            UIPreviewCS.SetVector("DestinationOffsetAndSize", new Vector4(min.x, min.y, width, height));
////            UIPreviewCS.SetVector("DestinationTextureSize", new Vector4(rectSize.x, rectSize.y));
////            UIPreviewCS.SetVector("SourceTextureSize", new Vector4(source.width, source.height, 1.0f / source.width, 1.0f / source.height));
////            UIPreviewCS.Dispatch(1, numTileX2, numTileY2, 1);
////        }
////
////        public static Bounds GetCanvasBounds(Canvas canvas)
////        {
////            Vector3[] corners = new Vector3[4];
////
////            RectTransform rectTransform = canvas.transform as RectTransform;
////            rectTransform?.GetWorldCorners(corners);
////
////            Vector3 min, max;
////            min.x = Mathf.Min(corners[0].x, corners[2].x);
////            min.y = Mathf.Min(corners[0].y, corners[2].y);
////            min.z = Mathf.Min(corners[0].z, corners[2].z);
////            max.x = Mathf.Max(corners[0].x, corners[2].x);
////            max.y = Mathf.Max(corners[0].y, corners[2].y);
////            max.z = Mathf.Max(corners[0].z, corners[2].z);
////
////            Vector3 center = (min + max) / 2.0f;
////            Vector3 size = max - min;
////
////            return new Bounds(center, size);
////        }
////
////        #if TEXTMESHPRO
////        private struct TMPMaterialPreviewCaptureScope : IDisposable
////        {
////            public int rectWidth;
////            public int rectHeight;
////
////            public Scene previewScene;
////
////            public GameObject cameraGameObject;
////            public Camera camera;
////
////            public GameObject tmpGameObject;
////            public TextMeshPro tmpComponent;
////            public RectTransform rectTransform;
////
////            public RenderTexture bigRenderTexture;
////            public RenderTexture smallRenderTexture;
////
////            public TMPMaterialPreviewCaptureScope(int rectWidth, int rectHeight)
////            {
////                this.rectWidth = rectWidth;
////                this.rectHeight = rectHeight;
////
////                previewScene = EditorSceneManager.NewPreviewScene();
////
////                cameraGameObject = EditorUtility.CreateGameObjectWithHideFlags("Camera", HideFlags.DontSave);
////                camera = cameraGameObject.AddComponent<Camera>();
////
////                tmpGameObject = EditorUtility.CreateGameObjectWithHideFlags("TMP Text", HideFlags.DontSave);
////                tmpComponent = tmpGameObject.AddComponent<TextMeshPro>();
////
////                rectTransform = tmpGameObject.GetComponent<RectTransform>();
////
////                bigRenderTexture = RenderTexture.GetTemporary(rectWidth * 2, rectHeight * 2, 0, GraphicsFormat.R8G8B8A8_SRGB);
////                smallRenderTexture = RenderTexture.GetTemporary(rectWidth, rectHeight, 0, GraphicsFormat.R8G8B8A8_SRGB);
////                bigRenderTexture.enableRandomWrite = true;
////                smallRenderTexture.enableRandomWrite = true;
////
////                int numTileX = Mathf.CeilToInt(rectWidth / 4.0f);
////                int numTileY = Mathf.CeilToInt(rectHeight / 4.0f);
////                int groupCount = numTileX * numTileY;
////                m_MaxCoordsNativeArray = new NativeArray<Vector2Int>(groupCount, Allocator.Persistent);
////                m_MinCoordsNativeArray = new NativeArray<Vector2Int>(groupCount, Allocator.Persistent);
////            }
////
////            public void Dispose()
////            {
////                EditorSceneManager.ClosePreviewScene(previewScene);
////                RenderTexture.ReleaseTemporary(bigRenderTexture);
////                RenderTexture.ReleaseTemporary(smallRenderTexture);
////                m_MaxCoordsNativeArray.Dispose();
////                m_MinCoordsNativeArray.Dispose();
////            }
////        }
////
////        public static Texture2D CaptureTMPMaterialPreview(TMP_FontAsset fontAsset, Material tmpMaterial, int rectWidth, int rectHeight)
////        {
////            Texture2D previewTexture;
////            using (var captureScope = new TMPMaterialPreviewCaptureScope(rectWidth, rectHeight))
////            {
////                SceneManager.MoveGameObjectToScene(captureScope.cameraGameObject, captureScope.previewScene);
////                captureScope.cameraGameObject.transform.localScale = Vector3.one;
////                captureScope.cameraGameObject.transform.position = new Vector3(0, 0, -10.0f);
////                captureScope.cameraGameObject.transform.rotation = Quaternion.identity;
////                captureScope.camera.cameraType = CameraType.Game;
////                captureScope.camera.clearFlags = CameraClearFlags.SolidColor;
////                captureScope.camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
////                captureScope.camera.scene = captureScope.previewScene;
////                captureScope.camera.orthographic = true;
////                captureScope.camera.orthographicSize = 3.0f;
////                captureScope.camera.aspect = (float)rectWidth / rectHeight;
////
////                SceneManager.MoveGameObjectToScene(captureScope.tmpGameObject, captureScope.previewScene);
////                captureScope.tmpGameObject.layer = LayerMask.NameToLayer("UI");
////
////                captureScope.rectTransform.sizeDelta = new Vector2(5.0f, 5.0f);
////
////                captureScope.tmpComponent.text = "Aa";
////                captureScope.tmpComponent.font = fontAsset ?? captureScope.tmpComponent.font;
////                captureScope.tmpComponent.fontMaterial = tmpMaterial;
////
////                captureScope.camera.targetTexture = captureScope.bigRenderTexture;
////                captureScope.camera.Render();
////
////                var (minCoord, maxCoord) = CalculateUIItemBoundingBox(captureScope.bigRenderTexture, new Vector2Int(captureScope.bigRenderTexture.width, captureScope.bigRenderTexture.height));
////
////                if (minCoord is { x: 9999, y: 9999 })
////                {
////                    previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.bigRenderTexture, new Vector2Int(captureScope.rectWidth, captureScope.rectHeight));
////                }
////                else
////                {
////                    ScaledBlit(captureScope.bigRenderTexture, captureScope.smallRenderTexture, new Vector2Int(rectWidth, rectHeight), minCoord, maxCoord);
////
////                    previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.smallRenderTexture, new Vector2Int(captureScope.rectWidth, captureScope.rectHeight));
////                }
////            }
////            return previewTexture;
////        }
////        #endif
////
////        [MenuItem("Assets/Refresh UI Preview", false, 65536)]
////        public static void RegenerateUIProjectWindowPreview(MenuCommand context)
////        {
////            UIProjectWindowPreview.m_UIPreviewCache.Clear();
////
////            #if TEXTMESHPRO
////            TMPMaterialPreview.m_TMPMaterialPreviewCache.Clear();
////            TMPMaterialPreview.m_TMPAtlasToFontAssetTable.Clear();
////            #endif
////        }
////    }
////}
