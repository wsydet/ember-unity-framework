using Cysharp.Threading.Tasks;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>平台模板的暂停菜单使用真实时间；通过 UI 引擎已有扩展点装配。</summary>
    public sealed class PlatformerUITransition : IEUITransitionHandler
    {
        #region 内部方法
        private async UniTask Fade(GameObject page, float duration, bool show)
        {
            var group = page != null ? page.GetComponent<CanvasGroup>() : null;
            float elapsed = 0;
            while (page != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (group != null) group.alpha = show ? Mathf.Clamp01(elapsed / duration) : 1 - Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            if (group != null) group.alpha = show ? 1 : 0;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public UniTask PlayShowAsync(GameObject page, float duration) => Fade(page, duration, true);
        public UniTask PlayHideAsync(GameObject page, float duration) => Fade(page, duration, false);
        #endregion
    }
}
