//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using Burner.Basic;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Sprites;
//using Burner.Extensions;
//
//namespace Burner.UIExtension
//{
//    public class MergedImageProxy
//    {
//
//        public MergedImageProxy(int imageIndex, MergedImage mergedImage)
//        {
//            mImageIndex = imageIndex;
//            mMergedImage = mergedImage;
//        }
//
//        private MergedImage.ImageInfo ImageInfo
//        {
//            get => mMergedImage.GetImageInfo(mImageIndex);
//        }
//
//        public string Sprite
//        {
//            get
//            {
//                if (ImageInfo.mSprite != null)
//                    return ImageInfo.mSprite.name;
//
//                return "";
//            }
//            set
//            {
//                mMergedImage.SetSprite(value, mImageIndex);
//            }
//        }
//
//        public int SpriteIndex
//        {
//            get => ImageInfo.mSpriteIndex;
//            set
//            {
//                mMergedImage.SetSpriteIndex(value, mImageIndex);
//            }
//        }
//
//        public bool Visible
//        {
//            get => ImageInfo.mTexVisible;
//            set
//            {
//                mMergedImage.SetTextureVisible(value, mImageIndex);
//            }
//        }
//
//        public Color Color
//        {
//            get => ImageInfo.mTexColor;
//            set
//            {
//                mMergedImage.SetTextureColor(value, mImageIndex);
//            }
//        }
//
//        public bool Gray
//        {
//            get => ImageInfo.mTexGray;
//            set
//            {
//                mMergedImage.SetTextureGray(value, mImageIndex);
//            }
//        }
//
//        public bool KeepNativeSize
//        {
//            get => ImageInfo.mKeepNativeSize;
//            set
//            {
//                mMergedImage.SetKeepNativeSize(mImageIndex, value);
//            }
//        }
//
//        int mImageIndex = 0;
//        MergedImage mMergedImage = null;
//    }
//
//    public class MergedImage : Image
//    {
//        [System.Serializable]
//        public struct ImageInfo
//        {
//            public string mTexName;
//            public Sprite mSprite;
//            public bool mSupportMultiLanguage;
//            public List<string> mSpriteNameList;
//            public List<Sprite> mSpriteList;
//            public int mSpriteIndex;
//            public Color mTexColor;
//            public bool mTexVisible;
//            public bool mTexGray;
//            public Vector4 mTexPos;
//            public int mTexParentIndex;
//            public bool mTexGenerateMask;
//            public bool mKeepNativeSize;
//        }
//
//        public struct ImageCache
//        {
//            public string mCachedSpriteName;
//            public IResourceHandle mResHandle;
//        }
//
//        private enum TextureParameterType
//        {
//            Texture,
//            TextureColor,
//            TextureVisible,
//            TextureGray,
//            TextureParentIndex,
//            TextureGenerateMask,
//            TexturePos,
//            TextureUvTransform,
//            TextureInvUvTransform,
//
//            Num
//        }
//
//        private Material mMaterialInstance = null;
//
//        private Material baseMaterial = null;
//
//        static List<int>[] TextureParameterShaderId = new List<int>[(int)TextureParameterType.Num];
//
//        private bool mImageProxyCreated = false;
//
//        [SerializeField]
//        private bool mEnableMask = false;
//
//        [SerializeField]
//        private List<ImageInfo> mImageInfo = new List<ImageInfo>();
//
//        private List<ImageCache> mImageCache = new List<ImageCache>();
//
//        List<MergedImageProxy> mImageProxy = new List<MergedImageProxy>();
//        bool needRefresh;
//        public int ImageCount
//        {
//            get => mImageInfo.Count;
//        }
//
//        public ImageInfo GetImageInfo(int index)
//        {
//            return mImageInfo[index];
//        }
//        void MakeSureImageProxy()
//        {
//            if (mImageProxyCreated == false)
//            {
//                for (int i = 0; i != ImageCount; ++i)
//                {
//                    mImageProxy.Add(new MergedImageProxy(i, this));
//                    mImageCache.Add(new ImageCache());
//                }
//                mImageProxyCreated = true;
//            }
//        }
//
//        public int GetImageProxyIndex(string name)
//        {
//            for (int i = 0; i != mImageInfo.Count; ++i)
//                if (mImageInfo[i].mTexName == name)
//                    return i;
//
//            return -1;
//        }
//
//        public MergedImageProxy GetImageProxy(int index)
//        {
//            MakeSureImageProxy();
//
//            if (index < 0 || index >= mImageProxy.Count)
//                return null;
//            return mImageProxy[index];
//        }
//
//        public MergedImageProxy GetImageProxy(string name)
//        {
//            return GetImageProxy(GetImageProxyIndex(name));
//        }
//
//        int GetTexParameterShaderId(TextureParameterType type, int index)
//        {
//            if (index < 0)
//                return 0;
//
//            List<int> shaderParameterList = TextureParameterShaderId[(int)type];
//
//            if (shaderParameterList == null)
//                shaderParameterList = TextureParameterShaderId[(int)type] = new List<int>();
//
//            while (index >= shaderParameterList.Count)
//                shaderParameterList.Add(0);
//
//            if (shaderParameterList[index] != 0)
//                return shaderParameterList[index];
//
//            if (index == 0)
//            {
//                int shaderParameterId = 0;
//                switch (type)
//                {
//                    case TextureParameterType.Texture:
//                        shaderParameterId = Shader.PropertyToID("_MainTex");
//                        break;
//                    case TextureParameterType.TextureColor:
//                        shaderParameterId = Shader.PropertyToID("_Color");
//                        break;
//                    case TextureParameterType.TextureVisible:
//                        shaderParameterId = Shader.PropertyToID("_Visible");
//                        break;
//                    case TextureParameterType.TextureGray:
//                        shaderParameterId = Shader.PropertyToID("_Gray");
//                        break;
//                    case TextureParameterType.TextureParentIndex:
//                        //第一张贴图无需设置父
//                        shaderParameterId = 0;
//                        break;
//                    case TextureParameterType.TextureGenerateMask:
//                        shaderParameterId = Shader.PropertyToID("_GenerateMask");
//                        break;
//                    case TextureParameterType.TexturePos:
//                        shaderParameterId = Shader.PropertyToID("_MainTexturePos");
//                        break;
//                    case TextureParameterType.TextureUvTransform:
//                        shaderParameterId = Shader.PropertyToID("_UvTransform");
//                        break;
//                    case TextureParameterType.TextureInvUvTransform:
//                        shaderParameterId = Shader.PropertyToID("_InvUvTransform");
//                        break;
//                }
//
//                shaderParameterList[index] = shaderParameterId;
//            }
//            else
//            {
//                int shaderParameterId = 0;
//                switch (type)
//                {
//                    case TextureParameterType.Texture:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_Texture_{0}", index));
//                        break;
//                    case TextureParameterType.TextureColor:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureColor_{0}", index));
//                        break;
//                    case TextureParameterType.TextureVisible:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureVisible_{0}", index));
//                        break;
//                    case TextureParameterType.TextureGray:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureGray_{0}", index));
//                        break;
//                    case TextureParameterType.TextureParentIndex:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureParentIndex_{0}", index));
//                        break;
//                    case TextureParameterType.TextureGenerateMask:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureGenerateMask_{0}", index));
//                        break;
//                    case TextureParameterType.TexturePos:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TexturePos_{0}", index));
//                        break;
//                    case TextureParameterType.TextureUvTransform:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureUvTransform_{0}", index));
//                        break;
//                    case TextureParameterType.TextureInvUvTransform:
//                        shaderParameterId = Shader.PropertyToID(string.Format("_TextureInvUvTransform_{0}", index));
//                        break;
//                }
//
//                shaderParameterList[index] = shaderParameterId;
//            }
//
//            return shaderParameterList[index];
//        }
//
//        public void SetKeepNativeSize(int index, bool value)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            
//            if (imageInfo.mKeepNativeSize != value)
//            {
//                imageInfo.mKeepNativeSize = value;
//                if (value)
//                {
//                    SetNativeSize(index);
//                }
//
//                mImageInfo[index] = imageInfo;
//            }
//        }
//
//        public bool GetKeepNativeSize(int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            return imageInfo.mKeepNativeSize;
//        }
//
//
//        public void SetNativeSize(int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            var activeSprite = imageInfo.mSprite;
//            if (activeSprite)
//            {
//                float w = activeSprite.rect.width;
//                float h = activeSprite.rect.height;
//                Rect fullRect = new Rect(Vector2.zero, rectTransform.rect.size);
//                var oldRect = CalcNodeRawRect(fullRect, imageInfo.mTexPos);
//                Vector2 newSize = new Vector2(w, h);
//                var diff = (newSize - oldRect.size) / 2f;
//                oldRect.min -= diff;
//                oldRect.max += diff;
//                SetTexturePos(CalcTransform(fullRect, oldRect), index);
//            }
//        }
//        public void SetEnableMask(bool enableMask)
//        {
//            mEnableMask = enableMask;
//        }
//
//        void MakeSureImageCount(int count)
//        {
//            while (mImageInfo.Count < count)
//                mImageInfo.Add(new ImageInfo
//                {
//                    mTexName = "unnamed",
//                    mSprite = null,
//                    mTexColor = Color.white,
//                    mTexVisible = true,
//                    mTexGray = false,
//                    mTexPos = Vector4.one,
//                    mTexParentIndex = mImageInfo.Count,
//                    mTexGenerateMask = false,
//                    mSpriteList = null,
//                    mSpriteIndex = -1,
//                });
//        }
//
//        public void SetTextureName(string name, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexName = name;
//            mImageInfo[index] = imageInfo;
//        }
//
//        void SetSpriteInternal(Material mat, Sprite sprite, int index)
//        {
//            if (!mat)
//            {
//                SetMaterialDirty();
//                return;
//            }
//            mat.SetTexture(GetTexParameterShaderId(TextureParameterType.Texture, index), sprite != null ? sprite.texture : null);
//
//            if (index == 0)
//            {
//                base.sprite = sprite;
//            }
//
//
//            var uv = (sprite != null) ? DataUtility.GetOuterUV(sprite) : Vector4.zero;
//
//            Vector4 invUvTransform;
//            Vector4 uvTransform;
//            CalcInvUvTransform(uv, out invUvTransform, out uvTransform);
//
//            mat.SetVector(GetTexParameterShaderId(TextureParameterType.TextureUvTransform, index), uvTransform);
//            mat.SetVector(GetTexParameterShaderId(TextureParameterType.TextureInvUvTransform, index), invUvTransform);
//        }
//
//        public void SetSprite(Sprite sprite, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mSprite = sprite;
//            mImageInfo[index] = imageInfo;
//
//            SetSpriteInternal(mMaterialInstance, imageInfo.mSprite, index);
//            if (imageInfo.mKeepNativeSize)
//                SetNativeSize(index);
//        }
//
//        void LoadSprite(string value, int index)
//        {
//            ImageCache imageCache = mImageCache[index];
//
//            if (imageCache.mResHandle != null)
//            {
//                if (imageCache.mResHandle.ResName == value)
//                    return;
//                imageCache.mResHandle.Dispose();
//                imageCache.mResHandle = null;
//                mImageCache[index] = imageCache;
//            }
//
//            imageCache.mResHandle = ResourceEngine.Proxy.LoadAssetAsync(value, (IResourceHandle resHandle) => { OnLoaded(resHandle, index); });
//            imageCache.mCachedSpriteName = value;
//
//            mImageCache[index] = imageCache;
//        }
//
//        void OnLoaded(IResourceHandle resHandle, int index)
//        {
//            Logger.Assert(mImageCache[index].mResHandle == resHandle || mImageCache[index].mResHandle == null, "Unexpected handle");
//            /*
//             * if (Disposed)
//             * {
//             *  XXX
//             *  }
//             */
//
//            SetSprite(resHandle.ResObject as Sprite, index);
//        }
//
//        public void SetSprite(string name, int index)
//        {
//            if (mImageInfo[index].mSpriteList.Count != 0)
//            {
//                throw new System.NotSupportedException("Please use Sprite Index to change image");
//            }
//
//            if (mImageCache[index].mCachedSpriteName != name)
//            {
//                SetSpriteInternal(mMaterialInstance, null, index);
//                LoadSprite(name, index);
//            }
//        }
//
//        public void SetSupportMultiLanguage(bool supportMultiLanguage, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mSupportMultiLanguage = supportMultiLanguage;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetSpriteNameList(List<string> spriteNameList, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mSpriteNameList = spriteNameList;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetSpriteList(List<Sprite> spriteList, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mSpriteList = spriteList;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetSpriteIndex(int spriteIndex, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//
//            ImageInfo imageInfo = mImageInfo[index];
//            if (imageInfo.mSpriteIndex != spriteIndex)
//            {
//                imageInfo.mSpriteIndex = spriteIndex;
//
//                if (imageInfo.mSupportMultiLanguage)
//                {
//                    SetSpriteInternal(mMaterialInstance, null, index);
//                    LoadSprite(imageInfo.mSpriteNameList[spriteIndex], index);
//                }
//                else
//                {
//                    imageInfo.mSprite = imageInfo.mSpriteList[spriteIndex];
//                    SetSpriteInternal(mMaterialInstance, imageInfo.mSprite, index);
//                    mImageInfo[index] = imageInfo;
//                }
//            }
//        }
//
//        public void SetTextureColor(Color color, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            if (mMaterialInstance)
//                mMaterialInstance.SetColor(GetTexParameterShaderId(TextureParameterType.TextureColor, index), color);
//            else
//                SetMaterialDirty();
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexColor = color;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetTextureGray(bool gray, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            if (mMaterialInstance)
//                mMaterialInstance.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureGray, index), gray ? 1f : 0f);
//            else
//                SetMaterialDirty();
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexGray = gray;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetTextureVisible(bool visible, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            if (mMaterialInstance)
//                mMaterialInstance.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureVisible, index), visible ? 1f : 0f);
//            else
//                SetMaterialDirty();
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexVisible = visible;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetTexturePos(Vector4 pos, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            if (mMaterialInstance)
//                mMaterialInstance.SetVector(GetTexParameterShaderId(TextureParameterType.TexturePos, index), pos);
//            else
//                SetMaterialDirty();
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexPos = pos;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetTextureParent(int parent, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            if (mMaterialInstance)
//                mMaterialInstance.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureParentIndex, index), parent * 1f);
//            else
//                SetMaterialDirty();
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexParentIndex = parent;
//            mImageInfo[index] = imageInfo;
//        }
//
//        public void SetTextureGenerateMask(bool generateMask, int index)
//        {
//            MakeSureImageCount(index + 1);
//
//            //为了优化，需要提前做1-运算，以取反
//            if (mMaterialInstance)
//                mMaterialInstance.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureGenerateMask, index), generateMask ? 0f : 1f);
//            else
//                SetMaterialDirty();
//
//            ImageInfo imageInfo = mImageInfo[index];
//            imageInfo.mTexGenerateMask = generateMask;
//            mImageInfo[index] = imageInfo;
//        }
//
//        private void RefreshShaderParameter(Material canvasMaterial)
//        {
//            for (int i = 0; i != ImageCount; ++i)
//            {
//                ImageInfo imageInfo = mImageInfo[i];
//
//                if (mImageCache[i].mResHandle != null && mImageCache[i].mResHandle.ResObject == null)
//                    SetSpriteInternal(canvasMaterial, GameImage.EmptySprite, i);
//                else
//                    SetSpriteInternal(canvasMaterial, imageInfo.mSprite, i);
//                canvasMaterial.SetColor(GetTexParameterShaderId(TextureParameterType.TextureColor, i), imageInfo.mTexColor);
//                canvasMaterial.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureGray, i), imageInfo.mTexGray ? 1f : 0f);
//                canvasMaterial.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureVisible, i), imageInfo.mTexVisible ? 1f : 0f);
//                canvasMaterial.SetVector(GetTexParameterShaderId(TextureParameterType.TexturePos, i), imageInfo.mTexPos);
//                canvasMaterial.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureParentIndex, i), imageInfo.mTexParentIndex * 1f);
//
//                //为了优化，需要提前做1-运算，以取反
//                canvasMaterial.SetFloat(GetTexParameterShaderId(TextureParameterType.TextureGenerateMask, i), imageInfo.mTexGenerateMask ? 0f : 1f);
//            }
//        }
//
//        public override Material GetModifiedMaterial(Material baseMaterial)
//        {
//            MakeSureImageProxy();
//            var baseMat = base.GetModifiedMaterial(baseMaterial);
//            if(this.baseMaterial != baseMat)
//            {
//                if (mMaterialInstance)
//                    DestroyImmediate(mMaterialInstance);
//                mMaterialInstance = Instantiate(baseMat);
//                this.baseMaterial = baseMat;
//            }
//            RefreshShaderParameter(mMaterialInstance);
//            return mMaterialInstance;
//        }
//
//        /*private void Update()
//        {
//            if (needRefresh)
//            {
//                needRefresh = false;
//                RefrashShaderParemeter(mMaterialInstance);
//            }
//        }*/
//
//
//        public static Vector4 CalcTransform(Rect fullyRect, Rect nodeRect)
//        {
//            float deltaX = nodeRect.width / fullyRect.width;
//            float deltaY = nodeRect.height / fullyRect.height;
//
//            float x = (nodeRect.x - fullyRect.x) / fullyRect.width;
//            float y = (nodeRect.y - fullyRect.y) / fullyRect.height;
//
//            return new Vector4(1 / deltaX - 1, 1 / deltaY - 1, -x / deltaX, -y / deltaY);
//        }
//
//        public static Rect CalcNodeRawRect(Rect fullyRect, Vector4 transform)
//        {
//            Rect rect = Rect.zero;
//            rect.width = fullyRect.width / (transform.x + 1);
//            rect.height = fullyRect.height / (transform.y + 1);
//            rect.x = fullyRect.x - transform.z * rect.width;
//            rect.y = fullyRect.y - transform.w * rect.height;
//
//            return rect;
//        }
//    }
//}
