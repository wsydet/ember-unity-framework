// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// 标记该 GameObject 及其子节点不参与 UIBinding 的自动收集和扫描。
    /// 适用于背景图片、纯装饰节点等不需要绑定的 UI 元素。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("EUI/EUI Binding Exclude")]
    public sealed class EUIBindingExclude : MonoBehaviour
    {
    }
}
