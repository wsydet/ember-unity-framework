// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// STTask 的 async/await 编译器构建器（无返回值）。
    /// </summary>
    public struct STAsyncTaskMethodBuilder
    {
        private STTaskCompletionSource _tcs;
        private Action _moveNext;

        [DebuggerHidden]
        public static STAsyncTaskMethodBuilder Create() => new();

        [DebuggerHidden]
        public STTask Task
        {
            get
            {
                if (_tcs != null)
                    return _tcs.Task;

                if (_moveNext == null)
                    return STTask.CompletedTask;

                _tcs = new STTaskCompletionSource();
                return _tcs.Task;
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            _tcs ??= new STTaskCompletionSource();

            if (exception is OperationCanceledException ex)
                _tcs.TrySetCanceled(ex);
            else
                _tcs.TrySetException(exception);
        }

        [DebuggerHidden]
        public void SetResult()
        {
            if (_moveNext == null) return;

            _tcs ??= new STTaskCompletionSource();
            _tcs.TrySetResult();
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_moveNext == null)
            {
                _tcs ??= new STTaskCompletionSource();
                var runner = new MoveNextRunner<TStateMachine>();
                _moveNext = runner.Run;
                runner.StateMachine = stateMachine;
            }

            awaiter.OnCompleted(_moveNext);
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_moveNext == null)
            {
                _tcs ??= new STTaskCompletionSource();
                var runner = new MoveNextRunner<TStateMachine>();
                _moveNext = runner.Run;
                runner.StateMachine = stateMachine;
            }

            awaiter.UnsafeOnCompleted(_moveNext);
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }

    /// <summary>
    /// STTask 的 async/await 编译器构建器（带返回值）。
    /// </summary>
    public struct STAsyncTaskMethodBuilder<T>
    {
        private T _result;
        private STTaskCompletionSource<T> _tcs;
        private Action _moveNext;

        [DebuggerHidden]
        public static STAsyncTaskMethodBuilder<T> Create() => new();

        [DebuggerHidden]
        public STTask<T> Task
        {
            get
            {
                if (_tcs != null)
                    return new STTask<T>(_tcs);

                if (_moveNext == null)
                    return new STTask<T>(_result);

                _tcs = new STTaskCompletionSource<T>();
                return _tcs.Task;
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            _tcs ??= new STTaskCompletionSource<T>();

            if (exception is OperationCanceledException ex)
                _tcs.TrySetCanceled(ex);
            else
                _tcs.TrySetException(exception);
        }

        [DebuggerHidden]
        public void SetResult(T ret)
        {
            if (_moveNext == null)
            {
                _result = ret;
            }
            else
            {
                _tcs ??= new STTaskCompletionSource<T>();
                _tcs.TrySetResult(ret);
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_moveNext == null)
            {
                _tcs ??= new STTaskCompletionSource<T>();
                var runner = new MoveNextRunner<TStateMachine>();
                _moveNext = runner.Run;
                runner.StateMachine = stateMachine;
            }

            awaiter.OnCompleted(_moveNext);
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_moveNext == null)
            {
                _tcs ??= new STTaskCompletionSource<T>();
                var runner = new MoveNextRunner<TStateMachine>();
                _moveNext = runner.Run;
                runner.StateMachine = stateMachine;
            }

            awaiter.UnsafeOnCompleted(_moveNext);
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
}
