using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Main 开屏动画基类（抽象） —— 挂在 MainScene 的任意 GameObject 上。
    ///
    /// 收到 <see cref="EmberBroadcastEvent.MainSceneReady"/> 后依次：
    /// 1. 加载兜底背景页（与开屏动画并行）
    /// 2. <see cref="PlayOpeningAnimation"/> 播放动画（子类实现）
    /// 3. 等背景页加载+显示完成、动画也结束后，才广播 <see cref="EmberBroadcastEvent.OpeningAnimationEnd"/>
    ///
    /// 这样保证 MainState 在背景兜底 UI 就绪后才打开首页，避免背景与 MainUI 抢跑穿帮。
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

        private async void OnMainSceneReady()
        {
            // 背景页与开屏动画并行加载；两者都完成后才结束开屏动画。
            // 保证 MainState 在背景兜底 UI 就绪后才打开首页，避免穿帮。
            var backgroundLoad = EUIManager.Instance.SetBackgroundAsync(GamePages.EUIBackgroundPage);
            await PlayOpeningAnimation();
            await backgroundLoad;

            EmberEventBus.OnNext(EmberBroadcastEvent.OpeningAnimationEnd);
        }

        /// <summary>
        /// 播放开屏动画。子类实现此方法，动画结束后返回（框架 await）。
        /// 框架会自动等待背景页就绪后才广播 OpeningAnimationEnd，子类无需手动触发完成。
        /// </summary>
        protected abstract UniTask PlayOpeningAnimation();
    }
}
