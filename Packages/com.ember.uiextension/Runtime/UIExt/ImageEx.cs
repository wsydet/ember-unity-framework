//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using Burner.Basic;
//using Burner.Extensions;
//using UnityEngine;
//
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class ImageEx : Image
//    {
//        [SerializeField]
//        private bool supportMultiLanguage;
//        [SerializeField]
//        private string[] spriteNameArray;
//        [SerializeField]
//        private Sprite[] spriteArray;
//        [SerializeField]
//        private int spriteIndex;
//        [SerializeField]
//        private bool irregularClickArea;
//        [SerializeField]
//        private float hitMinimalAlpha = 0.5f;
//        [SerializeField]
//        bool keepNativeSize;
//        [SerializeField]
//        private bool animated = false;
//        [SerializeField]
//        int fps = 25;
//        [SerializeField]
//        float delay;
//        [SerializeField]
//        bool playOnce;
//        [SerializeField]
//        float playbackSpeed = 1;
//
//        float accumulatedTime;
//        bool delayStarted;
//
//        Burner.Extensions.IResourceHandle imgHandle;
//
//        public bool GetSupportMultiLanguage()
//        {
//            return supportMultiLanguage;
//        }
//
//        public string[] GetSrpiteNameArray()
//        {
//            return spriteNameArray;
//        }
//
//        public Sprite[] GetSpriteArray()
//        {
//            return spriteArray;
//        }
//
//        public void SetSupportMultiLanguage(bool _supportMultiLanguage)
//        {
//            supportMultiLanguage = _supportMultiLanguage;
//        }
//
//        public void SetSpriteNameArray(string[] _spriteNameArray)
//        {
//            spriteNameArray = _spriteNameArray;
//        }
//
//        public void SetSpriteArray(Sprite[] _spriteArray)
//        {
//            spriteArray = _spriteArray;
//        }
//
//        public bool KeepNativeSize
//        {
//            get => keepNativeSize;
//            set
//            {
//                if (keepNativeSize != value)
//                {
//                    keepNativeSize = value;
//                    if (value)
//                        SetNativeSize();
//                }
//            }
//        }
//
//        public float PlaybackSpeed
//        {
//            get => playbackSpeed;
//            set
//            {
//                playbackSpeed = value;
//            }
//        }
//
//        protected override void Awake()
//        {
//            base.Awake();
//            if (irregularClickArea)
//                alphaHitTestMinimumThreshold = hitMinimalAlpha;
//        }
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            accumulatedTime = 0;
//            if (animated)
//                spriteIndex = 0;
//            RefreshSpriteState();
//        }
//
//        protected override void OnDisable()
//        {
//            base.OnDisable();
//            if (imgHandle != null)
//            {
//                imgHandle.Dispose();
//                imgHandle = null;
//            }
//            overrideSprite = null;
//        }
//
//        protected override void OnDestroy()
//        {
//            base.OnDestroy();
//
//            if (imgHandle != null)
//            {
//                imgHandle.Dispose();
//                imgHandle = null;
//            }
//            overrideSprite = null;
//        }
//
//        public int SpriteIndex
//        {
//            get => spriteIndex;
//            set
//            {
//                if (spriteIndex != value)
//                {
//                    spriteIndex = value;
//                    RefreshSpriteState();
//                }
//            }
//        }
//
//        public void RefreshSpriteState()
//        {
//            if (supportMultiLanguage)
//            {
//                if (spriteNameArray != null && spriteIndex < spriteArray.Length && spriteIndex >= 0)
//                {
//                    if(imgHandle != null)
//                    {
//                        imgHandle.Dispose();
//                        imgHandle = null;
//                    }
//
//                    imgHandle = ResourceEngine.Proxy.LoadAssetAsync(spriteNameArray[spriteIndex], OnLoadSprite);
//                }
//            }
//            else
//            {
//                if (spriteArray != null && spriteIndex < spriteArray.Length && spriteIndex >= 0)
//                {
//                    overrideSprite = spriteArray[spriteIndex];
//                    if (keepNativeSize)
//                    {
//                        SetNativeSize();
//                    }
//                }
//            }
//        }
//
//        void OnLoadSprite(IResourceHandle handle)
//        {
//            overrideSprite = handle.ResObject as Sprite;
//            if (keepNativeSize)
//            {
//                SetNativeSize();
//            }
//        }
//
//        void Update()
//        {
//            if (animated)
//            {
//                float tpf = 1f / (fps * playbackSpeed);
//
//                int idx = SpriteIndex;
//                int cnt = supportMultiLanguage ? spriteNameArray.Length : spriteArray.Length;
//                accumulatedTime += Time.deltaTime;
//                while (!delayStarted && accumulatedTime >= tpf)
//                {
//                    idx++;
//                    accumulatedTime -= tpf;
//
//                    if (playOnce)
//                    {
//                        if (idx >= cnt)
//                            idx = cnt - 1;
//                    }
//                    else
//                    {
//                        if (delay > 0 && idx >= cnt)
//                        {
//                            idx = 0;
//                            delayStarted = true;
//                            accumulatedTime = 0;
//                        }
//                    }
//                }
//                while (delayStarted && accumulatedTime >= tpf)
//                {
//                    if (accumulatedTime < delay)
//                    {
//                        return;
//                    }
//                    accumulatedTime -= delay;
//                    delayStarted = false;
//                }
//                idx = idx % cnt;
//
//                SpriteIndex = idx;
//            }
//        }
//    }
//}
