// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// ProgressBar UI 控件封装。
    /// 包装 Slider 或 Image（fill 模式），统一进度值设置。
    /// </summary>
    public class EmberUIProgressBar : EmberUIComponent
    {
        #region 内部参数

        private Slider _slider;
        private Image _fillImage;
        private bool _useSlider;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _slider = GetComponent<Slider>();
            if (_slider)
            {
                _useSlider = true;
                if (_slider.fillRect)
                    _fillImage = _slider.fillRect.GetComponent<Image>();
            }
            else
            {
                _fillImage = GetComponent<Image>();
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>进度值 (0-1)</summary>
        public float Value
        {
            get => _useSlider ? _slider.value : (_fillImage ? _fillImage.fillAmount : 0f);
            set
            {
                if (_useSlider) _slider.value = Mathf.Clamp01(value);
                else if (_fillImage) _fillImage.fillAmount = Mathf.Clamp01(value);
            }
        }

        /// <summary>是否使用 Slider 模式</summary>
        public bool IsSliderMode => _useSlider;

        /// <summary>Slider 引用（Slider 模式时非空）</summary>
        public Slider Slider => _slider;

        /// <summary>Fill Image 引用</summary>
        public Image FillImage => _fillImage;

        #endregion
    }
}
