//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Extensions;
//using System;
//using System.Collections.Generic;
//
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//
//namespace Burner
//{
//    [Legacy("Please use Burner.UIExtension.GameButton instead")]
//    public class BurnerButton : Button
//    {
//        private bool inside;
//        private Action<BurnerButton, string> m_PointerEvent;
//
//        [SerializeField]
//        protected bool isEnableScale = true;
//
//        [SerializeField]
//        protected float clickScale = 0.85f;
//
//        [SerializeField]
//        protected GameObject ScaleTarget;
//
//        [SerializeField]
//        protected bool isEnableLongPress = false;
//
//        public bool IsEnableLongPress
//        {
//            get { return isEnableLongPress; }
//            private set { isEnableLongPress = value; }
//        }
//        public void SetPointerEventListener(Action<BurnerButton, string> act) => m_PointerEvent = act;
//
//        /// <summary>
//        /// the global default block time (cool down time)
//        /// </summary>
//        public static float GlobalBlockTime = 0.2f;
//
//        private static float m_LastActionTime = 0;
//        bool m_CancelAction = false;
//
//        /// <summary>
//        /// set to true if indivadul click block time is needed
//        /// </summary>
//        [SerializeField]
//        protected bool UseClickBlockTime = false;
//
//        /// <summary>
//        /// a time that can forbid quick clicks on different buttons.
//        /// </summary>
//        [SerializeField]
//        protected float ClickBlockTime = GlobalBlockTime;
//
//        [SerializeField]
//        protected ButtonClickedEvent m_OnLongClick = new ButtonClickedEvent();
//        public ButtonClickedEvent onLongClick
//        {
//            get { return m_OnLongClick; }
//            set { m_OnLongClick = value; }
//        }
//
//        private bool m_IsStartPress = false;
//        private float m_CurrentPointerDownTime = 0f;
//
//        [SerializeField]
//        protected float m_LongPressTime = 0.5f;
//
//        private bool m_IsLongPressTrigger = false;
//
//        /// <summary>
//        /// Set the block time
//        /// to stop OnClick event triggering many times in a very short time.
//        /// </summary>
//        public void SetClickBlockTime(float time)
//        {
//            // force enable this local alternative block time if coder call this setter initiatively
//            UseClickBlockTime = true;
//
//            ClickBlockTime = time;
//        }
//
//        /// <summary>
//        /// get the block time
//        /// to stop OnClick event triggering many times in a very short time.
//        /// </summary>
//        public float GetClickBlockTime()
//        {
//            return ClickBlockTime;
//        }
//
//        protected override void DoStateTransition(SelectionState state, bool instant)
//        {
//            if (state == SelectionState.Pressed && !inside)
//                state = SelectionState.Normal;
//            base.DoStateTransition(state, instant);
//        }
//
//        protected override void InstantClearState()
//        {
//            base.InstantClearState();
//            inside = false;
//        }
//
//        public override void OnPointerDown(PointerEventData eventData)
//        {
//            if(Time.unscaledTime-m_LastActionTime<(UseClickBlockTime?ClickBlockTime:GlobalBlockTime))
//            {
//                m_CancelAction = true;
//                return;
//            }
//            else
//            {
//                m_CancelAction = false;
//                m_LastActionTime = Time.unscaledTime;
//            }
//
//            if (isEnableScale && interactable && !ScaleTarget.IsNull())
//            {
//                transform.localScale = new Vector3(clickScale, clickScale, clickScale);
//                ScaleTarget.transform.localScale = new Vector3(1.0f / clickScale, 1.0f / clickScale, 1.0f / clickScale);
//            }
//            base.OnPointerDown(eventData);
//            m_CurrentPointerDownTime = Time.time;
//            m_IsStartPress = true;
//            m_PointerEvent?.Invoke(this, "OnPointerDown");
//        }
//
//        public override void OnPointerUp(PointerEventData eventData)
//        {
//            if (m_CancelAction)
//            {
//                return;
//            }
//            if (isEnableScale && interactable && !ScaleTarget.IsNull())
//            {
//                transform.localScale = Vector3.one;
//                ScaleTarget.transform.localScale = Vector3.one;
//            }
//            base.OnPointerUp(eventData);
//            m_IsStartPress = false;
//            m_PointerEvent?.Invoke(this, "OnPointerUp");
//        }
//
//        public override void OnPointerExit(PointerEventData eventData)
//        {
//            if (m_CancelAction)
//            {
//                return;
//            }
//            inside = false;
//            base.OnPointerExit(eventData);
//            m_IsStartPress = false;
//            m_PointerEvent?.Invoke(this, "OnPointerExit");
//        }
//
//        public override void OnPointerEnter(PointerEventData eventData)
//        {
//            if (m_CancelAction)
//            {
//                return;
//            }
//            inside = true;
//            base.OnPointerEnter(eventData);
//            m_PointerEvent?.Invoke(this, "OnPointerEnter");
//        }
//
//        /*** NOTICE:
//         * to prevent calls from "onClick.AddListener", MUST override fellowing functions of base class "Button"
//         * and modify all places that calls Press().
//         *
//         * Add same functions of base class to avoid changes of UGUI source code.
//         * */
//        private void Press()
//        {
//            if(!IsActive() || !IsInteractable() || m_CancelAction)
//                return;
//            if(m_IsLongPressTrigger)
//            {
//                m_IsLongPressTrigger = false;
//                return;
//            }
//
//            UISystemProfilerApi.AddMarker("Button.onClick", this);
//            onClick.Invoke();
//        }
//
//        public override void OnPointerClick(PointerEventData eventData)
//        {
//            if(eventData.button != PointerEventData.InputButton.Left)
//                return;
//
//            Press();
//        }
//
//
//        public override void OnSubmit(BaseEventData eventData)
//        {
//            Press();
//
//            // if we get set disabled during the press
//            // don't run the coroutine.
//            if(!IsActive() || !IsInteractable())
//                return;
//
//            DoStateTransition(SelectionState.Pressed, false);
//            StartCoroutine(OnFinishSubmit());
//        }
//
//        private System.Collections.IEnumerator OnFinishSubmit()
//        {
//            var fadeTime = colors.fadeDuration;
//            var elapsedTime = 0f;
//
//            while(elapsedTime < fadeTime)
//            {
//                elapsedTime += Time.unscaledDeltaTime;
//                yield return null;
//            }
//
//            DoStateTransition(currentSelectionState, false);
//        }
//
//        private void CheckIsLongPress()
//        {
//            if(isEnableLongPress && m_IsStartPress)
//            {
//                if(onLongClick != null && Time.time > m_CurrentPointerDownTime + m_LongPressTime)
//                {
//                    m_IsLongPressTrigger = true;
//                    m_OnLongClick.Invoke();
//                }
//            }
//        }
//
//        private void Update()
//        {
//            if(isEnableLongPress)
//            {
//                CheckIsLongPress();
//            }
//        }
//
//#if UNITY_EDITOR
//        public bool CheckAddScaleTarget()
//        {
//            bool hasTrueGraphics = false;
//            var graphics = GetComponentsInChildren<Graphic>();
//            foreach (var g in graphics)
//            {
//                if (!(g is FakeGraphic)) hasTrueGraphics = true;
//            }
//            return (ScaleTarget.IsNull() && hasTrueGraphics);
//        }
//
//        public void AutoAddScaleTarget()
//        {
//            if(CheckAddScaleTarget())
//            {
//                try
//                {
//                    ScaleTarget = new GameObject("NonScaleTarget");
//                    var go = gameObject;
//                    string hirName = "";
//                    while (!go.transform.parent.IsNull())
//                    {
//                        go = go.transform.parent.gameObject;
//                        hirName = go.name + "/" + hirName;
//                    }
//                    Debug.Log("Create new NonScaleTarget!"+hirName);
//                    ScaleTarget.transform.SetParent(transform);
//                    ScaleTarget.transform.SetAsFirstSibling();
//                    ScaleTarget.transform.localPosition = Vector3.zero;
//
//                    var rct = ScaleTarget.AddComponent<RectTransform>();
//                    rct.sizeDelta = Vector2.zero;
//                    rct.anchorMin = Vector2.zero;
//                    rct.anchorMax = Vector2.one;
//
//                    ScaleTarget.AddComponent<FakeGraphic>();
//
//                    Image image;
//                    if (TryGetComponent<Image>(out image))
//                    {
//                        image.raycastTarget = false;
//                    }
//                }
//                catch (Exception e)
//                {
//                    Debug.LogException(e);
//                    Debug.LogError("Failed to Add Non-Scale Target for BurnerButton! ");
//
//                    if (!ScaleTarget.IsNull())
//                    {
//                        Destroy(ScaleTarget);
//                        ScaleTarget = null;
//                    }
//                }
//            }
//        }
//
//        protected override void OnValidate()
//        {
//            var stageHandle = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
//            if(stageHandle != null)
//            {
//                var btns = stageHandle.FindComponentsOfType<BurnerButton>();
//                var btnsList = new List<BurnerButton>(btns);
//
//                if(btnsList.Contains(this))
//                {
//                    AutoAddScaleTarget();
//                }
//            }
//        }
//
//        protected override void Reset()
//        {
//            FieldsInitializer.OnTypeReset(this);
//        }
//#endif
//
//    }
//}
