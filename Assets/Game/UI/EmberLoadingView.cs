using Ember.Core;
using Ember.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Loading 进度界面。挂在 UIRoot 下，全程常驻不销毁。
    /// 游戏内场景切换（Main↔Gameplay 等）时自动显隐 + 更新进度。
    /// </summary>
    public class EmberLoadingView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _progressBar;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private string _loadingFormat = "加载中... {0}%";

        private bool _isLoading;
        private bool _pendingHide;
        private bool _firstLoad = true; // Init→Main 跳过（BootSplash 覆盖）

        private void Awake()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadStart, Show);
            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadDone, OnLoadDone);
        }

        private void OnDestroy()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadStart, Show);
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnLoadDone);
        }

        private void Update()
        {
            if (!_isLoading) return;

            float progress = EmberSceneManager.Instance.DisplayProgress;
            SetProgress(progress);

            if (_pendingHide && progress >= 1f)
                Hide();
        }

        private void Show()
        {
            if (_firstLoad) return; // Init→Main 由 BootSplash 负责，跳过进度条

            _isLoading = true;
            _pendingHide = false;
            SetProgress(0f);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        private void Hide()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _isLoading = false;
            _pendingHide = false;
            SetProgress(0f);
        }

        private void OnLoadDone()
        {
            _firstLoad = false; // 首次加载完成，后续切换启用进度条
            _pendingHide = true;
        }

        private void SetProgress(float progress)
        {
            if (_progressBar != null)
                _progressBar.fillAmount = progress;
            if (_statusText != null)
                _statusText.text = string.Format(_loadingFormat, Mathf.RoundToInt(progress * 100f));
        }
    }
}
