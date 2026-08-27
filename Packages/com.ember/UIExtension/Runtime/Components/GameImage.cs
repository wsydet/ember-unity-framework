//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//using UnityEngine.UI;
//using TMPro;
//using Burner.Extensions;
//using System;
//using Burner.Basic;
//
//namespace Burner.UIExtension
//{
//    public class GameImage : GameUIComponent
//    {
//        IResourceHandle handle;
//        Image sprite;
//        ImageEx spriteEx;
//        MergedImage mergedImage;
//        bool keepNativeSize;
//        Color cacheColor;
//        Mask maskComponent;
//        static Sprite emptySprite;
//        string cachedSpriteName;
//
//        internal static Sprite EmptySprite
//        {
//            get
//            {
//                if (!emptySprite)
//                {
//                    emptySprite = Resources.Load<Sprite>("EmptySprite");
//                }
//                return emptySprite;
//            }
//        }
//
//        public System.Action<string> OnSpriteLoaded { get; set; }
//
//
//        public override void OnInit()
//        {
//            base.OnInit();
//            sprite = GetComponent<Image>();
//            spriteEx = sprite as ImageEx;
//            if (spriteEx)
//                keepNativeSize = spriteEx.KeepNativeSize;
//            mergedImage = sprite as MergedImage;
//            maskComponent = GetComponent<Mask>();
//            if (sprite)
//            {
//                cacheColor = sprite.color;
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
//        public override bool Enable
//        {
//            get => sprite ? sprite.enabled : base.Enable;
//            set
//            {
//                if (sprite)
//                    sprite.enabled = value;
//            }
//        }
//
//        /// <summary>
//        /// 设置是否可被遮罩
//        /// </summary>
//        public bool Maskable
//        {
//            get { return sprite.maskable; }
//            set { sprite.maskable = value; }
//        }
//
//        /// <summary>
//        /// 设置图片的顶点色
//        /// </summary>
//        public UnityEngine.Color Color
//        {
//            get { return sprite.color; }
//            set
//            {
//                if (sprite.color != value)
//                {
//                    sprite.color = value;
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
//            if (sprite)
//            {
//                if (gray)
//                {
//                    sprite.material = GrayMaterial;
//                }
//                else
//                    sprite.material = null;
//            }
//        }
//
//        /// <summary>
//        /// 设置填充模式时的填充量
//        /// </summary>
//        public float FillAmmount
//        {
//            get { return sprite.fillAmount; }
//            set { sprite.fillAmount = value; }
//        }
//
//        /// <summary>
//        /// 设置填充模式时的填充方向是否时顺时针
//        /// </summary>
//        public bool FillClockWise
//        {
//            get { return sprite.fillClockwise; }
//            set { sprite.fillClockwise = value; }
//        }
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
//                    if (spriteEx)
//                    {
//                        spriteEx.KeepNativeSize = value;
//                    }
//                    else
//                    {
//                        if (value)
//                        {
//                            sprite.SetNativeSize();
//                        }
//                    }
//                }
//            }
//        }
//
//        /// <summary>
//        /// 切换图片时如果图片还未加载上来是否先替换为空图片
//        /// </summary>
//        public bool ReplaceEmptySprite { get; set; } = true;
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            OnSpriteLoaded = null;
//        }
//
//        /// <summary>
//        /// 设置图片的图片资源名
//        /// </summary>
//        public string Sprite
//        {
//            get
//            {
//                if(string.IsNullOrEmpty(cachedSpriteName) && sprite.sprite)
//                {
//                    cachedSpriteName = sprite.sprite.name.ToLower() + ".png";
//                }
//                return cachedSpriteName;
//            }
//            set
//            {
//                if (spriteEx)
//                {
//                    throw new System.NotSupportedException("Please use Sprite Index to change image");
//                }
//
//                if (cachedSpriteName != value)
//                {
//                    cachedSpriteName = value;
//                    if (ReplaceEmptySprite)
//                        sprite.sprite = EmptySprite;
//                    LoadSprite(value, ref this.handle, OnLoaded);
//                }
//            }
//        }
//
//        /// <summary>
//        /// 设置图片Index，使用ImageEx脚本时使用
//        /// </summary>
//        public int SpriteIndex
//        {
//            get => spriteEx.SpriteIndex;
//            set
//            {
//                if (spriteEx.SpriteIndex != value)
//                {
//                    spriteEx.SpriteIndex = value;
//                    ActivateMask();
//                }
//            }
//        }
//
//        public int GetImageProxyIndex(string name)
//        {
//            if (mergedImage)
//                return mergedImage.GetImageProxyIndex(name);
//
//            return -1;
//        }
//
//        public MergedImageProxy GetImageProxyByIndex(int index)
//        {
//            if (mergedImage)
//                return mergedImage.GetImageProxy(index);
//
//            return null;
//        }
//
//        public MergedImageProxy GetImageProxyByName(string name)
//        {
//            if (mergedImage)
//                return mergedImage.GetImageProxy(name);
//
//            return null;
//        }
//
//        public void TweenFillAmountTo(float to, float time = 0.3f, int curveType = 0, Action onEnd = null)
//        {
//            float oldVal = FillAmmount;
//            var tween = UITweener.StartNewTween<TweenFillAmount>(gameObject, time);
//            tween.from = oldVal;
//            tween.to = to;
//            
//            if (time <= 0f)
//            {
//                tween.Sample(1f, true);
//                tween.enabled = false;
//            }
//            else
//                tween.Sample(0, false);
//
//            tween.OnFinishCallback = onEnd;
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
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
//        protected void LoadSprite(string value, ref IResourceHandle handle, System.Action<Sprite, IResourceHandle> onLoad)
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
//        void OnLoaded(Sprite loaded, IResourceHandle handle)
//        {
//            Logger.Assert(this.handle == handle || this.handle == null, "Unexpected handle");
//            if (Disposed)
//            {
//                handle.Dispose();
//                this.handle = null;
//            }
//            else
//            {
//                cachedSpriteName = handle.ResName;
//                sprite.sprite = loaded;
//                if (keepNativeSize)
//                    sprite.SetNativeSize();
//
//                ActivateMask();
//                OnSpriteLoaded?.Invoke(handle.ResName);
//            }
//        }
//    }
//}
