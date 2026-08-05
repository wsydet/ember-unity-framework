using System;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 启动动画基类（抽象） —— 挂在 MainScene 的任意 GameObject 上。
    ///
    /// 收到 <see cref="EmberBroadcastEvent.InitSceneReady"/> 后调用
    /// <see cref="PlayStartupAnimation"/>，子类实现动画逻辑，完成后调用 <c>onComplete</c>。
    /// 框架自动广播 <see cref="EmberBroadcastEvent.InitAnimationDone"/>，
    /// InitState 继续过渡到 MainState。
    ///
    /// <b>不要直接使用本类</b>，使用子类 <see cref="EmberDefaultInitAnimation"/>（无动画，立即完成）
    /// 或自定义子类 override <see cref="PlayStartupAnimation"/>。
    /// </summary>
    public abstract class EmberInitAnimationStarter : MonoBehaviour
    {
        private void Awake()
        {
            EmberEventBus.Subscribe(EmberBroadcastEvent.InitSceneReady, OnSceneReady);
        }

        private void OnDestroy()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.InitSceneReady, OnSceneReady);
        }

        private void OnSceneReady()
        {
            PlayStartupAnimation(() =>
            {
                EmberEventBus.OnNext(EmberBroadcastEvent.InitAnimationDone);
            });
        }

        /// <summary>
        /// 播放启动动画。子类实现此方法，动画结束后必须调用 <c>onComplete()</c>。
        /// </summary>
        /// <param name="onComplete">动画结束回调，<b>必须调用</b>，否则状态机卡在 Init</param>
        protected abstract void PlayStartupAnimation(Action onComplete);
    }
}
