//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.Sprites;
//using UnityEngine.UI;
//
//namespace Burner
//{
//    [AddComponentMenu("BurnerUI/CircleImage")]
//    public class CircleImage : Image
//    {
//        [SerializeField]
//        [Min(3)]
//        [Tooltip("圆形由多少块三角形拼成")]
//        private int m_Segements = 30;
//
//        [SerializeField]
//        [Range(0.0f, 1.0f)]
//        [Tooltip("显示部分占圆形的百分比")]
//        private float m_FillPercent = 1;
//
//        [SerializeField]
//        private Color32 m_GrayColor = new Color32(60, 60, 60, 255);
//
//        private Vector2 m_CenterPos;
//
//        private float m_RadiusSqrMagnitude;
//
//        protected override void OnPopulateMesh(VertexHelper vh)
//        {
//            vh.Clear();
//            AddVertex(vh);
//            AddTriangle(vh);
//        }
//
//        private void AddVertex(VertexHelper vh)
//        {
//            float width = rectTransform.rect.width;
//            float height = rectTransform.rect.height;
//            Vector4 uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;
//
//            float centerPos_x = (0.5f - rectTransform.pivot.x) * width;
//            float centerPos_y = (0.5f - rectTransform.pivot.y) * height;
//            float centerUV_x = (uv.x + uv.z) * 0.5f, centerUV_y = (uv.y + uv.w) * 0.5f;
//            Color32 grayColor = GetGrayColor();
//
//            // Add Vertex in CirCle Center
//            Vector2 vertexPos = new Vector2(centerPos_x, centerPos_y);
//            Color32 vertexColor = grayColor;
//            Vector2 vertexUV= new Vector2(centerUV_x, centerUV_y);
//            vh.AddVert(vertexPos, vertexColor, vertexUV);
//
//            m_CenterPos = vertexPos;
//
//            // Add Vertex in CirCle Edge
//            int vertexCount = m_Segements;
//            int filledVertexCount = (int)(m_Segements * m_FillPercent);
//            float radianStep = (2.0f * Mathf.PI) / m_Segements;
//            float curRadian = 0.0f;
//            float radius = width * 0.5f;
//            float uvScale_x = (uv.z - uv.x) / width, uvScale_y = (uv.w - uv.y) / height;
//            float posX = 0.0f, posY = 0.0f;
//            for (int i = 0; i < vertexCount; ++i)
//            {
//                posX = Mathf.Cos(curRadian) * radius;
//                posY = Mathf.Sin(curRadian) * radius;
//                curRadian += radianStep;
//
//                vertexPos.x = centerPos_x + posX;
//                vertexPos.y = centerPos_y + posY;
//                vertexColor = i < filledVertexCount ? (Color32)color : grayColor;
//                vertexUV.x = centerUV_x + posX * uvScale_x;
//                vertexUV.y = centerUV_y + posY * uvScale_y;
//                vh.AddVert(vertexPos, vertexColor, vertexUV);
//            }
//
//            m_RadiusSqrMagnitude = radius * radius;
//        }
//
//        private Color32 GetGrayColor()
//        {
//            Color32 colorTemp = (Color.white - m_GrayColor) * m_FillPercent;
//            return new Color32(
//                (byte)(m_GrayColor.r + colorTemp.r),
//                (byte)(m_GrayColor.g + colorTemp.g),
//                (byte)(m_GrayColor.b + colorTemp.b),
//                255);
//        }
//
//        private void AddTriangle(VertexHelper vh)
//        {
//            for (int id = 1; id <= m_Segements; ++id)
//            {
//                vh.AddTriangle(id, 0, id % m_Segements + 1);
//            }
//        }
//
//        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
//        {
//            Vector2 local;
//            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
//            {
//                return (local - m_CenterPos).sqrMagnitude <= m_RadiusSqrMagnitude;
//            }
//            return false;
//        }
//    }
//}
