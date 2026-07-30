//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    [AddComponentMenu("UI/Effects/MirrorNew")]
//    [RequireComponent(typeof(Image))]
//    public class MirrorNew : BaseMeshEffect
//    {
//        private enum MirrorTypes
//        {
//            Horizontal,
//            Vertical,
//            ClockWise,
//        }
//
//        [SerializeField] private MirrorTypes mirrorType;
//        [SerializeField] private bool isReverse;
//        [SerializeField] private bool keepImageSize;
//
//        private Image _image;
//        private RectTransform _rectTrans;
//
//        private RectTransform RectTrans
//        {
//            get
//            {
//                if (_rectTrans == null)
//                    _rectTrans = GetComponent<RectTransform>();
//                return _rectTrans;
//            }
//        }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            _image = GetComponent<Image>();
//            _rectTrans = GetComponent<RectTransform>();
//        }
//
//        public override void ModifyMesh(VertexHelper vh)
//        {
//            if (!IsActive() || vh.currentVertCount == 0)
//                return;
//            
//            List<UIVertex> originalVertices = new List<UIVertex>();
//            vh.GetUIVertexStream(originalVertices);
//            if (originalVertices.Count == 0) return;
//            
//            Rect rect = RectTrans.rect;
//            Vector3 originalCenter = rect.center;
//            float scaleRatio = GetScaleRatio();
//            Vector2 scaledSize = rect.size * scaleRatio;
//            
//            List<UIVertex> scaledOriginalVertices = ScaleVertices(originalVertices, originalCenter, scaleRatio);
//            
//            List<UIVertex> newVertices = new List<UIVertex>(scaledOriginalVertices);
//            
//            switch (mirrorType)
//            {
//                case MirrorTypes.Horizontal:
//                    CopyHorizontalVertices(scaledOriginalVertices, newVertices, originalCenter, scaledSize, isReverse);
//                    break;
//                case MirrorTypes.Vertical:
//                    CopyVerticalVertices(scaledOriginalVertices, newVertices, originalCenter, scaledSize, isReverse);
//                    break;
//                case MirrorTypes.ClockWise:
//                    CopyClockWiseVertices(scaledOriginalVertices, newVertices, originalCenter, scaledSize, isReverse);
//                    break;
//            }
//
//            vh.Clear();
//            vh.AddUIVertexTriangleStream(newVertices);
//        }
//        
//        private float GetScaleRatio()
//        {
//            if (keepImageSize) return 1f;
//            switch (mirrorType)
//            {
//                case MirrorTypes.Horizontal or MirrorTypes.Vertical:
//                    return 0.5f;
//                case MirrorTypes.ClockWise:
//                    return 0.25f;
//                default:
//                    return 1f;
//            }
//        }
//        
//        private List<UIVertex> ScaleVertices(List<UIVertex> vertices, Vector3 center, float scaleRatio)
//        {
//            List<UIVertex> scaledVertices = new List<UIVertex>();
//            foreach (UIVertex v in vertices)
//            {
//                UIVertex newV = v;
//                Vector3 offset = newV.position - center;
//                offset *= scaleRatio;
//                newV.position = center + offset;
//                scaledVertices.Add(newV);
//            }
//            return scaledVertices;
//        }
//        
//        private UIVertex RotateVertexByAngle(UIVertex vertex, Vector3 rotateCenter, float angle)
//        {
//            UIVertex newV = vertex;
//            Vector3 offset = newV.position - rotateCenter;
//            float rad = angle * Mathf.Deg2Rad;
//            float cos = Mathf.Cos(rad);
//            float sin = Mathf.Sin(rad);
//            float newX = offset.x * cos - offset.y * sin;
//            float newY = offset.x * sin + offset.y * cos;
//            newV.position = rotateCenter + new Vector3(newX, newY, offset.z);
//            return newV;
//        }
//        
//        private void CopyHorizontalVertices(List<UIVertex> scaledOriginal, List<UIVertex> newList, Vector3 center, Vector2 scaledSize, bool reverse)
//        {
//            float offsetX = reverse ? -scaledSize.x : scaledSize.x;
//            Vector3 copyCenter = new Vector3(center.x + offsetX, center.y, center.z);
//            CopyRotateAndOffsetVertices(scaledOriginal, newList, copyCenter, offsetX, 0, 180f);
//        }
//
//        private void CopyVerticalVertices(List<UIVertex> scaledOriginal, List<UIVertex> newList, Vector3 center, Vector2 scaledSize, bool reverse)
//        {
//            float offsetY = reverse ? -scaledSize.y : scaledSize.y;
//            Vector3 copyCenter = new Vector3(center.x, center.y + offsetY, center.z);
//            CopyRotateAndOffsetVertices(scaledOriginal, newList, copyCenter, 0, offsetY, 180f);
//        }
//        
//        private void CopyClockWiseVertices(List<UIVertex> scaledOriginal, List<UIVertex> newList, Vector3 center, Vector2 scaledSize, bool reverse)
//        {
//            float offsetSign = reverse ? -1f : 1f;
//            
//            float offsetX1 = scaledSize.x * offsetSign;
//            Vector3 copyCenter1 = new Vector3(center.x + offsetX1, center.y, center.z);
//            CopyRotateAndOffsetVertices(scaledOriginal, newList, copyCenter1, offsetX1, 0, 180f);
//            
//            float offsetY2 = scaledSize.y * offsetSign;
//            Vector3 copyCenter2 = new Vector3(center.x, center.y + offsetY2, center.z);
//            CopyRotateAndOffsetVertices(scaledOriginal, newList, copyCenter2, 0, offsetY2, 0);
//            
//            float offsetX3 = scaledSize.x * offsetSign;
//            float offsetY3 = scaledSize.y * offsetSign;
//            Vector3 copyCenter3 = new Vector3(center.x + offsetX3, center.y + offsetY3, center.z);
//            CopyRotateAndOffsetVertices(scaledOriginal, newList, copyCenter3, offsetX3, offsetY3, 180f);
//        }
//        
//        private void CopyRotateAndOffsetVertices(List<UIVertex> source, List<UIVertex> target, Vector3 rotateCenter, float offsetX, float offsetY, float rotateAngle)
//        {
//            foreach (UIVertex v in source)
//            {
//                UIVertex newV = v;
//                newV.position = new Vector3(newV.position.x + offsetX, newV.position.y + offsetY, newV.position.z);
//                newV = RotateVertexByAngle(newV, rotateCenter, rotateAngle);
//                target.Add(newV);
//            }
//        }
//    }
//}
