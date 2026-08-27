// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强组件实现此接口，声明它通过「槽位」持有的子组件。
    /// 绑定自动收集时会跳过这些子节点，避免与增强组件的槽位引用重复绑定
    /// （等价于这些子组件已被增强组件内部绑定，无需在顶层 EUIBinding 里再绑一次）。
    /// </summary>
    public interface IEUIExposedChildProvider
    {
        /// <summary>
        /// 返回本组件通过槽位持有的子组件。
        /// </summary>
        IEnumerable<Component> GetOwnedChildren();
    }
}
