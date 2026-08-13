using System;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Main 开屏动画基类（抽象） —— 挂在 MainScene 的任意 GameObject 上。
    ///
    /// 收到 <see cref="EmberBroadcastEvent.MainSceneReady"/> 后调用
    /// <see cref="PlayOpeningAnimation"/>，子类实现动画逻辑，完成后调用 <c>onComplete</c>。
    /// 框架自动广播 <see cref="EmberBroadcastEvent.OpeningAnimationEnd"/>，
    /// MainState 收到后调用 <see cref="Ember.Core.MainState.OnOpeningAnimationEnd()"/>。
    ///
    /// <b>不要直接使用本类</b>，使用子类 <see cref="EUIDefaultMainAnimation"/>（无动画，立即完成）
    /// 或自定义子类 override <see cref="PlayOpeningAnimation"/>。
    /// </summary>
    public abstract class EUIMainAnimationStarter : MonoBehaviour
    {
        private void Awake()
        {
            EmberEventBus.Subscribe(EmberBroadcastEvent.MainSceneReady, OnMainSceneReady);
        }

        private void OnDestroy()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.MainSceneReady, OnMainSceneReady);
        }

        private void OnMainSceneReady()
        {
            PlayOpeningAnimation(() =>
            {
                EmberEventBus.OnNext(EmberBroadcastEvent.OpeningAnimationEnd);
            });
        }

        /// <summary>
        /// 播放开屏动画。子类实现此方法，动画结束后必须调用 <c>onComplete()</c>。
        /// </summary>
        /// <param name="onComplete">动画结束回调，<b>必须调用</b>，否则 MainState 永远收不到 OpeningAnimationEnd</param>
        protected abstract void PlayOpeningAnimation(Action onComplete);
    }
}
