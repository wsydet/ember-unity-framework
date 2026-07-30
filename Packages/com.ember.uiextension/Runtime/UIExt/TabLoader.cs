//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class TabLoader : MonoBehaviour, IBindlessUIBehaviour
//    {
//        [Serializable]
//        public struct TabInfo
//        {
//            public string PrefabName;
//            public Vector2 AnchoredPosition;
//            public Vector2 AnchorMin;
//            public Vector2 AnchorMax;
//            public Vector2 SizeDelta;
//            public Vector2 Pivot;
//        }
//        [SerializeField]
//        GameObject[] tabPages;
//        [SerializeField]
//        TabInfo[] tabPageInfos;
//        [SerializeField]
//        bool prefabMode;
//
//        public GameObject[] TabPages => tabPages;
//        public TabInfo[] TabPageInfos => tabPageInfos;
//
//        public bool PrefabMode => prefabMode;
//
//        public GameUILogic BindingLogic { get; set; }
//
//        public GameUIComponent CreateUIComponent()
//        {
//            return new GameTabLoader();
//        }
//    }
//}
