// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Ember.Basic.Tasks
{
    /// <summary>
    /// STTask 的完成源（无返回值）。
    /// </summary>
    public class STTaskCompletionSource : IAwaiter
    {
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int _state;
        private ExceptionDispatchInfo _exception;
        private Action _continuation;

        /// <summary>
        /// 用于将多个 CompletionSource 串联为链表。
        /// </summary>
        public STTaskCompletionSource NextTask { get; set; }

        AwaiterStatus IAwaiter.Status => (AwaiterStatus)_state;
        bool IAwaiter.IsCompleted => _state != Pending;
        public STTask Task => new(this);

        void IAwaiter.GetResult()
        {
            switch (_state)
            {
                case Succeeded:
                    return;
                case Faulted:
                    _exception?.Throw();
                    _exception = null;
                    return;
                case Canceled:
                    _exception?.Throw();
                    _exception = null;
                    throw new OperationCanceledException();
                default:
                    throw new NotSupportedException(
                        "STTask does not allow calling GetResult directly when not completed. Use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            _continuation = action;
            if (_state != Pending)
                TryInvokeContinuation();
        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion)this).UnsafeOnCompleted(action);
        }

        private void TryInvokeContinuation()
        {
            _continuation?.Invoke();
            _continuation = null;
        }

        public void SetResult()
        {
            if (TrySetResult()) return;
            throw new InvalidOperationException("STTask already completed.");
        }

        public void SetException(Exception e)
        {
            if (TrySetException(e)) return;
            throw new InvalidOperationException("STTask already completed.");
        }

        public bool TrySetResult()
        {
            if (_state != Pending) return false;
            _state = Succeeded;
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetException(Exception e)
        {
            if (_state != Pending) return false;
            _state = Faulted;
            _exception = ExceptionDispatchInfo.Capture(e);
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetCanceled()
        {
            if (_state != Pending) return false;
            _state = Canceled;
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (_state != Pending) return false;
            _state = Canceled;
            _exception = ExceptionDispatchInfo.Capture(e);
            TryInvokeContinuation();
            return true;
        }
    }

    /// <summary>
    /// STTask 的完成源（带返回值）。
    /// </summary>
    public class STTaskCompletionSource<T> : IAwaiter<T>
    {
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int _state;
        private T _value;
        private ExceptionDispatchInfo _exception;
        private Action _continuation;

        public STTaskCompletionSource<T> NextTask { get; set; }

        bool IAwaiter.IsCompleted => _state != Pending;
        AwaiterStatus IAwaiter.Status => (AwaiterStatus)_state;
        public STTask<T> Task => new(this);

        T IAwaiter<T>.GetResult()
        {
            switch (_state)
            {
                case Succeeded:
                    return _value;
                case Faulted:
                    _exception?.Throw();
                    _exception = null;
                    return default;
                case Canceled:
                    _exception?.Throw();
                    _exception = null;
                    throw new OperationCanceledException();
                default:
                    throw new NotSupportedException(
                        "STTask does not allow calling GetResult directly when not completed. Use 'await'.");
            }
        }

        void IAwaiter.GetResult() => ((IAwaiter<T>)this).GetResult();

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            _continuation = action;
            if (_state != Pending)
                TryInvokeContinuation();
        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion)this).UnsafeOnCompleted(action);
        }

        private void TryInvokeContinuation()
        {
            _continuation?.Invoke();
            _continuation = null;
        }

        public void SetResult(T result)
        {
            if (TrySetResult(result)) return;
            throw new InvalidOperationException("STTask already completed.");
        }

        public void SetException(Exception e)
        {
            if (TrySetException(e)) return;
            throw new InvalidOperationException("STTask already completed.");
        }

        public bool TrySetResult(T result)
        {
            if (_state != Pending) return false;
            _state = Succeeded;
            _value = result;
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetException(Exception e)
        {
            if (_state != Pending) return false;
            _state = Faulted;
            _exception = ExceptionDispatchInfo.Capture(e);
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetCanceled()
        {
            if (_state != Pending) return false;
            _state = Canceled;
            TryInvokeContinuation();
            return true;
        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (_state != Pending) return false;
            _state = Canceled;
            _exception = ExceptionDispatchInfo.Capture(e);
            TryInvokeContinuation();
            return true;
        }
    }
}
