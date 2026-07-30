////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System;
////using UnityEditor;
////using UnityEditor.SceneManagement;
////using UnityEngine;
////using UnityEngine.Experimental.Rendering;
////using UnityEngine.SceneManagement;
////using Object = UnityEngine.Object;
////
////namespace Burner.UIExtension.Previews
////{
////    [CustomPreview(typeof(GameObject))]
////    public class UIInspectorPreview : ObjectPreview
////    {
////        public override GUIContent GetPreviewTitle()
////        {
////            return EditorGUIUtility.TrTextContent("UI Preview");
////        }
////
////        public override bool HasPreviewGUI()
////        {
////            if (target == null)
////                return false;
////
////            if (PrefabUtility.GetPrefabAssetType(target) != PrefabAssetType.Regular)
////                return false;
////
////            GameObject targetGameObject = target as GameObject;
////            return targetGameObject != null && targetGameObject.GetComponent<Canvas>() != null;
////        }
////
////        private struct UIInspectorPreviewCaptureScope : IDisposable
////        {
////            public int rectWidth;
////            public int rectHeight;
////
////            public Scene previewScene;
////
////            public GameObject cameraGameObject;
////            public Camera camera;
////
////            public GameObject clonedPrefabGameObject;
////            public Canvas canvas;
////            public RectTransform rectTransform;
////
////            public RenderTexture renderTexture;
////
////            public UIInspectorPreviewCaptureScope(GameObject prefabGameObject, int rectWidth, int rectHeight)
////            {
////                this.rectWidth = rectWidth;
////                this.rectHeight = rectHeight;
////
////                previewScene = EditorSceneManager.NewPreviewScene();
////
////                cameraGameObject = EditorUtility.CreateGameObjectWithHideFlags("Camera", HideFlags.DontSave);
////                camera = cameraGameObject.AddComponent<Camera>();
////
////                clonedPrefabGameObject = Object.Instantiate(prefabGameObject);
////                canvas = clonedPrefabGameObject.GetComponent<Canvas>();
////                rectTransform = clonedPrefabGameObject.GetComponent<RectTransform>();
////
////                renderTexture = RenderTexture.GetTemporary(rectWidth, rectHeight, 0, GraphicsFormat.R8G8B8A8_SRGB);
////            }
////
////            public void Dispose()
////            {
////                EditorSceneManager.ClosePreviewScene(previewScene);
////                RenderTexture.ReleaseTemporary(renderTexture);
////            }
////        }
////
////        public override void OnPreviewGUI(Rect rect, GUIStyle background)
////        {
////            if (Event.current.type == EventType.Repaint)
////            {
////                Texture2D previewTexture;
////                using (var captureScope = new UIInspectorPreviewCaptureScope((GameObject)target, (int)rect.width, (int)rect.height))
////                {
////                    SceneManager.MoveGameObjectToScene(captureScope.cameraGameObject, captureScope.previewScene);
////                    captureScope.cameraGameObject.transform.localScale = Vector3.one;
////                    captureScope.cameraGameObject.transform.position = new Vector3(0, 0, -10000.0f);
////                    captureScope.cameraGameObject.transform.rotation = Quaternion.identity;
////
////                    captureScope.camera.cameraType = CameraType.Game;
////                    captureScope.camera.clearFlags = CameraClearFlags.SolidColor;
////                    captureScope.camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
////                    captureScope.camera.scene = captureScope.previewScene;
////                    captureScope.camera.orthographic = true;
////                    captureScope.camera.nearClipPlane = 0.1f;
////                    captureScope.camera.farClipPlane = 20000.0f;
////                    captureScope.camera.aspect = (float)captureScope.rectWidth / captureScope.rectHeight;
////                    captureScope.camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
////
////                    SceneManager.MoveGameObjectToScene(captureScope.clonedPrefabGameObject, captureScope.previewScene);
////                    captureScope.clonedPrefabGameObject.hideFlags = HideFlags.DontSave;
////
////                    captureScope.canvas.transform.localScale = Vector3.one;
////                    captureScope.canvas.transform.position = Vector3.zero;
////                    captureScope.canvas.transform.rotation = Quaternion.identity;
////                    captureScope.canvas.renderMode = RenderMode.WorldSpace;
////
////                    UIPreviewConfig config = AssetDatabase.LoadAssetAtPath<UIPreviewConfig>("Packages/com.burner.uiextension/Editor/Previews/UIPreviewConfig.asset");
////                    captureScope.rectTransform.sizeDelta = new Vector2(config.UIPreviewResolution.x, config.UIPreviewResolution.y);
////
////                    Bounds canvasBounds = UIPreviewCommon.GetCanvasBounds(captureScope.canvas);
////                    captureScope.camera.transform.position = new Vector3(canvasBounds.center.x, canvasBounds.center.y, captureScope.camera.transform.position.z);
////
////                    float aspectRatioOuter = captureScope.camera.aspect;
////                    float aspectRatioInner = canvasBounds.size.x / canvasBounds.size.y;
////                    captureScope.camera.orthographicSize = aspectRatioOuter > aspectRatioInner ? canvasBounds.size.y / 2.0f : canvasBounds.size.x / (aspectRatioOuter * 2.0f);
////
////                    captureScope.camera.targetTexture = captureScope.renderTexture;
////                    captureScope.camera.Render();
////
////                    previewTexture = UIPreviewCommon.RenderTextureToTexture2D(captureScope.renderTexture, new Vector2Int(captureScope.renderTexture.width, captureScope.renderTexture.height));
////                }
////
////                GUI.DrawTexture(rect, previewTexture);
////            }
////        }
////    }
////}
