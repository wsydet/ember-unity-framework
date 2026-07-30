//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class ToggleGroupEx : ToggleGroup
//    {
//        UnityEvent<int, int> mToggleGroupValueChange = new UnityEvent<int, int>();
//        Dictionary<Toggle, int> callbackAddedToggle = new Dictionary<Toggle, int>();
//        public int currentOn = -1;
//        int pendingIndex = -1;
//        bool callbackAdded;
//        int toggleCount;
//        void AddCallback()
//        {
//            if(toggleCount != m_Toggles.Count)
//            {
//                callbackAdded = false;
//            }
//            if (!callbackAdded)
//            {
//                if (m_Toggles.Count > 0)
//                {
//                    callbackAdded = true;
//                    toggleCount = m_Toggles.Count;
//
//
//                    int i = 0;
//                    m_Toggles.Sort((a, b) =>
//                    {
//                        return a.transform.GetSiblingIndex() - b.transform.GetSiblingIndex();
//                    });
//                    for (i = 0; i < toggleCount; i++)
//                    {
//                        Toggle item = m_Toggles[i];
//                        int curIndex = i;
//                        if (item.isOn) currentOn = curIndex;
//                        if (!callbackAddedToggle.ContainsKey(item))
//                        {
//                            item.onValueChanged.AddListener(delegate (bool isOn)
//                            {
//                                if (isOn && callbackAddedToggle.TryGetValue(item, out var seletectIdx))
//                                {
//                                    mToggleGroupValueChange.Invoke(seletectIdx, currentOn);
//                                    currentOn = seletectIdx;
//                                }
//                            });
//                        }
//                        callbackAddedToggle[item] = curIndex;
//                    }
//                }
//            }
//        }
//
//        public UnityEvent<int, int> ToggleGroupValueChange
//        {
//            get
//            {
//                AddCallback();
//                return mToggleGroupValueChange;
//            }
//        }
//
//        public int GetCurrentOnToggle()
//        {
//            AddCallback();
//            for (int i = 0; i < m_Toggles.Count; i++)
//            {
//                if (m_Toggles[i].isOn)
//                {
//                    currentOn = i;
//                    return currentOn;
//                }
//            }
//            return -1;
//        }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            if (pendingIndex >= 0)
//            {
//                SetToggleOn(pendingIndex);
//            }
//        }
//
//        // protected override void OnRectTransformDimensionsChange()
//        // {
//        //     if (callbackAdded)
//        //     {
//        //         if(toggleCount != m_Toggles.Count)
//        //         {
//        //             callbackAdded = false;
//        //             AddCallback();
//        //         }
//        //     }
//        // }
//
//        public void SetToggleOn(int index)
//        {
//            AddCallback();
//            
//            if (!callbackAdded)
//            {
//                pendingIndex = index;
//                return;
//            }
//
//            if (index >= 0 && index < m_Toggles.Count && m_Toggles[index] != null)
//            {
//                currentOn = index;
//                m_Toggles[index].isOn = true;
//            }
//            else if (allowSwitchOff)
//            {
//                if (currentOn >= 0 && currentOn < m_Toggles.Count && m_Toggles[currentOn] != null)
//                {
//                    m_Toggles[currentOn].isOn = false;
//                    currentOn = -1;
//                }
//            }
//        }
//
//        private void Update()
//        {
//            if (!callbackAdded)
//            {
//                AddCallback();
//            }
//        }
//    }
//}
