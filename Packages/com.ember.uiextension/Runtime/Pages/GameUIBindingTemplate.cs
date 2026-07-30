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
//    public class GameUIBindingTemplate : ScriptableObject
//    {
//        [Serializable]
//        public struct BindingEntry
//        {
//            public string Name;
//            public string GameObjectPath;
//            public GameUIBinding.WidgetTypes Type;
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
//        private GameUIBinding.WidgetTypes selfWidgetType;
//        [SerializeField]
//        private string selfWidgetClassName;
//        [SerializeField]
//        private bool noCodeGen;
//        [SerializeField]
//        private BindingEntry[] bindings;
//
//        public BindingEntry[] Bindings => bindings;
//
//        public GameUIBinding.WidgetTypes SelfWidgetType => selfWidgetType;
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
//        public void CopyFromUIBinding(GameUIBinding binding)
//        {
//            noCodeGen = binding.NoCodeGeneration;
//            pageName = binding.PageName;
//            isPage = binding.IsPage;
//            classPath = binding.ClassPath;
//            className = binding.ClassName;
//            selfWidgetType = binding.SelfWidgetType;
//            selfWidgetClassName = binding.SelfWidgetClassName;
//
//            bindings = new BindingEntry[binding.Bindings.Length];
//            for(int i = 0; i < bindings.Length; i++)
//            {
//                var bd = binding.Bindings[i];
//                bindings[i] = BindingEntryToTemplate(bd, binding.gameObject);
//            }
//        }
//
//        public static BindingEntry BindingEntryToTemplate(GameUIBinding.BindingEntry bd, GameObject baseObj)
//        {
//            BindingEntry entry = new BindingEntry();
//            entry.ClassName = bd.ClassName;
//            entry.Name = bd.Name;
//            entry.Type = bd.Type;
//            entry.GameObjectPath = GetPathForObject(bd.GameObject, baseObj);
//            return entry;
//        }
//
//        internal static string GetPathForObject(GameObject target, GameObject relativeTo)
//        {
//            if (!target)
//                return null;
//            Transform endT = relativeTo.transform;
//            Transform cur = target.transform;
//            string res = null;
//
//            while(cur && cur != endT)
//            {
//                if (string.IsNullOrEmpty(res))
//                    res = cur.name;
//                else
//                    res = cur.name + "/" + res;
//                cur = cur.parent;
//            }
//            return res;
//        }
//    }
//}
