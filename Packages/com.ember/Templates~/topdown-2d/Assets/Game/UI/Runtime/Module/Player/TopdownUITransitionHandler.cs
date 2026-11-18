using Cysharp.Threading.Tasks;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>俯视玩法以 timeScale 暂停世界；UI 预设过渡继续使用非缩放时间。</summary>
    internal sealed class TopdownUITransitionHandler : IEUITransitionHandler
    {
        #region 外部方法

        public UniTask PlayShowAsync(GameObject page, float duration) => Fade(page, duration, true);

        public UniTask PlayHideAsync(GameObject page, float duration) => Fade(page, duration, false);

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static async UniTask Fade(GameObject page, float duration, bool opening)
        {
            var group = page ? page.GetComponent<CanvasGroup>() : null;
            float elapsed = 0;
            while (page && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (group)
                {
                    float amount = Mathf.Clamp01(elapsed / duration);
                    group.alpha = opening ? amount : 1 - amount;
                }
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        #endregion
    }
}
