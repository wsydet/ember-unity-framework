// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Cysharp.Threading.Tasks;

namespace Ember.UI
{
    /// <summary>
    /// 方块过渡效果接口。
    /// 核心 UI 程序集（Ember.UI.Runtime）通过此接口与过渡组件解耦，
    /// 具体实现（EUITransitionBlock）放在 Ember.UIExtension.Runtime 程序集中。
    /// 所有参数在具体实现的面板上配置，选中对应子物体即可编辑和预览。
    /// </summary>
    public interface IEUITransitionEffect
    {
        /// <summary>是否有活跃的方块</summary>
        bool HasActiveBlocks { get; }

        /// <summary>播放进入动画（方块覆盖屏幕）</summary>
        UniTask PlayEnterAsync(float duration = -1f);

        /// <summary>播放退出动画（方块移出揭示内容）</summary>
        UniTask PlayExitAsync(float duration = -1f);

        /// <summary>立即隐藏所有方块（无动画）</summary>
        void HideAllImmediate();
    }
}
