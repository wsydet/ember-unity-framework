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
//using System.Text.RegularExpressions;
//
//using Burner.UIExtension.Utils;
//
//namespace Burner.UIExtension
//{
//    [System.Serializable]
//    public class SymbolData
//    {
//        [SerializeField]
//        Sprite sprite;
//        [SerializeField]
//        string value;
//
//        public Sprite Sprite
//        {
//            get
//            { return sprite; }
//            set
//            {
//                sprite = value;
//            }
//        }
//        public string Value
//        {
//            get { return value; }
//            set { this.value = value; }
//        }
//    }
//
//    public struct SymbolSpriteInfo
//    {
//        public Vector2[] UV { get; set; }
//
//        public int Index { get; set; }
//        
//    }
//
//    public struct ReplacementInfo
//    {
//        public SymbolSpriteInfo[] Symbols { get; set; }
//        public string ReplacedText { get; set; }
//
//        public int ReplacedSpriteLength { get; set; }
//
//        public string OriginalText { get; set; }
//
//        //0 无效值 1 文本 2 表情
//        public byte[] isText { get; set; }
//        /*生成 Symbols对应的唯一编码 可用于缓存UV*/
//        public string CacheKey;
//    }
//    /// <summary>
//    /// 图文混排
//    /// </summary>
//    public class ImageFont : MonoBehaviour
//    {
//        [SerializeField]
//        SymbolData[] data = new SymbolData[0];
//
//        [System.NonSerialized]
//        bool needRebuild;
//        [System.NonSerialized]
//        DirtWordNode rootNode;
//        [System.NonSerialized]
//        Dictionary<string, Sprite> mapping;
//        [System.NonSerialized]        
//        Texture tex;
//        public SymbolData[] SymbolData { get { return data; } set { data = value; needRebuild = true; } }
//
//        public Texture Texture
//        {
//            get
//            {
//                if (tex == null)
//                {
//                    if (data != null && data.Length > 0)
//                        tex = data[0].Sprite.texture;
//                }
//                return tex;
//            }
//        }
//
//        int _TextureWidth = 0;
//        public int TextureWidth
//        {
//            get
//            {
//                if (_TextureWidth == 0 && Texture != null)
//                {
//                    _TextureWidth = Texture.width;
//                }
//                return _TextureWidth;
//            }
//        }
//        int _TextureHeight = 0;
//        public int TextureHeight
//        {
//            get
//            {
//                if (_TextureHeight == 0 && Texture != null)
//                {
//                    _TextureHeight = Texture.height;
//                }
//                return _TextureHeight;
//            }
//        }
//        void OnEnable()
//        {
//            needRebuild = true;
//        }
//
//        //CalcUV 已按顺时针返回UV信息 直接用
//
//        //图片 宽高差距在20以内 可以通用UV 暂时来看性价比最高
//        const int MistakeLength = 20;
//        const int MistakeLength_Half = 10;
//        char GenKey(Vector2[] uv)
//        {
//            if (uv?.Length != 4)
//            {
//                Burner.Logger.Error("Invalid UV");
//                return '0';
//            }
//            float x = uv[1].x - uv[0].x;
//            float y = uv[1].y - uv[2].y;
//
//            int width = TextureWidth;
//            int height = TextureHeight;
//            if (width <= 0 || height <= 0)
//                return '0';
//            int xI = (Mathf.CeilToInt(x * width) ) / MistakeLength;
//            int yI = (Mathf.CeilToInt(y * height) ) / MistakeLength;
//
//            while (xI > 16)
//            {
//                xI = (Mathf.CeilToInt(xI) + (MistakeLength_Half)) / MistakeLength;
//            }
//            while (yI > 16)
//            {
//                yI = (Mathf.CeilToInt(yI) + (MistakeLength_Half)) / MistakeLength;
//            }
//            return (char)(xI << 4 | yI);
//        }
//
//
//        static System.Text.StringBuilder StringBuilder = new System.Text.StringBuilder();
//        static List<byte> tempCharTextList = new List<byte>();
//        public ReplacementInfo ReplaceText(string text, float imgSize, bool isPureEmoji)
//        {
//            if (needRebuild || rootNode == null)
//            {
//                RebuildMapping();
//            }
//
//            ReplacementInfo res = new ReplacementInfo();
//            res.OriginalText = text;
//            List<DirtWordNode> replacement;
//            text.ReplaceDirtWord(rootNode, out replacement);
//            System.Text.StringBuilder sb = new System.Text.StringBuilder();
//            res.Symbols = new SymbolSpriteInfo[replacement.Count];
//
//            int curIdx = 0;
//            int newIdx = 0;
//            int appendLength = 0;
//            int idx = 0;
//            List<byte> isCharTextList = null;
//
//            StringBuilder.Clear();
//            StringBuilder.Append(replacement.Count);
//            StringBuilder.Append('|');
//            foreach (var i in replacement)
//            {
//                SymbolSpriteInfo info = new SymbolSpriteInfo();
//                
//                sb.Append(text.Substring(curIdx, i.Index - curIdx));
//                if (!isPureEmoji)
//                {
//                    if (isCharTextList == null)
//                    {
//                        isCharTextList = tempCharTextList;
//                        isCharTextList.Clear();
//                    }
//
//                    if (i.Index - curIdx > 0)
//                    {
//                        for (int j = 0; j < i.Index - curIdx; j++)
//                        {
//                            isCharTextList.Add(text[curIdx + j] == ' ' ? (byte)0 : (byte)1);
//                        }
//                    }
//                }
//
//                int prefixLength = 0;
//                if (imgSize != 0)
//                {
//                    int b = sb.Length;
//                    sb.Append("<size=");
//                    sb.Append(imgSize);
//                    sb.Append('>');
//                    prefixLength = sb.Length - b;
//
//                    res.ReplacedSpriteLength += prefixLength;
//                    if (!isPureEmoji)
//                    {
//                        for (int count = 0; count < prefixLength; count++)
//                        {
//                            isCharTextList.Add((byte)2);
//                        }
//                    }
//
//                }
//                appendLength += prefixLength;
//                newIdx += i.Index - curIdx + prefixLength;
//                info.Index = newIdx;
//                newIdx++;
//
//                Sprite sp = mapping[i.Word];
//                info.UV = CalcUV(sp);
//                StringBuilder.Append(GenKey(info.UV));
//                Rect r = sp.rect;
//                //float ratio = r.width / r.height;
//                
//                float ratio = ((info.UV[1].x - info.UV[0].x)*TextureWidth )
//                    / ((info.UV[1].y - info.UV[2].y) * TextureHeight);
//
//                string rep = string.Format("<quad width={0:0.00}>", ratio);
//                res.ReplacedSpriteLength += rep.Length;
//                if (!isPureEmoji)
//                {
//                    for (int count = 0; count < rep.Length; count++)
//                    {
//                        isCharTextList.Add((byte)2);
//                    }
//                }
//
//                sb.Append(rep);
//                curIdx = i.Index + i.Word.Length;
//                res.Symbols[idx++] = info;
//                if (imgSize != 0)
//                {
//                    sb.Append("</size>");
//                    appendLength += 7;
//                    newIdx += 7;
//                    res.ReplacedSpriteLength += 7;
//
//                    if (!isPureEmoji)
//                    {
//                        for (int count = 0; count < 7; count++)
//                        {
//                            isCharTextList.Add((byte)2);
//                        }
//                    }
//                }
//            }
//            sb.Append(text.Substring(curIdx));
//            if (text.Length > curIdx)
//            {
//                if (!isPureEmoji)
//                {
//                    if (isCharTextList == null)
//                    {
//                        isCharTextList = tempCharTextList;
//                        isCharTextList.Clear();
//                    }
//                    for (int j = 0; j < text.Length - curIdx; j++)
//                    {
//                        isCharTextList.Add(text[curIdx + j] == ' ' ? (byte)0 : (byte)1);
//                    }
//                }
//
//            }
//            res.ReplacedText = sb.ToString();
//            if (!isPureEmoji)
//                res.isText = isCharTextList?.ToArray();
//
//            res.CacheKey = StringBuilder.ToString();
//
//            return res;
//        }
//
//        void RebuildMapping()
//        {
//            needRebuild = false;
//            mapping = new Dictionary<string, Sprite>();
//            rootNode = new DirtWordNode();
//            if (data != null)
//            {
//                foreach (var i in data)
//                {
//                    mapping[i.Value] = i.Sprite;
//                    DirtWordService.AddDirtWord(i.Value, rootNode);
//                }
//            }
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
//    }
//}
