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
//    public class SoftMask : MonoBehaviour
//    {
//        [SerializeField]
//        Texture2D maskTexture;
//
//        [SerializeField, Range(0, 1)]
//        float maskThreshold = 0.5f;
//
//        [SerializeField]
//        Vector3 rectMaskParam = new Vector3(0.4f, 0.4f, 0.6f);
//
//        RawImage mRawImage;
//
//        bool mSoftMaskMode = false;
//        bool mExceptSoftMaskMode = false;
//
//        List<RectTransform> mSelfList;
//        NodePostProcessManager.NodeListStageInfo mNodeListStageInfo;
//
//        Material mSoftMaskMaterial;
//
//        int MaskTexShaderId = Shader.PropertyToID("_MaskTex");
//        int MaskThresholdShaderId = Shader.PropertyToID("_MaskThreshold");
//        int RectMaskParamShaderId = Shader.PropertyToID("_RectMaskParam");
//
//        public Material SoftMaskMaterial
//        {
//            get
//            {
//                if (mSoftMaskMaterial == null)
//                    mSoftMaskMaterial = GetDefaultMaskMaterial();
//
//                return mSoftMaskMaterial;
//            }
//
//            set
//            {
//                mSoftMaskMaterial = value;
//                if (mSoftMaskMaterial == null)
//                    mSoftMaskMaterial = GetDefaultMaskMaterial();
//            }
//        }
//
//        Material GetDefaultMaskMaterial()
//        {
//            Shader shader = Shader.Find("UI/SoftMaskShader");
//            Material material = new Material(shader);
//            return material;
//        }
//
//        void EnsureRawImage()
//        {
//            if(mRawImage == null)
//            {
//                Transform parent = transform.parent;
//
//                GameObject rawImageGameObject = new GameObject("SoftMaskRawImage");
//                RectTransform rectTransform = rawImageGameObject.AddComponent<RectTransform>();
//                rectTransform.parent = parent;
//                rectTransform.localScale = Vector3.one;
//                rectTransform.localPosition = Vector3.zero;
//
//                mRawImage = rawImageGameObject.AddComponent<RawImage>();
//                mRawImage.raycastTarget = false;
//                
//                SoftMaskDisableListener softMaskDisableListener = rawImageGameObject.AddComponent<SoftMaskDisableListener>();
//                softMaskDisableListener.softMask = this;
//
//                rawImageGameObject.SetActive(false);
//            }
//        }
//
//        void EnsureSelfList()
//        {
//            if (mSelfList == null)
//            {
//                mSelfList = new List<RectTransform>();
//                mSelfList.Add(transform as RectTransform);
//            }
//        }
//
//        private void OnEnable()
//        {
//            // initialize the soft mask camera and render target in next frame
//            SetSoftMaskMode(true);
//        }
//
//        public void SetSoftMaskMode(bool enable)
//        {
//            mExceptSoftMaskMode = enable;
//        }
//
//        // called by SoftMaskDisableListener
//        internal void LateUpdate()
//        {
//            if (mExceptSoftMaskMode)
//                EnableSoftMaskInternal();
//            else
//                DisableSoftMaskInternal();
//        }
//        
//        // called by SoftMaskDisableListener
//        internal void OnDestroyImpl()
//        {
//            if(mNodeListStageInfo != null && mNodeListStageInfo.IsValid)
//            {
//                NodePostProcessManager.DisableNodePostProcess(mSelfList, mRawImage, mNodeListStageInfo);
//                mNodeListStageInfo = null;
//            }
//        }
//
//        internal void EnableSoftMaskInternal()
//        {
//            if (mSoftMaskMode)
//                return;
//
//            if(mNodeListStageInfo == null)
//            {
//                EnsureRawImage();
//                EnsureSelfList();
//
//                mRawImage.material = SoftMaskMaterial;
//                mRawImage.materialForRendering.SetTexture(MaskTexShaderId, maskTexture);
//                mRawImage.materialForRendering.SetFloat(MaskThresholdShaderId, maskThreshold);
//                mRawImage.materialForRendering.SetVector(RectMaskParamShaderId, rectMaskParam);
//
//                Rect screenShotRect = Rect.zero;
//                NodePostProcessManager.EnableNodePostProcess(mSelfList, ref screenShotRect, mRawImage, out mNodeListStageInfo);
//            }
//            else
//            {
//                mNodeListStageInfo.mCameraGameObject.SetActive(true);
//            }
//
//            mSoftMaskMode = true;
//        }
//
//        internal void DisableSoftMaskInternal()
//        {
//            if (mSoftMaskMode == false)
//                return;
//            
//            mNodeListStageInfo?.mCameraGameObject.SetActive(false);
//
//            mSoftMaskMode = false;
//        }
//    }
//}
