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
        protected override void OnOpeningAnimationEnd()
        {
            EmberUIPageRouter.Instance.ShowMainPage(GamePages.MainMenu);
        }
    }
}
