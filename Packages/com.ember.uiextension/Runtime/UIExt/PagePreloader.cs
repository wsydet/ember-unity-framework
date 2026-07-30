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
//    public class PagePreloader : MonoBehaviour, IBindlessUIBehaviour
//    {
//        public enum ComponentTypes
//        {
//            Unknown,
//            Image,
//            RawImage
//        }
//        [Serializable]
//        public struct PreloadInfo
//        {
//            public Component Target;
//            public ComponentTypes Type;
//            public bool HasMultipleSprites;
//            public string[] AssetsToLoad;
//        }
//
//        [SerializeField]
//        PreloadInfo[] _preloadInfos;
//
//        public PreloadInfo[] PreloadInfos
//        {
//            get { return _preloadInfos; }
//            set { _preloadInfos = value; }
//        }
//
//        public GameUILogic BindingLogic { get; set; }
//
//        public GameUIComponent CreateUIComponent()
//        {
//            return new GamePagePreloader();
//        }
//    }
//}
