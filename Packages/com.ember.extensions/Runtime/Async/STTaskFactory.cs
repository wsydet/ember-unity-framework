//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using System;
//using System.Collections.Generic;
//
//namespace Burner.Extensions
//{
//    class AggregateTaskCompletionSource<T> : AggregateTaskCompletionSource
//    {
//        IEnumerable<STTask<T>> array;
//        public AggregateTaskCompletionSource(IEnumerable<STTask<T>> array)
//        {
//            this.array = array;
//        }
//
//        public override void CheckAllCompleted()
//        {
//            foreach (var i in array)
//            {
//                if (!i.IsCompleted)
//                    return;
//            }
//            SetResult();
//        }
//
//        public override void CheckAnyCompleted()
//        {
//            foreach (var i in array)
//            {
//                if (i.IsCompleted)
//                {
//                    SetResult();
//                    return;
//                }
//            }
//        }
//    }
//
//    class AggregateTaskCompletionSource : STTaskCompletionSource
//    {
//        IEnumerable<STTask> array;
//        public AggregateTaskCompletionSource(IEnumerable<STTask> array)
//        {
//            this.array = array;
//        }
//
//        public AggregateTaskCompletionSource()
//        {
//
//        }
//
//        public virtual void CheckAllCompleted()
//        {
//            foreach (var i in array)
//            {
//                if (!i.IsCompleted)
//                    return;
//            }
//            SetResult();
//        }
//
//        public virtual void CheckAnyCompleted()
//        {
//            foreach (var i in array)
//            {
//                if (i.IsCompleted)
//                {
//                    SetResult();
//                    return;
//                }
//            }
//        }
//    }
//    public static class STTaskExtensions
//    {
//        public static STTask WhenAll(this IEnumerable<STTask> tasks)
//        {
//            foreach(var i in tasks)
//            {
//                if (!i.IsCompleted)
//                {
//                    var tcs = new AggregateTaskCompletionSource(tasks);
//                    SingleThreadSynchronizationContext.Instance.QueueWhenAll(tcs);
//                    return tcs.Task;
//                }
//            }
//            return STTask.CompletedTask;
//        }
//
//        public static STTask WhenAll<T>(this IEnumerable<STTask<T>> tasks)
//        {
//            foreach (var i in tasks)
//            {
//                if (!i.IsCompleted)
//                {
//                    var tcs = new AggregateTaskCompletionSource<T>(tasks);
//                    SingleThreadSynchronizationContext.Instance.QueueWhenAll(tcs);
//                    return tcs.Task;
//                }
//            }
//            return STTask.CompletedTask;
//        }
//
//        public static STTask WhenAny(this IEnumerable<STTask> tasks)
//        {
//            foreach (var i in tasks)
//            {
//                if (i.IsCompleted)
//                {
//                    return STTask.CompletedTask;
//                }
//            }
//            var tcs = new AggregateTaskCompletionSource(tasks);
//            SingleThreadSynchronizationContext.Instance.QueueWhenAny(tcs);
//            return tcs.Task;
//        }
//
//        public static STTask WhenAny<T>(this IEnumerable<STTask<T>> tasks)
//        {
//            foreach (var i in tasks)
//            {
//                if (i.IsCompleted)
//                {
//                    return STTask.CompletedTask;
//                }
//            }
//            var tcs = new AggregateTaskCompletionSource<T>(tasks);
//            SingleThreadSynchronizationContext.Instance.QueueWhenAny(tcs);
//            return tcs.Task;
//        }
//    }
//    internal static class CompletedTasks
//    {
//        public static readonly STTask<bool> True = STTask.FromResult(true);
//        public static readonly STTask<bool> False = STTask.FromResult(false);
//        public static readonly STTask<int> Zero = STTask.FromResult(0);
//        public static readonly STTask<int> MinusOne = STTask.FromResult(-1);
//        public static readonly STTask<int> One = STTask.FromResult(1);
//    }
//}
