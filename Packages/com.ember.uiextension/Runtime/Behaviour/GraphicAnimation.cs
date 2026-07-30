//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.UI;
//
//namespace Burner
//{
//    [ExecuteInEditMode]
//    [DisallowMultipleComponent]
//    [RequireComponent(typeof(Graphic), typeof(MeshRenderer))]
//    public class GraphicAnimation : MonoBehaviour
//    {
//        private Graphic graphic;
//        [SerializeField]
//        [HideInInspector]
//        private Material m_Material;
//        private Material _mat;
//        public Material material
//        {
//            set
//            {
//                if (m_Material == value)
//                    return;
//                m_Material = value;
//                Bind();
//            }
//            get
//            {
//                return m_Material;
//            }
//        }
//
//        private Renderer m_Renderer;
//
//        private MaterialPropertyBlock m_MPB;
//
//        [SerializeField]
//        public AnimatableProperty[] m_AnimatedProperties = new AnimatableProperty[0];
//#if UNITY_EDITOR
//        public AnimationClip[] m_Clip;
//#endif
//
//        private void Awake()
//        {
//            graphic = GetComponent<Graphic>();
//            m_Renderer = GetComponent<Renderer>();
//            m_Renderer.enabled = false;
//        }
//        public void OnEnable()
//        {
//#if UNITY_EDITOR
//            if(graphic == null)
//            {
//                graphic = GetComponent<Graphic>();
//                m_Renderer = GetComponent<Renderer>();
//                m_Renderer.enabled = false;
//            }
//            if (m_Material == null && graphic.material != null && !graphic.material.name.Contains("Clone"))
//                material = graphic.material;
//            else
//#endif
//            Bind();
//        }
//
//        public bool IsValidmatMaterial(Material mat)
//        {
//            return mat == _mat;
//        }
//        public void Bind()
//        {
//            if (_mat != null)
//#if UNITY_EDITOR
//                Object.DestroyImmediate(_mat);
//#else
//                    Object.Destroy(_mat);
//#endif
//            if (m_Material == null)
//                _mat = null;
//            else
//                _mat = Instantiate(m_Material);
//            graphic.material = _mat;
//            m_Renderer.sharedMaterial = _mat;
//        }
//
//        private void OnDestroy()
//        {
//            if (_mat != null)
//            {
//#if UNITY_EDITOR
//                GameObject.DestroyImmediate(_mat);
//#else
//                GameObject.Destroy(_mat);
//#endif
//            }
//        }
//
//        public void Update()
//        {
//            //graphic.material = m_Renderer.sharedMaterial;
//#if UNITY_EDITOR
//            if (graphic == null)
//                return;
//#endif
//
//            if (graphic == null)
//                return;
//            if (m_MPB == null)
//                m_MPB = new MaterialPropertyBlock();
//            m_Renderer.GetPropertyBlock(m_MPB);
//            foreach (var prop in m_AnimatedProperties)
//            {
//                CopyMaterialPropery(graphic.materialForRendering, ref m_MPB, prop);
//            }
//            graphic.SetMaterialDirty();
//        }
//
//        private void CopyMaterialPropery(Material mat, ref MaterialPropertyBlock mpb, AnimatableProperty property)
//        {
//            var id = property.id;
//            switch (property.type)
//            {
//                case ShaderPropertyType.Color:
//                    mat.SetColor(id, mpb.GetColor(id));
//                    break;
//                case ShaderPropertyType.Vector:
//                    mat.SetVector(id, mpb.GetVector(id));
//                    break;
//                case ShaderPropertyType.Float:
//                case ShaderPropertyType.Range:
//                    mat.SetFloat(id, mpb.GetFloat(id));
//                    break;
//                case ShaderPropertyType.Texture:
//                    mat.SetTexture(id, mpb.GetTexture(id));
//                    break;
//
//            }
//        }
//    }
//}
