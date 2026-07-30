//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.Rendering;
//using UnityEngine.Serialization;
//
//namespace UnityEngine.UI
//{
//    [AddComponentMenu("UI/LWSoftMask", 13)]
//    [ExecuteAlways]
//    [RequireComponent(typeof(RectTransform))]
//    [DisallowMultipleComponent]
//    /// <summary>
//    /// A component for masking children elements.
//    /// </summary>
//    /// <remarks>
//    /// By using this element any children elements that have masking enabled will mask where a sibling Graphic would write 0 to the stencil buffer.
//    /// </remarks>
//    public class LWSoftMask : UIBehaviour, IMaterialModifier
//    {
//
//        [SerializeField]
//        Texture2D maskTexture;
//
//        [SerializeField, Range(0, 1)]
//        float maskThreshold = 0.5f;
//
//        public Texture2D MaskTexture
//        {
//            get { return maskTexture; }
//            set
//            {
//                maskTexture = value;
//                RefrashMaterialParameter();
//            }
//        }
//
//        public float MaskThreshold
//        {
//            get { return maskThreshold; }
//            set
//            {
//                maskThreshold = value;
//                RefrashMaterialParameter();
//            }
//        }
//
//        Graphic[] childGraphics = null;
//
//        protected override void OnEnable()
//        {
//            RefrashChildrenGraphicsList();
//        }
//
//        protected override void OnDisable()
//        {
//            foreach (var graphic in childGraphics)
//            {
//                graphic.materialModifierForRendering = null;
//            }
//        }
//
//        public virtual Material GetModifiedMaterial(Material baseMaterial)
//        {
//            Material newMaterial = new Material(baseMaterial);
//
//            newMaterial.EnableKeyword("SOFT_MASK_ON");
//
//            string xx = "";
//            foreach (var s in newMaterial.shaderKeywords)
//            {
//                xx += s;
//                xx += "\n";
//            }
//
//            Debug.LogError(xx);
//
//            var worldCorners = new Vector3[4];
//            (transform as RectTransform).GetWorldCorners(worldCorners);
//
//            var v = new Vector4(worldCorners[0].x, worldCorners[0].y, worldCorners[2].x, worldCorners[2].y);
//
//            newMaterial.EnableKeyword("SOFT_MASK_ON");
//            newMaterial.SetTexture("_SoftMaskTex", maskTexture);
//            newMaterial.SetVector("_SoftMaskRect", v); ;
//            newMaterial.SetFloat("_SoftMaskThreshold", maskThreshold);
//            newMaterial.name = newMaterial.name + "[Soft Mask]";
//            return newMaterial;
//        }
//
//        private void RefrashChildrenGraphicsList()
//        {
//            childGraphics = GetComponentsInChildren<Graphic>();
//
//            foreach (var graphic in childGraphics)
//            {
//                graphic.materialModifierForRendering = this;
//            }
//        }
//
//        private void RefrashMaterialParameter()
//        {
//            if (childGraphics == null)
//                return;
//
//            foreach (var graphic in childGraphics)
//            {
//                graphic.SetMaterialDirty();
//            }
//        }
//        
//        //大小变化时被触发
//        protected override void OnRectTransformDimensionsChange()
//        {
//            RefrashMaterialParameter();
//        }
//        
//        protected void OnTransformChildrenChanged()
//        {
//            RefrashChildrenGraphicsList();
//        }
//
//        protected void LateUpdate()
//        {
//            if (childGraphics == null)
//                return;
//
//            RefrashMaterialParameter();
//        }
//    }
//}
