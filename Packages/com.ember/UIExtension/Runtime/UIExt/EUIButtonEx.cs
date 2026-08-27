// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using Sirenix.OdinInspector;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 Button。
    /// 继承自 <see cref="Button"/>，额外支持：
    /// <list type="bullet">
    ///   <item>enable/disable 状态节点切换</item>
    ///   <item>多个附加 Graphic 同步 CrossFadeColor</item>
    /// </list>
    /// </summary>
    [EUIExtension(typeof(Button))]
    [AddComponentMenu("UI/EUI/EUI ButtonEx")]
    public class EUIButtonEx : Button, IEUIExposedChildProvider
    {
        #region 编辑器面板参数

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("启用节点")]
        [Tooltip("EnableState = true 时显示的 GameObject")]
        private GameObject _enableNode;

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("禁用节点")]
        [Tooltip("EnableState = false 时显示的 GameObject")]
        private GameObject _disableNode;

        [FoldoutGroup("附加图形")]
        [SerializeField]
        [LabelText("附加目标图形")]
        [Tooltip("ColorTint 过渡时，这些 Graphic 也会同步 CrossFadeColor")]
        private Graphic[] _additionalGraphics;

        [FoldoutGroup("状态节点")]
        [SerializeField]
        [LabelText("启用状态")]
        private bool _enableState;

        [FoldoutGroup("引用")]
        [SerializeField]
        [LabelText("文本")]
        [Tooltip("按钮的 label 文本槽位，Inspector 拖入子文本后可直接通过 Label 属性访问。")]
        private TMP_Text _label;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshEnableState();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 自定义启用状态。为 true 时显示 _enableNode 并隐藏 _disableNode。
        /// 如果两个节点都为空，始终返回 true。
        /// </summary>
        public bool EnableState
        {
            get => _enableState || (!_enableNode && !_disableNode);
            set
            {
                if (_enableState != value)
                {
                    _enableState = value;
                    RefreshEnableState();
                }
            }
        }

        /// <summary>刷新启用/禁用节点的可见性</summary>
        public void RefreshEnableState()
        {
            if (_enableState)
            {
                if (_enableNode) _enableNode.SetActive(true);
                if (_disableNode) _disableNode.SetActive(false);
            }
            else
            {
                if (_enableNode) _enableNode.SetActive(false);
                if (_disableNode) _disableNode.SetActive(true);
            }
        }

        /// <summary>附加的目标图形数组</summary>
        public Graphic[] AdditionalGraphics
        {
            get => _additionalGraphics;
            set => _additionalGraphics = value;
        }

        /// <summary>
        /// 按钮的 label 文本槽位。Inspector 拖入后可直接访问，
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

        // --------------------------------------------------------

        #region 内部方法

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (transition == Transition.ColorTint)
            {
                Color tintColor;
                switch (state)
                {
                    case SelectionState.Normal:    tintColor = colors.normalColor;    break;
                    case SelectionState.Highlighted: tintColor = colors.highlightedColor; break;
                    case SelectionState.Pressed:   tintColor = colors.pressedColor;   break;
                    case SelectionState.Selected:  tintColor = colors.selectedColor;  break;
                    case SelectionState.Disabled:  tintColor = colors.disabledColor;  break;
                    default:                       tintColor = Color.black;           break;
                }

                var duration = instant ? 0f : colors.fadeDuration;

                if (targetGraphic != null)
                    targetGraphic.CrossFadeColor(tintColor, duration, true, true);

                if (_additionalGraphics != null && _additionalGraphics.Length > 0)
                {
                    foreach (var g in _additionalGraphics)
                    {
                        g.CrossFadeColor(tintColor, duration, true, true);
                    }
                }
            }
            else
            {
                base.DoStateTransition(state, instant);
            }
        }

        #endregion
    }
}
