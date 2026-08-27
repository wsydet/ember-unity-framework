//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//
//using UnityEngine;
//
//using UnityEngine.UI;
//using TMPro;
//
//namespace Burner.UIExtension
//{
//    public class GameText : GameUIComponent
//    {
//        Text label;
//        TextMeshProUGUI textMeshPro;
//#if LOVE_ENGINE_MULTILANGUAGE
//        MultiLanguageTextBase multiLanguage;
//#endif
//        LayoutElement layout;
//        TMPMarquee marquee;
//        bool marqueeEnabled = false;
//        Color cacheColor;
//        string txt;
//        bool cbAdded;
//        private bool isText = false;
//
//        public bool ShouldEscapeReturn { get; set; }
//
//        TMPMarquee Marquee
//        {
//            get
//            {
//                if (!marquee)
//                {
//                    if (!textMeshPro)
//                        throw new System.NotSupportedException("Only TextmeshPro supports marquee");
//                    marquee = GetComponent<TMPMarquee>();
//                    if (!marquee)
//                        marquee = gameObject.AddComponent<TMPMarquee>();
//                }
//
//                return marquee;
//            }
//        }
//
//        public override void OnInit()
//        {
//            base.OnInit();
//            label = GetComponent<Text>();
//            textMeshPro = GetComponent<TextMeshProUGUI>();
//            if (label)
//            {
//                isText = true;
//                cacheColor = label.color;
//                layout = GetComponent<LayoutElement>();
//            }
//            else if (textMeshPro)
//            {
//                isText = false;
//                cacheColor = textMeshPro.color;
//                ShouldEscapeReturn = textMeshPro.parseCtrlCharacters;
//            }
//#if LOVE_ENGINE_MULTILANGUAGE
//            multiLanguage = GetComponent<MultiLanguageTextBase>();
//#endif
//        }
//
//        /// <summary>
//        /// 获取或设置文本框的文字
//        /// </summary>
//        public virtual string Text
//        {
//            set
//            {
//                if (!label && !textMeshPro)
//                    return;
//                if (value != txt)
//                {
//                    //如果找不到可匹配的目标，value会原封不动返回，不会产生额外的gc
//                    if (ShouldEscapeReturn && !string.IsNullOrEmpty(value))
//                        txt = value.Replace("\\n", "\n");
//                    else
//                        txt = value;
//#if LOVE_ENGINE_MULTILANGUAGE
//                    if (multiLanguage && !cbAdded)
//                    {
//                        cbAdded = true;
//                        multiLanguage.setText(GetTextForMultilanguage);
//                    }
//#endif
//                    if (isText)
//                    {
//                        label.text = txt;
//                        if (layout)
//                        {
//                            layout.enabled = false;
//                            layout.enabled = true;
//                        }
//                    }
//                    else
//                    {
//                        textMeshPro.text = txt;
//                    }
//                }
//            }
//            get
//            {
//                if (isText)
//                    return label.text;
//                else if (textMeshPro != null)
//                    return textMeshPro.text;
//                else
//                    return "";
//            }
//        }
//
//        /// <summary>
//        /// 获取或设置字体尺寸
//        /// </summary>
//        public int FontSize
//        {
//            get { return isText ? label.fontSize : (int)textMeshPro.fontSize; }
//            set
//            {
//                if (isText)
//                    label.fontSize = value;
//                else
//                    textMeshPro.fontSize = value;
//            }
//        }
//
//        // /// <summary>
//        /// 获得文本总行数
//        /// </summary>
//        public int TotalLines
//        {
//            get
//            {
//                if (textMeshPro != null)
//                {
//                    textMeshPro.ForceMeshUpdate();
//                    if (textMeshPro.textInfo != null)
//                        return textMeshPro.textInfo.lineCount;
//                }
//                return 0;
//            }
//        }
//
//        /// <summary>
//        /// 裁切文本最大行数
//        /// </summary>
//        public void StripTextWithLineCount(int count, bool ellipsis = false)
//        {
//            if (textMeshPro != null)
//            {
//                var textInfo = textMeshPro.GetTextInfo(textMeshPro.text);
//                if (textInfo.lineCount <= count) return;
//                var lastLine = textInfo.lineInfo[count - 1];
//                if (ellipsis)
//                {
//                    float ellipsisWidth = textMeshPro.GetPreferredValues("...").x;
//                    
//                    if (lastLine.length < textMeshPro.rectTransform.rect.width - ellipsisWidth)
//                        textMeshPro.text = string.Format("{0}...",
//                            textMeshPro.text.Substring(0, textInfo.characterInfo[lastLine.lastCharacterIndex + 1].index).Trim());
//                    else
//                    {
//                        int lastIndex =  lastLine.lastCharacterIndex;
//                        float lineWidth = 0;
//                        while (lineWidth < ellipsisWidth && lastIndex != 0)
//                        {
//                            var charInfo = textInfo.characterInfo[lastIndex];
//                            lineWidth += (charInfo.topRight - charInfo.topLeft).x;
//                            lastIndex--;
//                        }
//                       textMeshPro.text = string.Format("{0}...",textMeshPro.text.Substring(0, textInfo.characterInfo[lastIndex + 1].index));
//                    }
//                }
//                else
//                {
//                    textMeshPro.text = textMeshPro.text.Substring(0, textInfo.characterInfo[lastLine.lastCharacterIndex + 1].index).Trim();
//                }
//            }
//        }
//
//
//
//        /// <summary>
//        /// 获取或设置字体颜色
//        /// </summary>
//        public Color Color
//        {
//            get { return isText ? label.color : textMeshPro.color; }
//            set
//            {
//                cacheColor = value;
//                if (isText)
//                    label.color = value;
//                else
//                    textMeshPro.color = value;
//            }
//        }
//
//        string GetTextForMultilanguage()
//        {
//            return txt;
//        }
//
//        public void SetMarqueeEnabled(bool enabled, float speed = 10)
//        {
//            if (marqueeEnabled != enabled)
//            {
//                marqueeEnabled = enabled;
//                Marquee.SetEnable(enabled, speed);
//            }
//        }
//
//        public float PreferredWidth => textMeshPro.preferredWidth;
//        public float PreferredHeight => textMeshPro.preferredHeight;
//    }
//}
