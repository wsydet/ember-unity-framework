// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

#if UNITY_EDITOR

namespace Ember.Basic
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Editor 模式下用于追踪对象池泄漏的调试工具。
    ///
    /// 使用方式：在 Pool.Pop 时调用 <see cref="IncRef"/>，在 Pool.Push 时调用 <see cref="DecRef"/>。
    /// 如果 Push 次数少于 Pop 次数，说明存在泄漏，可通过 <see cref="AllLeakedObjStacks"/>
    /// 获取泄漏对象的分配堆栈。
    ///
    /// 注意：默认不启用追踪（<see cref="EnableCheck"/> = false），因为分配堆栈会产生大量 GC。
    /// 仅在排查池泄漏时手动开启。
    /// </summary>
    [ForDebug]
    public class PoolRefCount
    {
        /// <summary>
        /// 是否启用泄漏检测。默认 false，开启会产生大量 GC 分配（StackTrace）。
        /// </summary>
        public static bool EnableCheck = false;

        /// <summary>
        /// 当前被追踪的对象数量（已 Pop 但未 Push）。
        /// </summary>
        public int Count => _refStacks.Count;

        private readonly Dictionary<object, string> _refStacks = new();

        /// <summary>
        /// 记录一次 Pop 操作。如果启用了检测，会保存当前的调用堆栈。
        /// </summary>
        public void IncRef(object obj)
        {
            if (EnableCheck)
            {
                _refStacks.Add(obj, new System.Diagnostics.StackTrace(true).ToString());
            }
        }

        /// <summary>
        /// 记录一次 Push 操作（归还），与之前的 Pop 配对。
        /// </summary>
        public void DecRef(object obj)
        {
            if (EnableCheck)
            {
                _refStacks.Remove(obj);
            }
        }

        /// <summary>
        /// 获取所有泄漏对象的分配堆栈汇总。
        /// 相同堆栈的泄漏会被聚合计数。
        /// </summary>
        public string AllLeakedObjStacks()
        {
            var stack = new Dictionary<string, int>();
            foreach (var s in _refStacks.Values)
            {
                if (stack.ContainsKey(s))
                {
                    stack[s]++;
                }
                else
                {
                    stack.Add(s, 1);
                }
            }

            var sb = new StringBuilder();
            foreach (var kv in stack)
            {
                sb.Append($"{kv.Value} leaked objects with following stacktrace:\n")
                  .Append(kv.Key)
                  .Append("\n\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 清空所有追踪记录。
        /// </summary>
        public void ClearAllStacks()
        {
            _refStacks.Clear();
        }
    }
}

#endif
