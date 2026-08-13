// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.UI
{
    /// <summary>
    /// 标记 UI 元素为持久节点 —— EUIViewEngine 初始化时不会隐藏此节点。
    ///
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item>EUIBootSplash：开局黑幕，Init 退出时才关闭</item>
    ///   <item>其他需要在框架初始化期间保持可见的 UI 元素</item>
    /// </list>
    ///
    /// <para>实现此接口的 MonoBehaviour 会自动豁免 UIManager.Init 中的全量隐藏。</para>
    /// </summary>
    public interface IEUIPersistentUI
    {
    }
}
