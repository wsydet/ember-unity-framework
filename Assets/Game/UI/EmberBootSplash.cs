using Ember.Core;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 启动遮罩 —— Frame 0 遮挡画面，Init 退出时自动隐藏。
    /// 挂在 UIRoot 下的 BootSplash 上，默认显示，运行时自管理。
    ///
    /// <para>实现 <see cref="IEmberPersistentUI"/>，确保 EmberUIManager 初始化时
    /// 不会隐藏此节点（BootSplash 需要在 Init 期间持续显示）。</para>
    /// </summary>
    public class EmberBootSplash : MonoBehaviour, IEmberPersistentUI
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private void Awake()
        {
            // Frame 0 确保遮挡
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            // 监听 MainScene 加载完成 → Init 退出时关闭黑幕
            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);
        }

        private void OnDestroy()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);
        }

        private void OnFirstLoadDone()
        {
            // 只触发一次，Init→Main 的黑幕
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // 销毁自己，不再需要
            Destroy(gameObject);
        }
    }
}
