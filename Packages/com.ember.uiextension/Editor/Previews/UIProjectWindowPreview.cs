////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System;
////using System.Collections.Generic;
////using UnityEditor.SceneManagement;
////using UnityEngine;
////using UnityEngine.Experimental.Rendering;
////using UnityEngine.SceneManagement;
////using UnityEditor;
////using Object = UnityEngine.Object;
////using Unity.Collections;
////
////namespace Burner.UIExtension.Previews
////{
////    public static class UIProjectWindowPreview
////    {
////        public static Dictionary<string, Texture2D> m_UIPreviewCache = new Dictionary<string, Texture2D>();
////
////        private static Vector2Int m_UIItemCanvasSize = new Vector2Int(1080, 1920);
////
////        private struct UIProjectWindowPreviewCaptureScope : IDisposable
////        {
////            public int rectWidth;
////            public int rectHeight;
////
////            public Scene previewScene;
////
////            public GameObject cameraGameObject;
////            public Camera camera;
////
////            public bool isCanvasPrefab;
////
////            public GameObject clonedPrefabGameObject;
////            public GameObject environmentCanvasGameObject;
////            public Canvas canvas;
////            public RectTransform rectTransform;
////
////            public RenderTexture bigRenderTexture;
////            public RenderTexture smallRenderTexture;
////
////            public UIProjectWindowPreviewCaptureScope(GameObject prefabGameObject, int rectWidth, int rectHeight)
////            {
////                this.rectWidth = rectWidth;
////                this.rectHeight = rectHeight;
////
////                previewScene = EditorSceneManager.NewPreviewScene();
////
////                cameraGameObject = EditorUtility.CreateGameObjectWithHideFlags("Camera", HideFlags.DontSave);
////                camera = cameraGameObject.AddComponent<Camera>();
////                SceneManager.MoveGameObjectToScene(cameraGameObject, previewScene);
////
////                isCanvasPrefab = prefabGameObject.GetComponent<Canvas>() != null;
////                if (isCanvasPrefab)
////                {
////                    environmentCanvasGameObject = null;
////                    clonedPrefabGameObject = Object.Instantiate(prefabGameObject);
////                    canvas = clonedPrefabGameObject.GetComponent<Canvas>();
////                    rectTransform = clonedPrefabGameObject.GetComponent<RectTransform>();
////                }
////                else
////                {
////                    environmentCanvasGameObject = new GameObject("EnvironmentCanvas");
////                    rectTransform = environmentCanvasGameObject.AddComponent<RectTransform>();
////                    canvas = environmentCanvasGameObject.AddComponent<Canvas>();
////                    clonedPrefabGameObject = Object.Instantiate(prefabGameObject, environmentCanvasGameObject.transform, true);
////                }
////
////                bigRenderTexture = RenderTexture.GetTemporary(m_UIItemCanvasSize.x, m_UIItemCanvasSize.y, 0, GraphicsFormat.R8G8B8A8_SRGB);
////                smallRenderTexture = RenderTexture.GetTemporary(rectWidth, rectHeight, 0, GraphicsFormat.R8G8B8A8_SRGB);
////                bigRenderTexture.enableRandomWrite = true;
////                smallRenderTexture.enableRandomWrite = true;
////
////                int numTileX = Mathf.CeilToInt(m_UIItemCanvasSize.x / 8.0f);
////                int numTileY = Mathf.CeilToInt(m_UIItemCanvasSize.y / 8.0f);
////                int groupCount = numTileX * numTileY;
////
////                UIPreviewCommon.m_MaxCoordsNativeArray = new NativeArray<Vector2Int>(groupCount, Allocator.Persistent);
////                UIPreviewCommon.m_MinCoordsNativeArray = new NativeArray<Vector2Int>(groupCount, Allocator.Persistent);
////            }
////
////            public void Dispose()
////            {
////                EditorSceneManager.ClosePreviewScene(previewScene);
////                RenderTexture.ReleaseTemporary(bigRenderTexture);
////                RenderTexture.ReleaseTemporary(smallRenderTexture);
////                UIPreviewCommon.m_MaxCoordsNativeArray.Dispose();
////                UIPreviewCommon.m_MinCoordsNativeArray.Dispose();
////            }
////        }
////
////        [InitializeOnLoadMethod]
////        private static void ProjectWindow()
////        {
////            EditorApplication.projectWindowItemOnGUI += DrawUIProjectWindowPreview;
////        }
////
////        private static void DrawUIProjectWindowPreview(string guid, Rect rect) {
////            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
////            Type assetType = AssetDatabase.GetMainAssetTypeFromGUID(new GUID(guid));
////
////            if (assetType != typeof(GameObject))
////                return;
////
////            GameObject gameObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
////            if (gameObject == null || gameObject.GetComponent<RectTransform>() == null)
////                return;
////
////            if (rect.height < 20) return;
////
////            if (Event.current.type == EventType.Repaint)
////            {
////                int rectWidth = (int)rect.width;
////                int rectHeight = (int)rect.height - UIPreviewCommon.TitleHeight;
////
////                if (m_UIPreviewCache.TryGetValue(guid, out Texture2D cachedTexture))
////                {
////                    Rect newRect = new Rect(rect.x, rect.y, rectWidth, rectHeight);
////                    GUI.DrawTexture(newRect, cachedTexture);
////                }
////                else
////                {
////                    Texture2D previewTexture;
////                    using (var captureScope = new UIProjectWindowPreviewCaptureScope(gameObject, rectWidth, rectHeight))
////                    {
////                        captureScope.cameraGameObject.transform.localScale = Vector3.one;
////                        captureScope.cameraGameObject.transform.position = new Vector3(0, 0, -10000.0f);
////                        captureScope.cameraGameObject.transform.rotation = Quaternion.identity;
////
////                        captureScope.camera.cameraType = CameraType.Game;
////                        captureScope.camera.clearFlags = CameraClearFlags.SolidColor;
////                        captureScope.camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
////                        captureScope.camera.scene = captureScope.previewScene;
////                        captureScope.camera.orthographic = true;
////                        captureScope.camera.nearClipPlane = 0.1f;
////                        captureScope.camera.farClipPlane = 20000.0f;
////                        captureScope.camera.aspect = (float)rectWidth / rectHeight;
////                        captureScope.camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
////
////                        if (captureScope.isCanvasPrefab)
////                        {
////                            SceneManager.MoveGameObjectToScene(captureScope.clonedPrefabGameObject, captureScope.previewScene);
////                            captureScope.clonedPrefabGameObject.hideFlags = HideFlags.DontSave;
////                        }
////                        else
////                        {
////                            captureScope.environmentCanvasGameObject.layer = LayerMask.NameToLayer("UI");
////                            SceneManager.MoveGameObjectToScene(captureScope.environmentCanvasGameObject, captureScope.previewScene);
////                            captureScope.environmentCanvasGameObject.hideFlags = HideFlags.DontSave;
////                            captureScope.clonedPrefabGameObject.hideFlags = HideFlags.DontSave;
////
////                            Vector2 anchorMin = captureScope.clonedPrefabGameObject.GetComponent<RectTransform>().anchorMin;
////                            Vector2 anchorMax = captureScope.clonedPrefabGameObject.GetComponent<RectTransform>().anchorMax;
////                            if (anchorMin == anchorMax)
////                            {
////                                captureScope.clonedPrefabGameObject.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
////                                captureScope.clonedPrefabGameObject.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
////                            }
////                        }
////
////                        captureScope.canvas.transform.localScale = Vector3.one;
////                        captureScope.canvas.transform.localPosition = Vector3.zero;
////                        captureScope.canvas.transform.localRotation = Quaternion.identity;
////                        captureScope.canvas.renderMode = RenderMode.WorldSpace;
////
////                        if (captureScope.isCanvasPrefab)
////                        {
////                            UIPreviewConfig config = AssetDatabase.LoadAssetAtPath<UIPreviewConfig>("Packages/com.burner.uiextension/Editor/Previews/UIPreviewConfig.asset");
////                            captureScope.rectTransform.sizeDelta = new Vector2(config.UIPreviewResolution.x, config.UIPreviewResolution.y);
////                        }
////                        else
////                        {
////                            captureScope.rectTransform.sizeDelta = new Vector2(m_UIItemCanvasSize.x, m_UIItemCanvasSize.y);
////                        }
////
////                        Bounds canvasBounds = UIPreviewCommon.GetCanvasBounds(captureScope.canvas);
////                        captureScope.camera.transform.position = new Vector3(canvasBounds.center.x, canvasBounds.center.y, captureScope.camera.transform.position.z);
////
////                        if (captureScope.isCanvasPrefab)
////                        {
////                            float aspectRatioOuter = captureScope.camera.aspect;
////                            float aspectRatioInner = canvasBounds.size.x / canvasBounds.size.y;
////                            captureScope.camera.orthographicSize = aspectRatioOuter > aspectRatioInner ? canvasBounds.size.y / 2.0f : canvasBounds.size.x / (aspectRatioOuter * 2.0f);
////
////                            captureScope.camera.targetTexture = captureScope.smallRenderTexture;
////                            captureScope.camera.Render();
////
////                            previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.smallRenderTexture, new Vector2Int(captureScope.rectWidth, captureScope.rectHeight));
////                        }
////                        else
////                        {
////                            captureScope.camera.aspect = (float)m_UIItemCanvasSize.x / m_UIItemCanvasSize.y;
////                            captureScope.camera.orthographicSize = canvasBounds.size.y / 2.0f;
////
////                            captureScope.camera.targetTexture = captureScope.bigRenderTexture;
////                            captureScope.camera.Render();
////
////                            var (minCoord, maxCoord) = UIPreviewCommon.CalculateUIItemBoundingBox(captureScope.bigRenderTexture, m_UIItemCanvasSize);
////
////                            if (minCoord is { x: 9999, y: 9999 })
////                            {
////                                previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.bigRenderTexture, new Vector2Int(captureScope.rectWidth, captureScope.rectHeight));
////                            }
////                            else
////                            {
////                                UIPreviewCommon.ScaledBlit(captureScope.bigRenderTexture, captureScope.smallRenderTexture, new Vector2Int(rectWidth, rectHeight), minCoord, maxCoord);
////
////                                previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.smallRenderTexture, new Vector2Int(captureScope.rectWidth, captureScope.rectHeight));
////                            }
////                        }
////                    }
////
////                    Rect newRect = new Rect(rect.x, rect.y, rect.width, rect.height - UIPreviewCommon.TitleHeight);
////                    GUI.DrawTexture(newRect, previewTexture);
////
////                    m_UIPreviewCache.Add(guid, previewTexture);
////                }
////            }
////        }
////    }
////}
