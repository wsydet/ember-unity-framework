using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.UI;
using Game.UI;

namespace Game.State
{
    /// <summary>
    /// 游戏层 MainState 子类 —— override 框架钩子，注入业务逻辑。
    ///
    /// <b>背景页加载：</b>
    /// <see cref="LoadBackgroundAsync"/> 由 InitState 在 BootSplash 渐出前调用，
    /// 加载兜底背景页（EUIBackgroundPage），保证黑幕揭开时背景已在底层。
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
        }

        protected override UniTask LoadBackgroundAsync()
        {
            // 简单模式（UseUIBg=true）：InitState 在 BootSplash 渐出前调用本方法，
            // 加载兜底背景页（层级最底 sortingOrder=0），保证黑幕揭开时背景已在底层。
            return EUIManager.Instance.SetBackgroundAsync(GamePages.EUIBackgroundPage);
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
