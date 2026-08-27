////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEditor;
////using Burner.UIExtension;
////using UnityEngine;
////using UnityEngine.UI;
////
////namespace Burner
////{
////    public static class BurnerScrollViewEditor
////    {
////        [MenuItem("GameObject/Burner UI/Scroll View", false, 2000)]
////        static public void AddScrollView(MenuCommand menuCommand)
////        {
////            Vector2 size = new Vector2(200, 300);
////            Vector2 topPivot = new Vector2(0, 1);
////            var parent = menuCommand.context as GameObject;
////            if (parent == null)
////            {
////                parent = Selection.activeGameObject;
////            }
////
////            GameObject scrollView = new GameObject("Scroll View");
////            var scrollViewTransform = scrollView.AddComponent<RectTransform>();
////            GameObjectUtility.SetParentAndAlign(scrollView, parent);
////            scrollViewTransform.sizeDelta = size;
////            scrollViewTransform.pivot = scrollViewTransform.anchorMin = scrollViewTransform.anchorMax = topPivot;
////            scrollViewTransform.localScale = Vector3.one;
////            scrollViewTransform.anchoredPosition = Vector2.zero;
////
////            var raycastImage = scrollView.AddComponent<Image>();
////            raycastImage.color = new Color(1, 1, 1, 0);
////
////            var scrollRect = scrollView.AddComponent<ScrollRect>();
////            scrollRect.horizontal = false;
////            scrollRect.vertical = true;
////            scrollRect.movementType = ScrollRect.MovementType.Elastic;
////            scrollRect.scrollSensitivity = 1f;
////
////            GameObject viewport = new GameObject("Viewport");
////            var viewportTransform = viewport.AddComponent<RectTransform>();
////            viewportTransform.SetParent(scrollViewTransform, false);
////            viewportTransform.anchorMin = Vector2.zero;
////            viewportTransform.anchorMax = Vector2.one;
////            viewportTransform.pivot = topPivot;
////            viewportTransform.sizeDelta = Vector2.zero;
////            viewportTransform.anchoredPosition = Vector2.zero;
////
////            viewport.AddComponent<RectMask2D>();
////
////            GameObject content = new GameObject("m_Content");
////            var contentTransform = content.AddComponent<RectTransform>();
////            contentTransform.SetParent(viewportTransform, false);
////            contentTransform.anchorMin = new Vector2(0, 1);
////            contentTransform.anchorMax = Vector2.one;
////            contentTransform.pivot = new Vector2(0.5f, 1);
////            contentTransform.sizeDelta = Vector2.zero;
////            contentTransform.anchoredPosition = Vector2.zero;
////
////            var layout = content.AddComponent<VerticalLayoutGroup>();
////            layout.childAlignment = TextAnchor.UpperCenter;
////            layout.childControlWidth = true;
////            layout.childControlHeight = true;
////            layout.childForceExpandWidth = true;
////            layout.childForceExpandHeight = false;
////
////            var fitter = content.AddComponent<ContentSizeFitter>();
////            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
////            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
////
////            var container = content.AddComponent<UIContainer>();
////
////            GameObject template = new GameObject("Template");
////            var templateTransform = template.AddComponent<RectTransform>();
////            templateTransform.SetParent(contentTransform, false);
////            templateTransform.anchorMin = new Vector2(0, 1);
////            templateTransform.anchorMax = Vector2.one;
////            templateTransform.pivot = new Vector2(0.5f, 1);
////            templateTransform.sizeDelta = new Vector2(0, 80);
////            templateTransform.anchoredPosition = Vector2.zero;
////
////            var templateImage = template.AddComponent<Image>();
////            templateImage.color = new Color(1, 1, 1, 0.15f);
////
////            var templateLayout = template.AddComponent<LayoutElement>();
////            templateLayout.preferredHeight = 80;
////
////            var containerObject = new SerializedObject(container);
////            containerObject.FindProperty("templateNode").objectReferenceValue = template;
////            containerObject.FindProperty("templateType").enumValueIndex = (int)GameUIBinding.WidgetTypes.Component;
////            containerObject.FindProperty("templateClassName").stringValue = string.Empty;
////            containerObject.ApplyModifiedPropertiesWithoutUndo();
////
////            scrollRect.viewport = viewportTransform;
////            scrollRect.content = contentTransform;
////
////            SetLayerRecursively(scrollView, LayerMask.NameToLayer("UI"));
////
////            Undo.RegisterCreatedObjectUndo(scrollView, "Create Scroll View");
////            Selection.activeObject = scrollView;
////        }
////
////        static void SetLayerRecursively(GameObject go, int layer)
////        {
////            if (layer < 0)
////            {
////                return;
////            }
////
////            go.layer = layer;
////            foreach (Transform child in go.transform)
////            {
////                SetLayerRecursively(child.gameObject, layer);
////            }
////        }
////
////    }
////}
