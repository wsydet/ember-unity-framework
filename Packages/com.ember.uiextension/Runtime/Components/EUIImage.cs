// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// Image UI 控件封装。
    /// 包装 Unity Image，提供 Sprite / Color / Fill / Gray / KeepNativeSize 属性。
    /// 兼容 ImageEx（sprite 数组 + 帧动画）。
    /// </summary>
    public class EmberUIImage : EmberUIComponent
    {
        #region 内部参数

        private Image _image;
        private EmberImageEx _imageEx;
        private bool _keepNativeSize;
        private Color _cacheColor;

        private static Material _grayMaterial;
        private static Material GrayMat
        {
            get
            {
                if (!_grayMaterial)
                {
                    var shader = Shader.Find("UI/Gray");
                    if (shader) _grayMaterial = new Material(shader);
                }
                return _grayMaterial;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _image = GetComponent<Image>();
            _imageEx = _image as EmberImageEx;

            if (_imageEx)
                _keepNativeSize = _imageEx.KeepNativeSize;

            if (_image)
                _cacheColor = _image.color;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前 Sprite</summary>
        public Sprite Sprite
        {
            get => _image?.sprite;
            set
            {
                if (_image) _image.sprite = value;
                if (_keepNativeSize && _image) _image.SetNativeSize();
            }
        }

        /// <summary>顶点色</summary>
        public Color Color
        {
            get => _image ? _image.color : Color.white;
            set
            {
                if (_image && _image.color != value)
                {
                    _image.color = value;
                    _cacheColor = value;
                }
            }
        }

        /// <summary>填充模式下填充量 (0-1)</summary>
        public float FillAmount
        {
            get => _image ? _image.fillAmount : 0f;
            set { if (_image) _image.fillAmount = value; }
        }

        /// <summary>填充方向是否顺时针</summary>
        public bool FillClockwise
        {
            get => _image && _image.fillClockwise;
            set { if (_image) _image.fillClockwise = value; }
        }

        /// <summary>是否置灰</summary>
        public override void SetGray(bool gray)
        {
            if (!_image) return;
            _image.material = gray ? GrayMat : null;
        }

        /// <summary>保持原始尺寸（设置 Sprite 后自动 SetNativeSize）</summary>
        public bool KeepNativeSize
        {
            get => _keepNativeSize;
            set
            {
                _keepNativeSize = value;
                if (_imageEx) _imageEx.KeepNativeSize = value;
                else if (value && _image) _image.SetNativeSize();
            }
        }

        /// <summary>Image 可见性</summary>
        public override bool Enable
        {
            get => _image ? _image.enabled : base.Enable;
            set { if (_image) _image.enabled = value; }
        }

        /// <summary>是否可被遮罩</summary>
        public bool Maskable
        {
            get => _image && _image.maskable;
            set { if (_image) _image.maskable = value; }
        }

        /// <summary>ImageEx 精灵索引</summary>
        public int SpriteIndex
        {
            get => _imageEx ? _imageEx.SpriteIndex : 0;
            set { if (_imageEx) _imageEx.SpriteIndex = value; }
        }

        /// <summary>Unity Image 引用</summary>
        public Image UnityImage => _image;

        #endregion
    }
}
