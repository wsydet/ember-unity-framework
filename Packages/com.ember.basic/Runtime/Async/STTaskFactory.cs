// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Threading;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// STTask 工厂方法（partial struct）。
    /// </summary>
    public partial struct STTask
    {
        public static STTask FromException(Exception ex)
        {
            var tcs = new STTaskCompletionSource();
            tcs.TrySetException(ex);
            return tcs.Task;
        }

        public static STTask<T> FromException<T>(Exception ex)
        {
            var tcs = new STTaskCompletionSource<T>();
            tcs.TrySetException(ex);
            return tcs.Task;
        }

        public static STTask<T> FromResult<T>(T value) => new(value);

        public static STTask FromCanceled() => CanceledSTTaskCache.Task;

        public static STTask<T> FromCanceled<T>() => CanceledSTTaskCache<T>.Task;

        public static STTask FromCanceled(CancellationToken token)
        {
            var tcs = new STTaskCompletionSource();
            tcs.TrySetException(new OperationCanceledException(token));
            return tcs.Task;
        }

        public static STTask<T> FromCanceled<T>(CancellationToken token)
        {
            var tcs = new STTaskCompletionSource<T>();
            tcs.TrySetException(new OperationCanceledException(token));
            return tcs.Task;
        }

        private static class CanceledSTTaskCache
        {
            public static readonly STTask Task;

            static CanceledSTTaskCache()
            {
                var tcs = new STTaskCompletionSource();
                tcs.TrySetCanceled();
                Task = tcs.Task;
            }
        }

        private static class CanceledSTTaskCache<T>
        {
            public static readonly STTask<T> Task;

            static CanceledSTTaskCache()
            {
                var tcs = new STTaskCompletionSource<T>();
                tcs.TrySetCanceled();
                Task = tcs.Task;
            }
        }
    }
}
