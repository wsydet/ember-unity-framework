//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using System;
//
//namespace Burner.UIExtension
//{
//    public class AdvancedTextImage : MaskableGraphic
//    {
//        BetterList<UIVertex> vertexStream = new BetterList<UIVertex>();
//        Texture tex;
//        bool isDirty;
//        public Texture ImageTexture
//        {
//            get { return tex; }
//            set
//            {
//                if (tex != value)
//                {
//                    tex = value;
//                    SetMaterialDirty();
//                    SetVerticesDirty();
//                }
//            }
//        }
//
//        public override Texture mainTexture
//        {
//            get
//            {
//                return tex;
//            }
//        }
//        public BetterList<UIVertex> ImageVertices
//        {
//            set
//            {
//                vertexStream = value;
//                isDirty = true;
//            }
//        }
//
//        protected override void UpdateGeometry()
//        {
//            base.UpdateGeometry();
//        }
//
//        protected override void OnPopulateMesh(VertexHelper vh)
//        {
//            vh.Clear();
//            //AdvancedText.FillUIQuad(vh, vertexStream);
//            FillUIQuadWithOverlap(vh, vertexStream);
//        }
//
//        public float OverlapPixels { set; get; }// 设置重叠像素
//        void FillUIQuadWithOverlap(VertexHelper vh, BetterList<UIVertex> uivert)
//        {
//            int quadCount = uivert.size / 4;
//
//            for (int i = 0; i < quadCount; i++)
//            {
//                int startIndex = i * 4;
//                UIVertex v0 = uivert[startIndex];
//                UIVertex v1 = uivert[startIndex + 1];
//                UIVertex v2 = uivert[startIndex + 2];
//                UIVertex v3 = uivert[startIndex + 3];
//
//                if (i > 0)
//                {
//                    v0.position.x -= OverlapPixels * i;
//                    v1.position.x -= OverlapPixels * i;
//                    v2.position.x -= OverlapPixels * i;
//                    v3.position.x -= OverlapPixels * i;
//                }
//
//                vh.AddVert(v0);
//                vh.AddVert(v1);
//                vh.AddVert(v2);
//                vh.AddVert(v3);
//
//                int baseIndex = i * 4;
//                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
//                vh.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex);
//            }
//        }
//
//        void Update()
//        {
//            if (isDirty)
//            {
//                SetVerticesDirty();
//                isDirty = false;
//            }
//        }
//    }
//}
