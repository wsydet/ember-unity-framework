// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.UI;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// 将 EUIItem 适配为 SceneUI View。它是纯 C# 包装，不需要业务 Prefab 挂载额外 Behaviour。
    /// </summary>
    public sealed class EUIItemSceneUIView : ISceneUIView, IDisposable
    {
        private readonly EUIItem _item;

        public EUIItemSceneUIView(EUIItem item)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
        }

        /// <summary>底层 EUI Item。</summary>
        public EUIItem Item => _item;

        /// <summary>Item 的业务逻辑实例。</summary>
        public EUILogic Logic => _item.Logic;

        /// <summary>Item 根对象。</summary>
        public GameObject GameObject => _item.GameObject;

        public RectTransform RectTransform => _item.RectTransform;

        /// <summary>获取强类型 EUI 逻辑；类型不匹配时返回 null。</summary>
        public TLogic GetLogic<TLogic>() where TLogic : EUILogic
        {
            return _item.Logic as TLogic;
        }

        /// <summary>开始本次对象池借用，应早于业务 Binder 写入内容。</summary>
        internal void BeginUse()
        {
            _item.BeginUse();
        }

        public void ApplySpatialState(in SceneUISpatialState state)
        {
            RectTransform rectTransform = _item.RectTransform;
            if (!rectTransform)
                return;

            rectTransform.anchoredPosition = state.AnchoredPosition;
            rectTransform.localScale = Vector3.one * state.Scale;
        }

        public void SetVisible(bool visible)
        {
            if (visible)
                _item.Show();
            else
                _item.Hide();
        }

        public void ResetView()
        {
            _item.ResetForPool();
        }

        public void Dispose()
        {
            _item.Dispose();
        }
    }
}
