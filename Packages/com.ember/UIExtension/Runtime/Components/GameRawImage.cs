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
//
//using UnityEngine.UI;
//using TMPro;
//using Burner.Extensions;
//
//namespace Burner.UIExtension
//{
//    public class GameRawImage : GameUIComponent
//    {
//        IResourceHandle handle;
//        RawImage img;
//        bool keepNativeSize;
//        Color cacheColor;
//        Mask maskComponent;
//        string cachedSpriteName;
//
//
//        public override void OnInit()
//        {
//            base.OnInit();
//            img = GetComponent<RawImage>();
//            maskComponent = GetComponent<Mask>();
//            if (img)
//            {
//                cacheColor = img.color;
//            }
//        }
//
//        public override void OnDispose()
//        {
//            if (handle != null)
//            {
//                handle.Dispose();
//                handle = null;
//                cachedSpriteName = null;
//            }
//        }
//
//        /// <summary>
//        /// 设置是否可被遮罩
//        /// </summary>
//        public bool Maskable
//        {
//            get { return img.maskable; }
//            set { img.maskable = value; }
//        }
//        public override bool Enable
//        {
//            get => img ? img.enabled : base.Enable;
//            set
//            {
//                if (img)
//                    img.enabled = value;
//            }
//        }
//        /// <summary>
//        /// 设置图片的顶点色
//        /// </summary>
//        public UnityEngine.Color Color
//        {
//            get { return img.color; }
//            set
//            {
//                if (img.color != value)
//                {
//                    img.color = value;
//                    cacheColor = value;
//                }
//            }
//        }
//
//        /// <summary>
//        /// 设置是否置灰
//        /// </summary>
//        /// <param name="gray"></param>
//        public override void SetGray(bool gray)
//        {
//            if (img)
//            {
//                if (gray)
//                {
//                    img.material = GrayMaterial;
//                }
//                else
//                    img.material = null;
//            }
//        }
//
//        internal RawImage RawImage => img;
//
//        /// <summary>
//        /// 是否维持图片原始尺寸，设置为true时，图片加载完毕会自动设置图片尺寸
//        /// </summary>
//        public bool KeepNativeSize
//        {
//            get => keepNativeSize;
//            set
//            {
//                if (keepNativeSize != value)
//                {
//                    keepNativeSize = value;
//                    if (value)
//                    {
//                        img.SetNativeSize();
//                    }
//                }
//            }
//        }
//
//        /// <summary>
//        /// 设置图片的图片资源名
//        /// </summary>
//        public string Sprite
//        {
//            get
//            {
//                return img.texture.name;
//            }
//            set
//            {
//                if (cachedSpriteName != value)
//                {
//                    cachedSpriteName = value;
//                    img.texture = GameImage.EmptySprite.texture;
//                    LoadSprite(value, ref this.handle, OnLoaded);
//                }
//            }
//        }
//
//        public Texture RawTexture
//        {
//            get => img.texture;
//            set
//            {
//                if(handle !=null && (Object)handle.ResObject != value)
//                {
//                    handle.Dispose();
//                    handle = null;
//                }
//                img.texture = value;
//            }
//        }
//
//        void ActivateMask()
//        {
//            if (maskComponent)
//            {
//                maskComponent.enabled = false;
//                maskComponent.enabled = true;
//            }
//        }
//
//        protected void LoadSprite(string value, ref IResourceHandle handle, System.Action<IResourceHandle> onLoad)
//        {
//            if (handle != null)
//            {
//                if (handle.ResName == value)
//                    return;
//                handle.Dispose();
//                handle = null;
//            }
//            handle = ResourceEngine.Proxy.LoadAssetAsync(value, onLoad);
//        }
//
//        void OnLoaded(IResourceHandle handle)
//        {
//            Logger.Assert(this.handle == handle || this.handle == null, "Unexpected handle");
//            if (Disposed)
//            {
//                handle.Dispose();
//                this.handle = null;
//            }
//            else
//            {
//                img.texture = handle.ResObject as Texture2D;
//                if (keepNativeSize)
//                    img.SetNativeSize();
//
//                ActivateMask();
//            }
//        }
//    }
//}
