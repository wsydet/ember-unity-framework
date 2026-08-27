// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.Runtime.CompilerServices;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// 异步操作状态。
    /// </summary>
    public enum AwaiterStatus
    {
        Pending = 0,
        Succeeded = 1,
        Faulted = 2,
        Canceled = 3
    }

    /// <summary>
    /// 无返回值的 Awaiter 接口。
    /// </summary>
    public interface IAwaiter : ICriticalNotifyCompletion
    {
        AwaiterStatus Status { get; }
        bool IsCompleted { get; }
        void GetResult();
    }

    /// <summary>
    /// 有返回值的 Awaiter 接口。
    /// </summary>
    public interface IAwaiter<out T> : IAwaiter
    {
        new T GetResult();
    }

    /// <summary>
    /// AwaiterStatus 扩展方法。
    /// </summary>
    public static class AwaiterStatusExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompleted(this AwaiterStatus status) => status != AwaiterStatus.Pending;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompletedSuccessfully(this AwaiterStatus status) => status == AwaiterStatus.Succeeded;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanceled(this AwaiterStatus status) => status == AwaiterStatus.Canceled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFaulted(this AwaiterStatus status) => status == AwaiterStatus.Faulted;
    }
}
