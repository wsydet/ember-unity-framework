// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Ember.UIExtension
{
    /// <summary>
    /// Text/TMP UI 控件封装。
    /// 自动识别 Text 或 TextMeshProUGUI，统一提供 Text / Color / FontSize 属性。
    /// </summary>
    public class EUIText : EUIComponent
    {
        #region 内部参数

        private Text _text;
        private TextMeshProUGUI _tmp;
        private bool _isLegacyText;
        private Color _cacheColor;
        private string _txt;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _text = GetComponent<Text>();
            _tmp = GetComponent<TextMeshProUGUI>();

            if (_text)
            {
                _isLegacyText = true;
                _cacheColor = _text.color;
            }
            else if (_tmp)
            {
                _isLegacyText = false;
                _cacheColor = _tmp.color;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>文本内容</summary>
        public virtual string Text
        {
            get => _isLegacyText ? _text?.text : _tmp?.text;
            set
            {
                if (value == _txt) return;
                _txt = value;

                if (_isLegacyText && _text)
                    _text.text = value;
                else if (_tmp)
                    _tmp.text = value;
            }
        }

        /// <summary>字体颜色</summary>
        public Color Color
        {
            get => _isLegacyText ? _text.color : _tmp.color;
            set
            {
                _cacheColor = value;
                if (_isLegacyText && _text) _text.color = value;
                else if (_tmp) _tmp.color = value;
            }
        }

        /// <summary>字体尺寸</summary>
        public float FontSize
        {
            get => _isLegacyText ? _text.fontSize : _tmp.fontSize;
            set
            {
                if (_isLegacyText && _text) _text.fontSize = (int)value;
                else if (_tmp) _tmp.fontSize = value;
            }
        }

        /// <summary>TMP 首选宽度</summary>
        public float PreferredWidth => _tmp ? _tmp.preferredWidth : 0f;

        /// <summary>TMP 首选高度</summary>
        public float PreferredHeight => _tmp ? _tmp.preferredHeight : 0f;

        /// <summary>TMP 总行数</summary>
        public int TotalLines
        {
            get
            {
                if (_tmp)
                {
                    _tmp.ForceMeshUpdate();
                    return _tmp.textInfo?.lineCount ?? 0;
                }
                return 0;
            }
        }

        /// <summary>是否为 TMP 文本</summary>
        public bool IsTMP => !_isLegacyText;

        /// <summary>TMP 引用（只读）</summary>
        public TextMeshProUGUI TMP => _tmp;

        #endregion
    }
}
