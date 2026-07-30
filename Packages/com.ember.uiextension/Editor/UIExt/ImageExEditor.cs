////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////using UnityEngine.UI;
////using UnityEditor.UI;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(ImageEx), true)]
////    public class ImageExEditor : ImageEditor
////    {
////        SerializedProperty spriteArray, spriteIndex, spriteNameArray, supportMultiLanguage, irregularClickArea, hitMinimalAlpha, animated, fps, delay, keepNativeSize, playOnce, playbackSpeed;
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////            spriteArray = serializedObject.FindProperty("spriteArray");
////            spriteIndex = serializedObject.FindProperty("spriteIndex");
////            spriteNameArray = serializedObject.FindProperty("spriteNameArray");
////            supportMultiLanguage = serializedObject.FindProperty("supportMultiLanguage");
////            irregularClickArea = serializedObject.FindProperty("irregularClickArea");
////            hitMinimalAlpha = serializedObject.FindProperty("hitMinimalAlpha");
////            animated = serializedObject.FindProperty("animated");
////            fps = serializedObject.FindProperty("fps");
////            delay = serializedObject.FindProperty("delay");
////            keepNativeSize = serializedObject.FindProperty("keepNativeSize");
////            playOnce = serializedObject.FindProperty("playOnce");
////            playbackSpeed = serializedObject.FindProperty("playbackSpeed");
////        }
////        [MenuItem("CONTEXT/Image/替换成ImageEx")]
////        static void SwitchToImageEx(MenuCommand command)
////        {
////            Image body = (Image)command.context;
////            if (body is ImageEx)
////                return;
////            var sprite = body.sprite;
////            var color = body.color;
////            var fillAmount = body.fillAmount;
////            var fillCenter = body.fillCenter;
////            var fillClockwise = body.fillClockwise;
////            var fillMethod = body.fillMethod;
////            var fillOrigin = body.fillOrigin;
////            var material = body.material == body.defaultMaterial ? null : body.material;
////            var raycastTarget = body.raycastTarget;
////            var type = body.type;
////            var spriteMesh = body.useSpriteMesh;
////
////            var go = body.gameObject;
////            Object.DestroyImmediate(body);
////            var imgEx = go.AddComponent<ImageEx>();
////            imgEx.sprite = sprite;
////            imgEx.color = color;
////            imgEx.fillAmount = fillAmount;
////            imgEx.fillCenter = fillCenter;
////            imgEx.fillClockwise = fillClockwise;
////            imgEx.fillMethod = fillMethod;
////            imgEx.fillOrigin = fillOrigin;
////            imgEx.material = material;
////            imgEx.raycastTarget = raycastTarget;
////            imgEx.type = type;
////            imgEx.useSpriteMesh = spriteMesh;
////            EditorUtility.SetDirty(go);
////        }
////        public override void OnInspectorGUI()
////        {
////            base.OnInspectorGUI();
////            
////            int oldState = spriteIndex.intValue;
////            EditorGUILayout.PropertyField(irregularClickArea, new GUIContent("支持不规则点击区域"));
////            bool wasKeep = keepNativeSize.boolValue;
////            EditorGUILayout.PropertyField(keepNativeSize, new GUIContent("保持原始图片尺寸"));
////            bool needRefresh = keepNativeSize.boolValue && !wasKeep;
////
////            if (irregularClickArea.boolValue)
////            {
////                EditorGUILayout.PropertyField(hitMinimalAlpha, new GUIContent("点击最低Alpha值"));
////                Image img = target as Image;
////                if (img.sprite)
////                {
////                    var path = AssetDatabase.GetAssetPath(img.sprite);
////                    TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
////                    if (ti)
////                    {
////                        if(!ti.isReadable)
////                            EditorGUILayout.HelpBox($"{System.IO.Path.GetFileName(path)} 需要开启Read/Write", MessageType.Error);
////                    }
////                    else
////                    {
////                        EditorGUILayout.HelpBox($"{System.IO.Path.GetFileName(path)} 需要开启Read/Write", MessageType.Error);
////                    }
////                }
////                else
////                {
////                    EditorGUILayout.HelpBox($"请选择合适的Sprite", MessageType.Error);
////                }
////            }
////            EditorGUILayout.PropertyField(supportMultiLanguage, new GUIContent("是否支持多语言"));
////            EditorGUI.BeginDisabledGroup(spriteArray.arraySize == 0 || animated.boolValue);
////            spriteIndex.intValue = EditorGUILayout.IntSlider("图片Index", oldState, 0, spriteArray.arraySize - 1);
////            if (!needRefresh)
////                needRefresh = oldState != spriteIndex.intValue;
////            EditorGUI.EndDisabledGroup();
////            EditorGUILayout.PropertyField(spriteArray, new GUIContent("图片列表"));
////            if (serializedObject.hasModifiedProperties)
////            {
////                if (supportMultiLanguage.boolValue)
////                {
////                    spriteNameArray.ClearArray();
////                    for(int i = 0; i < spriteArray.arraySize; i++)
////                    {
////                        var sp = spriteArray.GetArrayElementAtIndex(i);
////                        var path = AssetDatabase.GetAssetPath(sp.objectReferenceValue);
////                        var name = System.IO.Path.GetFileName(path).ToLower();
////                        spriteNameArray.InsertArrayElementAtIndex(i);
////                        spriteNameArray.GetArrayElementAtIndex(i).stringValue = name;
////                    }
////                }
////                else
////                {
////                    spriteNameArray.ClearArray();
////                }
////            }
////
////            EditorGUILayout.PropertyField(animated, new GUIContent("序列帧动画"));
////            if (animated.boolValue)
////            {
////                EditorGUILayout.PropertyField(fps, new GUIContent("FPS帧率"));
////                EditorGUILayout.PropertyField(delay, new GUIContent("播放延迟"));
////                EditorGUILayout.Slider(playbackSpeed, 0.1f, 10f, new GUIContent("播放速度"));
////                playOnce.boolValue = !EditorGUILayout.Toggle("循环播放", !playOnce.boolValue); 
////            }
////
////            serializedObject.ApplyModifiedProperties();
////
////            if (needRefresh)
////            {
////                ((ImageEx)target).RefreshSpriteState();
////            }
////        }
////    }
////}
