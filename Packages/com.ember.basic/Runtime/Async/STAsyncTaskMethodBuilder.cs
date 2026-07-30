//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using System.Diagnostics;
//using System.Security;
//
//namespace Burner.Basic.Tasks
//{
//    public struct STAsyncTaskMethodBuilder
//    {
//        private STTaskCompletionSource tcs;
//        private Action moveNext;
//
//        // 1. Static Create method.
//        [DebuggerHidden]
//        public static STAsyncTaskMethodBuilder Create()
//        {
//            STAsyncTaskMethodBuilder builder = new STAsyncTaskMethodBuilder();
//            return builder;
//        }
//
//        // 2. TaskLike Task property.
//        [DebuggerHidden]
//        public STTask Task
//        {
//            get
//            {
//                if (this.tcs != null)
//                {
//                    return this.tcs.Task;
//                }
//
//                if (moveNext == null)
//                {
//                    return STTask.CompletedTask;
//                }
//
//                this.tcs = new STTaskCompletionSource();
//                return this.tcs.Task;
//            }
//        }
//
//        // 3. SetException
//        [DebuggerHidden]
//        public void SetException(Exception exception)
//        {
//            if (this.tcs == null)
//            {
//                this.tcs = new STTaskCompletionSource();
//            }
//
//            if (exception is OperationCanceledException ex)
//            {
//                this.tcs.TrySetCanceled(ex);
//            }
//            else
//            {
//                this.tcs.TrySetException(exception);
//            }
//        }
//
//        // 4. SetResult
//        [DebuggerHidden]
//        public void SetResult()
//        {
//            if (moveNext == null)
//            {
//            }
//            else
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource();
//                }
//
//                this.tcs.TrySetResult();
//            }
//        }
//
//        // 5. AwaitOnCompleted
//        [DebuggerHidden]
//        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//                where TAwaiter : INotifyCompletion
//                where TStateMachine : IAsyncStateMachine
//        {
//            if (moveNext == null)
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource(); // built future.
//                }
//
//                var runner = new MoveNextRunner<TStateMachine>();
//                moveNext = runner.Run;
//                runner.StateMachine = stateMachine; // set after create delegate.
//            }
//
//            awaiter.OnCompleted(moveNext);
//        }
//
//        // 6. AwaitUnsafeOnCompleted
//        [DebuggerHidden]
//        [SecuritySafeCritical]
//        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//                where TAwaiter : ICriticalNotifyCompletion
//                where TStateMachine : IAsyncStateMachine
//        {
//            if (moveNext == null)
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource(); // built future.
//                }
//
//                var runner = new MoveNextRunner<TStateMachine>();
//                moveNext = runner.Run;
//                runner.StateMachine = stateMachine; // set after create delegate.
//            }
//
//            awaiter.UnsafeOnCompleted(moveNext);
//        }
//
//        // 7. Start
//        [DebuggerHidden]
//        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
//        {
//            stateMachine.MoveNext();
//        }
//
//        // 8. SetStateMachine
//        [DebuggerHidden]
//        public void SetStateMachine(IAsyncStateMachine stateMachine)
//        {
//        }
//    }
//
//    public struct STAsyncTaskMethodBuilder<T>
//    {
//        private T result;
//        private STTaskCompletionSource<T> tcs;
//        private Action moveNext;
//
//        // 1. Static Create method.
//        [DebuggerHidden]
//        public static STAsyncTaskMethodBuilder<T> Create()
//        {
//            var builder = new STAsyncTaskMethodBuilder<T>();
//            return builder;
//        }
//
//        // 2. TaskLike Task property.
//        [DebuggerHidden]
//        public STTask<T> Task
//        {
//            get
//            {
//                if (this.tcs != null)
//                {
//                    return new STTask<T>(this.tcs);
//                }
//
//                if (moveNext == null)
//                {
//                    return new STTask<T>(result);
//                }
//
//                this.tcs = new STTaskCompletionSource<T>();
//                return this.tcs.Task;
//            }
//        }
//
//        // 3. SetException
//        [DebuggerHidden]
//        public void SetException(Exception exception)
//        {
//            if (this.tcs == null)
//            {
//                this.tcs = new STTaskCompletionSource<T>();
//            }
//
//            if (exception is OperationCanceledException ex)
//            {
//                this.tcs.TrySetCanceled(ex);
//            }
//            else
//            {
//                this.tcs.TrySetException(exception);
//            }
//        }
//
//        // 4. SetResult
//        [DebuggerHidden]
//        public void SetResult(T ret)
//        {
//            if (moveNext == null)
//            {
//                this.result = ret;
//            }
//            else
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource<T>();
//                }
//
//                this.tcs.TrySetResult(ret);
//            }
//        }
//
//        // 5. AwaitOnCompleted
//        [DebuggerHidden]
//        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//                where TAwaiter : INotifyCompletion
//                where TStateMachine : IAsyncStateMachine
//        {
//            if (moveNext == null)
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource<T>(); // built future.
//                }
//
//                var runner = new MoveNextRunner<TStateMachine>();
//                moveNext = runner.Run;
//                runner.StateMachine = stateMachine; // set after create delegate.
//            }
//
//            awaiter.OnCompleted(moveNext);
//        }
//
//        // 6. AwaitUnsafeOnCompleted
//        [DebuggerHidden]
//        [SecuritySafeCritical]
//        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
//                where TAwaiter : ICriticalNotifyCompletion
//                where TStateMachine : IAsyncStateMachine
//        {
//            if (moveNext == null)
//            {
//                if (this.tcs == null)
//                {
//                    this.tcs = new STTaskCompletionSource<T>(); // built future.
//                }
//
//                var runner = new MoveNextRunner<TStateMachine>();
//                moveNext = runner.Run;
//                runner.StateMachine = stateMachine; // set after create delegate.
//            }
//
//            awaiter.UnsafeOnCompleted(moveNext);
//        }
//
//        // 7. Start
//        [DebuggerHidden]
//        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
//        {
//            stateMachine.MoveNext();
//        }
//
//        // 8. SetStateMachine
//        [DebuggerHidden]
//        public void SetStateMachine(IAsyncStateMachine stateMachine)
//        {
//        }
//    }
//}
