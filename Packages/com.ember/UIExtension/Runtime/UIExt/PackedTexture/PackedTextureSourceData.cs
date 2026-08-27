//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    public class PackedTextureSourceData : ScriptableObject
//    {
//        [System.Serializable]
//        public class Atlas
//        {
//            public string name;
//            public int width;
//            public int height;
//            public float fWidth;
//            public float fHeight;
//            public float widthRecip;
//            public float heightRecip;
//            public List<AtlasItem> listItems = new List<AtlasItem>();
//        }
//
//        [System.Serializable]
//        public class AtlasItem
//        {
//            public string name;
//            public int x;
//            public int y;
//            public int width;
//            public int height;
//        }
//
//        public class Sprite
//        {
//            public Sprite(AtlasItem item, Atlas atlas)
//            {
//                this.atlasItem = item;
//                this.atlas = atlas;
//
//                st = new Vector4(item.width * atlas.widthRecip,  item.height * atlas.heightRecip,
//                    item.x * atlas.widthRecip, item.y * atlas.heightRecip);
//            }
//            private AtlasItem atlasItem;
//            private Atlas atlas;
//            private Vector4 st;
//
//            public string Name => atlasItem.name;
//
//            public Vector4 ST => st;
//
//            public int width => atlasItem.width;
//            public int height => atlasItem.height;
//        }
//
//        public List<Atlas> listAtlas;
//
//        private Dictionary<string, Sprite> mapSprite;
//
//        public Dictionary<string, Sprite> MapSprite
//        {
//            get
//            {
//                if (mapSprite == null)
//                {
//                    BuildMap();
//                }
//
//                return mapSprite;
//            }
//        }
//
//        private void BuildMap()
//        {
//            mapSprite = new Dictionary<string, Sprite>();
//            foreach (var atlas in listAtlas)
//            {
//                foreach (var atlasItem in atlas.listItems)
//                {
//                    Sprite sprite = new Sprite(atlasItem, atlas);
//                    mapSprite.Add(sprite.Name, sprite);
//                }
//            }
//        }
//
//        private List<string> listSpriteName;
//        public List<string> ListSpriteName
//        {
//            get
//            {
//                // if (listSpriteName == null)
//                // {
//                    BuildItemNameList();
//                // }
//
//                return listSpriteName;
//            }
//        }
//
//        private void BuildItemNameList()
//        {
//            listSpriteName = new List<string>();
//            foreach (var atlas in listAtlas)
//            {
//                foreach (var atlasItem in atlas.listItems)
//                {
//                    listSpriteName.Add(atlasItem.name);
//                }
//            }
//        }
//
//        public Sprite GetSprite(string spriteName)
//        {
//            if(MapSprite.TryGetValue(spriteName, out var sprite))
//            {
//                return sprite;
//            }
//
//            return null;
//        }
//    }
//}
