// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 顶点色渐变组件。
    /// 基于 <see cref="BaseMeshEffect"/>，为 Graphic（Text / Image / RawImage）的顶点着色，
    /// 支持上下渐变、左右渐变、四角独立颜色。兼容九宫格（Sliced）图片。
    /// </summary>
    [AddComponentMenu("UI/Effects/EUI Gradient")]
    public class EUIGradient : BaseMeshEffect
    {
        #region 编辑器面板参数

        [FoldoutGroup("渐变模式")]
        [SerializeField]
        [LabelText("四色模式")]
        [Tooltip("启用后四个角可独立设置颜色；关闭时只用 TopColor + BottomColor")]
        private bool _fourColors;

        [FoldoutGroup("渐变模式")]
        [SerializeField]
        [LabelText("水平渐变")]
        [Tooltip("启用后从左到右渐变（仅非四色模式有效）；关闭时从上到下渐变")]
        private bool _isLeftToRight;

        [FoldoutGroup("颜色")]
        [SerializeField]
        [LabelText("上 / 左")]
        private Color32 _topColor = Color.white;

        [FoldoutGroup("颜色")]
        [SerializeField]
        [ShowIf("_fourColors")]
        [LabelText("右上")]
        private Color32 _topRightColor = Color.white;

        [FoldoutGroup("颜色")]
        [SerializeField]
        [LabelText("下 / 右")]
        private Color32 _bottomColor = Color.black;

        [FoldoutGroup("颜色")]
        [SerializeField]
        [ShowIf("_fourColors")]
        [LabelText("右下")]
        private Color32 _bottomRightColor = Color.black;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>是否启用四角独立颜色模式</summary>
        public bool IsFourColors
        {
            get => _fourColors;
            set => _fourColors = value;
        }

        /// <summary>是否水平渐变（仅非四色模式有效）</summary>
        public bool IsLeftToRight
        {
            get => _isLeftToRight;
            set => _isLeftToRight = value;
        }

        /// <summary>顶部 / 左侧颜色</summary>
        public Color32 TopColor
        {
            get => _topColor;
            set => _topColor = value;
        }

        /// <summary>右上角颜色（仅四色模式）</summary>
        public Color32 TopRightColor
        {
            get => _topRightColor;
            set => _topRightColor = value;
        }

        /// <summary>底部 / 右侧颜色</summary>
        public Color32 BottomColor
        {
            get => _bottomColor;
            set => _bottomColor = value;
        }

        /// <summary>右下角颜色（仅四色模式）</summary>
        public Color32 BottomRightColor
        {
            get => _bottomRightColor;
            set => _bottomRightColor = value;
        }

        /// <summary>重写 BaseMeshEffect，在顶点着色阶段注入渐变颜色。</summary>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            var vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);

            if (graphic is Image img && img.type == Image.Type.Sliced)
                SlicedChangeColor(vertexList);
            else
                SimpleChangeColor(vertexList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>获取四个角的颜色：左下、左上、右上、右下</summary>
        private Color32[] GetColors()
        {
            var ret = new Color32[4];

            if (_isLeftToRight && !_fourColors)
            {
                ret[0] = _topColor;
                ret[1] = _topColor;
                ret[2] = _topRightColor;
                ret[3] = _topRightColor;
            }
            else
            {
                ret[0] = _bottomColor;
                ret[1] = _topColor;
                ret[2] = _fourColors ? _topRightColor : _topColor;
                ret[3] = _fourColors ? _bottomRightColor : _bottomColor;
            }

            return ret;
        }

        /// <summary>普通模式：每个 quad（6 顶点）使用统一四角色</summary>
        private void SimpleChangeColor(List<UIVertex> vertexList)
        {
            var colors = GetColors();
            for (int i = 0; i < vertexList.Count && vertexList.Count - i >= 6;)
            {
                ChangeQuadColors(vertexList, i, colors[0], colors[1], colors[2], colors[3]);
                i += 6;
            }
        }

        /// <summary>九宫格模式：每个内部顶点的颜色在两个方向做双线性插值</summary>
        private void SlicedChangeColor(List<UIVertex> vertexList)
        {
            var boundRect = GetBoundRect(vertexList);
            var colors = GetColors();

            for (int i = 0; i < vertexList.Count && vertexList.Count - i >= 6;)
            {
                ChangeQuadColors(vertexList, i,
                    GetLerpColor(colors, boundRect, vertexList[i].position),
                    GetLerpColor(colors, boundRect, vertexList[i + 1].position),
                    GetLerpColor(colors, boundRect, vertexList[i + 2].position),
                    GetLerpColor(colors, boundRect, vertexList[i + 4].position));
                i += 6;
            }
        }

        /// <summary>根据顶点在边界矩形中的位置，对四个角色做双线性插值</summary>
        private static Color32 GetLerpColor(Color32[] colors, Rect boundRect, Vector2 pos)
        {
            var xLerp = (pos.x - boundRect.x) / boundRect.width;
            var xTopColor = Color32.Lerp(colors[1], colors[2], xLerp);
            var xBottomColor = Color32.Lerp(colors[0], colors[3], xLerp);
            return Color32.Lerp(xBottomColor, xTopColor, (pos.y - boundRect.y) / boundRect.height);
        }

        /// <summary>计算顶点列表的包围矩形</summary>
        private static Rect GetBoundRect(List<UIVertex> vertexList)
        {
            var leftBottomPos = Vector2.zero;
            var rightTopPos = Vector2.zero;
            var firstFlag = false;

            foreach (var vertex in vertexList)
            {
                if (!firstFlag)
                {
                    firstFlag = true;
                    rightTopPos = leftBottomPos = vertex.position;
                    continue;
                }

                if (vertex.position.x <= leftBottomPos.x && vertex.position.y <= leftBottomPos.y)
                    leftBottomPos = vertex.position;
                else if (vertex.position.x >= rightTopPos.x && vertex.position.y >= rightTopPos.y)
                    rightTopPos = vertex.position;
            }

            return new Rect(leftBottomPos.x, leftBottomPos.y,
                rightTopPos.x - leftBottomPos.x, rightTopPos.y - leftBottomPos.y);
        }

        /// <summary>
        /// 为 quad 的 6 个顶点（索引 i 到 i+5）分别着色。
        /// 顶点布局：左下(0,5) 左上(1) 右上(2,3) 右下(4)
        /// </summary>
        private static void ChangeQuadColors(List<UIVertex> verList, int i,
            Color32 bottomLeft, Color32 topLeft, Color32 topRight, Color32 bottomRight)
        {
            SetVertexColor(verList, i, bottomLeft);
            SetVertexColor(verList, i + 1, topLeft);
            SetVertexColor(verList, i + 2, topRight);
            SetVertexColor(verList, i + 3, topRight);
            SetVertexColor(verList, i + 4, bottomRight);
            SetVertexColor(verList, i + 5, bottomLeft);
        }

        private static void SetVertexColor(List<UIVertex> verList, int index, Color32 color)
        {
            var temp = verList[index];
            temp.color = (Color)temp.color * (Color)color;
            verList[index] = temp;
        }

        #endregion
    }
}
