// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 Toggle。
    /// 继承自 <see cref="Toggle"/>，额外支持三个状态节点的自动切换：
    /// on 节点 / off 节点 / disable 节点。
    /// </summary>
    [AddComponentMenu("UI/EUI/Toggle Ex")]
    public class EUIToggleEx : Toggle
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

        #endregion
    }
}
