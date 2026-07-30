//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.Rendering;
//
//namespace Burner
//{
//
//    [System.Serializable]
//    public class AnimatableProperty
//    {
//        // Must sync with UnityEditor.ShaderUtil.ShaderPropertyType
//       /* public enum ShaderPropertyType
//        {
//            Color,
//            Vector,
//            Float,
//            Range,
//            Texture
//        };*/
//
//        [SerializeField]
//        public string m_Name = string.Empty;
//
//        [SerializeField]
//        public ShaderPropertyType m_Type = ShaderPropertyType.Vector;
//
//        private int _id = -1;
//        public int id
//        {
//            get
//            {
//                if(_id <= 0)
//                    _id = Shader.PropertyToID(m_Name);
//                return _id;
//            }
//        }
//        public ShaderPropertyType type => m_Type;
//    }
//}
