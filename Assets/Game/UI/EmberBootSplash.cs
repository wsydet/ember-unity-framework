using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 启动遮罩 —— Frame 0 遮挡画面，首次 Init→Main 完成后自动隐藏。
    /// 挂在 UIRoot 下的 BootSplash 上，默认显示，运行时自管理。
    /// </summary>
    public class EmberBootSplash : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private void Awake()
        {
            // Frame 0 确保遮挡
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);
        }

        private void OnDestroy()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);
        }

        private void OnFirstLoadDone()
        {
            // Init→Main 只触发一次，之后取消订阅防止重复触发
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnFirstLoadDone);

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // 销毁自己，不再需要
            Destroy(gameObject);
        }
    }
}
