// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;

using Sirenix.OdinInspector;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 Toggle。
    /// 继承自 <see cref="Toggle"/>，额外支持三个状态节点的自动切换：
    /// on 节点 / off 节点 / disable 节点。
    /// </summary>
    [EUIExtension(typeof(Toggle))]
    [AddComponentMenu("UI/EUI/Toggle Ex")]
    public class EUIToggleEx : Toggle, IEUIExposedChildProvider
    {
        #region 编辑器面板参数

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("On 节点")]
        [Tooltip("isOn = true 时显示的 GameObject")]
        private GameObject _onNode;

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("Off 节点")]
        [Tooltip("isOn = false 时显示的 GameObject")]
        private GameObject _offNode;

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("禁用节点")]
        [Tooltip("interactable = false 时显示的 GameObject")]
        private GameObject _disableNode;

        [FoldoutGroup("引用")]
        [SerializeField]
        [LabelText("文本")]
        [Tooltip("开关的 label 文本槽位，Inspector 拖入子文本后可直接通过 Label 属性访问。")]
        private TMP_Text _label;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(OnValueChanged);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshVisibility();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void RefreshVisibility()
        {
            if (interactable || !_disableNode)
            {
                if (isOn)
                {
                    if (_onNode) _onNode.SetActive(true);
                    if (_offNode) _offNode.SetActive(false);
                }
                else
                {
                    if (_onNode) _onNode.SetActive(false);
                    if (_offNode) _offNode.SetActive(true);
                }

                if (_disableNode) _disableNode.SetActive(false);
            }
            else
            {
                if (_disableNode) _disableNode.SetActive(true);
                if (_onNode) _onNode.SetActive(false);
                if (_offNode) _offNode.SetActive(false);
            }
        }

        private void OnValueChanged(bool isOn)
        {
            RefreshVisibility();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            RefreshVisibility();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>手动刷新节点可见性</summary>
        public void Refresh() => RefreshVisibility();

        /// <summary>On 状态节点</summary>
        public GameObject OnNode { get => _onNode; set => _onNode = value; }
        /// <summary>Off 状态节点</summary>
        public GameObject OffNode { get => _offNode; set => _offNode = value; }
        /// <summary>禁用状态节点</summary>
        public GameObject DisableNode { get => _disableNode; set => _disableNode = value; }

        /// <summary>
        /// 开关的 label 文本槽位。Inspector 拖入后可直接访问，
        /// 无需在顶层 EUIBinding 里额外为文本建绑定。
        /// </summary>
        public TMP_Text Label
        {
            get => _label;
            set => _label = value;
        }

        /// <summary>返回槽位持有的子组件（自动收集时会跳过这些节点）。</summary>
        public IEnumerable<Component> GetOwnedChildren()
        {
            if (_label != null)
                yield return _label;
        }

        #endregion
    }
}
