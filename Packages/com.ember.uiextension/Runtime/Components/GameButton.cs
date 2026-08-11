//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using Burner;
//using Burner.Basic;
//using Burner.Extensions;
//namespace Burner.UIExtension
//{
//    public class GameSelectable : GameImage
//    {
//        protected Selectable selectable;
//        IResourceHandle highlightHandle, pressHandle;
//        string cachedHighlightName, cachedPressName;
//
//        public override void OnInit()
//        {
//            base.OnInit();
//            selectable = GetComponent<Selectable>();
//        }
//
//        public override void OnDispose()
//        {
//            base.OnDispose();
//            if (highlightHandle != null)
//            {
//                highlightHandle.Dispose();
//                highlightHandle = null;
//                cachedHighlightName = null;
//            }
//            if (pressHandle != null)
//            {
//                pressHandle.Dispose();
//                pressHandle = null;
//                cachedPressName = null;
//            }
//        }
//        /// <summary>
//        /// 设置高光状态的图片
//        /// </summary>
//        public string HighlightSprite
//        {
//            get
//            {
//                return cachedHighlightName ?? selectable.spriteState.highlightedSprite.name;
//            }
//            set
//            {
//                if (cachedHighlightName != value)
//                {
//                    cachedHighlightName = value;
//                    var state = selectable.spriteState;
//                    if (ReplaceEmptySprite)
//                        state.highlightedSprite = EmptySprite;
//                    selectable.spriteState = state;
//                    LoadSprite(value, ref this.highlightHandle, OnHighlightLoaded);
//                }
//            }
//        }
//
//        void OnHighlightLoaded(Sprite loaded, IResourceHandle handle)
//        {
//            Logger.Assert(this.highlightHandle == handle || this.highlightHandle == null, "Unexpected handle");
//            if (Disposed)
//            {
//                handle.Dispose();
//                this.highlightHandle = null;
//            }
//            else
//            {
//                cachedHighlightName = handle.ResName;
//                var state = selectable.spriteState;
//                state.highlightedSprite = loaded;
//                selectable.spriteState = state;
//            }
//        }
//
//        /// <summary>
//        /// 设置按下状态的图片
//        /// </summary>
//        public string PressSprite
//        {
//            get
//            {
//                return cachedPressName ?? selectable.spriteState.pressedSprite.name;
//            }
//            set
//            {
//                if (cachedPressName != value)
//                {
//                    cachedPressName = value;
//                    var state = selectable.spriteState;
//                    if (ReplaceEmptySprite)
//                        state.pressedSprite = EmptySprite;
//                    selectable.spriteState = state;
//                    LoadSprite(value, ref this.pressHandle, OnPressLoaded);
//                }
//            }
//        }
//
//        void OnPressLoaded(Sprite loaded, IResourceHandle handle)
//        {
//            Logger.Assert(this.pressHandle == handle || this.pressHandle == null, "Unexpected handle");
//            if (Disposed)
//            {
//                handle.Dispose();
//                this.pressHandle = null;
//            }
//            else
//            {
//                cachedPressName = handle.ResName;
//                var state = selectable.spriteState;
//                state.pressedSprite = loaded;
//                selectable.spriteState = state;
//            }
//        }
//    }
//
//    public class GameButton : GameSelectable
//    {
//        private Button button;
//        private ButtonEx buttonEx;
//        bool canClickWhenDisable;
//        public bool CanClickWhenDisable
//        {
//            get => canClickWhenDisable;
//            set
//            {
//                canClickWhenDisable = value;
//                if(canClickWhenDisable && !buttonEx)
//                {
//                    Burner.Logger.Error("Only ButtonEx supports this feature");
//                }
//            }
//        }
//
//        public override void OnInit()
//        {
//            base.OnInit();
//            button = selectable as Button;
//            buttonEx = button as ButtonEx;
//        }
//
//        /// <summary>
//        /// 设置按钮是否为激活状态
//        /// </summary>
//        public override bool Enable
//        {
//            get { return buttonEx ? buttonEx.EnableState : button.interactable; }
//            set
//            {
//                if (buttonEx)
//                {
//                    buttonEx.EnableState = value;
//                    if (!canClickWhenDisable && !value)
//                    {
//                        buttonEx.interactable = false;
//                    }
//                    if (value)
//                        buttonEx.interactable = true;
//                        
//                }
//                else
//                {
//                    if (button.interactable != value)
//                    {
//                        button.interactable = value;
//                    }
//                }
//            }
//        }
//
//        protected override void AddClickCallBack()
//        {
//            button.onClick.AddListener(OnClickButton);
//        }
//
//        void OnClickButton()
//        {
//            if (!canClickWhenDisable && buttonEx && !buttonEx.EnableState)
//                return;
//            if (longPressFrameCnt == Time.frameCount)
//                return;
//            BurnerUIManager.Instance.GlobalEvents.DispatchClickEvent(this);
//            clickAction?.Invoke(this);
//        }
//    }
//}
