//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;
//
//namespace Burner.Basic.Tasks
//{
//    /// <summary>
//    /// Lightweight single threaded task-like object.
//    /// </summary>
//    [AsyncMethodBuilder(typeof(STAsyncTaskMethodBuilder))]
//    public partial struct STTask : IEquatable<STTask>
//    {
//        public static STTask CompletedTask => new STTask();
//
//        private readonly IAwaiter awaiter;
//
//        [DebuggerHidden]
//        public STTask(IAwaiter awaiter)
//        {
//            this.awaiter = awaiter;
//        }
//
//        [DebuggerHidden]
//        public AwaiterStatus Status => awaiter?.Status ?? AwaiterStatus.Succeeded;
//
//        [DebuggerHidden]
//        public bool IsCompleted => awaiter?.IsCompleted ?? true;
//
//        [DebuggerHidden]
//        public void GetResult()
//        {
//            if (awaiter != null)
//            {
//                awaiter.GetResult();
//            }
//        }
//
//        public void Coroutine()
//        {
//        }
//
//        [DebuggerHidden]
//        public Awaiter GetAwaiter()
//        {
//            return new Awaiter(this);
//        }
//
//        public bool Equals(STTask other)
//        {
//            if (this.awaiter == null && other.awaiter == null)
//            {
//                return true;
//            }
//
//            if (this.awaiter != null && other.awaiter != null)
//            {
//                return this.awaiter == other.awaiter;
//            }
//
//            return false;
//        }
//
//        public override int GetHashCode()
//        {
//            if (this.awaiter == null)
//            {
//                return 0;
//            }
//
//            return this.awaiter.GetHashCode();
//        }
//
//        public override string ToString()
//        {
//            return this.awaiter == null ? "()"
//                    : this.awaiter.Status == AwaiterStatus.Succeeded ? "()"
//                    : "(" + this.awaiter.Status + ")";
//        }
//
//        public struct Awaiter : IAwaiter
//        {
//            private readonly STTask task;
//
//            [DebuggerHidden]
//            public Awaiter(STTask task)
//            {
//                this.task = task;
//            }
//
//            [DebuggerHidden]
//            public bool IsCompleted => task.IsCompleted;
//
//            [DebuggerHidden]
//            public AwaiterStatus Status => task.Status;
//
//            [DebuggerHidden]
//            public void GetResult()
//            {
//                task.GetResult();
//            }
//
//            [DebuggerHidden]
//            public void OnCompleted(Action continuation)
//            {
//                if (task.awaiter != null)
//                {
//                    task.awaiter.OnCompleted(continuation);
//                }
//                else
//                {
//                    continuation();
//                }
//            }
//
//            [DebuggerHidden]
//            public void UnsafeOnCompleted(Action continuation)
//            {
//                if (task.awaiter != null)
//                {
//                    task.awaiter.UnsafeOnCompleted(continuation);
//                }
//                else
//                {
//                    continuation();
//                }
//            }
//        }
//    }
//
//    /// <summary>
//    /// Lightweight unity specified task-like object.
//    /// </summary>
//    [AsyncMethodBuilder(typeof(STAsyncTaskMethodBuilder<>))]
//    public struct STTask<T> : IEquatable<STTask<T>>
//    {
//        private readonly T result;
//        private readonly IAwaiter<T> awaiter;
//
//        [DebuggerHidden]
//        public STTask(T result)
//        {
//            this.result = result;
//            this.awaiter = null;
//        }
//
//        [DebuggerHidden]
//        public STTask(IAwaiter<T> awaiter)
//        {
//            this.result = default(T);
//            this.awaiter = awaiter;
//        }
//
//        [DebuggerHidden]
//        public AwaiterStatus Status => awaiter?.Status ?? AwaiterStatus.Succeeded;
//
//        [DebuggerHidden]
//        public bool IsCompleted => awaiter?.IsCompleted ?? true;
//
//        [DebuggerHidden]
//        public T Result
//        {
//            get
//            {
//                if (awaiter == null)
//                {
//                    return result;
//                }
//
//                return this.awaiter.GetResult();
//            }
//        }
//
//        public void Coroutine()
//        {
//        }
//
//        [DebuggerHidden]
//        public Awaiter GetAwaiter()
//        {
//            return new Awaiter(this);
//        }
//
//        public bool Equals(STTask<T> other)
//        {
//            if (this.awaiter == null && other.awaiter == null)
//            {
//                return EqualityComparer<T>.Default.Equals(this.result, other.result);
//            }
//
//            if (this.awaiter != null && other.awaiter != null)
//            {
//                return this.awaiter == other.awaiter;
//            }
//
//            return false;
//        }
//
//        public override int GetHashCode()
//        {
//            if (this.awaiter == null)
//            {
//                if (result == null)
//                {
//                    return 0;
//                }
//
//                return result.GetHashCode();
//            }
//
//            return this.awaiter.GetHashCode();
//        }
//
//        public override string ToString()
//        {
//            return this.awaiter == null ? result.ToString()
//                    : this.awaiter.Status == AwaiterStatus.Succeeded ? this.awaiter.GetResult().ToString()
//                    : "(" + this.awaiter.Status + ")";
//        }
//
//        public static implicit operator STTask(STTask<T> task)
//        {
//            if (task.awaiter != null)
//            {
//                return new STTask(task.awaiter);
//            }
//
//            return new STTask();
//        }
//
//        public struct Awaiter : IAwaiter<T>
//        {
//            private readonly STTask<T> task;
//
//            [DebuggerHidden]
//            public Awaiter(STTask<T> task)
//            {
//                this.task = task;
//            }
//
//            [DebuggerHidden]
//            public bool IsCompleted => task.IsCompleted;
//
//            [DebuggerHidden]
//            public AwaiterStatus Status => task.Status;
//
//            [DebuggerHidden]
//            void IAwaiter.GetResult()
//            {
//                GetResult();
//            }
//
//            [DebuggerHidden]
//            public T GetResult()
//            {
//                return task.Result;
//            }
//
//            [DebuggerHidden]
//            public void OnCompleted(Action continuation)
//            {
//                if (task.awaiter != null)
//                {
//                    task.awaiter.OnCompleted(continuation);
//                }
//                else
//                {
//                    continuation();
//                }
//            }
//
//            [DebuggerHidden]
//            public void UnsafeOnCompleted(Action continuation)
//            {
//                if (task.awaiter != null)
//                {
//                    task.awaiter.UnsafeOnCompleted(continuation);
//                }
//                else
//                {
//                    continuation();
//                }
//            }
//        }
//    }
//}
