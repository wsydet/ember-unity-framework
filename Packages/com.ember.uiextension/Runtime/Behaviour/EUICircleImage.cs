// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 圆形/环形 Image。
    /// 继承自 <see cref="Image"/>，重写 OnPopulateMesh 用三角形拼接圆形。
    /// 支持 FillPercent 显示百分比（环形进度条效果）和圆形精确 Raycast。
    /// </summary>
    [AddComponentMenu("UI/Ember/Circle Image")]
    public class EmberCircleImage : Image
    {
        #region 编辑器面板参数

        [FoldoutGroup("圆形设置")]
        [SerializeField]
        [Min(3)]
        [LabelText("分段数")]
        [Tooltip("圆形由多少块三角形拼成，数值越大边缘越平滑")]
        private int _segments = 30;

        [FoldoutGroup("圆形设置")]
        [SerializeField]
        [Range(0f, 1f)]
        [LabelText("填充百分比")]
        [Tooltip("显示部分占圆形的比例，1 = 完整圆形，0.5 = 半圆")]
        private float _fillPercent = 1f;

        [FoldoutGroup("圆形设置")]
        [SerializeField]
        [LabelText("未填充颜色")]
        [Tooltip("FillPercent 之外区域的灰度颜色")]
        private Color32 _unfilledColor = new Color32(60, 60, 60, 255);

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private Vector2 _centerPos;
        private float _radiusSqrMagnitude;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>圆形分段数（三角形数量）</summary>
        public int Segments
        {
            get => _segments;
            set { _segments = Mathf.Max(3, value); SetVerticesDirty(); }
        }

        /// <summary>填充百分比 0-1</summary>
        public float FillPercent
        {
            get => _fillPercent;
            set { _fillPercent = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        /// <summary>圆形精确 Raycast：只有点在圆形内部才命中</summary>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
            {
                return (local - _centerPos).sqrMagnitude <= _radiusSqrMagnitude;
            }
            return false;
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            AddVertex(vh);
            AddTriangle(vh);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void AddVertex(VertexHelper vh)
        {
            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;
            var uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;

            var centerX = (0.5f - rectTransform.pivot.x) * width;
            var centerY = (0.5f - rectTransform.pivot.y) * height;
            var centerUVX = (uv.x + uv.z) * 0.5f;
            var centerUVY = (uv.y + uv.w) * 0.5f;
            var unfilledColor = GetUnfilledColor();

            // 圆心顶点
            var vertexPos = new Vector2(centerX, centerY);
            vh.AddVert(vertexPos, unfilledColor, new Vector2(centerUVX, centerUVY));
            _centerPos = vertexPos;

            // 圆周顶点
            var vertexCount = _segments;
            var filledVertexCount = (int)(_segments * _fillPercent);
            var radianStep = 2f * Mathf.PI / _segments;
            var curRadian = 0f;
            var radius = width * 0.5f;
            var uvScaleX = (uv.z - uv.x) / width;
            var uvScaleY = (uv.w - uv.y) / height;

            for (int i = 0; i < vertexCount; ++i)
            {
                var posX = Mathf.Cos(curRadian) * radius;
                var posY = Mathf.Sin(curRadian) * radius;
                curRadian += radianStep;

                vertexPos.x = centerX + posX;
                vertexPos.y = centerY + posY;
                var vertColor = i < filledVertexCount ? (Color32)color : unfilledColor;
                var vertUV = new Vector2(centerUVX + posX * uvScaleX, centerUVY + posY * uvScaleY);
                vh.AddVert(vertexPos, vertColor, vertUV);
            }

            _radiusSqrMagnitude = radius * radius;
        }

        private Color32 GetUnfilledColor()
        {
            var colorTemp = (Color.white - _unfilledColor) * _fillPercent;
            return new Color32(
                (byte)(_unfilledColor.r + colorTemp.r),
                (byte)(_unfilledColor.g + colorTemp.g),
                (byte)(_unfilledColor.b + colorTemp.b),
                255);
        }

        private void AddTriangle(VertexHelper vh)
        {
            for (int id = 1; id <= _segments; ++id)
            {
                vh.AddTriangle(id, 0, id % _segments + 1);
            }
        }

        #endregion
    }
}
