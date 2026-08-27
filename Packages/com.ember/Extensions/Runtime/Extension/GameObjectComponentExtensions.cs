// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using Ember.Basic;
using UnityEngine;

namespace Ember.Extensions
{
    /// <summary>
    /// GameObject / Component 扩展方法 —— 组件获取与添加。
    /// </summary>
    public static class GameObjectComponentExtensions
    {
        #region 外部方法

        /// <summary>
        /// 获取指定类型的组件，如果不存在则自动添加。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="obj">目标 GameObject</param>
        /// <returns>已有的或新添加的组件实例</returns>
        [NoGC]
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            var c = obj.GetComponent<T>();
            return c ? c : obj.AddComponent<T>();
        }

        /// <summary>
        /// 在同一个 GameObject 上获取指定类型的组件，如果不存在则自动添加。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">目标组件（操作在其所在的 GameObject 上执行）</param>
        /// <returns>已有的或新添加的组件实例</returns>
        [NoGC]
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            var c = component.GetComponent<T>();
            return c ? c : component.gameObject.AddComponent<T>();
        }

        #endregion
    }
}
