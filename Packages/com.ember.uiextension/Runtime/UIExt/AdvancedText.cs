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
//    static class VertCacheGlobalData
//    {
//        private static Dictionary<ImageFont, Dictionary<string, List<UIVertex>>> GlobalCacheDic =
//            new Dictionary<ImageFont, Dictionary<string, List<UIVertex>>>();
//
//        public static bool TryGetCacheDic(ImageFont imageFont, ref Dictionary<string, List<UIVertex>> dic)
//        {
//            if (imageFont == null)
//                return false;
//
//            if (GlobalCacheDic.TryGetValue(imageFont, out dic))
//            {
//                return true;
//            }
//
//            dic = new Dictionary<string, List<UIVertex>>();
//            GlobalCacheDic.Add(imageFont, dic);
//
//            return true;
//        }
//    }
//    /// <summary>
//    /// 图文混排
//    /// </summary>
//    [AddComponentMenu("UI/Advanced Text", 11)]
//    public class AdvancedText : Text, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
//    {
//        VertexHelper _toFill;
//        [SerializeField]
//        ImageFont m_ImageFont;
//        [SerializeField]
//        string originalText;
//        [SerializeField]
//        float imageSize = 0;
//        [SerializeField]
//        float overlapPixels = 0;
//        [SerializeField]
//        bool isPureEmoji;
//        [SerializeField]
//        bool _raycastTarget;
//
//        [System.NonSerialized]
//        AdvancedTextImage m_CachedInputRenderer;
//        [System.NonSerialized]
//        RectTransform caretRectTrans;
//        //[System.NonSerialized]
//        //UIVertex[] m_TempVerts = new UIVertex[4];
//        [System.NonSerialized]
//        ReplacementInfo info = default(ReplacementInfo);
//        [System.NonSerialized]
//        RectTransform cachedTransform;
//
//        private HrefText m_hrefText;
//        bool isTextDirty = true;
//        BetterList<UIVertex> vertImg = new BetterList<UIVertex>();
//        BetterList<UIVertex> vertTxt = new BetterList<UIVertex>();
//        Dictionary<string, List<UIVertex>> vertCache;
//
//        private Action<string> m_hrefClickEvent;
//        Vector2 lastExtents;
//        bool isVerticisDirty = false;
//        RectTransform CachedRectTransform
//        {
//            get
//            {
//                if (!cachedTransform)
//                    cachedTransform = gameObject.GetComponent<RectTransform>();
//
//                return cachedTransform;
//            }
//        }
//
//        public ImageFont ImageFont
//        {
//            get { return m_ImageFont; }
//            set
//            {
//                m_ImageFont = value;
//                isTextDirty = true;
//                SetVerticesDirty();
//                SetLayoutDirty();
//
//                if (isPureEmoji)
//                {
//                    VertCacheGlobalData.TryGetCacheDic(m_ImageFont, ref vertCache);
//                }
//                //引擎里面m_Text未赋值就已经布局了, 在赋值的时候重新调用一次
//                GenerateText();
//                CheckImage();                
//            }
//        }
//
//        public bool IgnoreVertexCache { get; set; }
//
//        public override string text
//        {
//            get
//            {
//                return originalText;
//            }
//            set
//            {
//                if (string.IsNullOrEmpty(value))
//                {
//                    if (string.IsNullOrEmpty(originalText))
//                        return;
//                    originalText = "";
//                    isTextDirty = true;
//                    SetVerticesDirty();
//                    SetLayoutDirty();
//                    GenerateText();
//                }
//                else if (originalText != value)
//                {
//                    originalText = value;
//                    isTextDirty = true;
//                    SetVerticesDirty();
//                    SetLayoutDirty();
//                    GenerateText();
//                }
//            }
//        }
//
//        public bool RayCastTarget
//        {
//            get { return _raycastTarget; }
//            set
//            {  
//                _raycastTarget = value;
//                CheckImage();
//                if (m_CachedInputRenderer)
//                {
//                    m_CachedInputRenderer.raycastTarget = value;
//                }
//            }
//        }
//
//        protected override void Start()
//        {
//            base.Start();
//            isTextDirty = true;
//            CheckImage();
//        }
//
//        void CheckImage()
//        {
//            //if (Application.isPlaying)
//            {
//                if (m_CachedInputRenderer == null)
//                {
//                    GameObject go = new GameObject("Images");
//                    go.hideFlags = HideFlags.DontSave;
//                    go.transform.SetParent(transform);
//                    go.transform.SetAsFirstSibling();
//                    go.layer = gameObject.layer;
//                    caretRectTrans = go.AddComponent<RectTransform>();
//                    caretRectTrans.anchorMin = Vector2.zero;
//                    caretRectTrans.anchorMax = Vector2.one;
//                    caretRectTrans.pivot = CachedRectTransform.pivot;
//                    caretRectTrans.anchoredPosition3D = Vector3.zero;
//                    caretRectTrans.sizeDelta = Vector2.zero;
//                    caretRectTrans.localScale = Vector2.one;
//                    m_CachedInputRenderer = go.AddComponent<AdvancedTextImage>();
//                    m_CachedInputRenderer.raycastTarget = _raycastTarget;
//                    var cr = m_CachedInputRenderer.GetComponent<CanvasRenderer>();
//                    if (!cr)
//                        go.AddComponent<CanvasRenderer>();
//                    // Needed as if any layout is present we want the caret to always be the same as the text area.
//                    //go.AddComponent<LayoutElement>().ignoreLayout = true;
//                }
//                base.raycastTarget = false;
//                if (m_ImageFont != null)
//                {
//                    m_CachedInputRenderer.ImageTexture = m_ImageFont.Texture;
//                }
//            }
//        }
//
//        void GenerateText()
//        {
//            bool regenText = isTextDirty || lastExtents != rectTransform.rect.size;
//            if (isTextDirty)
//            {
//                isTextDirty = false;
//                if (m_hrefText == null)
//                {
//                    m_hrefText = new HrefText();
//                }
//                if (info.OriginalText != originalText)
//                {
//                    m_hrefText.getHrefList.Clear();
//                    string m_outText = m_hrefText.getResolveText(originalText);
//
//                    if (m_ImageFont)
//                        info = m_ImageFont.ReplaceText(m_outText, imageSize, isPureEmoji);
//                    else
//                    {
//                        return;
//                    }
//                }
//                m_Text = info.ReplacedText;
//            }
//            if (info.Symbols == null)
//                return;
//            if (regenText)
//            {
//                lastExtents = rectTransform.rect.size;
//                List<UIVertex> textVertList = null;
//                List<bool> charValidList = null;
//                if (!isPureEmoji || IgnoreVertexCache || vertCache == null || !vertCache.TryGetValue(info.CacheKey, out textVertList))
//                {
//                    textVertList = new List<UIVertex>();
//                    charValidList = new List<bool>();
//                    OnTextFillVBO(textVertList, charValidList);
//
//                    if (!string.IsNullOrEmpty(m_Text) && textVertList.Count == 0)
//                        return;
//
//                    // 处理超链接包含的点击区域
//                    foreach (var hrefInfo in m_hrefText.getHrefList)
//                    {
//                        hrefInfo.boxes.Clear();
//                        if (hrefInfo.startIndex >= textVertList.Count)
//                        {
//                            continue;
//                        }
//
//                        // 将超链接里面的文本顶点索引坐标加入到点击区域内
//                        var pos = textVertList[hrefInfo.startIndex];
//                        var bounds = new Bounds(pos.position, Vector3.zero);
//                        for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
//                        {
//                            if (i >= textVertList.Count)
//                            {
//                                break;
//                            }
//
//                            pos = textVertList[i];
//                            if (pos.position.x < bounds.min.x) // 换行重新添加点击区域
//                            {
//                                hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
//                                bounds = new Bounds(pos.position, Vector3.zero);
//                            }
//                            else
//                            {
//                                bounds.Encapsulate(pos.position); //再次扩展范围
//                            }
//                        }
//                        hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
//                    }
//
//                    if (isPureEmoji && !IgnoreVertexCache)
//                    {
//                        if (vertCache == null)
//                            vertCache = new Dictionary<string, List<UIVertex>>();
//                        vertCache[info.CacheKey] = textVertList;
//                    }
//                }
//
//                Vector3 offset = new Vector3(0, -fontSize / 4f, 0);
//                vertImg.Clear();
//                vertTxt.Clear();
//
//#if UNITY_2019_1_OR_NEWER
//
//                int spriteIndex = 0;
//                int charIndex = 0;
//                
//                if (isPureEmoji)
//                {
//                    //只有图片的情况
//                    int imageCount = textVertList.Count / 4;
//                    for(int i = 0; i < imageCount; i ++)
//                    {
//                        if (spriteIndex >= info.Symbols.Length)
//                        {
//                            Burner.Logger.Warn($"[AdvancedText]纯图片模式下Error。含有不在Imagefont内的字符/文本宽高小于实际显示宽高, text:{text}");
//                            continue;
//                        }
//                        SymbolSpriteInfo spriteInfo = info.Symbols[spriteIndex];
//                        for (int j = 0; j < 4; j++)
//                        {
//                            UIVertex vert = new UIVertex();
//                            UIVertex old = textVertList[4 * i + j];
//
//                            vert.position = old.position + offset;
//                            vert.uv0 = spriteInfo.UV[j];
//                            vert.normal = old.normal;
//                            vert.tangent = old.tangent;
//                            Color32 col = new Color32(255, 255, 255, 255);
//                            col.a = old.color.a;
//                            vert.color = col;
//                            vertImg.Add(vert);
//                        }
//                        spriteIndex++;
//                    }
//                }
//                else {
//                    bool needInc = charValidList.Count == textVertList.Count / 4;
//                    for (int i = 0; i < charValidList.Count - 1; i++)
//                    {
//                        if (charValidList[i] == false)
//                        {
//                            if (needInc)
//                                charIndex++;
//                            continue;
//                        }
//                        bool charText = true;
//                        if (info.isText != null)
//                        {
//                            if (info.isText[i] == 0)
//                            {
//                                if (needInc)
//                                    charIndex++;
//                                continue;
//                            }
//                            charText = info.isText[i] == 1;
//                        }
//
//                        if (charText)
//                        {
//                            vertTxt.Add(textVertList[charIndex * 4]);
//                            vertTxt.Add(textVertList[charIndex * 4 + 1]);
//                            vertTxt.Add(textVertList[charIndex * 4 + 2]);
//                            vertTxt.Add(textVertList[charIndex * 4 + 3]);
//                        }
//                        else
//                        {
//                            SymbolSpriteInfo spriteInfo = info.Symbols[spriteIndex];
//                            for (int j = 0; j < 4; j++)
//                            {
//                                UIVertex vert = new UIVertex();
//                                UIVertex old = textVertList[4 * charIndex + j];
//
//                                vert.position = old.position + offset;
//                                vert.uv0 = spriteInfo.UV[j];
//                                vert.normal = old.normal;
//                                vert.tangent = old.tangent;
//                                Color32 col = new Color32(255, 255, 255, 255);
//                                col.a = old.color.a;
//                                vert.color = col;
//                                vertImg.Add(vert);
//                            }
//                            spriteIndex++;
//
//                        }
//
//                        charIndex++;
//                    }
//                }
//#else
//                int newIdx = 0;
//                int skip = 0;
//#if !UNITY_5_2
//                int totalSkip = 0;
//#endif
//                SymbolSpriteInfo lastSprite = default(SymbolSpriteInfo);
//                int counter = 0;
//                bool isLastSkip = false;
//
//
//                for (int i = 0; i < textVertList.Count; i++)
//                {
//                    if (skip > 0)
//                    {
//#if UNITY_5_2
//                        if (skip <= 4 && skip > 0)
//#else
//                        int processed = totalSkip - skip;
//                        if (processed < 4)
//#endif
//                        {
//                            UIVertex vert = new UIVertex();
//                            UIVertex old = textVertList[i];
//#if UNITY_5_2
//                            int idx = 4 - skip;
//#else
//                            int idx = processed;
//#endif
//                            vert.position = old.position + offset;
//                            vert.uv0 = lastSprite.UV[idx];
//                            vert.normal = old.normal;
//                            vert.tangent = old.tangent;
//                            Color32 col = new Color32(255, 255, 255, 255);
//                            col.a = old.color.a;
//                            vert.color = col;
//                            vertImg.Add(vert);
//                            /*m_TempVerts[idx] = vert;
//                            if (idx == 3)
//                            {                                
//                                vhImg.AddUIVertexQuad(m_TempVerts);
//                            }*/
//                            counter++;
//                        }
//                        skip--;
//                        continue;
//                    }
//                    if (newIdx < info.Symbols.Length)
//                    {
//                        int idx = counter / 4;
//                        lastSprite = info.Symbols[newIdx];
//                        if (idx == lastSprite.Index)
//                        {
//#if UNITY_5_2
//                            skip = info.ReplacedSpriteLength * 4 - 1;
//#else
//                            totalSkip = info.ReplacedSpriteLength * 4;
//                            i--;
//                            skip = totalSkip;
//#endif
//                            newIdx++;
//                            continue;
//                        }
//                    }
//                    int tmpIdx = counter % 4;
//                    if (tmpIdx == 0)
//                    {
//                        //filter junk vertices
//                        if (textVertList[i].position == textVertList[i + 1].position)
//                        {
//                            isLastSkip = true;
//                        }
//                        else
//                            isLastSkip = false;
//                    }
//                    if (!isLastSkip)
//                        vertTxt.Add(textVertList[i]);
//                    /*m_TempVerts[tmpIdx] = textVertList[i];
//                    if (tmpIdx == 3)
//                    {
//                        if (_toFill != null)
//                        {
//                            _toFill.AddUIVertexQuad(m_TempVerts);
//                        }
//                    }*/
//                    counter++;
//                }
//#endif
//
//            }
//            else
//            {
//                Color32 col = color;
//                for (int i = 0; i < vertImg.size; i++)
//                {
//                    var vert = vertImg[i];
//                    vert.color = new Color32(255, 255, 255, col.a);
//                    vertImg[i] = vert;
//                }
//                for (int i = 0; i < vertTxt.size; i++)
//                {
//                    var vert = vertTxt[i];
//                    vert.color = col;
//                    vertTxt[i] = vert;
//                }
//            }
//            if (_toFill != null)
//                FillUIQuad(_toFill, vertTxt);
//            if (m_CachedInputRenderer)
//            {
//                m_CachedInputRenderer.OverlapPixels = overlapPixels;
//                m_CachedInputRenderer.ImageVertices = vertImg;
//            }
//        }
//
//        internal static void FillUIQuad(VertexHelper vh, BetterList<UIVertex> uivert)
//        {
//            int startIndex = 0;
//            for (int i = 0; i < uivert.size; i++)
//            {
//                if (i % 4 == 0)
//                {
//                    startIndex = ((i / 4) - 1) * 4;
//                    if (startIndex >= 0)
//                    {
//                        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
//                        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
//                    }
//                }
//                vh.AddVert(uivert[i]);                
//            }
//
//            //last quad
//            startIndex = ((uivert.size / 4) - 1) * 4;
//            if (startIndex >= 0)
//            {
//                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
//                vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
//            }
//        }
//
//        protected override void OnPopulateMesh(VertexHelper toFill)
//        {
//            isVerticisDirty = false;
//            _toFill = toFill;
//            _toFill.Clear();
//            GenerateText();      
//        }
//
//        void OnTextFillVBO(List<UIVertex> vbo, List<bool> charValid)
//        {
//            if (font == null)
//                return;
//
//            // We dont care if we the font Texture changes while we are doing our Update.
//            // The end result of cachedTextGenerator will be valid for this instance.
//            // Otherwise we can get issues like Case 619238.
//            m_DisableFontTextureRebuiltCallback = true;
//
//            Vector2 extents = rectTransform.rect.size;
//
//            var settings = GetGenerationSettings(extents);
//            cachedTextGenerator.Populate(m_Text, settings);
//
//            Rect inputRect = rectTransform.rect;
//
//            // get the text alignment anchor point for the text in local space
//            Vector2 textAnchorPivot = GetTextAnchorPivot(alignment);
//            Vector2 refPoint = Vector2.zero;
//            refPoint.x = (textAnchorPivot.x == 1 ? inputRect.xMax : inputRect.xMin);
//            refPoint.y = (textAnchorPivot.y == 0 ? inputRect.yMin : inputRect.yMax);
//
//            // Determine fraction of pixel to offset text mesh.
//            Vector2 roundingOffset = PixelAdjustPoint(refPoint) - refPoint;
//
//            // Apply the offset to the vertices
//            IList<UIVertex> verts = cachedTextGenerator.verts;
//            float unitsPerPixel = 1 / pixelsPerUnit;
//            //Pre-allocate Memory
//            vbo.Capacity = verts.Count;
//            if (roundingOffset != Vector2.zero)
//            {
//                for (int i = 0; i < verts.Count; i++)
//                {
//                    UIVertex uiv = verts[i];
//                    uiv.position *= unitsPerPixel;
//                    uiv.position.x += roundingOffset.x;
//                    uiv.position.y += roundingOffset.y;
//                    vbo.Add(uiv);
//                }
//            }
//            else
//            {
//                for (int i = 0; i < verts.Count; i++)
//                {
//                    UIVertex uiv = verts[i];
//                    uiv.position *= unitsPerPixel;
//                    vbo.Add(uiv);
//                }
//            }
//            
//            {
//                charValid.Capacity = cachedTextGenerator.characters.Count;
//                foreach (var chaInfo in cachedTextGenerator.characters)
//                {
//                    charValid.Add(chaInfo.charWidth != 0);
//                }
//            }
//
//            m_DisableFontTextureRebuiltCallback = false;
//        }
//
//        protected override void UpdateGeometry()
//        {
//            //字体重构的时候会调用此方法，需要重新生成文字
//            if (!isVerticisDirty)
//                isTextDirty = true;
//            cachedTextGenerator.Invalidate();
//            base.UpdateGeometry();
//            
//        }
//        public override void SetAllDirty()
//        {
//            //字体重构的时候会调用此方法，需要重新生成文字
//            isTextDirty = true;
//
//            cachedTextGenerator.Invalidate();
//            if (m_CachedInputRenderer != null)
//            {
//                m_CachedInputRenderer.SetVerticesDirty();
//            }
//            base.SetAllDirty();
//        }
//
//        Vector2[] CalcUV(Sprite sprite)
//        {
//            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
//            foreach (var i in sprite.uv)
//            {
//                if (i.x < minX)
//                    minX = i.x;
//                if (i.x > maxX)
//                    maxX = i.x;
//                if (i.y < minY)
//                    minY = i.y;
//                if (i.y > maxY)
//                    maxY = i.y;
//            }
//            return new Vector2[4]{
//                new Vector2(minX,maxY),
//                new Vector2(maxX,maxY),
//                new Vector2(maxX,minY),
//                new Vector2(minX,minY)
//            };
//        }
//
//        public override void SetVerticesDirty()
//        {
//            base.SetVerticesDirty();
//            isVerticisDirty = true;
//            if (m_CachedInputRenderer != null)
//            {
//                m_CachedInputRenderer.SetVerticesDirty();
//            }
//        }
//
//        public override void SetLayoutDirty()
//        {
//            base.SetLayoutDirty();
//            isVerticisDirty = true;
//            if (m_CachedInputRenderer != null)
//            {
//                m_CachedInputRenderer.SetLayoutDirty();
//            }
//        }
//        public Action<string> HrefClickEvent
//        {
//            set
//            {
//                m_hrefClickEvent = value;
//            }
//            get
//            {
//                return m_hrefClickEvent;
//            }
//        }
//        public void OnPointerClick(PointerEventData eventData)
//        {
//            Vector2  pos;
//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                rectTransform, eventData.position, eventData.pressEventCamera, out pos);
//
//            foreach (var hrefInfo in m_hrefText.getHrefList)
//            {
//                var boxes = hrefInfo.boxes;
//                for (var i = 0; i < boxes.size; ++i)
//                {
//                    if (boxes[i].Contains(pos))
//                    {
//                        if (HrefClickEvent != null)
//                            HrefClickEvent?.Invoke(hrefInfo.name);
//                        return;
//                    }
//                }
//            }
//        }
//
//        public void OnPointerDown(PointerEventData eventData)
//        {
//            
//        }
//
//        public void OnPointerUp(PointerEventData eventData)
//        {
//            
//        }
//    }
//}
