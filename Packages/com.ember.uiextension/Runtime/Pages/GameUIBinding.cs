//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    [DisallowMultipleComponent]
//    public class GameUIBinding : MonoBehaviour
//    {
//        public enum WidgetTypes
//        {
//            Component = 0,
//            Text,
//            Toggle,
//            Button,
//            ProgressBar,
//            Image,
//            UIContainer,
//            UILogic,
//            InputField,
//            ToggleGroup,
//            ScrollRect,
//            RawImage,
//            Canvas,
//            TabLoader,
//            //新加类型一定要写在这行上面，以免影响序列化信息
//            //新加类型一定要写在这行上面，以免影响序列化信息
//            //新加类型一定要写在这行上面，以免影响序列化信息
//            //新加类型一定要写在这行上面，以免影响序列化信息
//            End,
//            Extension = 65535,
//        }
//        [Serializable]
//        public struct BindingEntry
//        {
//            public string Name;
//            public GameObject GameObject;
//            public WidgetTypes Type;
//            public string ClassName;
//        }
//        [SerializeField]
//        private string pageName;
//        [SerializeField]
//        private string classPath;
//        [SerializeField]
//        private string className;
//        [SerializeField]
//        private bool isPage;
//        [SerializeField]
//        private WidgetTypes selfWidgetType;
//        [SerializeField]
//        private string selfWidgetClassName;
//        [SerializeField]
//        private BindingEntry[] bindings;
//        [SerializeField]
//        private bool noCodeGen;
//        [SerializeField]
//        private string baseBindingUUID;
//        [SerializeField] 
//        private PageFlags pageFlags;
//
//        public BindingEntry[] Bindings => bindings;
//
//        public WidgetTypes SelfWidgetType => selfWidgetType;
//
//        public string SelfWidgetClassName => selfWidgetClassName;
//
//        public bool IsPage => isPage;
//
//        public string PageName => pageName;
//
//        public string ClassName => className;
//
//        public string ClassPath => classPath;
//
//        public bool NoCodeGeneration => noCodeGen;
//        
//        public PageFlags PageFlags => pageFlags;
//    }
//}
