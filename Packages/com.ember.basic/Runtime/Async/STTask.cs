// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// 轻量级单线程 Task-like 值类型。零 GC 分配的异步原语。
    /// </summary>
    [AsyncMethodBuilder(typeof(STAsyncTaskMethodBuilder))]
    public partial struct STTask : IEquatable<STTask>
    {
        public static STTask CompletedTask => new();

        private readonly IAwaiter _awaiter;

        [DebuggerHidden]
        public STTask(IAwaiter awaiter)
        {
            _awaiter = awaiter;
        }

        [DebuggerHidden]
        public AwaiterStatus Status => _awaiter?.Status ?? AwaiterStatus.Succeeded;

        [DebuggerHidden]
        public bool IsCompleted => _awaiter?.IsCompleted ?? true;

        [DebuggerHidden]
        public void GetResult()
        {
            _awaiter?.GetResult();
        }

        public void Coroutine() { }

        [DebuggerHidden]
        public Awaiter GetAwaiter() => new(this);

        public bool Equals(STTask other)
        {
            if (_awaiter == null && other._awaiter == null) return true;
            if (_awaiter != null && other._awaiter != null) return _awaiter == other._awaiter;
            return false;
        }

        public override int GetHashCode() => _awaiter?.GetHashCode() ?? 0;

        public override string ToString()
        {
            return _awaiter == null ? "()"
                : _awaiter.Status == AwaiterStatus.Succeeded ? "()"
                : $"({_awaiter.Status})";
        }

        public struct Awaiter : IAwaiter
        {
            private readonly STTask _task;

            [DebuggerHidden]
            public Awaiter(STTask task) => _task = task;

            [DebuggerHidden]
            public bool IsCompleted => _task.IsCompleted;

            [DebuggerHidden]
            public AwaiterStatus Status => _task.Status;

            [DebuggerHidden]
            public void GetResult() => _task.GetResult();

            [DebuggerHidden]
            public void OnCompleted(Action continuation)
            {
                if (_task._awaiter != null)
                    _task._awaiter.OnCompleted(continuation);
                else
                    continuation();
            }

            [DebuggerHidden]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (_task._awaiter != null)
                    _task._awaiter.UnsafeOnCompleted(continuation);
                else
                    continuation();
            }
        }
    }

    /// <summary>
    /// 轻量级单线程 Task-like 值类型（带返回值）。
    /// </summary>
    [AsyncMethodBuilder(typeof(STAsyncTaskMethodBuilder<>))]
    public struct STTask<T> : IEquatable<STTask<T>>
    {
        private readonly T _result;
        private readonly IAwaiter<T> _awaiter;

        [DebuggerHidden]
        public STTask(T result)
        {
            _result = result;
            _awaiter = null;
        }

        [DebuggerHidden]
        public STTask(IAwaiter<T> awaiter)
        {
            _result = default;
            _awaiter = awaiter;
        }

        [DebuggerHidden]
        public AwaiterStatus Status => _awaiter?.Status ?? AwaiterStatus.Succeeded;

        [DebuggerHidden]
        public bool IsCompleted => _awaiter?.IsCompleted ?? true;

        [DebuggerHidden]
        public T Result => _awaiter == null ? _result : _awaiter.GetResult();

        public void Coroutine() { }

        [DebuggerHidden]
        public Awaiter GetAwaiter() => new(this);

        public bool Equals(STTask<T> other)
        {
            if (_awaiter == null && other._awaiter == null)
                return EqualityComparer<T>.Default.Equals(_result, other._result);
            if (_awaiter != null && other._awaiter != null)
                return _awaiter == other._awaiter;
            return false;
        }

        public override int GetHashCode()
        {
            if (_awaiter == null)
                return _result?.GetHashCode() ?? 0;
            return _awaiter.GetHashCode();
        }

        public override string ToString()
        {
            return _awaiter == null ? _result?.ToString()
                : _awaiter.Status == AwaiterStatus.Succeeded ? _awaiter.GetResult()?.ToString()
                : $"({_awaiter.Status})";
        }

        public static implicit operator STTask(STTask<T> task)
        {
            return task._awaiter != null ? new STTask(task._awaiter) : new STTask();
        }

        public struct Awaiter : IAwaiter<T>
        {
            private readonly STTask<T> _task;

            [DebuggerHidden]
            public Awaiter(STTask<T> task) => _task = task;

            [DebuggerHidden]
            public bool IsCompleted => _task.IsCompleted;

            [DebuggerHidden]
            public AwaiterStatus Status => _task.Status;

            [DebuggerHidden]
            void IAwaiter.GetResult() => GetResult();

            [DebuggerHidden]
            public T GetResult() => _task.Result;

            [DebuggerHidden]
            public void OnCompleted(Action continuation)
            {
                if (_task._awaiter != null)
                    _task._awaiter.OnCompleted(continuation);
                else
                    continuation();
            }

            [DebuggerHidden]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (_task._awaiter != null)
                    _task._awaiter.UnsafeOnCompleted(continuation);
                else
                    continuation();
            }
        }
    }
}
