using System;

namespace Ember.UI
{
    /// <summary>
    /// UI 界面生命周期接口。
    ///
    /// 每个需要被 EmberUIManager 管理的界面（面板、弹窗、全屏页等）
    /// 都必须实现此接口。Manager 在适当的时机调用对应方法。
    ///
    /// 生命周期顺序：
    /// <code>
    /// Push → OnOpen(args)   ← 界面首次展示
    ///     → OnPause()       ← 另一个界面 Push 到上面
    ///     → OnResume()      ← 上面的界面 Pop 了
    ///     → OnClose()       ← 自己被 Pop / Close
    /// </code>
    ///
    /// 典型实现：
    /// <code>
    /// public class UISettingsPanel : MonoBehaviour, IUIView
    /// {
    ///     public void OnOpen(object args)  { /* 初始化控件，注册事件 */ }
    ///     public void OnClose()            { /* 注销事件，释放引用 */ }
    ///     public void OnPause()            { /* 停止动画、计时器 */ }
    ///     public void OnResume()           { /* 恢复动画，刷新数据 */ }
    /// }
    /// </code>
    /// </summary>
    public interface IUIView
    {
        /// <summary>
        /// 界面首次打开时调用。在预制体实例化之后触发。
        /// 在此方法中进行控件绑定、事件注册、UI 初始化。
        /// </summary>
        /// <param name="args">打开时传入的参数，无参数时为 null</param>
        void OnOpen(object args);

        /// <summary>
        /// 界面被关闭时调用。在此方法中注销事件、释放引用、清理资源。
        /// 之后 GameObject 会被 EmberUIManager 销毁。
        /// </summary>
        void OnClose();

        /// <summary>
        /// 另一个界面被 Push 到此界面上方时调用。
        /// 此界面不会被销毁，只是被遮挡。可在此暂停动画、计时器等。
        /// </summary>
        void OnPause();

        /// <summary>
        /// 上方界面被 Pop 后，此界面重新回到栈顶时调用。
        /// 在此刷新数据、恢复动画。接在 OnPause 之后触发。
        /// </summary>
        void OnResume();
    }
}
