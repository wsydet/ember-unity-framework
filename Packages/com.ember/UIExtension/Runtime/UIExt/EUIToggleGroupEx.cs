// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 ToggleGroup。
    /// 继承自 <see cref="ToggleGroup"/>，额外提供：
    /// <list type="bullet">
    ///   <item>按 sibling index 排序的 Toggle 索引</item>
    ///   <item>索引变化事件（新选中索引, 旧选中索引）</item>
    ///   <item>SetToggleOn(index) 按索引切换选中</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("UI/EUI/Toggle Group Ex")]
    public class EUIToggleGroupEx : ToggleGroup
    {
        #region 编辑器面板参数

        // ToggleGroup 本身不需要额外序列化字段

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private readonly UnityEvent<int, int> _toggleGroupValueChange = new UnityEvent<int, int>();
        private readonly Dictionary<Toggle, int> _callbackAddedToggle = new Dictionary<Toggle, int>();
        private int _currentOn = -1;
        private int _pendingIndex = -1;
        private bool _callbackAdded;
        private int _toggleCount;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_pendingIndex >= 0)
                SetToggleOn(_pendingIndex);
        }

        private void Update()
        {
            if (!_callbackAdded)
                AddCallback();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// ToggleGroup 值变化事件。
        /// 参数：(新选中索引, 旧选中索引)
        /// </summary>
        public UnityEvent<int, int> ToggleGroupValueChange
        {
            get
            {
                AddCallback();
                return _toggleGroupValueChange;
            }
        }

        /// <summary>获取当前选中的 Toggle 索引，无选中返回 -1</summary>
        public int GetCurrentOnToggle()
        {
            AddCallback();
            for (int i = 0; i < m_Toggles.Count; i++)
            {
                if (m_Toggles[i].isOn)
                {
                    _currentOn = i;
                    return _currentOn;
                }
            }
            return -1;
        }

        /// <summary>按索引设置选中的 Toggle</summary>
        public void SetToggleOn(int index)
        {
            AddCallback();

            if (!_callbackAdded)
            {
                _pendingIndex = index;
                return;
            }

            if (index >= 0 && index < m_Toggles.Count && m_Toggles[index] != null)
            {
                _currentOn = index;
                m_Toggles[index].isOn = true;
            }
            else if (allowSwitchOff)
            {
                if (_currentOn >= 0 && _currentOn < m_Toggles.Count && m_Toggles[_currentOn] != null)
                {
                    m_Toggles[_currentOn].isOn = false;
                    _currentOn = -1;
                }
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void AddCallback()
        {
            if (_toggleCount != m_Toggles.Count)
                _callbackAdded = false;

            if (!_callbackAdded && m_Toggles.Count > 0)
            {
                _callbackAdded = true;
                _toggleCount = m_Toggles.Count;

                m_Toggles.Sort((a, b) => a.transform.GetSiblingIndex() - b.transform.GetSiblingIndex());

                for (int i = 0; i < _toggleCount; i++)
                {
                    var item = m_Toggles[i];
                    var curIndex = i;
                    if (item.isOn) _currentOn = curIndex;

                    if (!_callbackAddedToggle.ContainsKey(item))
                    {
                        item.onValueChanged.AddListener(delegate (bool isOn)
                        {
                            if (isOn && _callbackAddedToggle.TryGetValue(item, out var selectIdx))
                            {
                                _toggleGroupValueChange.Invoke(selectIdx, _currentOn);
                                _currentOn = selectIdx;
                            }
                        });
                    }
                    _callbackAddedToggle[item] = curIndex;
                }
            }
        }

        #endregion
    }
}
