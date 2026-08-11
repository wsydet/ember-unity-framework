using Ember.Core;
using Ember.UI;
using Game.UI;

namespace Game.State
{
    /// <summary>
    /// 游戏层 MainState 子类 —— override 框架钩子，注入业务逻辑。
    ///
    /// <b>开屏动画结束 → 打开首页：</b>
    /// <see cref="OnOpeningAnimationEnd"/> 在 MainState 收到 OpeningAnimationEnd 事件后调用。
    /// 此时 MainScene 已就绪，开屏动画已播放完毕。
    /// </summary>
    public class GameMainState : MainState
    {
        protected override void OnMainEnter(object args)
        {
            base.OnMainEnter(args);
            // 全局注册 Loading 页面：之后所有跨场景 TransitionTo 自动使用
            EUIManager.DefaultLoadingPageDef = GamePages.EUILoadingPage;
            // 背景页：兜底 UI，层级最底（sortingOrder=0）
            EUIManager.Instance.SetBackground(GamePages.EUIBackgroundPage);
        }

        protected override void OnMainExit()
        {
            EUIManager.Instance.ClearBackground();
        }

        protected override void OnOpeningAnimationEnd()
        {
            EUIManager.Instance.ShowMainPage(GamePages.MainMenu);
        }
    }
}
