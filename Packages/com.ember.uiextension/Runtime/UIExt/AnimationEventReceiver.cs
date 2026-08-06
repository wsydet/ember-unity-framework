// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// Animation 事件接收器。
    /// 挂载到带有 Animator 的 GameObject 上，将 Animation Event 字符串转发为 C# 回调，
    /// 避免直接在 Animation Clip 中绑定 MonoBehaviour 方法。
    /// </summary>
    /// <example>
    /// Animation Clip 中添加 Event，参数填 "OnShowDone"，然后：
    /// <code>
    /// var receiver = GetComponent《AnimationEventReceiver》();
    /// receiver.AnimationEventCallback += (evt) => Debug.Log($"动画事件: {evt}");
    /// </code>
    /// </example>
    public class AnimationEventReceiver : MonoBehaviour
    {
        #region 内部参数

        private Action<string> _animationEventCallback;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 动画事件回调。Animation Clip 触发 Event 时传入的字符串参数将作为回调参数。
        /// </summary>
        public Action<string> AnimationEventCallback
        {
            get => _animationEventCallback;
            set => _animationEventCallback = value;
        }

        /// <summary>
        /// 由 Animation Clip 的 Event 调用。
        /// 将事件名称转发给 <see cref="AnimationEventCallback"/>。
        /// </summary>
        /// <param name="evtName">Animation Event 中填写的字符串参数</param>
        public void OnAnimationEvent(string evtName)
        {
            _animationEventCallback?.Invoke(evtName);
        }

        #endregion
    }
}
