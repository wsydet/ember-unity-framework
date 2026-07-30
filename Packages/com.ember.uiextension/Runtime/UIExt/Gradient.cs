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
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    [AddComponentMenu("UI/Effects/Gradient")]
//    public class Gradient : BaseMeshEffect
//    {
//        [SerializeField]
//        private bool fourColors;
//        [SerializeField]
//        private bool isLeft2Right;
//        [SerializeField]
//        private Color32 topColor = Color.white;
//        [SerializeField]
//        private Color32 topRightColor = Color.white;
//        [SerializeField]
//        private Color32 bottomColor = Color.black;
//        [SerializeField]
//        private Color32 bottomRightColor = Color.black;
//
//        public bool IsFourColors { get { return fourColors; } set { fourColors = value; } }
//
//        public bool IsLeftToRight { get { return isLeft2Right; } set { isLeft2Right = value; } }
//        public Color32 TopColor { get { return topColor; } set { topColor = value; } }
//
//        public Color32 TopRightColor { get { return topRightColor; } set { topRightColor = value; } }
//        public Color32 BottomColor { get { return bottomColor; } set { bottomColor = value; } }
//
//        public Color32 BottomRightColor { get { return bottomRightColor; } set { bottomRightColor = value; } }
//        public override void ModifyMesh(VertexHelper vh)
//        {
//            if (!IsActive())
//            {
//                return;
//            }
//            List<UIVertex> vertexList = new List<UIVertex>();
//
//            vh.GetUIVertexStream(vertexList);
//
//            if (graphic is Image img)
//            {
//                switch (img.type)
//                {
//                    //只单独处理九宫格，其他格式统一用Simple
//                    case Image.Type.Sliced:
//                        SlicedChangeColor(vertexList);
//                        break;
//                    default:
//                        SimpleChangeColor(vertexList);
//                        break;
//
//                }
//            }
//            else
//            {
//                SimpleChangeColor(vertexList);
//            }
//            vh.Clear();
//            vh.AddUIVertexTriangleStream(vertexList);
//        }
//        
//        private void SimpleChangeColor(List<UIVertex> vertexList)
//        {
//            var colors = GetColors();
//            for (int i = 0; i < vertexList.Count && vertexList.Count - i >= 6;)
//            {
//                ChangeColorImpl(vertexList, i, colors[0], colors[1], colors[2], colors[3]);
//                i += 6;
//            }
//        }
//
//        //九宫格颜色处理
//        private void SlicedChangeColor(List<UIVertex> vertexList)
//        {
//            var boundRect = GetBoundRect(vertexList);
//            var colors = GetColors();
//            
//            //九宫格，每格6个点，共54点。从左下角顺时针绘制
//                //6个点有两对是重合的，左下(0,5) 左上(1) 右上(2,3) 右下(4) 
//            for (int i = 0; i < vertexList.Count && vertexList.Count - i >= 6;)
//            {
//                ChangeColorImpl(vertexList, i, 
//                    GetLerpColor(colors, boundRect, vertexList[i].position),
//                    GetLerpColor(colors, boundRect, vertexList[i + 1].position),
//                    GetLerpColor(colors, boundRect, vertexList[i + 2].position),
//                    GetLerpColor(colors, boundRect, vertexList[i + 4].position));
//                i += 6;
//            }
//        }
//
//        //计算graphic内部的顶点的颜色，做两次lerp计算
//        private Color32 GetLerpColor(Color32[] colors, Rect boundRect, Vector2 pos)
//        {
//            var xLerp = (pos.x - boundRect.x) / boundRect.width;
//            var xTopColor = Color32.Lerp(colors[1], colors[2], xLerp);
//            var xBottomColor = Color32.Lerp(colors[0], colors[3], xLerp);
//            return Color32.Lerp(xBottomColor, xTopColor, (pos.y - boundRect.y) / boundRect.height);
//        }
//
//        //获取这个graphic的范围
//        private Rect GetBoundRect(List<UIVertex> vertexList)
//        {
//            var leftBottomPos = Vector2.zero;
//            var rightTopPos = Vector2.zero;
//            var firstFlag = false;
//            foreach (var vertex in vertexList)
//            {
//                if (!firstFlag)
//                {
//                    firstFlag = true;
//                    rightTopPos = leftBottomPos = vertex.position;
//                    continue;
//                }
//
//                if (vertex.position.x <= leftBottomPos.x && vertex.position.y <= leftBottomPos.y)
//                {
//                    leftBottomPos = vertex.position;
//                    continue;
//                }
//
//                if (vertex.position.x >= rightTopPos.x && vertex.position.y >= rightTopPos.y)
//                {
//                    rightTopPos = vertex.position;
//                    continue;
//                }
//            }
//
//            return new Rect(leftBottomPos.x, leftBottomPos.y, rightTopPos.x - leftBottomPos.x,
//                rightTopPos.y - leftBottomPos.y);
//        }
//
//        //分别给六个索引(四个顶点)指定颜色
//        private void ChangeColorImpl(List<UIVertex> vertexList, int i,
//            Color32 perBottomColor, Color32 perTopColor, Color32 perTopRightColor, Color32 perBottomRightColor)
//        {
//            //索引法
//            //6个点有两对是重合的，左下(0,5) 左上(1) 右上(2,3) 右下(4)  
//            ChangeColor(vertexList, i, perBottomColor);
//            ChangeColor(vertexList, i + 1, perTopColor);
//            ChangeColor(vertexList, i + 2, perTopRightColor);
//            ChangeColor(vertexList, i + 3, perTopRightColor);
//            ChangeColor(vertexList, i + 4, perBottomRightColor);
//            ChangeColor(vertexList, i + 5, perBottomColor);
//        }
//
//        //计算出四个角的颜色，依次是左下、左上、右上、右下
//        private Color32[] GetColors()
//        {
//            var ret = new Color32[4];
//            if (isLeft2Right && !IsFourColors)
//            {
//                ret[0] = topColor;
//                ret[1] = topColor;
//                ret[2] = topRightColor;
//                ret[3] = topRightColor;
//            }
//            else
//            {
//                ret[0] = bottomColor;
//                ret[1] = topColor;
//                ret[2] = fourColors ? topRightColor : topColor;
//                ret[3] = fourColors ? bottomRightColor : bottomColor;
//            }
//
//            return ret;
//        }
//
//        private void ChangeColor(List<UIVertex> verList, int index, Color color)
//        {
//            UIVertex temp = verList[index];
//            temp.color *= color;
//            verList[index] = temp;
//        }
//    }
//}
