// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// Animator 过渡桥组件 —— 页面启用 Animator 过渡时，框架会优先复用 Animator 节点上预挂的本组件，
    /// 承接动画片段尾帧的 Animation Event，把动画完成信号转发给 <see cref="EUIPage"/>。
    /// </summary>
    /// <para><b>帧事件方法名约定（动画片段尾帧添加）：</b>
    /// <list type="bullet">
    ///   <item>打开动画 <c>EmberOpen</c> → 事件调用 <c>OnEmberOpenAnimationEnd</c></item>
    ///   <item>关闭动画 <c>EmberClose</c> → 事件调用 <c>OnEmberCloseAnimationEnd</c></item>
    /// </list>
    /// 框架对帧事件有超时兜底（5 秒强制完成 + 警告），事件拼错/漏加不会卡死页面，但会拖慢转场。</para>
    public class EmberPageAnimatorBridge : MonoBehaviour
    {
        private UniTaskCompletionSource _openTcs;
        private UniTaskCompletionSource _closeTcs;

        /// <summary>开始等待打开动画完成（由 EUIPage 调用）。</summary>
        internal UniTask WaitOpenAsync()
        {
            _openTcs = new UniTaskCompletionSource();
            return _openTcs.Task;
        }

        /// <summary>开始等待关闭动画完成（由 EUIPage 调用）。</summary>
        internal UniTask WaitCloseAsync()
        {
            _closeTcs = new UniTaskCompletionSource();
            return _closeTcs.Task;
        }

        /// <summary>清空等待状态（过渡结束后由 EUIPage 调用，支持页面复用）。</summary>
        internal void Reset()
        {
            _openTcs = null;
            _closeTcs = null;
        }

        /// <summary>打开动画帧事件入口 —— 在 EmberOpen 动画片段尾帧添加 Animation Event 调用本方法。</summary>
        public void OnEmberOpenAnimationEnd()
        {
            _openTcs?.TrySetResult();
        }

        /// <summary>关闭动画帧事件入口 —— 在 EmberClose 动画片段尾帧添加 Animation Event 调用本方法。</summary>
        public void OnEmberCloseAnimationEnd()
        {
            _closeTcs?.TrySetResult();
        }
    }
}
