// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 扩展组件标记 Attribute。
    /// 用于标记一个类为 UI 扩展组件，并关联对应的 Unity 原生组件类型。
    /// 编辑器工具通过此 Attribute 发现可替换原生组件的扩展组件。
    /// </summary>
    /// <example>
    /// <code>
    /// [EmberUIExtension(typeof(Button))]
    /// public class EmberButtonEx : GameUIComponent { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EmberUIExtensionAttribute : Attribute
    {
        #region 内部参数

        private Type _componentType;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 关联的原生 Unity 组件类型。
        /// </summary>
        public Type ComponentType
        {
            get => _componentType;
            set => _componentType = value;
        }

        /// <summary>
        /// 组件在 Inspector 中的显示名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 标记目标类为 UI 扩展组件。
        /// </summary>
        /// <param name="componentType">关联的原生 Unity 组件类型</param>
        public EmberUIExtensionAttribute(Type componentType)
        {
            _componentType = componentType;
        }

        #endregion
    }
}
