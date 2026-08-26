using System;
using Cysharp.Threading.Tasks;

namespace Ember.Core
{
    /// <summary>
    /// BootSplash 淡出等待桥接。
    ///
    /// BootSplash（业务层）在 Awake 时注册此委托，
    /// InitState（框架层）在 TransitionTo《MainState》前 await 此委托，
    /// 确保黑幕完全消失后开屏动画才开始。
    ///
    /// 如果业务层未注册（无 BootSplash），await 立即完成。
    /// </summary>
    public static class EmberBootSplashBridge
    {
        /// <summary>
        /// 等待 BootSplash 淡出完成。
        /// BootSplash 在 Awake 时设置此委托，InitState 在 TransitionTo 前 await。
        /// 如果为 null 则立即返回 CompletedTask。
        /// </summary>
        public static Func<UniTask> WaitForFadeOut { get; set; }
    }
}
