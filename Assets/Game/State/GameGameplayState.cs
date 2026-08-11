using Ember.Core;
using Ember.UI;
using Game.UI;

namespace Game.State
{
    /// <summary>
    /// 游戏层 GameplayState 子类 —— override 框架钩子，注入业务逻辑。
    ///
    /// <b>进入玩法 → 打开游戏内 UI：</b>
    /// <see cref="OnGameplayEnter"/> 在场景加载完成后调用，打开 InGameUI。
    ///
    /// <b>退出玩法 → 关闭所有 UI：</b>
    /// <see cref="OnGameplayExit"/> 在离开玩法前调用，清理 UI。
    /// </summary>
    public class GameGameplayState : GameplayState
    {
        protected override void OnGameplayEnter(object args)
        {
            base.OnGameplayEnter(args);
            EUIManager.Instance.ShowMainPage(GamePages.InGameUI);
        }

        protected override void OnGameplayExit()
        {
            EUIManager.Instance.CloseAllPopups();
            EUIManager.Instance.ClosePageByDef(GamePages.InGameUI);
            base.OnGameplayExit();
        }
    }
}
