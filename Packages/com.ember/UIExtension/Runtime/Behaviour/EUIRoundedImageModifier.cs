// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 圆角矩形 Image 修改器。
    /// 基于 <see cref="BaseMeshEffect"/>，将 Image（Simple 模式）的四个角裁剪为圆角。
    /// 不适用于 Sliced / Tiled 模式的图片。
    /// </summary>
    [AddComponentMenu("UI/EUI/Rounded Image Modifier")]
    [RequireComponent(typeof(Image))]
    public class EUIRoundedImageModifier : BaseMeshEffect
    {
        #region 编辑器面板参数

        [FoldoutGroup("圆角")]
        [SerializeField]
        [LabelText("圆角半径")]
        [Tooltip("四个角的圆角半径（像素）")]
        private float _radius = 10f;

        [FoldoutGroup("圆角")]
        [SerializeField]
        [Range(4, 16)]
        [LabelText("三角形数量")]
        [Tooltip("每个角的四分之一圆用多少个三角形逼近，数值越大圆角越平滑")]
        private int _triangleNum = 6;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private const int MaxTriangleNum = 16;
        private const int MinTriangleNum = 4;

        private Image TargetImage => graphic as Image;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>圆角半径（像素）</summary>
        public float Radius
        {
            get => _radius;
            set { _radius = value; graphic.SetVerticesDirty(); }
        }

        /// <summary>每个角的三角形数量</summary>
        public int TriangleNum
        {
            get => _triangleNum;
            set { _triangleNum = Mathf.Clamp(value, MinTriangleNum, MaxTriangleNum); graphic.SetVerticesDirty(); }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (TargetImage.type != Image.Type.Simple)
                return;

            Profiler.BeginSample("GenerateRoundedSimpleSprite");
            GenerateSimpleSprite(vh);
            Profiler.EndSample();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            var padding = TargetImage.overrideSprite == null
                ? Vector4.zero
                : DataUtility.GetPadding(TargetImage.overrideSprite);
            var r = TargetImage.GetPixelAdjustedRect();
            var size = TargetImage.overrideSprite == null
                ? new Vector2(r.width, r.height)
                : new Vector2(TargetImage.overrideSprite.rect.width, TargetImage.overrideSprite.rect.height);

            var spriteW = Mathf.RoundToInt(size.x);
            var spriteH = Mathf.RoundToInt(size.y);

            if (shouldPreserveAspect && size.sqrMagnitude > 0f)
            {
                var spriteRatio = size.x / size.y;
                var rectRatio = r.width / r.height;

                if (spriteRatio > rectRatio)
                {
                    var oldHeight = r.height;
                    r.height = r.width * (1f / spriteRatio);
                    r.y += (oldHeight - r.height) * TargetImage.rectTransform.pivot.y;
                }
                else
                {
                    var oldWidth = r.width;
                    r.width = r.height * spriteRatio;
                    r.x += (oldWidth - r.width) * TargetImage.rectTransform.pivot.x;
                }
            }

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            return new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w);
        }

        private void GenerateSimpleSprite(VertexHelper vh)
        {
            var v = GetDrawingDimensions(false);
            var uv = TargetImage.overrideSprite != null
                ? DataUtility.GetOuterUV(TargetImage.overrideSprite)
                : Vector4.zero;

            vh.Clear();
            var color32 = TargetImage.color;

            // 限制半径范围
            var radius = _radius;
            if (radius > (v.z - v.x) / 2) radius = (v.z - v.x) / 2;
            if (radius > (v.w - v.y) / 2) radius = (v.w - v.y) / 2;
            if (radius < 0) radius = 0;

            var uvRadiusX = radius / (v.z - v.x) * (uv.z - uv.x);
            var uvRadiusY = radius / (v.w - v.y) * (uv.w - uv.y);

            // 布局顶点 (0-11)
            vh.AddVert(new Vector3(v.x, v.w - radius), color32, new Vector2(uv.x, uv.w - uvRadiusY));
            vh.AddVert(new Vector3(v.x, v.y + radius), color32, new Vector2(uv.x, uv.y + uvRadiusY));
            vh.AddVert(new Vector3(v.x + radius, v.w), color32, new Vector2(uv.x + uvRadiusX, uv.w));
            vh.AddVert(new Vector3(v.x + radius, v.w - radius), color32, new Vector2(uv.x + uvRadiusX, uv.w - uvRadiusY));
            vh.AddVert(new Vector3(v.x + radius, v.y + radius), color32, new Vector2(uv.x + uvRadiusX, uv.y + uvRadiusY));
            vh.AddVert(new Vector3(v.x + radius, v.y), color32, new Vector2(uv.x + uvRadiusX, uv.y));
            vh.AddVert(new Vector3(v.z - radius, v.w), color32, new Vector2(uv.z - uvRadiusX, uv.w));
            vh.AddVert(new Vector3(v.z - radius, v.w - radius), color32, new Vector2(uv.z - uvRadiusX, uv.w - uvRadiusY));
            vh.AddVert(new Vector3(v.z - radius, v.y + radius), color32, new Vector2(uv.z - uvRadiusX, uv.y + uvRadiusY));
            vh.AddVert(new Vector3(v.z - radius, v.y), color32, new Vector2(uv.z - uvRadiusX, uv.y));
            vh.AddVert(new Vector3(v.z, v.w - radius), color32, new Vector2(uv.z, uv.w - uvRadiusY));
            vh.AddVert(new Vector3(v.z, v.y + radius), color32, new Vector2(uv.z, uv.y + uvRadiusY));

            // 三个矩形区域
            vh.AddTriangle(1, 0, 3);
            vh.AddTriangle(1, 3, 4);
            vh.AddTriangle(5, 2, 6);
            vh.AddTriangle(5, 6, 9);
            vh.AddTriangle(8, 7, 10);
            vh.AddTriangle(8, 10, 11);

            // 四个圆角
            var vCenterList = Ember.Basic.ListPool<Vector2>.Get();
            var uvCenterList = Ember.Basic.ListPool<Vector2>.Get();
            var vCenterVertList = Ember.Basic.ListPool<int>.Get();

            vCenterList.Add(new Vector2(v.z - radius, v.w - radius));
            uvCenterList.Add(new Vector2(uv.z - uvRadiusX, uv.w - uvRadiusY));
            vCenterVertList.Add(7);

            vCenterList.Add(new Vector2(v.x + radius, v.w - radius));
            uvCenterList.Add(new Vector2(uv.x + uvRadiusX, uv.w - uvRadiusY));
            vCenterVertList.Add(3);

            vCenterList.Add(new Vector2(v.x + radius, v.y + radius));
            uvCenterList.Add(new Vector2(uv.x + uvRadiusX, uv.y + uvRadiusY));
            vCenterVertList.Add(4);

            vCenterList.Add(new Vector2(v.z - radius, v.y + radius));
            uvCenterList.Add(new Vector2(uv.z - uvRadiusX, uv.y + uvRadiusY));
            vCenterVertList.Add(8);

            var degreeDelta = Mathf.PI / 2 / _triangleNum;
            var curDegree = 0f;

            for (int i = 0; i < vCenterVertList.Count; i++)
            {
                var preVertNum = vh.currentVertCount;
                for (int j = 0; j <= _triangleNum; j++)
                {
                    var cosA = Mathf.Cos(curDegree);
                    var sinA = Mathf.Sin(curDegree);
                    var vPos = new Vector3(vCenterList[i].x + cosA * radius, vCenterList[i].y + sinA * radius);
                    var uvPos = new Vector2(uvCenterList[i].x + cosA * uvRadiusX, uvCenterList[i].y + sinA * uvRadiusY);
                    vh.AddVert(vPos, color32, uvPos);
                    curDegree += degreeDelta;
                }
                curDegree -= degreeDelta;
                for (int j = 0; j <= _triangleNum - 1; j++)
                {
                    vh.AddTriangle(vCenterVertList[i], preVertNum + j + 1, preVertNum + j);
                }
            }

            Ember.Basic.ListPool<Vector2>.Return(vCenterList);
            Ember.Basic.ListPool<Vector2>.Return(uvCenterList);
            Ember.Basic.ListPool<int>.Return(vCenterVertList);
        }

        #endregion
    }
}
