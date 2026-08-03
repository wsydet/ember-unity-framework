using System;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 启动动画基类 —— 挂在 MainScene 的任意 GameObject 上。
    ///
    /// 收到 <see cref="EmberBroadcastEvent.InitSceneReady"/> 后调用
    /// <see cref="PlayStartupAnimation"/>，完成后自动广播 InitAnimationDone，
    /// InitState 继续过渡到 MainState。
    ///
    /// <b>自定义动画：</b>
    /// 子类 override <see cref="PlayStartupAnimation"/>，动画完成后调用 <c>onComplete</c>。
    /// </summary>
    public class EmberInitAnimationStarter : MonoBehaviour
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

        protected virtual void PlayStartupAnimation(Action onComplete)
        {
            onComplete();
        }
    }
}
